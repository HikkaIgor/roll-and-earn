const https = require("https");
const { Keypair, Transaction, TransactionInstruction, PublicKey, SystemProgram, ComputeBudgetProgram } = require("@solana/web3.js");
const { TOKEN_PROGRAM_ID, ASSOCIATED_TOKEN_PROGRAM_ID, getAssociatedTokenAddress } = require("@solana/spl-token");
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

const ROLAND_MINT = new PublicKey("G47BKqhe57LFL8hSL4MwSJ545d5EL6XFgAy9nkoHNjbB");
const PROGRAM_ID = new PublicKey("GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T");

function computeDiscriminator(name) {
    const preimage = `global:${name}`;
    const hash = crypto.createHash("sha256").update(preimage).digest();
    return hash.subarray(0, 8);
}

function rpcCall(method, params) {
    return new Promise((resolve, reject) => {
        const data = JSON.stringify({ jsonrpc: "2.0", id: 1, method, params });
        const req = https.request({
            hostname: "api.devnet.solana.com",
            method: "POST",
            headers: { "Content-Type": "application/json", "Content-Length": Buffer.byteLength(data) },
        }, (res) => {
            let body = "";
            res.on("data", (d) => body += d);
            res.on("end", () => { try { resolve(JSON.parse(body)); } catch (e) { reject(e); } });
        });
        req.on("error", reject);
        req.setTimeout(60000, () => { req.destroy(); reject(new Error("timeout")); });
        req.write(data);
        req.end();
    });
}

async function main() {
    const keypairPath = path.join(process.env.USERPROFILE, ".config", "solana", "id.json");
    const keypairData = JSON.parse(fs.readFileSync(keypairPath, "utf-8"));
    const authority = Keypair.fromSecretKey(Uint8Array.from(keypairData));
    console.log("Authority:", authority.publicKey.toString());

    const [gameState] = PublicKey.findProgramAddressSync([Buffer.from("game_state")], PROGRAM_ID);
    const [treasury] = PublicKey.findProgramAddressSync([Buffer.from("treasury")], PROGRAM_ID);
    const [mintAuthority] = PublicKey.findProgramAddressSync([Buffer.from("mint_authority")], PROGRAM_ID);
    const treasuryAta = await getAssociatedTokenAddress(ROLAND_MINT, treasury, true);

    console.log("Game State:", gameState.toString());
    console.log("Treasury:", treasury.toString());
    console.log("Mint Authority:", mintAuthority.toString());
    console.log("Treasury ATA:", treasuryAta.toString());

    const gameStateInfo = await rpcCall("getAccountInfo", [gameState.toString(), { encoding: "base64" }]);
    if (gameStateInfo.result && gameStateInfo.result.value) {
        console.log("\nGame state already exists! Checking treasury...");
        const treasuryInfo = await rpcCall("getAccountInfo", [treasury.toString(), { encoding: "base64" }]);
        if (treasuryInfo.result && treasuryInfo.result.value) {
            console.log("Treasury also exists. Game fully initialized!");
        } else {
            console.log("Need to init treasury only.");
            await sendInitTreasury(authority, gameState, treasury, treasuryAta);
        }
        return;
    }

    // Step 1: init_game
    console.log("\n--- Step 1: init_game ---");
    await sendInitGame(authority, gameState, mintAuthority);

    // Step 2: init_treasury
    console.log("\n--- Step 2: init_treasury ---");
    await sendInitTreasury(authority, gameState, treasury, treasuryAta);

    console.log("\n=== Game fully initialized! ===");
}

async function sendInitGame(authority, gameState, mintAuthority) {
    const blockhashResp = await rpcCall("getLatestBlockhash", [{ commitment: "confirmed" }]);
    if (!blockhashResp.result?.value) throw new Error("No blockhash: " + JSON.stringify(blockhashResp));
    const blockhash = blockhashResp.result.value.blockhash;

    const ix = new TransactionInstruction({
        programId: PROGRAM_ID,
        keys: [
            { pubkey: authority.publicKey, isSigner: true, isWritable: true },
            { pubkey: gameState, isSigner: false, isWritable: true },
            { pubkey: mintAuthority, isSigner: false, isWritable: true },
            { pubkey: ROLAND_MINT, isSigner: false, isWritable: false },
            { pubkey: TOKEN_PROGRAM_ID, isSigner: false, isWritable: false },
            { pubkey: SystemProgram.programId, isSigner: false, isWritable: false },
        ],
        data: Buffer.from(computeDiscriminator("init_game")),
    });

    const tx = new Transaction({ feePayer: authority.publicKey, blockhash, lastValidBlockHeight: blockhashResp.result.value.lastValidBlockHeight });
    tx.add(ComputeBudgetProgram.setComputeUnitLimit({ units: 400_000 }));
    tx.add(ComputeBudgetProgram.setComputeUnitPrice({ microLamports: 140_000 }));
    tx.add(ix);
    tx.sign(authority);

    const b64 = tx.serialize().toString("base64");

    const sig = await sendAndConfirm(b64);
    console.log("init_game tx:", sig);
}

async function sendInitTreasury(authority, gameState, treasury, treasuryAta) {
    const blockhashResp = await rpcCall("getLatestBlockhash", [{ commitment: "confirmed" }]);
    if (!blockhashResp.result?.value) throw new Error("No blockhash");
    const blockhash = blockhashResp.result.value.blockhash;

    const ix = new TransactionInstruction({
        programId: PROGRAM_ID,
        keys: [
            { pubkey: authority.publicKey, isSigner: true, isWritable: true },
            { pubkey: gameState, isSigner: false, isWritable: true },
            { pubkey: treasury, isSigner: false, isWritable: true },
            { pubkey: treasuryAta, isSigner: false, isWritable: true },
            { pubkey: ROLAND_MINT, isSigner: false, isWritable: false },
            { pubkey: TOKEN_PROGRAM_ID, isSigner: false, isWritable: false },
            { pubkey: ASSOCIATED_TOKEN_PROGRAM_ID, isSigner: false, isWritable: false },
            { pubkey: SystemProgram.programId, isSigner: false, isWritable: false },
        ],
        data: Buffer.from(computeDiscriminator("init_treasury")),
    });

    const tx = new Transaction({ feePayer: authority.publicKey, blockhash, lastValidBlockHeight: blockhashResp.result.value.lastValidBlockHeight });
    tx.add(ComputeBudgetProgram.setComputeUnitLimit({ units: 400_000 }));
    tx.add(ComputeBudgetProgram.setComputeUnitPrice({ microLamports: 140_000 }));
    tx.add(ix);
    tx.sign(authority);

    const b64 = tx.serialize().toString("base64");

    const sig = await sendAndConfirm(b64);
    console.log("init_treasury tx:", sig);
}

async function sendAndConfirm(b64) {
    const sendResp = await rpcCall("sendTransaction", [b64, { encoding: "base64", skipPreflight: true }]);
    if (sendResp.error) throw new Error("Send error: " + JSON.stringify(sendResp.error));
    const signature = sendResp.result;
    for (let i = 0; i < 30; i++) {
        await new Promise(r => setTimeout(r, 2000));
        const statusResp = await rpcCall("getSignatureStatuses", [[signature]]);
        const status = statusResp.result?.value?.[0];
        if (status?.err) throw new Error("Tx failed: " + JSON.stringify(status.err));
        if (status?.confirmationStatus === "confirmed" || status?.confirmationStatus === "finalized") return signature;
        process.stdout.write(".");
    }
    throw new Error("Confirmation timeout");
}

main().then(() => process.exit(0)).catch(err => { console.error(err); process.exit(1); });
