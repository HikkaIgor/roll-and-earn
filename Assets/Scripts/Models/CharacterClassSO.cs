using UnityEngine;

namespace RollAndEarn
{
    [CreateAssetMenu(menuName = "RollAndEarn/CharacterClass")]
    public class CharacterClassSO : ScriptableObject
    {
        public string className;
        public byte classType;
        public byte baseStrength;
        public byte baseAgility;
        public byte baseIntelligence;
        public byte baseLuck;
        public string rollBonusDescription;
        public Sprite classIcon;
        public Sprite artSprite;
    }
}
