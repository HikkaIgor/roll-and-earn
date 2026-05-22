import {
    Connection, Keypair, Transaction, TransactionInstruction,
    PublicKey, SystemProgram, ComputeBudgetProgram
} from "@solana/web3.js";
import {
    TOKEN_PROGRAM_ID,
    ASSOCIATED_TOKEN_PROGRAM_ID,
    getAssociatedTokenAddress,
    createAssociatedTokenAccountIdl
} from "@solana/spl-token";
import * as fs from "fs";
import * as path from "path";

const ROLAND_MINT = new PublicKey("G47BKqhe57LFL8hSL4MwSJ545d5EL6XFgAy9nkoHNjbB");
const PROGRAM_ID = new PublicKey("GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T");

function computeDiscriminator(name: string): Buffer {
    const preimage = `global:${name}`;
    const hash = crypto.createHash("sha256").update(preimage).digest();
    return hash.subarray(0, 8);
}

import * as crypto from "crypto";

async function main() {
    const connection = new Connection("https://api.devnet.solana.com", "confirmed");

    const keypairPath = path.join(process.env.HOME!, ".config", "solana", "id.json");
    const keypairData = JSON.parse(fs.readFileSync(keypairPath, "utf-8"));
    const authority = Keypair.fromSecretKey(Uint8Array.from(keypairData));

    console.log("Authority:", authority.publicKey.toString());

    const [gameState] = PublicKey.findProgramAddressSync(
        [Buffer.from("game_state")],
        PROGRAM_ID
    );
    const [treasury] = PublicKey.findProgramAddressSync(
        [Buffer.from("treasury")],
        PROGRAM_ID
    );
    const [mintAuthority] = PublicKey.findProgramAddressSync(
        [Buffer.from("mint_authority")],
        PROGRAM_ID
    );

    const treasuryAta = await getAssociatedTokenAddress(ROLAND_MINT, treasury, true);

    console.log("Game State PDA:", gameState.toString());
    console.log("Treasury PDA:", treasury.toString());
    console.log("Mint Authority PDA:", mintAuthority.toString());
    console.log("Treasury ATA:", treasuryAta.toString());

    const existingAccount = await connection.getAccountInfo(gameState);
    if (existingAccount) {
        console.log("\nGame already initialized! Skipping.");
        return;
    }

    console.log("\nBuilding initialize_game transaction...");

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
        data: discriminator,
    });

    const blockhash = await connection.getLatestBlockhash("confirmed");

    const tx = new Transaction({
        feePayer: authority.publicKey,
        blockhash: blockhash.blockhash,
        lastValidBlockHeight: blockhash.lastValidBlockHeight,
    });

    tx.add(ComputeBudgetProgram.setComputeUnitLimit({ units: 400_000 }));
    tx.add(ComputeBudgetProgram.setComputeUnitPrice({ microLamports: 140_000 }));
    tx.add(ix);
    tx.sign(authority);

    console.log("Sending transaction...");
    const signature = await connection.sendRawTransaction(tx.serialize(), {
        skipPreflight: false,
        preflightCommitment: "confirmed",
    });

    console.log("Transaction signature:", signature);

    const confirmation = await connection.confirmTransaction({
        signature,
        blockhash: blockhash.blockhash,
        lastValidBlockHeight: blockhash.lastValidBlockHeight,
    }, "confirmed");

    if (confirmation.value.err) {
        console.error("Transaction failed:", confirmation.value.err);
    } else {
        console.log("\nGame initialized successfully!");
    }

    const verifyAccount = await connection.getAccountInfo(gameState);
    if (verifyAccount) {
        console.log("Game state account confirmed:");
        console.log("  Owner:", verifyAccount.owner.toString());
        console.log("  Lamports:", verifyAccount.lamports);
        console.log("  Data length:", verifyAccount.data.length, "bytes");
    }
}

main().then(() => process.exit(0)).catch(err => {
    console.error(err);
    process.exit(1);
});
