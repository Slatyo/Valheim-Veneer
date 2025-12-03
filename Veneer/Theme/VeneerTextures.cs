using UnityEngine;

namespace Veneer.Theme
{
    /// <summary>
    /// Procedurally generated textures for Veneer UI components.
    /// Creates solid colors, borders, and gradients at runtime.
    /// </summary>
    public static class VeneerTextures
    {
        // Cached textures
        private static Texture2D _white;
        private static Texture2D _background;
        private static Texture2D _backgroundSolid;
        private static Texture2D _backgroundLight;
        private static Texture2D _border;
        private static Texture2D _borderHighlight;

        /// <summary>
        /// Simple 1x1 white texture for tinting.
        /// </summary>
        public static Texture2D White
        {
            get
            {
                if (_white == null)
                    _white = CreateSolidTexture(Color.white);
                return _white;
            }
        }

        /// <summary>
        /// Default semi-transparent background texture.
        /// </summary>
        public static Texture2D Background
        {
            get
            {
                if (_background == null)
                    _background = CreateSolidTexture(VeneerColors.Background);
                return _background;
            }
        }

        /// <summary>
        /// Solid (opaque) background texture.
        /// </summary>
        public static Texture2D BackgroundSolid
        {
            get
            {
                if (_backgroundSolid == null)
                    _backgroundSolid = CreateSolidTexture(VeneerColors.BackgroundSolid);
                return _backgroundSolid;
            }
        }

        /// <summary>
        /// Lighter background for nested elements.
        /// </summary>
        public static Texture2D BackgroundLight
        {
            get
            {
                if (_backgroundLight == null)
                    _backgroundLight = CreateSolidTexture(VeneerColors.BackgroundLight);
                return _backgroundLight;
            }
        }

        /// <summary>
        /// Standard border texture.
        /// </summary>
        public static Texture2D Border
        {
            get
            {
                if (_border == null)
                    _border = CreateSolidTexture(VeneerColors.Border);
                return _border;
            }
        }

        /// <summary>
        /// Gold highlight border texture.
        /// </summary>
        public static Texture2D BorderHighlight
        {
            get
            {
                if (_borderHighlight == null)
                    _borderHighlight = CreateSolidTexture(VeneerColors.BorderHighlight);
                return _borderHighlight;
            }
        }

        /// <summary>
        /// Creates a simple 1x1 solid color texture.
        /// </summary>
        public static Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            return texture;
        }

        /// <summary>
        /// Creates a bordered texture with inner fill.
        /// </summary>
        /// <param name="width">Texture width</param>
        /// <param name="height">Texture height</param>
        /// <param name="borderColor">Color of the border</param>
        /// <param name="fillColor">Color of the interior</param>
        /// <param name="borderWidth">Width of the border in pixels</param>
        public static Texture2D CreateBorderedTexture(int width, int height, Color borderColor, Color fillColor, int borderWidth = 1)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x < borderWidth || x >= width - borderWidth ||
                                    y < borderWidth || y >= height - borderWidth;
                    texture.SetPixel(x, y, isBorder ? borderColor : fillColor);
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        /// <summary>
        /// Creates a 9-slice compatible bordered texture for UI sprites.
        /// Border pixels are at edges, corners are single color.
        /// </summary>
        public static Texture2D CreateSlicedBorderTexture(int size, Color borderColor, Color fillColor, int borderWidth = 1)
        {
            // Minimum size to support 9-slice: borderWidth * 2 + 1
            int minSize = borderWidth * 2 + 1;
            size = Mathf.Max(size, minSize);

            return CreateBorderedTexture(size, size, borderColor, fillColor, borderWidth);
        }

        /// <summary>
        /// Creates a horizontal gradient texture.
        /// </summary>
        public static Texture2D CreateHorizontalGradient(int width, Color left, Color right)
        {
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false);

            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                texture.SetPixel(x, 0, Color.Lerp(left, right, t));
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        /// <summary>
        /// Creates a vertical gradient texture.
        /// </summary>
        public static Texture2D CreateVerticalGradient(int height, Color bottom, Color top)
        {
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                texture.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        /// <summary>
        /// Creates a sprite from a texture with proper settings.
        /// </summary>
        public static Sprite CreateSprite(Texture2D texture)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        /// <summary>
        /// Creates a 9-sliced sprite for bordered UI elements.
        /// </summary>
        public static Sprite CreateSlicedSprite(Texture2D texture, int border)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border)
            );
        }

        /// <summary>
        /// Creates a standard Veneer panel background sprite (bordered, dark).
        /// </summary>
        public static Sprite CreatePanelSprite(int size = 16, int borderWidth = 1)
        {
            var texture = CreateSlicedBorderTexture(size, VeneerColors.Border, VeneerColors.Background, borderWidth);
            return CreateSlicedSprite(texture, borderWidth);
        }

        /// <summary>
        /// Creates a button background sprite.
        /// </summary>
        public static Sprite CreateButtonSprite(int size = 16, int borderWidth = 1)
        {
            var texture = CreateSlicedBorderTexture(size, VeneerColors.Border, VeneerColors.ButtonNormal, borderWidth);
            return CreateSlicedSprite(texture, borderWidth);
        }

        /// <summary>
        /// Creates a slot background sprite.
        /// </summary>
        public static Sprite CreateSlotSprite(int size = 16, int borderWidth = 1)
        {
            var texture = CreateSlicedBorderTexture(size, VeneerColors.Border, VeneerColors.SlotEmpty, borderWidth);
            return CreateSlicedSprite(texture, borderWidth);
        }

        /// <summary>
        /// Cleanup cached textures.
        /// </summary>
        public static void Cleanup()
        {
            if (_white != null) Object.Destroy(_white);
            if (_background != null) Object.Destroy(_background);
            if (_backgroundSolid != null) Object.Destroy(_backgroundSolid);
            if (_backgroundLight != null) Object.Destroy(_backgroundLight);
            if (_border != null) Object.Destroy(_border);
            if (_borderHighlight != null) Object.Destroy(_borderHighlight);

            _white = null;
            _background = null;
            _backgroundSolid = null;
            _backgroundLight = null;
            _border = null;
            _borderHighlight = null;
        }
    }
}
