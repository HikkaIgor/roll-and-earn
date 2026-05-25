const fs = require("fs");
const path = require("path");
const FormData = require("form-data");

const METADATA_DIR = path.join(__dirname, "metadata");
const IMAGES_DIR = path.join(__dirname, "..", "Assets", "Resources", "RollAndEarn", "Sprites");

const IMAGE_FILES = {
  WARRIOR_CID: "WarriorCard.png",
  ROGUE_CID: "RogueCard.png",
  MAGE_CID: "MageCard.png",
  ITEM_CID: "ItemPlaceholder.png",
};

function pinFileToPinata(filePath, fileName, jwt) {
  return new Promise((resolve, reject) => {
    const form = new FormData();
    form.append("file", fs.createReadStream(filePath), fileName);

    const https = require("https");
    const opts = {
      hostname: "api.pinata.cloud",
      path: "/pinning/pinFileToIPFS",
      method: "POST",
      headers: {
        Authorization: `Bearer ${jwt}`,
        ...form.getHeaders(),
      },
    };

    const req = https.request(opts, (res) => {
      let data = "";
      res.on("data", (c) => (data += c));
      res.on("end", () => {
        try {
          const parsed = JSON.parse(data);
          resolve(parsed);
        } catch (e) {
          reject(new Error(`Parse error: ${data}`));
        }
      });
    });
    req.on("error", reject);
    form.pipe(req);
  });
}

function pinBufferToPinata(buffer, fileName, jwt) {
  return new Promise((resolve, reject) => {
    const form = new FormData();
    form.append("file", buffer, { filename: fileName, contentType: "application/json" });

    const https = require("https");
    const opts = {
      hostname: "api.pinata.cloud",
      path: "/pinning/pinFileToIPFS",
      method: "POST",
      headers: {
        Authorization: `Bearer ${jwt}`,
        ...form.getHeaders(),
      },
    };

    const req = https.request(opts, (res) => {
      let data = "";
      res.on("data", (c) => (data += c));
      res.on("end", () => {
        try {
          resolve(JSON.parse(data));
        } catch (e) {
          reject(new Error(`Parse error: ${data}`));
        }
      });
    });
    req.on("error", reject);
    form.pipe(req);
  });
}

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

    try {
      const result = await pinFileToPinata(imgPath, filename, PINATA_JWT);
      if (!result.IpfsHash) {
        console.error(`Failed to pin ${filename}:`, result);
        continue;
      }
      imageCids[placeholder] = result.IpfsHash;
      console.log(`Pinned ${filename} -> ipfs://${result.IpfsHash}`);
    } catch (e) {
      console.error(`Error pinning ${filename}:`, e.message);
    }
  }

  console.log("\n--- Uploading metadata ---\n");

  for (const file of fs.readdirSync(METADATA_DIR)) {
    if (!file.endsWith(".json")) continue;

    let content = fs.readFileSync(path.join(METADATA_DIR, file), "utf8");
    for (const [placeholder, cid] of Object.entries(imageCids)) {
      content = content.replace(new RegExp(placeholder, "g"), cid);
    }

    try {
      const result = await pinBufferToPinata(Buffer.from(content), file, PINATA_JWT);
      if (result.IpfsHash) {
        console.log(`${file} -> ipfs://${result.IpfsHash}`);
      } else {
        console.error(`Failed: ${file}`, result);
      }
    } catch (e) {
      console.error(`Error pinning ${file}:`, e.message);
    }
  }
}

const provider = process.argv[2] || "pinata";

if (provider === "pinata") {
  pinWithPinata();
} else {
  console.error("Only 'pinata' provider is supported in this version");
  process.exit(1);
}
