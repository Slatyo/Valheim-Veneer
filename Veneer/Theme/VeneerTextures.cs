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
        /// Creates a circular texture with anti-aliased edges.
        /// </summary>
        /// <param name="size">Texture dimensions (square)</param>
        /// <param name="fillColor">Interior color</param>
        /// <param name="borderColor">Border ring color</param>
        /// <param name="borderWidth">Width of the border ring in pixels</param>
        public static Texture2D CreateCircleTexture(int size, Color fillColor, Color borderColor, int borderWidth = 3)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float outerRadius = size / 2f - 1f; // Leave 1px margin for AA
            float innerRadius = outerRadius - borderWidth;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > outerRadius + 1f)
                    {
                        // Outside circle - transparent
                        texture.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > outerRadius)
                    {
                        // Outer edge AA
                        float alpha = 1f - (dist - outerRadius);
                        texture.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * alpha));
                    }
                    else if (dist > innerRadius)
                    {
                        // Border ring
                        texture.SetPixel(x, y, borderColor);
                    }
                    else if (dist > innerRadius - 1f)
                    {
                        // Inner edge AA (blend border to fill)
                        float t = 1f - (innerRadius - dist);
                        texture.SetPixel(x, y, Color.Lerp(fillColor, borderColor, t));
                    }
                    else
                    {
                        // Fill
                        texture.SetPixel(x, y, fillColor);
                    }
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        /// <summary>
        /// Creates a circular sprite.
        /// </summary>
        public static Sprite CreateCircleSprite(int size, Color fillColor, Color borderColor, int borderWidth = 3)
        {
            var texture = CreateCircleTexture(size, fillColor, borderColor, borderWidth);
            return CreateSprite(texture);
        }

        /// <summary>
        /// Creates a circular ring texture (border only, transparent center).
        /// </summary>
        public static Texture2D CreateCircleRingTexture(int size, Color ringColor, int ringWidth = 3)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float outerRadius = size / 2f - 1f;
            float innerRadius = outerRadius - ringWidth;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > outerRadius + 1f || dist < innerRadius - 1f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > outerRadius)
                    {
                        float alpha = 1f - (dist - outerRadius);
                        texture.SetPixel(x, y, new Color(ringColor.r, ringColor.g, ringColor.b, ringColor.a * alpha));
                    }
                    else if (dist < innerRadius)
                    {
                        float alpha = dist - (innerRadius - 1f);
                        texture.SetPixel(x, y, new Color(ringColor.r, ringColor.g, ringColor.b, ringColor.a * alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, ringColor);
                    }
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        /// <summary>
        /// Creates a circular ring sprite.
        /// </summary>
        public static Sprite CreateCircleRingSprite(int size, Color ringColor, int ringWidth = 3)
        {
            var texture = CreateCircleRingTexture(size, ringColor, ringWidth);
            return CreateSprite(texture);
        }

        // === Rounded Rectangle Textures ===

        /// <summary>
        /// Creates a rounded rectangle texture with anti-aliased corners.
        /// 9-slice compatible for scaling at any size.
        /// </summary>
        /// <param name="size">Texture size (square)</param>
        /// <param name="fillColor">Interior color</param>
        /// <param name="borderColor">Border color</param>
        /// <param name="cornerRadius">Corner radius in pixels</param>
        /// <param name="borderWidth">Border thickness in pixels</param>
        public static Texture2D CreateRoundedRectTexture(int size, Color fillColor, Color borderColor, int cornerRadius = 5, int borderWidth = 1)
        {
            // Ensure size is large enough for corners
            int minSize = cornerRadius * 2 + borderWidth * 2 + 2;
            size = Mathf.Max(size, minSize);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color pixel = GetRoundedRectPixel(x, y, size, size, fillColor, borderColor, cornerRadius, borderWidth);
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        /// <summary>
        /// Gets the color for a pixel in a rounded rectangle.
        /// </summary>
        private static Color GetRoundedRectPixel(int x, int y, int width, int height, Color fill, Color border, int radius, int borderWidth)
        {
            // Check if we're in a corner region
            bool inCorner = false;
            float cornerDist = 0f;
            float cornerX = 0f, cornerY = 0f;

            // Top-left corner
            if (x < radius && y < radius)
            {
                cornerX = radius;
                cornerY = radius;
                inCorner = true;
            }
            // Top-right corner
            else if (x >= width - radius && y < radius)
            {
                cornerX = width - radius - 1;
                cornerY = radius;
                inCorner = true;
            }
            // Bottom-left corner
            else if (x < radius && y >= height - radius)
            {
                cornerX = radius;
                cornerY = height - radius - 1;
                inCorner = true;
            }
            // Bottom-right corner
            else if (x >= width - radius && y >= height - radius)
            {
                cornerX = width - radius - 1;
                cornerY = height - radius - 1;
                inCorner = true;
            }

            if (inCorner)
            {
                float dx = x - cornerX;
                float dy = y - cornerY;
                cornerDist = Mathf.Sqrt(dx * dx + dy * dy);

                // Outside the rounded corner - transparent
                if (cornerDist > radius + 0.5f)
                {
                    return Color.clear;
                }
                // Anti-aliased outer edge
                else if (cornerDist > radius - 0.5f)
                {
                    float alpha = 1f - (cornerDist - (radius - 0.5f));
                    return new Color(border.r, border.g, border.b, border.a * alpha);
                }
                // Border region
                else if (cornerDist > radius - borderWidth - 0.5f)
                {
                    return border;
                }
                // Anti-aliased inner edge (border to fill)
                else if (cornerDist > radius - borderWidth - 1.5f)
                {
                    float t = (radius - borderWidth - 0.5f) - cornerDist;
                    return Color.Lerp(border, fill, t);
                }
                // Fill
                else
                {
                    return fill;
                }
            }
            else
            {
                // Non-corner regions - standard border logic
                bool isBorder = x < borderWidth || x >= width - borderWidth ||
                                y < borderWidth || y >= height - borderWidth;
                return isBorder ? border : fill;
            }
        }

        /// <summary>
        /// Creates a 9-sliced sprite from a rounded rect texture.
        /// </summary>
        public static Sprite CreateRoundedSprite(Texture2D texture, int cornerRadius)
        {
            // Border for 9-slice should be cornerRadius to preserve corners
            int border = cornerRadius + 1;
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
        /// Creates a rounded panel sprite with glass effect styling.
        /// </summary>
        /// <param name="size">Texture size</param>
        /// <param name="tint">Window tint preset</param>
        public static Sprite CreateGlassPanelSprite(int size = 32, WindowTint tint = WindowTint.Default)
        {
            int cornerRadius = (int)VeneerTheme.CornerRadius;
            Color fillColor = VeneerTheme.GetWindowTint(tint);
            Color borderColor = VeneerColors.Border;

            var texture = CreateRoundedRectTexture(size, fillColor, borderColor, cornerRadius, 1);
            return CreateRoundedSprite(texture, cornerRadius);
        }

        /// <summary>
        /// Creates a rounded button sprite.
        /// </summary>
        public static Sprite CreateRoundedButtonSprite(int size = 16)
        {
            int cornerRadius = (int)VeneerTheme.CornerRadiusSmall;
            var texture = CreateRoundedRectTexture(size, VeneerColors.ButtonNormal, VeneerColors.Border, cornerRadius, 1);
            return CreateRoundedSprite(texture, cornerRadius);
        }

        /// <summary>
        /// Creates a rounded slot sprite.
        /// </summary>
        public static Sprite CreateRoundedSlotSprite(int size = 16)
        {
            int cornerRadius = (int)VeneerTheme.CornerRadiusSmall;
            var texture = CreateRoundedRectTexture(size, VeneerColors.SlotEmpty, VeneerColors.Border, cornerRadius, 1);
            return CreateRoundedSprite(texture, cornerRadius);
        }

        /// <summary>
        /// Creates a rounded bar background sprite.
        /// </summary>
        public static Sprite CreateRoundedBarSprite(int size = 16, Color fillColor = default, Color borderColor = default)
        {
            if (fillColor == default) fillColor = VeneerColors.BackgroundDark;
            if (borderColor == default) borderColor = VeneerColors.Border;

            int cornerRadius = (int)VeneerTheme.CornerRadiusSmall;
            var texture = CreateRoundedRectTexture(size, fillColor, borderColor, cornerRadius, 1);
            return CreateRoundedSprite(texture, cornerRadius);
        }

        // === Frost/Glass Textures ===

        /// <summary>
        /// Creates a frosted glass overlay texture with visible noise pattern.
        /// </summary>
        /// <param name="size">Texture size (square)</param>
        /// <param name="tintColor">Base tint color</param>
        /// <param name="intensity">Noise intensity (0-1), default 0.35</param>
        public static Texture2D CreateFrostTexture(int size, Color tintColor, float intensity = 0.35f)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            // Use multi-layer noise for more organic frost effect
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Generate multi-octave noise for organic frost pattern
                    float noise1 = SimpleNoise(x, y, size);
                    float noise2 = SimpleNoise(x * 2, y * 2, size * 2) * 0.5f;
                    float noise3 = SimpleNoise(x * 4, y * 4, size * 4) * 0.25f;
                    float combinedNoise = (noise1 + noise2 + noise3) / 1.75f;

                    // Apply intensity with more variation
                    float noiseValue = combinedNoise * intensity;

                    // Create visible variation in the frost
                    // Brighter spots simulate light scattering in frosted glass
                    Color pixel = new Color(
                        tintColor.r + noiseValue * 0.4f,
                        tintColor.g + noiseValue * 0.4f,
                        tintColor.b + noiseValue * 0.35f,
                        tintColor.a * (0.8f + noiseValue * 0.4f)  // Vary alpha too for depth
                    );

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Repeat; // For tiling
            return texture;
        }

        /// <summary>
        /// Simple noise function for frost effect.
        /// </summary>
        private static float SimpleNoise(int x, int y, int size)
        {
            // Simple pseudo-random based on position (deterministic)
            int hash = (x * 374761393 + y * 668265263 + size * 1013904223) ^ (x * y);
            hash = (hash ^ (hash >> 13)) * 1274126177;
            hash = hash ^ (hash >> 16);

            // Normalize to 0-1
            float value = (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;

            // Apply smoothing by averaging with neighbors conceptually
            // This creates a more organic look
            int hash2 = ((x + 1) * 374761393 + y * 668265263) ^ ((x + 1) * y);
            hash2 = (hash2 ^ (hash2 >> 13)) * 1274126177;
            float value2 = (hash2 & 0x7FFFFFFF) / (float)0x7FFFFFFF;

            int hash3 = (x * 374761393 + (y + 1) * 668265263) ^ (x * (y + 1));
            hash3 = (hash3 ^ (hash3 >> 13)) * 1274126177;
            float value3 = (hash3 & 0x7FFFFFFF) / (float)0x7FFFFFFF;

            return (value + value2 + value3) / 3f;
        }

        /// <summary>
        /// Creates a frost overlay sprite for a specific window tint.
        /// </summary>
        public static Sprite CreateFrostSprite(int size, WindowTint tint)
        {
            Color frostColor = VeneerTheme.GetFrostColor(tint);
            var texture = CreateFrostTexture(size, frostColor, VeneerTheme.GlassFrostIntensity);
            return CreateSprite(texture);
        }

        /// <summary>
        /// Creates a combined glass panel with integrated frost effect.
        /// This creates a single texture with the frost pattern baked in.
        /// </summary>
        public static Sprite CreateGlassPanelWithFrostSprite(int size = 64, WindowTint tint = WindowTint.Default)
        {
            int cornerRadius = (int)VeneerTheme.CornerRadius;
            Color baseColor = VeneerTheme.GetWindowTint(tint);
            Color frostColor = VeneerTheme.GetFrostColor(tint);
            Color borderColor = VeneerColors.Border;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Get base rounded rect color
                    Color pixel = GetRoundedRectPixel(x, y, size, size, baseColor, borderColor, cornerRadius, 1);

                    // If it's a fill pixel (not border, not transparent), add frost
                    if (pixel.a > 0.5f && pixel != borderColor)
                    {
                        float noise = SimpleNoise(x, y, size) * VeneerTheme.GlassFrostIntensity;
                        pixel = new Color(
                            pixel.r + frostColor.r * noise,
                            pixel.g + frostColor.g * noise,
                            pixel.b + frostColor.b * noise,
                            pixel.a
                        );
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return CreateRoundedSprite(texture, cornerRadius);
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
