import {
    Keypair, Transaction, TransactionInstruction,
    PublicKey, SystemProgram, ComputeBudgetProgram
} from "@solana/web3.js";
import {
    TOKEN_PROGRAM_ID,
    ASSOCIATED_TOKEN_PROGRAM_ID,
    getAssociatedTokenAddress
} from "@solana/spl-token";
import * as fs from "fs";
import * as path from "path";
import * as crypto from "crypto";

const ROLAND_MINT = new PublicKey("G47BKqhe57LFL8hSL4MwSJ545d5EL6XFgAy9nkoHNjbB");
const PROGRAM_ID = new PublicKey("GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T");

function computeDiscriminator(name: string): Buffer {
    const preimage = `global:${name}`;
    const hash = crypto.createHash("sha256").update(preimage).digest();
    return hash.subarray(0, 8);
}

async function main() {
    const blockhash = process.argv[2];
    if (!blockhash) {
        console.error("Usage: tsx build-init-tx.ts <recent_blockhash>");
        process.exit(1);
    }

    const keypairPath = path.join(process.env.HOME!, ".config", "solana", "id.json");
    const keypairData = JSON.parse(fs.readFileSync(keypairPath, "utf-8"));
    const authority = Keypair.fromSecretKey(Uint8Array.from(keypairData));

    const [gameState] = PublicKey.findProgramAddressSync([Buffer.from("game_state")], PROGRAM_ID);
    const [treasury] = PublicKey.findProgramAddressSync([Buffer.from("treasury")], PROGRAM_ID);
    const [mintAuthority] = PublicKey.findProgramAddressSync([Buffer.from("mint_authority")], PROGRAM_ID);
    const treasuryAta = await getAssociatedTokenAddress(ROLAND_MINT, treasury, true);

    console.error("Game State PDA:", gameState.toString());
    console.error("Treasury PDA:", treasury.toString());
    console.error("Mint Authority PDA:", mintAuthority.toString());
    console.error("Treasury ATA:", treasuryAta.toString());

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

    const tx = new Transaction({
        feePayer: authority.publicKey,
        blockhash,
        lastValidBlockHeight: 999999999,
    });

    tx.add(ComputeBudgetProgram.setComputeUnitLimit({ units: 400_000 }));
    tx.add(ComputeBudgetProgram.setComputeUnitPrice({ microLamports: 140_000 }));
    tx.add(ix);
    tx.sign(authority);

    const serialized = tx.serialize();
    const b64 = serialized.toString("base64");

    console.log(b64);
}

main().then(() => process.exit(0)).catch(err => {
    console.error(err);
    process.exit(1);
});
