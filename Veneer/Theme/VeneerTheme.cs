using UnityEngine;

namespace Veneer.Theme
{
    /// <summary>
    /// Window tint presets for the glass effect system.
    /// Each window type gets a curated color tint.
    /// </summary>
    public enum WindowTint
    {
        /// <summary>Neutral dark - default for most windows.</summary>
        Default,
        /// <summary>Warm amber - inventory and container windows.</summary>
        Inventory,
        /// <summary>Cool blue - skills and character windows.</summary>
        Skills,
        /// <summary>Earth green - crafting windows.</summary>
        Crafting,
        /// <summary>Deep navy - map windows.</summary>
        Map,
        /// <summary>Bronze - container/chest windows.</summary>
        Container,
        /// <summary>Subtle gray - tooltips.</summary>
        Tooltip,
        /// <summary>Dark red - boss frames.</summary>
        Boss,
        /// <summary>Neutral - dialog windows.</summary>
        Dialog
    }

    /// <summary>
    /// Animation types for show/hide transitions.
    /// </summary>
    public enum AnimationType
    {
        /// <summary>No animation.</summary>
        None,
        /// <summary>Fade alpha in/out.</summary>
        Fade,
        /// <summary>Scale in/out.</summary>
        Scale,
        /// <summary>Combined fade and scale.</summary>
        FadeScale,
        /// <summary>Slide from direction.</summary>
        Slide,
        /// <summary>Quick scale overshoot (pop effect).</summary>
        Pop
    }

    /// <summary>
    /// Central theme configuration for Veneer UI.
    /// Provides glass tints, rounded corners, and animation settings.
    /// </summary>
    public static class VeneerTheme
    {
        // === Corner Radius ===

        /// <summary>Default corner radius (5px).</summary>
        public const float CornerRadius = 5f;

        /// <summary>Smaller corner radius for compact elements (4px).</summary>
        public const float CornerRadiusSmall = 4f;

        /// <summary>Larger corner radius for bigger panels (6px).</summary>
        public const float CornerRadiusLarge = 6f;

        // === Animation Durations ===

        /// <summary>Duration for window show animation.</summary>
        public const float WindowShowDuration = 0.2f;

        /// <summary>Duration for window hide animation.</summary>
        public const float WindowHideDuration = 0.15f;

        /// <summary>Duration for button hover transition.</summary>
        public const float ButtonHoverDuration = 0.1f;

        /// <summary>Duration for slot hover transition.</summary>
        public const float SlotHoverDuration = 0.08f;

        /// <summary>Duration for tab selection transition.</summary>
        public const float TabTransitionDuration = 0.15f;

        /// <summary>Duration for card hover transition.</summary>
        public const float CardHoverDuration = 0.12f;

        /// <summary>Duration for tooltip fade.</summary>
        public const float TooltipFadeDuration = 0.1f;

        // === Glass Effect Settings ===

        /// <summary>Base opacity for glass panels.</summary>
        public const float GlassOpacity = 0.94f;

        /// <summary>Intensity of the frost noise overlay (0-1).</summary>
        public const float GlassFrostIntensity = 0.35f;

        /// <summary>Border glow intensity on hover.</summary>
        public const float BorderGlowIntensity = 0.4f;

        /// <summary>Highlight strip opacity at top edge of glass panels.</summary>
        public const float GlassHighlightOpacity = 0.12f;

        /// <summary>Height of the highlight strip in pixels.</summary>
        public const float GlassHighlightHeight = 2f;

        // === Animation Easing ===

        /// <summary>Default animation speed multiplier.</summary>
        public const float DefaultEaseSpeed = 12f;

        // === Window Tint Colors ===
        // Visible color shifts that maintain dark glass feel while being distinctive

        private static readonly Color TintDefault = new Color(0.06f, 0.06f, 0.08f, GlassOpacity);
        private static readonly Color TintInventory = new Color(0.10f, 0.07f, 0.04f, GlassOpacity);    // Warm amber
        private static readonly Color TintSkills = new Color(0.04f, 0.06f, 0.12f, GlassOpacity);       // Cool blue
        private static readonly Color TintCrafting = new Color(0.05f, 0.10f, 0.06f, GlassOpacity);     // Earth green
        private static readonly Color TintMap = new Color(0.04f, 0.05f, 0.10f, GlassOpacity);          // Deep navy
        private static readonly Color TintContainer = new Color(0.10f, 0.06f, 0.04f, GlassOpacity);    // Bronze
        private static readonly Color TintTooltip = new Color(0.05f, 0.05f, 0.06f, 0.96f);             // Subtle gray
        private static readonly Color TintBoss = new Color(0.12f, 0.04f, 0.04f, GlassOpacity);         // Dark red
        private static readonly Color TintDialog = new Color(0.06f, 0.06f, 0.08f, GlassOpacity);       // Neutral

        // === Frost Overlay Colors ===
        // Visible frost tints that give glass panels texture and depth

