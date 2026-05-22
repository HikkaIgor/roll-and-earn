# NFT Metadata Upload Guide

## Files

- `metadata/warrior.json` - Warrior character NFT metadata
- `metadata/rogue.json` - Rogue character NFT metadata
- `metadata/mage.json` - Mage character NFT metadata
- `metadata/item_weapon.json` - Weapon item NFT metadata
- `metadata/item_armor.json` - Armor item NFT metadata
- `upload_to_ipfs.js` - Upload script (Pinata or nft.storage)

## Step 1: Get an API key

### Option A: Pinata (recommended)
1. Go to https://app.pinata.cloud/ and create account
2. Go to Developers > API Keys > New Key
3. Copy the JWT token

### Option B: nft.storage
1. Go to https://nft.storage/ and create account
2. Get API token

## Step 2: Upload images and metadata

```bash
cd Anchor

# Option A: Pinata
PINATA_JWT="your_jwt_token_here" node upload_to_ipfs.js pinata

# Option B: nft.storage
NFT_STORAGE_TOKEN="your_token_here" node upload_to_ipfs.js nft-storage
```

The script will:
1. Upload all images (WarriorCard.png, RogueCard.png, MageCard.png, ItemPlaceholder.png) to IPFS
2. Replace `ipfs://PLACEHOLDER` in JSON files with actual CIDs
3. Upload all metadata JSON files to IPFS
4. Print the final `ipfs://CID` URIs

## Step 3: Update GameConfig in Unity

1. Open Unity
2. Find `Assets/Resources/RollAndEarn/GameConfig.asset`
3. Set the metadata URIs:
   - `Warrior Metadata Uri` = `ipfs://CID` (from warrior.json upload)
   - `Rogue Metadata Uri` = `ipfs://CID` (from rogue.json upload)
   - `Mage Metadata Uri` = `ipfs://CID` (from mage.json upload)
   - `Weapon Metadata Uri` = `ipfs://CID` (from item_weapon.json upload)
   - `Armor Metadata Uri` = `ipfs://CID` (from item_armor.json upload)

## Step 4: Deploy updated program to Devnet (if using Metaplex CPI)

```bash
cd Anchor
anchor build
anchor deploy --provider.cluster devnet
```
