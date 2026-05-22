using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.Networking;

namespace RollAndEarn
{
    public class NFTManager : MonoBehaviour
    {
        private GameConfig gameConfig;

        private GameConfig GetConfig() => gameConfig ??= GameConfigProvider.Instance != null ? GameConfigProvider.Instance.Config : null;

        private const string RAE_ITEM = "RAE_ITEM";

        public async UniTask<List<NftMetadata>> GetNftsByOwnerAsync(string ownerPubkey)
        {
            var rpc = SolanaManager.Instance.GetRpcClient();
            var owner = new PublicKey(ownerPubkey);
            var result = await rpc.GetTokenAccountsByOwnerAsync(owner, null, TokenProgram.ProgramIdKey);

            var nftList = new List<NftMetadata>();

            if (result.Result?.Value == null || result.Result.Value.Count == 0)
                return nftList;

            foreach (var tokenAccount in result.Result.Value)
            {
                var parsedInfo = tokenAccount.Account.Data.Parsed.Info;
                if (parsedInfo.TokenAmount.AmountUlong != 1)
                    continue;

                var mintAddress = parsedInfo.Mint;
                var metadata = await GetMetadataAsync(mintAddress);
                if (metadata != null)
                {
                    metadata.mint = mintAddress;
                    nftList.Add(metadata);
                }
            }

            return nftList;
        }

        public async UniTask<NftMetadata> GetMetadataAsync(string mintAddress)
        {
            var rpc = SolanaManager.Instance.GetRpcClient();
            var metaplexId = new PublicKey("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt918Cms");
            var mint = new PublicKey(mintAddress);

            PublicKey.TryFindProgramAddress(
                new[] { Encoding.UTF8.GetBytes("metadata"), metaplexId.KeyBytes, mint.KeyBytes },
                metaplexId, out var metadataPda, out _);

            var accountInfo = await rpc.GetAccountInfoAsync(metadataPda);
            if (accountInfo.Result?.Value?.Data == null) return null;

            var rawData = Convert.FromBase64String(accountInfo.Result.Value.Data[0]);
            if (rawData.Length < 100) return null;

            var onChain = ParseOnChainMetadata(rawData);

            if (!string.IsNullOrEmpty(onChain.uri))
            {
                var jsonUrl = onChain.uri;
                if (jsonUrl.StartsWith("ipfs://"))
                    jsonUrl = GetConfig().ipfsGateway + jsonUrl.Substring("ipfs://".Length);

                var json = await HttpGetAsync(jsonUrl);
                if (json != null)
                {
                    var response = JsonUtility.FromJson<NftJsonResponse>(json);
                    if (response != null)
                    {
                        onChain.image = response.image;
                        if (onChain.image != null && onChain.image.StartsWith("ipfs://"))
                            onChain.image = GetConfig().ipfsGateway + onChain.image.Substring("ipfs://".Length);

                        onChain.attributes = new List<Attribute>();
                        if (response.attributes != null)
                            foreach (var attr in response.attributes)
                                onChain.attributes.Add(new Attribute { trait_type = attr.trait_type, value = attr.value });
                    }
                }
            }

            return onChain;
        }

        public async UniTask<List<NftMetadata>> GetCharacterNftsAsync(string ownerPubkey)
        {
            var all = await GetNftsByOwnerAsync(ownerPubkey);
            var characters = new List<NftMetadata>();
            foreach (var nft in all)
                if (!IsItemNft(nft)) characters.Add(nft);
            return characters;
        }

