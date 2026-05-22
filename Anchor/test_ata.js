const { Connection, Keypair, PublicKey } = require("@solana/web3.js");
const {
  getAssociatedTokenAddress,
  TOKEN_PROGRAM_ID,
} = require("@solana/spl-token");

(async () => {
  const conn = new Connection("http://127.0.0.1:8899");
  console.log("connected:", (await conn.getVersion())["solana-core"]);
  const mint = Keypair.generate();
  const treasuryPDA = new PublicKey(
    "11111111111111111111111111111111"
  );
  const ata = await getAssociatedTokenAddress(
    mint.publicKey,
    treasuryPDA,
    true,
    TOKEN_PROGRAM_ID
  );
  console.log("ATA:", ata.toBase58());
})().catch((e) => console.error(e.message));