        private static readonly Color FrostDefault = new Color(0.20f, 0.20f, 0.25f, 0.25f);
        private static readonly Color FrostInventory = new Color(0.30f, 0.22f, 0.12f, 0.28f);     // Warm amber frost
        private static readonly Color FrostSkills = new Color(0.12f, 0.18f, 0.30f, 0.28f);        // Cool blue frost
        private static readonly Color FrostCrafting = new Color(0.15f, 0.28f, 0.15f, 0.26f);      // Green frost
        private static readonly Color FrostMap = new Color(0.12f, 0.15f, 0.28f, 0.26f);           // Navy frost
        private static readonly Color FrostContainer = new Color(0.28f, 0.18f, 0.10f, 0.28f);     // Bronze frost
        private static readonly Color FrostTooltip = new Color(0.15f, 0.15f, 0.18f, 0.18f);       // Subtle frost
        private static readonly Color FrostBoss = new Color(0.30f, 0.12f, 0.12f, 0.28f);          // Red frost
        private static readonly Color FrostDialog = new Color(0.18f, 0.18f, 0.22f, 0.22f);        // Neutral frost

        // === Border Glow Colors ===
        // Accent colors for hover glow effects

        private static readonly Color GlowDefault = new Color(0.78f, 0.61f, 0.43f, 0.3f);     // Gold
        private static readonly Color GlowInventory = new Color(0.78f, 0.61f, 0.43f, 0.4f);   // Amber gold
        private static readonly Color GlowSkills = new Color(0.43f, 0.61f, 0.78f, 0.4f);      // Blue
        private static readonly Color GlowCrafting = new Color(0.43f, 0.78f, 0.61f, 0.4f);    // Green
        private static readonly Color GlowMap = new Color(0.43f, 0.55f, 0.78f, 0.4f);         // Navy blue
        private static readonly Color GlowContainer = new Color(0.78f, 0.55f, 0.43f, 0.4f);   // Bronze
        private static readonly Color GlowTooltip = new Color(0.53f, 0.53f, 0.53f, 0.3f);     // Gray
        private static readonly Color GlowBoss = new Color(0.78f, 0.43f, 0.43f, 0.4f);        // Red
        private static readonly Color GlowDialog = new Color(0.78f, 0.61f, 0.43f, 0.3f);      // Gold

        /// <summary>
        /// Gets the base tint color for a specific window type.
        /// </summary>
        /// <param name="tint">The window tint preset.</param>
        /// <returns>The tint color.</returns>
        public static Color GetWindowTint(WindowTint tint)
        {
            return tint switch
            {
                WindowTint.Inventory => TintInventory,
                WindowTint.Skills => TintSkills,
                WindowTint.Crafting => TintCrafting,
                WindowTint.Map => TintMap,
                WindowTint.Container => TintContainer,
                WindowTint.Tooltip => TintTooltip,
                WindowTint.Boss => TintBoss,
                WindowTint.Dialog => TintDialog,
                _ => TintDefault
            };
        }

        /// <summary>
        /// Gets the frosted glass overlay color for a window type.
        /// </summary>
        /// <param name="tint">The window tint preset.</param>
        /// <returns>The frost overlay color.</returns>
        public static Color GetFrostColor(WindowTint tint)
        {
            return tint switch
            {
                WindowTint.Inventory => FrostInventory,
                WindowTint.Skills => FrostSkills,
                WindowTint.Crafting => FrostCrafting,
                WindowTint.Map => FrostMap,
                WindowTint.Container => FrostContainer,
                WindowTint.Tooltip => FrostTooltip,
                WindowTint.Boss => FrostBoss,
                WindowTint.Dialog => FrostDialog,
                _ => FrostDefault
            };
        }

        /// <summary>
        /// Gets the border glow color for a window type.
        /// </summary>
        /// <param name="tint">The window tint preset.</param>
        /// <returns>The glow color.</returns>
        public static Color GetBorderGlow(WindowTint tint)
        {
            return tint switch
            {
                WindowTint.Inventory => GlowInventory,
                WindowTint.Skills => GlowSkills,
                WindowTint.Crafting => GlowCrafting,
                WindowTint.Map => GlowMap,
                WindowTint.Container => GlowContainer,
                WindowTint.Tooltip => GlowTooltip,
                WindowTint.Boss => GlowBoss,
                WindowTint.Dialog => GlowDialog,
                _ => GlowDefault
            };
        }

        /// <summary>
        /// Gets the accent color for interactive elements within a window type.
        /// </summary>
        /// <param name="tint">The window tint preset.</param>
        /// <returns>The accent color (opaque version of glow).</returns>
        public static Color GetAccentColor(WindowTint tint)
        {
            var glow = GetBorderGlow(tint);
            return new Color(glow.r, glow.g, glow.b, 1f);
        }

        /// <summary>
        /// Lerps between two colors with configurable speed.
        /// </summary>
        /// <param name="current">Current color.</param>
        /// <param name="target">Target color.</param>
        /// <param name="speed">Interpolation speed multiplier.</param>
        /// <returns>Interpolated color.</returns>
        public static Color LerpColor(Color current, Color target, float speed = DefaultEaseSpeed)
        {
            return Color.Lerp(current, target, Time.unscaledDeltaTime * speed);
        }
    }
}
