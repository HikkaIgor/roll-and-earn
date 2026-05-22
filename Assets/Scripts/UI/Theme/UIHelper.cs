using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public static class UIHelper
    {
        private static Sprite _roundedRectSprite;
        private static Sprite _roundedRectSmallSprite;

        public static Sprite GetRoundedRect()
        {
            if (_roundedRectSprite != null) return _roundedRectSprite;

            int size = 64;
            int radius = 10;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "UIHelper_RoundedRect";
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, size, size, radius);
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            _roundedRectSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            _roundedRectSprite.name = "UIHelper_RoundedRect";
            return _roundedRectSprite;
        }

        public static Sprite GetRoundedRectSmall()
        {
            if (_roundedRectSmallSprite != null) return _roundedRectSmallSprite;

            int size = 32;
            int radius = 6;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "UIHelper_RoundedRectSmall";
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, size, size, radius);
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            _roundedRectSmallSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            _roundedRectSmallSprite.name = "UIHelper_RoundedRectSmall";
            return _roundedRectSmallSprite;
        }

        private static bool IsInsideRoundedRect(int x, int y, int w, int h, int r)
        {
            if (x < r && y < r)
                return (x - r) * (x - r) + (y - r) * (y - r) <= r * r;
            if (x >= w - r && y < r)
                return (x - (w - r - 1)) * (x - (w - r - 1)) + (y - r) * (y - r) <= r * r;
            if (x < r && y >= h - r)
                return (x - r) * (x - r) + (y - (h - r - 1)) * (y - (h - r - 1)) <= r * r;
            if (x >= w - r && y >= h - r)
                return (x - (w - r - 1)) * (x - (w - r - 1)) + (y - (h - r - 1)) * (y - (h - r - 1)) <= r * r;
            return true;
        }

        public static RectTransform CreateStretch(string name, Transform parent, float inset = 0f)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        public static RectTransform CreateOutset(string name, Transform parent, float outset)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-outset, -outset);
            rect.offsetMax = new Vector2(outset, outset);
            return rect;
        }

        public static RectTransform CreateAnchored(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image AddRoundedImage(RectTransform rect, Color color)
        {
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = GetRoundedRect();
            img.color = color;
            img.type = Image.Type.Sliced;
            return img;
        }

        public static Image AddRoundedImageSmall(RectTransform rect, Color color)
        {
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = GetRoundedRectSmall();
            img.color = color;
            img.type = Image.Type.Sliced;
            return img;
        }

        public static Image AddGlowLayer(Transform parent, Color color, float outset)
        {
            var rect = CreateOutset("Glow", parent, outset);
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = GetRoundedRect();
            img.color = color;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static RectTransform CreateGlassPanel(string name, Transform parent)
        {
            var border = CreateStretch(name, parent);
            var borderImg = border.gameObject.AddComponent<Image>();
            borderImg.sprite = GetRoundedRect();
            borderImg.color = ThemeColors.GlassBorder;
            borderImg.type = Image.Type.Sliced;

            var inner = CreateStretch("Glass", border, 2f);
            var innerImg = inner.gameObject.AddComponent<Image>();
            innerImg.sprite = GetRoundedRect();
            innerImg.color = ThemeColors.GlassPanel;
            innerImg.type = Image.Type.Sliced;

            return border;
        }

        public static Image CreateGradientBar(Transform parent, Color topColor, Color bottomColor, float height)
        {
            int texWidth = 4;
            int texHeight = 32;
            var tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            tex.name = "UIHelper_Gradient";
            var pixels = new Color32[texWidth * texHeight];

            for (int y = 0; y < texHeight; y++)
            {
                float t = (float)y / (texHeight - 1);
                var c = Color.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < texWidth; x++)
                    pixels[y * texWidth + x] = c;
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            tex.wrapMode = TextureWrapMode.Clamp;

            var sprite = Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight),
                new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);

            var barRect = CreateAnchored("GradientBar", parent,
                new Vector2(0, 1f), Vector2.one);
            barRect.offsetMin = new Vector2(0, -height);
            barRect.offsetMax = Vector2.zero;

            var img = barRect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }
    }
}
