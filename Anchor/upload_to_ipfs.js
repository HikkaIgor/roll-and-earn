const fs = require("fs");
const path = require("path");

const METADATA_DIR = path.join(__dirname, "metadata");
const IMAGES_DIR = path.join(__dirname, "..", "Assets", "Resources", "RollAndEarn", "Sprites");

const IMAGE_FILES = {
  WARRIOR_CID: "WarriorCard.png",
  ROGUE_CID: "RogueCard.png",
  MAGE_CID: "MageCard.png",
  ITEM_CID: "ItemPlaceholder.png",
};

async function pinWithPinata() {
  const PINATA_JWT = process.env.PINATA_JWT;
  if (!PINATA_JWT) {
    console.error("Set PINATA_JWT env var with your Pinata JWT token");
    console.error("Get one at https://app.pinata.cloud/developers/api-keys");
    process.exit(1);
  }

  const imageCids = {};

  for (const [placeholder, filename] of Object.entries(IMAGE_FILES)) {
    const imgPath = path.join(IMAGES_DIR, filename);
    if (!fs.existsSync(imgPath)) {
      console.error(`Image not found: ${imgPath}`);
      continue;
    }

    const formData = new (require("form-data"))();
    formData.append("file", fs.createReadStream(imgPath), filename);

    const pinFileRes = await fetch("https://api.pinata.cloud/pinning/pinFileToIPFS", {
      method: "POST",
      headers: { Authorization: `Bearer ${PINATA_JWT}` },
      body: formData,
    });

    const pinFileData = await pinFileRes.json();
    if (!pinFileData.IpfsHash) {
      console.error(`Failed to pin ${filename}:`, pinFileData);
      continue;
    }

    imageCids[placeholder] = pinFileData.IpfsHash;
    console.log(`Pinned ${filename} -> ipfs://${pinFileData.IpfsHash}`);
  }

  for (const file of fs.readdirSync(METADATA_DIR)) {
    if (!file.endsWith(".json")) continue;

    const filePath = path.join(METADATA_DIR, file);
    let content = fs.readFileSync(filePath, "utf8");

    for (const [placeholder, cid] of Object.entries(imageCids)) {
      content = content.replace(new RegExp(placeholder, "g"), cid);
    }

    const formData = new (require("form-data"))();
    formData.append("file", Buffer.from(content), file);

    const pinRes = await fetch("https://api.pinata.cloud/pinning/pinFileToIPFS", {
      method: "POST",
      headers: { Authorization: `Bearer ${PINATA_JWT}` },
      body: formData,
    });

    const pinData = await pinRes.json();
    if (pinData.IpfsHash) {
      console.log(`Pinned ${file} -> ipfs://${pinData.IpfsHash}`);
      console.log(`  URI: ipfs://${pinData.IpfsHash}`);
    } else {
      console.error(`Failed to pin ${file}:`, pinData);
    }
  }
}

async function pinWithNftStorage() {
  const NFT_STORAGE_TOKEN = process.env.NFT_STORAGE_TOKEN;
  if (!NFT_STORAGE_TOKEN) {
    console.error("Set NFT_STORAGE_TOKEN env var");
    console.error("Get one at https://nft.storage/");
    process.exit(1);
  }

  const imageCids = {};

  for (const [placeholder, filename] of Object.entries(IMAGE_FILES)) {
    const imgPath = path.join(IMAGES_DIR, filename);
    if (!fs.existsSync(imgPath)) continue;

    const fileBuf = fs.readFileSync(imgPath);
    const res = await fetch("https://api.nft.storage/upload", {
      method: "POST",
      headers: { Authorization: `Bearer ${NFT_STORAGE_TOKEN}` },
      body: fileBuf,
    });

    const data = await res.json();
    if (data.ok && data.value?.cid) {
      imageCids[placeholder] = data.value.cid;
      console.log(`Pinned ${filename} -> ipfs://${data.value.cid}`);
    } else {
      console.error(`Failed: ${filename}`, data);
    }
  }

  for (const file of fs.readdirSync(METADATA_DIR)) {
    if (!file.endsWith(".json")) continue;

    let content = fs.readFileSync(path.join(METADATA_DIR, file), "utf8");
    for (const [p, cid] of Object.entries(imageCids)) {
      content = content.replace(new RegExp(p, "g"), cid);
    }

    const res = await fetch("https://api.nft.storage/upload", {
      method: "POST",
      headers: { Authorization: `Bearer ${NFT_STORAGE_TOKEN}` },
      body: Buffer.from(content),
    });

    const data = await res.json();
    if (data.ok && data.value?.cid) {
      console.log(`Pinned ${file} -> ipfs://${data.value.cid}`);
      console.log(`  URI: ipfs://${data.value.cid}`);
    }
  }
}

const provider = process.argv[2] || "pinata";

if (provider === "nft-storage") {
  pinWithNftStorage();
} else {
  pinWithPinata();
}
