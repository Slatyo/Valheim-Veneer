using UnityEngine;

namespace Veneer.Theme
{
    /// <summary>
    /// Color palette for Veneer UI components.
    /// Dark backgrounds, subtle borders, muted gold accents.
    /// </summary>
    public static class VeneerColors
    {
        // Backgrounds - Nearly black with higher opacity for better visibility
        public static readonly Color Background = new Color(0.05f, 0.05f, 0.05f, 0.95f);       // #0d0d0d @ 95%
        public static readonly Color BackgroundSolid = new Color(0.05f, 0.05f, 0.05f, 1f);     // #0d0d0d @ 100%
        public static readonly Color BackgroundLight = new Color(0.12f, 0.12f, 0.12f, 0.95f);  // #1f1f1f @ 95%
        public static readonly Color BackgroundDark = new Color(0.02f, 0.02f, 0.02f, 0.98f);   // #050505 @ 98%

        // Borders - Subtle grays
        public static readonly Color Border = new Color(0.2f, 0.2f, 0.2f, 1f);                 // #333333
        public static readonly Color BorderLight = new Color(0.3f, 0.3f, 0.3f, 1f);            // #4d4d4d
        public static readonly Color BorderDark = new Color(0.12f, 0.12f, 0.12f, 1f);          // #1f1f1f
        public static readonly Color BorderHighlight = new Color(0.78f, 0.61f, 0.43f, 1f);     // #c79c6e (gold)

        // Text colors
        public static readonly Color Text = new Color(1f, 1f, 1f, 1f);                         // #ffffff
        public static readonly Color TextMuted = new Color(0.6f, 0.6f, 0.6f, 1f);              // #999999
        public static readonly Color TextDark = new Color(0.4f, 0.4f, 0.4f, 1f);               // #666666
        public static readonly Color TextGold = new Color(0.78f, 0.61f, 0.43f, 1f);            // #c79c6e

        // Accent colors - Muted gold
        public static readonly Color Accent = new Color(0.78f, 0.61f, 0.43f, 1f);              // #c79c6e
        public static readonly Color AccentHover = new Color(0.88f, 0.71f, 0.53f, 1f);         // Lighter gold
        public static readonly Color AccentPressed = new Color(0.68f, 0.51f, 0.33f, 1f);       // Darker gold
        public static readonly Color AccentDisabled = new Color(0.5f, 0.4f, 0.3f, 0.5f);       // Faded gold

        // Status bars - Valheim-appropriate colors
        public static readonly Color Health = new Color(0.77f, 0.12f, 0.23f, 1f);              // #c41f3b (red)
        public static readonly Color HealthBackground = new Color(0.3f, 0.05f, 0.09f, 1f);     // Dark red
        public static readonly Color Stamina = new Color(0.85f, 0.65f, 0f, 1f);                // #d9a600 (yellow)
        public static readonly Color StaminaBackground = new Color(0.3f, 0.23f, 0f, 1f);       // Dark yellow
        public static readonly Color Eitr = new Color(0.0f, 0.44f, 0.87f, 1f);                 // #0070de (blue)
        public static readonly Color EitrBackground = new Color(0f, 0.15f, 0.3f, 1f);          // Dark blue

        // Rarity colors
        public static readonly Color Poor = new Color(0.62f, 0.62f, 0.62f, 1f);                // #9d9d9d (gray)
        public static readonly Color Common = new Color(1f, 1f, 1f, 1f);                       // #ffffff (white)
        public static readonly Color Uncommon = new Color(0.12f, 1f, 0f, 1f);                  // #1eff00 (green)
        public static readonly Color Rare = new Color(0f, 0.44f, 0.87f, 1f);                   // #0070dd (blue)
        public static readonly Color Epic = new Color(0.64f, 0.21f, 0.93f, 1f);                // #a335ee (purple)
        public static readonly Color Legendary = new Color(1f, 0.5f, 0f, 1f);                  // #ff8000 (orange)
        public static readonly Color Artifact = new Color(0.9f, 0.8f, 0.5f, 1f);               // #e6cc80 (tan/heirloom)

        // Feedback colors
        public static readonly Color Success = new Color(0.0f, 0.8f, 0.0f, 1f);                // Green
        public static readonly Color Warning = new Color(1f, 0.8f, 0f, 1f);                    // Yellow
        public static readonly Color Error = new Color(1f, 0.2f, 0.2f, 1f);                    // Red
        public static readonly Color Info = new Color(0.4f, 0.6f, 1f, 1f);                     // Light blue

        // Button states
        public static readonly Color ButtonNormal = BackgroundLight;
        public static readonly Color ButtonHover = new Color(0.18f, 0.18f, 0.18f, 1f);
        public static readonly Color ButtonPressed = new Color(0.08f, 0.08f, 0.08f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        // Slot colors - more visible with stronger borders
        public static readonly Color SlotEmpty = new Color(0.06f, 0.06f, 0.06f, 0.95f);
        public static readonly Color SlotFilled = new Color(0.10f, 0.10f, 0.10f, 0.98f);
        public static readonly Color SlotHover = new Color(0.18f, 0.18f, 0.18f, 1f);
        public static readonly Color SlotSelected = new Color(0.25f, 0.25f, 0.25f, 1f);
        public static readonly Color SlotBorder = new Color(0.35f, 0.35f, 0.35f, 1f);           // More visible slot borders

        /// <summary>
        /// Gets the color for a given rarity tier (0-6).
        /// </summary>
        public static Color GetRarityColor(int tier)
        {
            return tier switch
            {
                0 => Poor,
                1 => Common,
                2 => Uncommon,
                3 => Rare,
                4 => Epic,
                5 => Legendary,
                6 => Artifact,
                _ => Common
            };
        }

        /// <summary>
        /// Applies alpha to a color.
        /// </summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Lightens a color by a factor (0-1).
        /// </summary>
        public static Color Lighten(Color color, float factor)
        {
            return new Color(
                Mathf.Min(1f, color.r + (1f - color.r) * factor),
                Mathf.Min(1f, color.g + (1f - color.g) * factor),
                Mathf.Min(1f, color.b + (1f - color.b) * factor),
                color.a
            );
        }

        /// <summary>
        /// Darkens a color by a factor (0-1).
        /// </summary>
        public static Color Darken(Color color, float factor)
        {
            return new Color(
                color.r * (1f - factor),
                color.g * (1f - factor),
                color.b * (1f - factor),
                color.a
            );
        }
    }
}
