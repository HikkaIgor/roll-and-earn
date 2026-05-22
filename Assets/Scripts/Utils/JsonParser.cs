using System.Collections.Generic;
using UnityEngine;

namespace RollAndEarn
{
    public static class JsonParser
    {
        public static NftMetadata ParseMetadata(string json)
        {
            var response = JsonUtility.FromJson<NftJsonResponse>(json);
            if (response == null) return new NftMetadata();

            var meta = new NftMetadata
            {
                name = response.name ?? "",
                image = response.image ?? "",
            };

            if (response.attributes != null)
            {
                foreach (var attr in response.attributes)
                {
                    meta.attributes.Add(new Attribute
                    {
                        trait_type = attr.trait_type ?? "",
                        value = attr.value ?? ""
                    });
                }
            }

            return meta;
        }
    }
}
