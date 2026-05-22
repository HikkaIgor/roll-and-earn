using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RollAndEarn
{
    public static class IPFSLoader
    {
        private const string GatewayBase = "https://ipfs.io/ipfs/";
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static async UniTask<Sprite> LoadSpriteAsync(string ipfsUri)
        {
            if (Cache.TryGetValue(ipfsUri, out var cached)) return cached;

            string url = ConvertToGatewayUrl(ipfsUri);

            using var request = UnityWebRequestTexture.GetTexture(url);
            await request.SendWebRequest().ToUniTask();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, 100f);
            Cache[ipfsUri] = sprite;
            return sprite;
        }

        private static string ConvertToGatewayUrl(string ipfsUri)
        {
            if (ipfsUri.StartsWith("ipfs://"))
                return GatewayBase + ipfsUri.Substring(7);
            if (ipfsUri.StartsWith("ipfs/"))
                return GatewayBase + ipfsUri.Substring(5);
            return ipfsUri;
        }
    }
}
