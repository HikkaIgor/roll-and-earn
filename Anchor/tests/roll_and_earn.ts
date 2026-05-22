import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import {
  PublicKey,
  SystemProgram,
  SYSVAR_RENT_PUBKEY,
  LAMPORTS_PER_SOL,
} from "@solana/web3.js";
import {
  createMint,
  mintTo,
  getOrCreateAssociatedTokenAccount,
  TOKEN_PROGRAM_ID,
  ASSOCIATED_TOKEN_PROGRAM_ID,
} from "@solana/spl-token";
import { expect } from "chai";

const RollAndEarnIDL = {
  version: "0.1.0",
  name: "roll_and_earn",
  instructions: [
    {
      name: "initializeGame",
      accounts: [
        { name: "authority" },
        { name: "gameState" },
        { name: "treasury" },
        { name: "mintAuthority" },
        { name: "rewardMint" },
        { name: "treasuryTokenAccount" },
        { name: "tokenProgram" },
        { name: "systemProgram" },
      ],
      args: [],
    },
    {
      name: "createCharacter",
      accounts: [
        { name: "player" },
        { name: "playerProfile" },
        { name: "characterMint" },
        { name: "playerCharacterAta" },
        { name: "characterMetadata" },
        { name: "mintAuthority" },
        { name: "gameState" },
        { name: "tokenProgram" },
        { name: "associatedTokenProgram" },
        { name: "systemProgram" },
        { name: "rent" },
        { name: "tokenMetadataProgram" },
      ],
      args: [{ name: "characterClass", type: "u8" }],
    },
    {
      name: "rollAction",
      accounts: [
        { name: "player" },
        { name: "playerProfile" },
        { name: "gameState" },
        { name: "treasury" },
        { name: "treasuryTokenAccount" },
        { name: "playerTokenAccount" },
        { name: "rewardMint" },
        { name: "tokenProgram" },
        { name: "clock" },
      ],
      args: [{ name: "adventureType", type: "u8" }],
    },
    {
      name: "claimItem",
      accounts: [
        { name: "player" },
        { name: "playerProfile" },
        { name: "itemMint" },
        { name: "playerItemAta" },
        { name: "itemMetadata" },
        { name: "mintAuthority" },
        { name: "gameState" },
        { name: "tokenProgram" },
        { name: "associatedTokenProgram" },
        { name: "systemProgram" },
        { name: "rent" },
        { name: "tokenMetadataProgram" },
      ],
      args: [],
    },
    {
      name: "equipItem",
      accounts: [
        { name: "player" },
        { name: "playerProfile" },
        { name: "gameState" },
      ],
      args: [{ name: "itemType", type: "u8" }],
    },
    {
      name: "levelUp",
      accounts: [
        { name: "player" },
        { name: "playerProfile" },
        { name: "gameState" },
        { name: "treasury" },
        { name: "treasuryTokenAccount" },
        { name: "playerTokenAccount" },
        { name: "rewardMint" },
        { name: "tokenProgram" },
      ],
      args: [],
    },
    {
      name: "requestAirdrop",
      accounts: [
        { name: "player" },
        { name: "playerProfile" },
        { name: "gameState" },
        { name: "treasury" },
        { name: "treasuryTokenAccount" },
        { name: "playerTokenAccount" },
        { name: "rewardMint" },
        { name: "tokenProgram" },
      ],
      args: [],
    },
  ],
};

const PROGRAM_ID = new PublicKey("RoLLAND1CH3ZQuS8YWcGNAyvSEe7qVfNPz2PQy7rDvK");
const MPL_TOKEN_METADATA_PROGRAM_ID = new PublicKey(
  "metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt918Cms"
);

