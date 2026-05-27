# Roll&Earn — Solana Play-to-Earn RPG

A fantasy RPG built on Solana with an on-chain smart contract (Anchor/Rust) and a Unity client (C#). Players create characters (Warrior/Rogue/Mage), roll D20 dice on adventures, earn ROLAND tokens and XP, find and equip NFT items, level up, and claim daily rewards.

**Deployed on Devnet:** `GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T`

## Architecture

```
RollAndEarn/
├── Anchor/                          # Solana smart contract
│   ├── programs/roll_and_earn/      # Rust program (anchor-lang 0.30.1)
│   ├── tests/roll_and_earn.ts       # 21 tests (Mocha + Anchor)
│   ├── scripts/                     # Init-game, PDA derivation helpers
│   ├── metadata/                    # IPFS metadata JSON templates
│   └── Anchor.toml                  # Program ID, cluster config
├── Assets/
│   ├── Scenes/MainScene.unity       # Main game scene
│   ├── Scripts/                     # C# game code
│   │   ├── Core/                    # SolanaManager, AnchorClient, NFTManager, TokenManager, SoundManager
│   │   ├── GameLogic/               # AdventureManager, CooldownManager, RewardCalculator
│   │   ├── Models/                  # PlayerProfile, RollResult, CharacterData, ItemData
│   │   ├── UI/                      # Screens, Components, Navigation, Theme
│   │   └── Utils/                   # IPFSLoader, JsonParser, CoroutineUtils
│   ├── Editor/RollAndEarnSetup.cs   # Programmatic scene generator
│   ├── Fonts/                       # Cinzel, MedievalSharp (TTF)
│   └── Resources/RollAndEarn/       # GameConfig, ScriptableObjects, Sprites
```

## Prerequisites

### Smart Contract (Anchor)
- **Solana CLI** ≥ 1.17 (`sh -c "$(curl -sSfL https://release.anza.xyz/stable/install)"`)
- **Anchor CLI** ≥ 0.30 (`cargo install --git https://github.com/coral-xyz/anchor anchor-cli --tag v0.30.1`)
- **Node.js** ≥ 18
- **Rust** ≥ 1.75 (`rustup default stable`)

### Unity Client
- **Unity** ≥ 2022.3 (URP template)
- **TextMeshPro** (via Package Manager)
- **UniTask** (via Package Manager — `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`)
- **Solana Unity SDK** (`https://github.com/garbles-labs/Solana.Unity-SDK.git`)

## Smart Contract Setup

### 1. Install Dependencies

```bash
cd Anchor
npm install
```

### 2. Build

```bash
anchor build
```

The compiled `.so` binary is at `target/deploy/roll_and_earn.so`.

### 3. Run Tests (Local Validator)

```bash
anchor test
```

This starts a local `solana-test-validator`, deploys the program, and runs 21 tests covering all instructions.

### 4. Deploy to Devnet

```bash
# Configure Solana CLI for Devnet
solana config set --url devnet

# Fund your wallet (airdrop SOL for deployment fees)
solana airdrop 2

# Deploy
anchor deploy --provider.cluster devnet
```

### 5. Initialize the Game

After deployment, run the init script to set up on-chain state:

```bash
# Update the program ID in scripts if needed
npx ts-node scripts/init-game.ts
```

This creates:
- **GameState** PDA — global game state with reward mint and treasury references
- **Treasury** PDA — holds ROLAND tokens for rewards
- **MintAuthority** PDA — authority for minting NFTs (character + items)

## Unity Setup

### 1. Open Project

Open the `RollAndEarn` folder in Unity Hub (Unity 2022.3+ with URP).

### 2. Install Dependencies

Via **Window > Package Manager > Add package from git URL**:
- `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- `https://github.com/garbles-labs/Solana.Unity-SDK.git`

Ensure **TextMeshPro** is installed via Package Manager.

### 3. Generate Font Assets

Menu: **RollAndEarn > Generate Font Assets**

This creates TMP SDF font assets from the TTF files in `Assets/Fonts/`.

### 4. Configure GameConfig

Open `Assets/Resources/RollAndEarn/GameConfig.asset` in the Inspector:

| Field | Value |
|-------|-------|
| RPC Endpoint | `https://api.devnet.solana.com` |
| Roland Mint Address | Your deployed ROLAND token mint |
| Program ID | `GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T` |
| IPFS Gateway | `https://ipfs.io/ipfs/` |
| NFT Metadata URIs | IPFS URIs (pre-filled) |

### 5. Setup Scene

Menu: **RollAndEarn > Setup Everything**

This programmatically generates the entire UI (6 screens, bottom nav, all components) in the active scene. No manual prefab setup needed.

### 6. Run

Press **Play** in the Unity Editor. The game starts at the Wallet Connect screen.

## Game Flow

1. **Connect Wallet** — In-editor wallet or WebGL Phantom/Solflare
2. **Create Character** — Choose Warrior, Rogue, or Mage; enter a name
3. **Claim Airdrop** — One-time 200 ROLAND faucet
4. **Go on Adventures**:
   - Enchanted Forest (free, 5 min cooldown)
   - Dark Dungeon (10 ROLAND, 15 min cooldown)
   - Dragon's Lair (50 ROLAND, 60 min cooldown)
5. **Roll D20** — Animated dice, tier-based rewards (tokens + XP + special items)
6. **Equip Items** — Weapons and armor grant bonus to D20 rolls
7. **Level Up** — Burn tokens + XP to increase stats
8. **Daily Rewards** — 50-120 ROLAND with streak bonuses

## Smart Contract Instructions (12)

| Instruction | Description |
|-------------|-------------|
| `init_game` | Initialize GameState + MintAuthority PDAs |
| `init_treasury` | Initialize Treasury PDA + ATA |
| `create_character` | Create profile, mint character NFT, set class stats |
| `roll_action` | D20 roll, rewards, cooldown, XP |
| `claim_item` | Mint item NFT from unclaimed specials |
| `equip_item` | Equip weapon/armor, compute bonus |
| `unequip_item` | Unequip weapon/armor |
| `level_up` | Burn tokens + XP to level up |
| `request_airdrop` | One-time 200 ROLAND faucet |
| `claim_daily_reward` | Daily 50-120 ROLAND with streak |
| `init_profile` | Create empty profile PDA via CPI |
| `recreate_profile` | Overwrite profile for existing mint |

## Tech Stack

- **Smart Contract:** Rust, Anchor Framework 0.30.1, Solana (devnet)
- **Unity Client:** C#, Unity 2022.3 URP, TextMeshPro, UniTask
- **Blockchain:** Solana Web3.js, Solana Unity SDK, spl-token
- **NFT Metadata:** IPFS (Pinata pinning), on-chain URI references
- **Audio:** Procedural synthesis (no external audio files)

## License

MIT
