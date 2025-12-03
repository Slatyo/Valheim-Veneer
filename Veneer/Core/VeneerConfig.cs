using BepInEx.Configuration;
using UnityEngine;

namespace Veneer.Core
{
    /// <summary>
    /// BepInEx configuration for Veneer.
    /// Manages all user-configurable settings.
    /// </summary>
    public static class VeneerConfig
    {
        // General
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> ShowMoverTooltips { get; private set; }
        public static ConfigEntry<float> GridSnapSize { get; private set; }
        public static ConfigEntry<bool> ShowGridLines { get; private set; }
        public static ConfigEntry<KeyboardShortcut> EditModeKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> CursorModeKey { get; private set; }

        // Theme
        public static ConfigEntry<float> BackgroundOpacity { get; private set; }
        public static ConfigEntry<string> AccentColorHex { get; private set; }
        public static ConfigEntry<BorderStyle> BorderStyleSetting { get; private set; }
        public static ConfigEntry<float> FontScale { get; private set; }

        // HUD Replacement
        public static ConfigEntry<bool> ReplaceHealthBar { get; private set; }
        public static ConfigEntry<bool> ReplaceStaminaBar { get; private set; }
        public static ConfigEntry<bool> ReplaceFoodSlots { get; private set; }
        public static ConfigEntry<bool> ReplaceStatusEffects { get; private set; }
        public static ConfigEntry<bool> ReplaceMinimap { get; private set; }
        public static ConfigEntry<bool> ReplaceCompass { get; private set; }
        public static ConfigEntry<bool> ReplaceBossHealth { get; private set; }
        public static ConfigEntry<bool> ReplaceHotbar { get; private set; }
        public static ConfigEntry<bool> ReplaceMap { get; private set; }

        // Inventory Replacement
        public static ConfigEntry<bool> ReplaceInventory { get; private set; }
        public static ConfigEntry<bool> ReplaceContainer { get; private set; }
        public static ConfigEntry<bool> ReplaceCrafting { get; private set; }
        public static ConfigEntry<float> SlotSize { get; private set; }

        // Minimap
        public static ConfigEntry<float> MinimapSize { get; private set; }
        public static ConfigEntry<float> MinimapZoom { get; private set; }
        public static ConfigEntry<bool> MinimapShowCoordinates { get; private set; }
        public static ConfigEntry<bool> MinimapShowBiome { get; private set; }

        // Advanced
        public static ConfigEntry<bool> UseVanillaTooltips { get; private set; }
        public static ConfigEntry<bool> DebugMode { get; private set; }

        private static ConfigFile _config;

        /// <summary>
        /// Initializes all configuration entries.
        /// </summary>
        public static void Initialize(ConfigFile config)
        {
            _config = config;

            // Disable auto-save during binding to prevent multiple writes
            config.SaveOnConfigSet = false;

            // General
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Enable Veneer UI replacement"
            );

            ShowMoverTooltips = config.Bind(
                "General",
                "ShowMoverTooltips",
                true,
                "Show element names when in edit mode"
            );

            GridSnapSize = config.Bind(
                "General",
                "GridSnapSize",
                5f,
                new ConfigDescription(
                    "Snap-to-grid size in pixels (0 = disabled)",
                    new AcceptableValueRange<float>(0f, 50f)
                )
            );

            ShowGridLines = config.Bind(
                "General",
                "ShowGridLines",
                true,
                "Show grid lines in edit mode"
            );

            EditModeKey = config.Bind(
                "General",
                "EditModeKey",
                new KeyboardShortcut(KeyCode.F8),
                "Keybind to toggle edit mode for repositioning UI elements"
            );

            CursorModeKey = config.Bind(
                "General",
                "CursorModeKey",
                new KeyboardShortcut(KeyCode.Q, KeyCode.LeftAlt),
                "Keybind to toggle cursor mode (Alt+Q) - enables/disables mouse cursor for UI interaction"
            );

            // Theme
            BackgroundOpacity = config.Bind(
                "Theme",
                "BackgroundOpacity",
                0.85f,
                new ConfigDescription(
                    "Background transparency (0-1)",
                    new AcceptableValueRange<float>(0.1f, 1f)
                )
            );

            AccentColorHex = config.Bind(
                "Theme",
                "AccentColor",
                "#c79c6e",
                "Primary accent color (hex format)"
            );

