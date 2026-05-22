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
            res.on("end", () => {
                try { resolve(JSON.parse(body)); } catch (e) { reject(e); }
            });
        });
        req.on("error", reject);
        req.setTimeout(30000, () => { req.destroy(); reject(new Error("timeout")); });
        req.write(data);
        req.end();
    });
}

async function main() {
    const keypairPath = path.join(process.env.HOME, ".config", "solana", "id.json");
    const keypairData = JSON.parse(fs.readFileSync(keypairPath, "utf-8"));
    const authority = Keypair.fromSecretKey(Uint8Array.from(keypairData));

    console.log("Authority:", authority.publicKey.toString());

    const [gameState] = PublicKey.findProgramAddressSync([Buffer.from("game_state")], PROGRAM_ID);
    const [treasury] = PublicKey.findProgramAddressSync([Buffer.from("treasury")], PROGRAM_ID);
    const [mintAuthority] = PublicKey.findProgramAddressSync([Buffer.from("mint_authority")], PROGRAM_ID);
    const treasuryAta = await getAssociatedTokenAddress(ROLAND_MINT, treasury, true);

    console.log("Game State PDA:", gameState.toString());
    console.log("Treasury PDA:", treasury.toString());
    console.log("Mint Authority PDA:", mintAuthority.toString());
    console.log("Treasury ATA:", treasuryAta.toString());

    // Check if already initialized
    const accountResp = await rpcCall("getAccountInfo", [gameState.toString(), { encoding: "base64" }]);
    if (accountResp.result && accountResp.result.value) {
        console.log("\nGame already initialized! Skipping.");
        return;
    }

    // Get blockhash
    const blockhashResp = await rpcCall("getLatestBlockhash", [{ commitment: "confirmed" }]);
    if (!blockhashResp.result || !blockhashResp.result.value) {
        throw new Error("Failed to get blockhash: " + JSON.stringify(blockhashResp));
    }
    const blockhash = blockhashResp.result.value.blockhash;
    console.log("Blockhash:", blockhash);

    const discriminator = computeDiscriminator("initialize_game");

    const ix = new TransactionInstruction({
        programId: PROGRAM_ID,
        keys: [
            { pubkey: authority.publicKey, isSigner: true, isWritable: true },
            { pubkey: gameState, isSigner: false, isWritable: true },
            { pubkey: treasury, isSigner: false, isWritable: true },
            { pubkey: mintAuthority, isSigner: false, isWritable: true },
            { pubkey: treasuryAta, isSigner: false, isWritable: true },
            { pubkey: ROLAND_MINT, isSigner: false, isWritable: false },
            { pubkey: TOKEN_PROGRAM_ID, isSigner: false, isWritable: false },
            { pubkey: ASSOCIATED_TOKEN_PROGRAM_ID, isSigner: false, isWritable: false },
            { pubkey: SystemProgram.programId, isSigner: false, isWritable: false },
        ],
        data: Buffer.from(discriminator),
    });

    const tx = new Transaction({
        feePayer: authority.publicKey,
        blockhash,
        lastValidBlockHeight: blockhashResp.result.value.lastValidBlockHeight,
    });

    tx.add(ComputeBudgetProgram.setComputeUnitLimit({ units: 400_000 }));
    tx.add(ComputeBudgetProgram.setComputeUnitPrice({ microLamports: 140_000 }));
    tx.add(ix);
    tx.sign(authority);

    const serialized = tx.serialize();
    const b64 = serialized.toString("base64");
    console.log("Transaction size:", serialized.length, "bytes");

    // Send transaction
    const sendResp = await rpcCall("sendTransaction", [
        b64,
        { encoding: "base64", skipPreflight: false, preflightCommitment: "confirmed" }
    ]);

    if (sendResp.error) {
        console.error("Send error:", JSON.stringify(sendResp.error));
        return;
    }

    const signature = sendResp.result;
    console.log("Transaction signature:", signature);

    // Confirm
    console.log("Waiting for confirmation...");
    for (let i = 0; i < 30; i++) {
        await new Promise(r => setTimeout(r, 2000));
        const statusResp = await rpcCall("getSignatureStatuses", [[signature]]);
        if (statusResp.result && statusResp.result.value && statusResp.result.value[0]) {
            const status = statusResp.result.value[0];
            if (status.err) {
                console.error("Transaction failed:", JSON.stringify(status.err));
                return;
            }
            if (status.confirmationStatus === "confirmed" || status.confirmationStatus === "finalized") {
                console.log("\nGame initialized successfully!");
                break;
            }
        }
        process.stdout.write(".");
    }

    // Verify
    const verifyResp = await rpcCall("getAccountInfo", [gameState.toString(), { encoding: "base64" }]);
    if (verifyResp.result && verifyResp.result.value) {
        console.log("Game state account confirmed on-chain:");
        console.log("  Data length:", verifyResp.result.value.data[1] === "base64" 
            ? Buffer.from(verifyResp.result.value.data[0], "base64").length 
            : "unknown", "bytes");
        console.log("  Lamports:", verifyResp.result.value.lamports);
        console.log("  Owner:", verifyResp.result.value.owner);
    }
}

main().then(() => process.exit(0)).catch(err => {
    console.error(err);
    process.exit(1);
});