describe("roll_and_earn", () => {
  const provider = anchor.AnchorProvider.env();
  anchor.setProvider(provider);
  const payer = provider.wallet as anchor.Wallet;
  const program = new Program(RollAndEarnIDL, PROGRAM_ID, provider);

  let rewardMint: PublicKey;
  let treasuryTokenAccount: PublicKey;
  let playerTokenAccount: PublicKey;
  let gameStatePDA: PublicKey;
  let treasuryPDA: PublicKey;
  let mintAuthorityPDA: PublicKey;
  let playerProfilePDA: PublicKey;
  let characterMintPDA: PublicKey;
  let playerCharacterAta: PublicKey;

  const adventureCosts: Record<number, number> = {
    0: 0,
    1: 10_000_000_000,
    2: 50_000_000_000,
  };

  before(async () => {
    rewardMint = await createMint(
      provider.connection,
      payer.payer,
      payer.publicKey,
      null,
      9
    );

    const authorityAta = await getOrCreateAssociatedTokenAccount(
      provider.connection,
      payer.payer,
      rewardMint,
      payer.publicKey
    );
    await mintTo(
      provider.connection,
      payer.payer,
      rewardMint,
      authorityAta.address,
      payer.publicKey,
      1_000_000_000_000
    );

    [gameStatePDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("game_state")],
      PROGRAM_ID
    );
    [treasuryPDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("treasury")],
      PROGRAM_ID
    );
    [mintAuthorityPDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("mint_authority")],
      PROGRAM_ID
    );
    [playerProfilePDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("player_profile"),
      payer.publicKey.toBuffer()],
      PROGRAM_ID
    );
    [characterMintPDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("character_mint"),
      payer.publicKey.toBuffer()],
      PROGRAM_ID
    );

    playerCharacterAta = (
      await getOrCreateAssociatedTokenAccount(
        provider.connection,
        payer.payer,
        characterMintPDA,
        payer.publicKey
      )
    ).address;
  });

  it("initializes the game", async () => {
    treasuryTokenAccount = (
      await getOrCreateAssociatedTokenAccount(
        provider.connection,
        payer.payer,
        rewardMint,
        treasuryPDA,
        true
      )
    ).address;

    await program.methods
      .initializeGame()
      .accounts({
        authority: payer.publicKey,
        gameState: gameStatePDA,
        treasury: treasuryPDA,
        mintAuthority: mintAuthorityPDA,
        rewardMint: rewardMint,
        treasuryTokenAccount: treasuryTokenAccount,
        tokenProgram: TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
      })
      .rpc();

    const gameState = await program.account.gameState.fetch(gameStatePDA);
    expect(gameState.rewardMint.toString()).to.equal(rewardMint.toString());

    const fundAta = await getOrCreateAssociatedTokenAccount(
      provider.connection,
      payer.payer,
      rewardMint,
      payer.publicKey
    );
    await mintTo(
      provider.connection,
      payer.payer,
      rewardMint,
      fundAta.address,
      payer.publicKey,
      500_000_000_000
    );

    const treasuryAta = await getOrCreateAssociatedTokenAccount(
      provider.connection,
      payer.payer,
      rewardMint,
      treasuryPDA,
      true
    );
    await mintTo(
      provider.connection,
      payer.payer,
      rewardMint,
      treasuryAta.address,
      payer.publicKey,
      500_000_000_000
    );

    playerTokenAccount = (
      await getOrCreateAssociatedTokenAccount(
        provider.connection,
        payer.payer,
        rewardMint,
        payer.publicKey
      )
    ).address;

    console.log("Game initialized successfully");
  });

  it("creates a Warrior character", async () => {
    const [characterMetadataPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("metadata"),
        MPL_TOKEN_METADATA_PROGRAM_ID.toBuffer(),
        characterMintPDA.toBuffer(),
      ],
      MPL_TOKEN_METADATA_PROGRAM_ID
    );

    await program.methods
      .createCharacter(0)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        characterMint: characterMintPDA,
        playerCharacterAta: playerCharacterAta,
        characterMetadata: characterMetadataPDA,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
        rent: SYSVAR_RENT_PUBKEY,
        tokenMetadataProgram: MPL_TOKEN_METADATA_PROGRAM_ID,
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.characterClass).to.equal(0);
    expect(profile.level.toNumber()).to.equal(1);
    console.log("Warrior character created");
  });

  it("creates a Rogue character", async () => {
    const roguePlayer = anchor.web3.Keypair.generate();
    await provider.connection.confirmTransaction(
      await provider.connection.requestAirdrop(
        roguePlayer.publicKey,
        2 * LAMPORTS_PER_SOL
      )
    );

    const [rogueProfilePDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("player_profile"), roguePlayer.publicKey.toBuffer()],
      PROGRAM_ID
    );
    const [rogueCharacterMintPDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("character_mint"), roguePlayer.publicKey.toBuffer()],
      PROGRAM_ID
    );
    const rogueCharacterAta = (
      await getOrCreateAssociatedTokenAccount(
        provider.connection,
        payer.payer,
        rogueCharacterMintPDA,
        roguePlayer.publicKey
      )
    ).address;
    const [rogueMetadataPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("metadata"),
        MPL_TOKEN_METADATA_PROGRAM_ID.toBuffer(),
        rogueCharacterMintPDA.toBuffer(),
      ],
      MPL_TOKEN_METADATA_PROGRAM_ID
    );

    await program.methods
      .createCharacter(1)
      .accounts({
        player: roguePlayer.publicKey,
        playerProfile: rogueProfilePDA,
        characterMint: rogueCharacterMintPDA,
        playerCharacterAta: rogueCharacterAta,
        characterMetadata: rogueMetadataPDA,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
        rent: SYSVAR_RENT_PUBKEY,
        tokenMetadataProgram: MPL_TOKEN_METADATA_PROGRAM_ID,
      })
      .signers([roguePlayer])
      .rpc();

    const profile = await program.account.playerProfile.fetch(rogueProfilePDA);
    expect(profile.characterClass).to.equal(1);
    console.log("Rogue character created");
  });

  it("creates a Mage character", async () => {
    const magePlayer = anchor.web3.Keypair.generate();
    await provider.connection.confirmTransaction(
      await provider.connection.requestAirdrop(
        magePlayer.publicKey,
        2 * LAMPORTS_PER_SOL
      )
    );

    const [mageProfilePDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("player_profile"), magePlayer.publicKey.toBuffer()],
      PROGRAM_ID
    );
    const [mageCharacterMintPDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("character_mint"), magePlayer.publicKey.toBuffer()],
      PROGRAM_ID
    );
    const mageCharacterAta = (
      await getOrCreateAssociatedTokenAccount(
        provider.connection,
        payer.payer,
        mageCharacterMintPDA,
        magePlayer.publicKey
      )
    ).address;
    const [mageMetadataPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("metadata"),
        MPL_TOKEN_METADATA_PROGRAM_ID.toBuffer(),
        mageCharacterMintPDA.toBuffer(),
      ],
      MPL_TOKEN_METADATA_PROGRAM_ID
    );

    await program.methods
      .createCharacter(2)
      .accounts({
        player: magePlayer.publicKey,
        playerProfile: mageProfilePDA,
        characterMint: mageCharacterMintPDA,
        playerCharacterAta: mageCharacterAta,
        characterMetadata: mageMetadataPDA,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
        rent: SYSVAR_RENT_PUBKEY,
        tokenMetadataProgram: MPL_TOKEN_METADATA_PROGRAM_ID,
      })
      .signers([magePlayer])
      .rpc();

    const profile = await program.account.playerProfile.fetch(mageProfilePDA);
    expect(profile.characterClass).to.equal(2);
    console.log("Mage character created");
  });

  it("rolls Forest adventure (type 0)", async () => {
    await program.methods
      .rollAction(0)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
        treasury: treasuryPDA,
        treasuryTokenAccount: treasuryTokenAccount,
        playerTokenAccount: playerTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
        clock: new PublicKey("SysvarC1ock11111111111111111111111111111111"),
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.lastAdventure.toNumber()).to.be.greaterThan(0);
    expect(profile.xp.toNumber()).to.be.greaterThan(0);
    console.log("Forest adventure completed");
  });

  it("rolls Dungeon adventure (type 1)", async () => {
    await mintTo(
      provider.connection,
      payer.payer,
      rewardMint,
      playerTokenAccount,
      payer.publicKey,
      100_000_000_000
    );

    await program.methods
      .rollAction(1)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
        treasury: treasuryPDA,
        treasuryTokenAccount: treasuryTokenAccount,
        playerTokenAccount: playerTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
        clock: new PublicKey("SysvarC1ock11111111111111111111111111111111"),
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.lastAdventure.toNumber()).to.be.greaterThan(0);
    console.log("Dungeon adventure completed");
  });

  it("rolls Dragon adventure (type 2)", async () => {
    await mintTo(
      provider.connection,
      payer.payer,
      rewardMint,
      playerTokenAccount,
      payer.publicKey,
      100_000_000_000
    );

    await program.methods
      .rollAction(2)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
        treasury: treasuryPDA,
        treasuryTokenAccount: treasuryTokenAccount,
        playerTokenAccount: playerTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
        clock: new PublicKey("SysvarC1ock11111111111111111111111111111111"),
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.lastAdventure.toNumber()).to.be.greaterThan(0);
    console.log("Dragon adventure completed");
  });

  it("claims an item", async () => {
    const profileBefore = await program.account.playerProfile.fetch(playerProfilePDA);
    const itemsMintedCounter = profileBefore.itemsMinted.toNumber();

    const [itemMintPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("item_mint"),
        payer.publicKey.toBuffer(),
        new anchor.BN(itemsMintedCounter).toArrayLike(Buffer, "le", 8),
      ],
      PROGRAM_ID
    );

    const playerItemAta = (
      await getOrCreateAssociatedTokenAccount(
        provider.connection,
        payer.payer,
        itemMintPDA,
        payer.publicKey
      )
    ).address;

    const [itemMetadataPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("metadata"),
        MPL_TOKEN_METADATA_PROGRAM_ID.toBuffer(),
        itemMintPDA.toBuffer(),
      ],
      MPL_TOKEN_METADATA_PROGRAM_ID
    );

    await program.methods
      .claimItem()
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        itemMint: itemMintPDA,
        playerItemAta: playerItemAta,
        itemMetadata: itemMetadataPDA,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
        rent: SYSVAR_RENT_PUBKEY,
        tokenMetadataProgram: MPL_TOKEN_METADATA_PROGRAM_ID,
      })
      .rpc();

    const profileAfter = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profileAfter.itemsMinted.toNumber()).to.equal(itemsMintedCounter + 1);
    console.log("Item claimed");
  });

  it("equips a weapon (type 0)", async () => {
    await program.methods
      .equipItem(0)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    console.log("Weapon equipped");
  });

  it("equips armor (type 1)", async () => {
    await program.methods
      .equipItem(1)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    console.log("Armor equipped");
  });

  it("levels up", async () => {
    await mintTo(
      provider.connection,
      payer.payer,
      rewardMint,
      playerTokenAccount,
      payer.publicKey,
      1_000_000_000_000
    );

    const profileBefore = await program.account.playerProfile.fetch(playerProfilePDA);

    await program.methods
      .levelUp()
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
        treasury: treasuryPDA,
        treasuryTokenAccount: treasuryTokenAccount,
        playerTokenAccount: playerTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
      })
      .rpc();

    const profileAfter = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profileAfter.level.toNumber()).to.be.greaterThan(
      profileBefore.level.toNumber()
    );
    console.log("Leveled up");
  });

  it("requests airdrop of 100 ROLAND", async () => {
    await program.methods
      .requestAirdrop()
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
        treasury: treasuryPDA,
        treasuryTokenAccount: treasuryTokenAccount,
        playerTokenAccount: playerTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.airdropClaimed).to.equal(true);
    console.log("Airdrop claimed");
  });
});
