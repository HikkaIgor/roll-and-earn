import {
    Keypair, Transaction, TransactionInstruction,
    PublicKey, SystemProgram, ComputeBudgetProgram,
    Message
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
    const keypairPath = path.join(process.env.HOME!, ".config", "solana", "id.json");
    const keypairData = JSON.parse(fs.readFileSync(keypairPath, "utf-8"));
    const authority = Keypair.fromSecretKey(Uint8Array.from(keypairData));

    const [gameState] = PublicKey.findProgramAddressSync(
        [Buffer.from("game_state")], PROGRAM_ID
    );
    const [treasury] = PublicKey.findProgramAddressSync(
        [Buffer.from("treasury")], PROGRAM_ID
    );
    const [mintAuthority] = PublicKey.findProgramAddressSync(
        [Buffer.from("mint_authority")], PROGRAM_ID
    );
    const treasuryAta = await getAssociatedTokenAddress(ROLAND_MINT, treasury, true);

    console.log("Game State PDA:", gameState.toString());
    console.log("Treasury PDA:", treasury.toString());
    console.log("Mint Authority PDA:", mintAuthority.toString());
    console.log("Treasury ATA:", treasuryAta.toString());

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

    const message = new Message({
        header: { numRequiredSignatures: 1, numReadonlySignedAccounts: 0, numReadonlyUnsignedAccounts: 4 },
        accountKeys: [
            authority.publicKey,
            gameState,
            treasury,
            mintAuthority,
            treasuryAta,
            ROLAND_MINT,
            TOKEN_PROGRAM_ID,
            ASSOCIATED_TOKEN_PROGRAM_ID,
            SystemProgram.programId,
            ComputeBudgetProgram.programId,
            PROGRAM_ID,
        ],
        recentBlockhash: "11111111111111111111111111111111",
        instructions: [
            {
                programIdIndex: 9,
                accounts: [],
                data: Buffer.from(ComputeBudgetProgram.setComputeUnitLimit({ units: 400_000 }).data),
            },
            {
                programIdIndex: 9,
                accounts: [],
                data: Buffer.from(ComputeBudgetProgram.setComputeUnitPrice({ microLamports: 140_000 }).data),
            },
            {
                programIdIndex: 10,
                accounts: [0, 1, 2, 3, 4, 5, 6, 7, 8],
                data: discriminator,
            },
        ],
    });

    const outDir = "/mnt/d/Diplom/RollAndEarn/Anchor/target";
    fs.mkdirSync(outDir, { recursive: true });

    const pdaInfo = {
        gameState: gameState.toString(),
        treasury: treasury.toString(),
        mintAuthority: mintAuthority.toString(),
        treasuryAta: treasuryAta.toString(),
        rolandMint: ROLAND_MINT.toString(),
        programId: PROGRAM_ID.toString(),
        authority: authority.publicKey.toString(),
    };
    fs.writeFileSync(path.join(outDir, "pda-info.json"), JSON.stringify(pdaInfo, null, 2));
    console.log("\nPDA info saved to target/pda-info.json");
    console.log("\nNow run the following command to initialize the game:");
    console.log("  solana program initialize --program-id GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T");
    console.log("\nOr use the anchor CLI:");
    console.log("  anchor run init-game");
}

main().then(() => process.exit(0)).catch(err => {
    console.error(err);
    process.exit(1);
});
