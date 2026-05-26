using TMPro;
using UnityEngine;

namespace RollAndEarn
{
    public static class FontProvider
    {
        private static TMP_FontAsset _titleFont;
        private static TMP_FontAsset _bodyFont;
        private static TMP_FontAsset _defaultFont;

        public static TMP_FontAsset TitleFont
        {
            get
            {
                if (_titleFont == null)
                    _titleFont = Resources.Load<TMP_FontAsset>("RollAndEarn/Fonts/Cinzel SDF");
                return _titleFont;
            }
        }

        public static TMP_FontAsset BodyFont
        {
            get
            {
                if (_bodyFont == null)
                    _bodyFont = Resources.Load<TMP_FontAsset>("RollAndEarn/Fonts/MedievalSharp SDF");
                return _bodyFont;
            }
        }

        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_defaultFont != null) return _defaultFont;
                if (BodyFont != null) { _defaultFont = BodyFont; return _defaultFont; }
                if (TitleFont != null) { _defaultFont = TitleFont; return _defaultFont; }
                _defaultFont = TMP_Settings.defaultFontAsset;
                return _defaultFont;
            }
        }

        public static TMP_FontAsset Fallback => TMP_Settings.defaultFontAsset;

        public static void ApplyToText(TMP_Text tmp, bool isTitle = false, float size = 0)
        {
            if (tmp == null) return;
            var font = isTitle ? TitleFont : BodyFont;
            if (font != null)
                tmp.font = font;
            if (size > 0)
                tmp.fontSize = size;
        }
    }
}
