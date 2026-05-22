using UnityEngine;

namespace RollAndEarn
{
    [CreateAssetMenu(menuName = "RollAndEarn/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Solana")]
        public string rpcEndpoint = "https://api.devnet.solana.com";
        public string websocketEndpoint = "wss://api.devnet.solana.com";
        public string rolandMintAddress;
        public string programId;

        [Header("IPFS")]
        public string ipfsGateway = "https://ipfs.io/ipfs/";

        [Header("NFT Metadata URIs")]
        public string warriorMetadataUri;
        public string rogueMetadataUri;
        public string mageMetadataUri;
        public string weaponMetadataUri;
        public string armorMetadataUri;

        [Header("Faucet")]
        public string faucetApiEndpoint;
        public ulong faucetAmount = 100_000_000_000;

        [Header("Game")]
        public int cooldownCheckIntervalSeconds = 10;
        public int transactionTimeoutSeconds = 30;

        [Header("References")]
        public AdventureConfigSO[] Adventures;
        public CharacterClassSO[] CharacterClasses;
    }
}