        public async UniTask<List<NftMetadata>> GetItemNftsAsync(string ownerPubkey)
        {
            var rpc = SolanaManager.Instance.GetRpcClient();
            var owner = new PublicKey(ownerPubkey);
            var result = await rpc.GetTokenAccountsByOwnerAsync(owner, null, TokenProgram.ProgramIdKey);

            var items = new List<NftMetadata>();

            if (result.Result?.Value == null || result.Result.Value.Count == 0)
                return items;

            var anchorClient = FindAnyObjectByType<AnchorClient>();
            var profile = SolanaManager.Instance?.CurrentProfile;
            byte itemsMinted = profile?.itemsMinted ?? 0;

            var knownItemMints = new HashSet<string>();
            if (anchorClient != null && itemsMinted > 0)
            {
                for (byte i = 0; i < itemsMinted; i++)
                {
                    var itemMintPda = anchorClient.DeriveItemMintPda(owner, i);
                    knownItemMints.Add(itemMintPda.ToString());
                }
            }

            string characterMint = profile?.characterMint ?? "";
            var rolandMint = GetConfig()?.rolandMintAddress ?? "";

            foreach (var tokenAccount in result.Result.Value)
            {
                var parsedInfo = tokenAccount.Account.Data.Parsed.Info;
                if (parsedInfo.TokenAmount.AmountUlong != 1)
                    continue;

                var mintAddress = parsedInfo.Mint;
                if (mintAddress == characterMint || mintAddress == rolandMint)
                    continue;

                bool isKnownItem = knownItemMints.Contains(mintAddress);

                var metadata = await GetMetadataAsync(mintAddress);
                if (metadata != null)
                {
                    metadata.mint = mintAddress;
                    if (IsItemNft(metadata) || isKnownItem)
                        items.Add(metadata);
                }
                else if (isKnownItem)
                {
                    items.Add(new NftMetadata
                    {
                        mint = mintAddress,
                        name = $"RAE Item",
                        symbol = RAE_ITEM,
                        uri = "",
                    });
                }
            }

            return items;
        }

        public async UniTask<List<NftMetadata>> LoadAllNFTsAsync(string ownerPubkey)
        {
            return await GetNftsByOwnerAsync(ownerPubkey);
        }

        public async UniTask<List<NftMetadata>> LoadItemNFTsAsync(string ownerPubkey)
        {
            return await GetItemNftsAsync(ownerPubkey);
        }

        private bool IsItemNft(NftMetadata metadata)
        {
            if (metadata.symbol == RAE_ITEM)
                return true;
            foreach (var attr in metadata.attributes)
            {
                if (attr.trait_type == "type" && attr.value == "item")
                    return true;
            }
            return false;
        }

        private static NftMetadata ParseOnChainMetadata(byte[] data)
        {
            int offset = 1;
            offset += 32;
            offset += 32;

            var name = ReadBorshString(data, ref offset, 32);
            var symbol = ReadBorshString(data, ref offset, 10);
            var uri = ReadBorshString(data, ref offset, 200);

            return new NftMetadata { name = name, symbol = symbol, uri = uri };
        }

        private static string ReadBorshString(byte[] data, ref int offset, int fixedBufferSize)
        {
            if (offset + 4 > data.Length) return "";
            var length = (int)BitConverter.ToUInt32(data, offset);
            offset += 4;
            if (length <= 0 || offset + length > data.Length) { offset += fixedBufferSize; return ""; }
            var str = Encoding.UTF8.GetString(data, offset, length).TrimEnd('\0');
            offset += fixedBufferSize;
            return str;
        }

        private static async UniTask<string> HttpGetAsync(string url)
        {
            using var request = UnityWebRequest.Get(url);
            await request.SendWebRequest().ToUniTask();
            if (request.result != UnityWebRequest.Result.Success) return null;
            return request.downloadHandler.text;
        }
    }

    [Serializable]
    public class NftMetadata
    {
        public string mint;
        public string name;
        public string symbol;
        public string uri;
        public string image;
        public List<Attribute> attributes = new();
    }

    [Serializable]
    public class Attribute
    {
        public string trait_type;
        public string value;
    }

    [Serializable]
    public class NftJsonResponse
    {
        public string name;
        public string image;
        public NftAttributeJson[] attributes;
    }

    [Serializable]
    public class NftAttributeJson
    {
        public string trait_type;
        public string value;
    }
}
