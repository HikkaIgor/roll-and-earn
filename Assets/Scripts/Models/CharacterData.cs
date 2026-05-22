using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollAndEarn
{
    [Serializable]
    public class CharacterData
    {
        public string characterName;
        public byte classType;
        public string className;
        public byte strength;
        public byte agility;
        public byte intelligence;
        public byte luck;
        public byte level;
        public uint xp;
        public uint xpToNextLevel;
        public string imageUri;
        public Sprite artSprite;

        public static CharacterData FromNftMetadata(NftMetadata meta)
        {
            var data = new CharacterData();
            data.characterName = meta.name;
            data.className = "";
            foreach (var attr in meta.attributes)
            {
                switch (attr.trait_type)
                {
                    case "Class":
                        data.className = attr.value;
                        data.classType = attr.value switch
                        {
                            "Warrior" => 0,
                            "Rogue" => 1,
                            "Mage" => 2,
                            _ => 0
                        };
                        break;
                    case "Level":
                        if (byte.TryParse(attr.value, out var lv)) data.level = lv;
                        break;
                    case "Strength":
                        if (byte.TryParse(attr.value, out var str)) data.strength = str;
                        break;
                    case "Agility":
                        if (byte.TryParse(attr.value, out var agi)) data.agility = agi;
                        break;
                    case "Intelligence":
                        if (byte.TryParse(attr.value, out var int_)) data.intelligence = int_;
                        break;
                    case "Luck":
                        if (byte.TryParse(attr.value, out var lck)) data.luck = lck;
                        break;
                    case "Experience":
                        if (uint.TryParse(attr.value, out var xp)) data.xp = xp;
                        break;
                }
            }
            data.xpToNextLevel = (uint)(data.level + 1) * 100;
            data.imageUri = meta.image;
            return data;
        }
    }
}