            BorderStyleSetting = config.Bind(
                "Theme",
                "BorderStyle",
                BorderStyle.Thin,
                "Border thickness style"
            );

            FontScale = config.Bind(
                "Theme",
                "FontScale",
                1f,
                new ConfigDescription(
                    "Global font size multiplier",
                    new AcceptableValueRange<float>(0.5f, 2f)
                )
            );

            // HUD Replacement
            ReplaceHealthBar = config.Bind(
                "HUD",
                "ReplaceHealthBar",
                true,
                "Replace vanilla health bar"
            );

            ReplaceStaminaBar = config.Bind(
                "HUD",
                "ReplaceStaminaBar",
                true,
                "Replace vanilla stamina bar"
            );

            ReplaceFoodSlots = config.Bind(
                "HUD",
                "ReplaceFoodSlots",
                true,
                "Replace vanilla food slots"
            );

            ReplaceStatusEffects = config.Bind(
                "HUD",
                "ReplaceStatusEffects",
                true,
                "Replace vanilla status effects display"
            );

            ReplaceMinimap = config.Bind(
                "HUD",
                "ReplaceMinimap",
                true,
                "Replace vanilla minimap"
            );

            ReplaceCompass = config.Bind(
                "HUD",
                "ReplaceCompass",
                true,
                "Replace vanilla compass"
            );

            ReplaceBossHealth = config.Bind(
                "HUD",
                "ReplaceBossHealth",
                true,
                "Replace vanilla boss health bar"
            );

            ReplaceHotbar = config.Bind(
                "HUD",
                "ReplaceHotbar",
                true,
                "Replace vanilla hotbar (slots 1-8)"
            );

            ReplaceMap = config.Bind(
                "HUD",
                "ReplaceMap",
                true,
                "Replace vanilla large map (M key)"
            );

            // Inventory Replacement
            ReplaceInventory = config.Bind(
                "Inventory",
                "ReplaceInventory",
                true,
                "Replace vanilla inventory panel"
            );

            ReplaceContainer = config.Bind(
                "Inventory",
                "ReplaceContainer",
                true,
                "Replace vanilla container (chest) UI"
            );

            ReplaceCrafting = config.Bind(
                "Inventory",
                "ReplaceCrafting",
                true,
                "Replace vanilla crafting panel"
            );

            SlotSize = config.Bind(
                "Inventory",
                "SlotSize",
                40f,
                new ConfigDescription(
                    "Item slot size in pixels",
                    new AcceptableValueRange<float>(32f, 64f)
                )
            );

            // Minimap
            MinimapSize = config.Bind(
                "Minimap",
                "Size",
                200f,
                new ConfigDescription(
                    "Minimap size in pixels",
                    new AcceptableValueRange<float>(100f, 400f)
                )
            );

            MinimapZoom = config.Bind(
                "Minimap",
                "Zoom",
                1f,
                new ConfigDescription(
                    "Default zoom level",
                    new AcceptableValueRange<float>(0.5f, 3f)
                )
            );

            MinimapShowCoordinates = config.Bind(
                "Minimap",
                "ShowCoordinates",
                true,
                "Show player coordinates on minimap"
            );

            MinimapShowBiome = config.Bind(
                "Minimap",
                "ShowBiome",
                true,
                "Show current biome on minimap"
            );

            // Advanced
            UseVanillaTooltips = config.Bind(
                "Advanced",
                "UseVanillaTooltips",
                false,
                "Use Valheim's tooltip style instead of Veneer tooltips"
            );

            DebugMode = config.Bind(
                "Advanced",
                "DebugMode",
                false,
                "Show debug information"
            );

            // Re-enable auto-save and save all
            config.SaveOnConfigSet = true;
            config.Save();
        }

        /// <summary>
        /// Gets the accent color from the hex config.
        /// </summary>
        public static Color GetAccentColor()
        {
            if (ColorUtility.TryParseHtmlString(AccentColorHex.Value, out var color))
            {
                return color;
            }
            return new Color(0.78f, 0.61f, 0.43f, 1f); // Default gold
        }

        /// <summary>
        /// Gets the scaled font size.
        /// </summary>
        public static int GetScaledFontSize(int baseSize)
        {
            return Mathf.RoundToInt(baseSize * FontScale.Value);
        }
    }

    /// <summary>
    /// Border style options.
    /// </summary>
    public enum BorderStyle
    {
        None,
        Thin,
        Thick
    }
}
