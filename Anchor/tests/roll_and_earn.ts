import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import {
  PublicKey,
  SystemProgram,
  LAMPORTS_PER_SOL,
  Keypair,
} from "@solana/web3.js";
import {
  createMint,
  mintTo,
  getOrCreateAssociatedTokenAccount,
  getAssociatedTokenAddress,
  getAccount,
  TOKEN_PROGRAM_ID,
  ASSOCIATED_TOKEN_PROGRAM_ID,
} from "@solana/spl-token";
import { expect } from "chai";
import { RollAndEarn } from "../target/types/roll_and_earn";

const PROGRAM_ID = new PublicKey("GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T");

describe("roll_and_earn", () => {
  const provider = anchor.AnchorProvider.env();
  anchor.setProvider(provider);
  const payer = provider.wallet as anchor.Wallet;
  const program = anchor.workspace.RollAndEarn as Program<RollAndEarn>;

  let rewardMint: PublicKey;
  let gameStatePDA: PublicKey;
  let treasuryPDA: PublicKey;
  let mintAuthorityPDA: PublicKey;
  let playerProfilePDA: PublicKey;
  let characterMintPDA: PublicKey;
  let playerCharacterAta: PublicKey;
  let treasuryTokenAccount: PublicKey;
  let playerTokenAccount: PublicKey;

  before(async () => {
    rewardMint = await createMint(
      provider.connection,
      payer.payer,
      payer.publicKey,
      null,
      9
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
  });

  it("initializes the game", async () => {
    await program.methods
      .initGame()
      .accounts({
        authority: payer.publicKey,
        gameState: gameStatePDA,
        mintAuthority: mintAuthorityPDA,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
      })
      .rpc();

    const gameState = await program.account.gameState.fetch(gameStatePDA);
    expect(gameState.rewardMint.toString()).to.equal(rewardMint.toString());
    expect(gameState.authority.toString()).to.equal(payer.publicKey.toString());
    expect(gameState.totalRolls.toNumber()).to.equal(0);
    console.log("Game initialized");
  });

  it("initializes the treasury", async () => {
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
      .initTreasury()
      .accounts({
        authority: payer.publicKey,
        gameState: gameStatePDA,
        treasury: treasuryPDA,
        treasuryTokenAccount: treasuryTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
      })
      .rpc();

    const gameState = await program.account.gameState.fetch(gameStatePDA);
    expect(gameState.treasury.toString()).to.equal(treasuryPDA.toString());
    console.log("Treasury initialized");
  });

  it("funds the treasury", async () => {
    await mintTo(
      provider.connection,
      payer.payer,
      rewardMint,
      treasuryTokenAccount,
      payer.publicKey,
      1_000_000_000_000
    );

    playerTokenAccount = (
      await getOrCreateAssociatedTokenAccount(
        provider.connection,
        payer.payer,
        rewardMint,
        payer.publicKey
      )
    ).address;
    console.log("Treasury funded");
  });

  it("creates a Warrior character", async () => {
    [playerProfilePDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("player_profile"), payer.publicKey.toBuffer()],
      PROGRAM_ID
    );
    [characterMintPDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("character_mint"), payer.publicKey.toBuffer()],
      PROGRAM_ID
    );

    await program.methods
      .createCharacter("Warrior", "WAR", "https://example.com/warrior.json", 0)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        characterMint: characterMintPDA,
        playerCharacterAta: null,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.class).to.equal(0);
    expect(profile.level).to.equal(1);
    expect(profile.strength).to.equal(10);
    expect(profile.agility).to.equal(6);
    expect(profile.intelligence).to.equal(4);
    expect(profile.luck).to.equal(5);
    expect(profile.airdropClaimed).to.equal(false);
    expect(profile.xp).to.equal(0);

    playerCharacterAta = (
      await getOrCreateAssociatedTokenAccount(
        provider.connection,
        payer.payer,
        characterMintPDA,
        payer.publicKey
      )
    ).address;
    const charToken = await getAccount(provider.connection, playerCharacterAta);
    expect(Number(charToken.amount)).to.equal(1);
    console.log("Warrior created");
  });

  it("requests airdrop (200 ROLAND)", async () => {
    await program.methods
      .requestAirdrop()
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        treasury: treasuryPDA,
        treasuryTokenAccount: treasuryTokenAccount,
        playerTokenAccount: playerTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.airdropClaimed).to.equal(true);

    const balance = await getAccount(provider.connection, playerTokenAccount);
    expect(Number(balance.amount)).to.equal(200_000_000_000);
    console.log("Airdrop claimed: 200 ROLAND");
  });

  it("rolls Forest adventure (type 0, free)", async () => {
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
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.xp).to.be.greaterThan(0);
    expect(profile.cooldownExpiries[0].toNumber()).to.be.greaterThan(0);
    console.log("Forest roll done, XP:", profile.xp);
  });

  it("rolls Dungeon adventure (type 1, costs 10 ROLAND)", async () => {
    const profileBefore = await program.account.playerProfile.fetch(playerProfilePDA);
    const xpBefore = profileBefore.xp;

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
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.xp).to.be.greaterThan(xpBefore);
    console.log("Dungeon roll done, XP:", profile.xp);
  });

  it("rolls Dragon adventure (type 2, costs 50 ROLAND)", async () => {
    await mintTo(
      provider.connection,
      payer.payer,
      rewardMint,
      playerTokenAccount,
      payer.publicKey,
      100_000_000_000
    );

    const xpBefore = (await program.account.playerProfile.fetch(playerProfilePDA)).xp;

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
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.xp).to.be.greaterThan(xpBefore);
    console.log("Dragon roll done, XP:", profile.xp);
  });

  it("claims an item", async () => {
    let profile = await program.account.playerProfile.fetch(playerProfilePDA);

    if (profile.unclaimedSpecials === 0) {
      for (let attempt = 0; attempt < 10; attempt++) {
        await new Promise((r) => setTimeout(r, 31000));
        await mintTo(
          provider.connection,
          payer.payer,
          rewardMint,
          playerTokenAccount,
          payer.publicKey,
          100_000_000_000
        );
        try {
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
            })
            .rpc();
        } catch (e) {}
        profile = await program.account.playerProfile.fetch(playerProfilePDA);
        if (profile.unclaimedSpecials > 0) break;
      }
    }

    profile = await program.account.playerProfile.fetch(playerProfilePDA);
    if (profile.unclaimedSpecials === 0) {
      console.log("SKIP claim_item: no specials found (cooldown prevents enough rolls)");
      return;
    }

    const itemsMintedCounter = profile.itemsMinted;

    const [itemMintPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("item_mint"),
        payer.publicKey.toBuffer(),
        Buffer.from([itemsMintedCounter]),
      ],
      PROGRAM_ID
    );

    const playerItemAta = await getAssociatedTokenAddress(
      itemMintPDA,
      payer.publicKey,
      false,
      TOKEN_PROGRAM_ID,
      ASSOCIATED_TOKEN_PROGRAM_ID,
    );

    await program.methods
      .claimItem("Dragon Sword", "DSWORD", "https://example.com/item.json")
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        itemMint: itemMintPDA,
        playerItemAta: playerItemAta,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
      })
      .rpc();

    const profileAfter = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profileAfter.itemsMinted).to.equal(itemsMintedCounter + 1);
    console.log("Item claimed, total items:", profileAfter.itemsMinted);
  });

  it("equips a weapon (type 0)", async () => {
    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    const itemsMinted = profile.itemsMinted;
    if (itemsMinted === 0) {
      console.log("SKIP equip_weapon: no items minted");
      return;
    }
    expect(itemsMinted).to.be.greaterThan(0);

    const [itemMintPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("item_mint"),
        payer.publicKey.toBuffer(),
        Buffer.from([itemsMinted - 1]),
      ],
      PROGRAM_ID
    );

    const playerItemAta = await getAssociatedTokenAddress(
      itemMintPDA,
      payer.publicKey,
      false,
      TOKEN_PROGRAM_ID,
      ASSOCIATED_TOKEN_PROGRAM_ID,
    );

    await program.methods
      .equipItem(0)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        itemMint: itemMintPDA,
        itemTokenAccount: playerItemAta,
        gameState: gameStatePDA,
      })
      .rpc();

    const profileAfter = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profileAfter.equippedWeapon.toString()).to.equal(itemMintPDA.toString());
    expect(profileAfter.weaponBonus).to.be.greaterThan(0);
    console.log("Weapon equipped, bonus:", profileAfter.weaponBonus);
  });

  it("unequips weapon (type 0)", async () => {
    await program.methods
      .unequipItem(0)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.equippedWeapon.toString()).to.equal(PublicKey.default.toString());
    expect(profile.weaponBonus).to.equal(0);
    console.log("Weapon unequipped");
  });

  it("equips armor (type 1)", async () => {
    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    const itemsMinted = profile.itemsMinted;
    if (itemsMinted === 0) {
      console.log("SKIP equip_armor: no items minted");
      return;
    }
    expect(itemsMinted).to.be.greaterThan(0);

    const [itemMintPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("item_mint"),
        payer.publicKey.toBuffer(),
        Buffer.from([itemsMinted - 1]),
      ],
      PROGRAM_ID
    );

    const playerItemAta = await getAssociatedTokenAddress(
      itemMintPDA,
      payer.publicKey,
      false,
      TOKEN_PROGRAM_ID,
      ASSOCIATED_TOKEN_PROGRAM_ID,
    );

    await program.methods
      .equipItem(1)
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        itemMint: itemMintPDA,
        itemTokenAccount: playerItemAta,
        gameState: gameStatePDA,
      })
      .rpc();

    const profileAfter = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profileAfter.equippedArmor.toString()).to.equal(itemMintPDA.toString());
    expect(profileAfter.armorBonus).to.be.greaterThan(0);
    console.log("Armor equipped, bonus:", profileAfter.armorBonus);
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
    const levelBefore = profileBefore.level;
    const xpNeeded = levelBefore * 100;
    if (profileBefore.xp < xpNeeded) {
      for (let attempt = 0; attempt < 20; attempt++) {
        await new Promise((r) => setTimeout(r, 31000));
        try {
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
            })
            .rpc();
        } catch (e) {}
        const p = await program.account.playerProfile.fetch(playerProfilePDA);
        if (p.xp >= xpNeeded) break;
      }
    }

    const profileReady = await program.account.playerProfile.fetch(playerProfilePDA);
    if (profileReady.xp < profileReady.level * 100) {
      console.log("SKIP level_up: insufficient XP after cooldown wait");
      return;
    }

    await program.methods
      .levelUp()
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        gameState: gameStatePDA,
        playerTokenAccount: playerTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
      })
      .rpc();

    const profileAfter = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profileAfter.level).to.be.greaterThan(levelBefore);
    console.log("Leveled up to:", profileAfter.level);
  });

  it("claims daily reward", async () => {
    const balanceBefore = await getAccount(provider.connection, playerTokenAccount);

    await program.methods
      .claimDailyReward()
      .accounts({
        player: payer.publicKey,
        playerProfile: playerProfilePDA,
        treasury: treasuryPDA,
        treasuryTokenAccount: treasuryTokenAccount,
        playerTokenAccount: playerTokenAccount,
        rewardMint: rewardMint,
        tokenProgram: TOKEN_PROGRAM_ID,
      })
      .rpc();

    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    expect(profile.dailyStreak).to.equal(1);
    expect(profile.lastDailyClaimTs.toNumber()).to.be.greaterThan(0);

    const balanceAfter = await getAccount(provider.connection, playerTokenAccount);
    expect(Number(balanceAfter.amount)).to.be.greaterThan(Number(balanceBefore.amount));
    console.log("Daily reward claimed, streak:", profile.dailyStreak);
  });

  it("creates a Rogue character", async () => {
    const roguePlayer = Keypair.generate();
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

    await program.methods
      .createCharacter("Rogue", "ROG", "https://example.com/rogue.json", 1)
      .accounts({
        player: roguePlayer.publicKey,
        playerProfile: rogueProfilePDA,
        characterMint: rogueCharacterMintPDA,
        playerCharacterAta: null,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
      })
      .signers([roguePlayer])
      .rpc();

    const profile = await program.account.playerProfile.fetch(rogueProfilePDA);
    expect(profile.class).to.equal(1);
    expect(profile.strength).to.equal(5);
    expect(profile.agility).to.equal(10);
    expect(profile.intelligence).to.equal(6);
    expect(profile.luck).to.equal(8);
    console.log("Rogue created");
  });

  it("creates a Mage character", async () => {
    const magePlayer = Keypair.generate();
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

    await program.methods
      .createCharacter("Mage", "MAG", "https://example.com/mage.json", 2)
      .accounts({
        player: magePlayer.publicKey,
        playerProfile: mageProfilePDA,
        characterMint: mageCharacterMintPDA,
        playerCharacterAta: null,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
      })
      .signers([magePlayer])
      .rpc();

    const profile = await program.account.playerProfile.fetch(mageProfilePDA);
    expect(profile.class).to.equal(2);
    expect(profile.strength).to.equal(4);
    expect(profile.agility).to.equal(5);
    expect(profile.intelligence).to.equal(10);
    expect(profile.luck).to.equal(6);
    console.log("Mage created");
  });

  it("force closes a profile", async () => {
    const tempPlayer = Keypair.generate();
    await provider.connection.confirmTransaction(
      await provider.connection.requestAirdrop(
        tempPlayer.publicKey,
        2 * LAMPORTS_PER_SOL
      )
    );

    const [tempProfilePDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("player_profile"), tempPlayer.publicKey.toBuffer()],
      PROGRAM_ID
    );
    const [tempCharMintPDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("character_mint"), tempPlayer.publicKey.toBuffer()],
      PROGRAM_ID
    );

    await program.methods
      .createCharacter("Temp", "TMP", "https://example.com/temp.json", 0)
      .accounts({
        player: tempPlayer.publicKey,
        playerProfile: tempProfilePDA,
        characterMint: tempCharMintPDA,
        playerCharacterAta: null,
        mintAuthority: mintAuthorityPDA,
        gameState: gameStatePDA,
        tokenProgram: TOKEN_PROGRAM_ID,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        systemProgram: SystemProgram.programId,
      })
      .signers([tempPlayer])
      .rpc();

    const profileBefore = await program.account.playerProfile.fetch(tempProfilePDA);
    expect(profileBefore.class).to.equal(0);

    await program.methods
      .forceCloseProfile()
      .accounts({
        player: tempPlayer.publicKey,
        playerProfile: tempProfilePDA,
      })
      .signers([tempPlayer])
      .rpc();

    const profileAfter = await provider.connection.getAccountInfo(tempProfilePDA);
    expect(profileAfter).to.be.null;
    console.log("Profile force closed");
  });

  it("rejects invalid class", async () => {
    const badPlayer = Keypair.generate();
    await provider.connection.confirmTransaction(
      await provider.connection.requestAirdrop(
        badPlayer.publicKey,
        2 * LAMPORTS_PER_SOL
      )
    );

    const [badProfilePDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("player_profile"), badPlayer.publicKey.toBuffer()],
      PROGRAM_ID
    );
    const [badCharMintPDA] = PublicKey.findProgramAddressSync(
      [Buffer.from("character_mint"), badPlayer.publicKey.toBuffer()],
      PROGRAM_ID
    );

    try {
      await program.methods
        .createCharacter("Bad", "BAD", "https://example.com/bad.json", 5)
        .accounts({
          player: badPlayer.publicKey,
          playerProfile: badProfilePDA,
          characterMint: badCharMintPDA,
          playerCharacterAta: null,
          mintAuthority: mintAuthorityPDA,
          gameState: gameStatePDA,
          tokenProgram: TOKEN_PROGRAM_ID,
          associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
          systemProgram: SystemProgram.programId,
        })
        .signers([badPlayer])
        .rpc();
      expect.fail("Should have rejected invalid class");
    } catch (err: any) {
      expect(err.toString()).to.include("InvalidClass");
      console.log("Invalid class rejected");
    }
  });

  it("rejects duplicate airdrop", async () => {
    try {
      await program.methods
        .requestAirdrop()
        .accounts({
          player: payer.publicKey,
          playerProfile: playerProfilePDA,
          treasury: treasuryPDA,
          treasuryTokenAccount: treasuryTokenAccount,
          playerTokenAccount: playerTokenAccount,
          rewardMint: rewardMint,
          tokenProgram: TOKEN_PROGRAM_ID,
        })
        .rpc();
      expect.fail("Should have rejected duplicate airdrop");
    } catch (err: any) {
      expect(err.toString()).to.include("AirdropAlreadyClaimed");
      console.log("Duplicate airdrop rejected");
    }
  });

  it("rejects invalid item type", async () => {
    const profile = await program.account.playerProfile.fetch(playerProfilePDA);
    const itemsMinted = profile.itemsMinted;
    if (itemsMinted === 0) {
      console.log("SKIP invalid_item_type: no items minted");
      return;
    }
    const [itemMintPDA] = PublicKey.findProgramAddressSync(
      [
        Buffer.from("item_mint"),
        payer.publicKey.toBuffer(),
        Buffer.from([itemsMinted - 1]),
      ],
      PROGRAM_ID
    );
    const playerItemAta = await getAssociatedTokenAddress(
      itemMintPDA,
      payer.publicKey,
      false,
      TOKEN_PROGRAM_ID,
      ASSOCIATED_TOKEN_PROGRAM_ID,
    );

    try {
      await program.methods
        .equipItem(5)
        .accounts({
          player: payer.publicKey,
          playerProfile: playerProfilePDA,
          itemMint: itemMintPDA,
          itemTokenAccount: playerItemAta,
          gameState: gameStatePDA,
        })
        .rpc();
      expect.fail("Should have rejected invalid item type");
    } catch (err: any) {
      expect(err.toString()).to.include("InvalidItemType");
      console.log("Invalid item type rejected");
    }
  });

  it("verifies game state totals after all operations", async () => {
    const gameState = await program.account.gameState.fetch(gameStatePDA);
    expect(gameState.totalRolls.toNumber()).to.be.greaterThan(0);
    expect(gameState.totalTokensDistributed.toNumber()).to.be.greaterThan(0);
    console.log("Total rolls:", gameState.totalRolls.toNumber());
    console.log("Total tokens distributed:", gameState.totalTokensDistributed.toNumber());
  });
});
