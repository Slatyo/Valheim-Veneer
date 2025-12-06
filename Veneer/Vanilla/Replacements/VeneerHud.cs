using UnityEngine;
using Veneer.Components.Specialized;
using Veneer.Core;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Manages all Veneer HUD replacement elements.
    /// Coordinates creation, updates, and visibility of custom HUD components.
    /// </summary>
    public class VeneerHud : MonoBehaviour
    {
        private static VeneerHud _instance;
        public static VeneerHud Instance => _instance;

        // HUD Components
        private VeneerUnitFrame _playerFrame;
        private VeneerFoodBar _foodBar;
        private VeneerStatusBar _statusBar;
        private VeneerMinimapFrame _minimapFrame;
        private VeneerBossFrame _bossFrame;
        private VeneerBossGroup _bossGroup;
        private VeneerHotbar _hotbar;

        // References to vanilla HUD for hiding
        private Hud _vanillaHud;
        private bool _initialized;

        /// <summary>
        /// Player unit frame component.
        /// </summary>
        public VeneerUnitFrame PlayerFrame => _playerFrame;

        /// <summary>
        /// Food bar component.
        /// </summary>
        public VeneerFoodBar FoodBar => _foodBar;

        /// <summary>
        /// Status effects bar component.
        /// </summary>
        public VeneerStatusBar StatusBar => _statusBar;

        /// <summary>
        /// Minimap frame component.
        /// </summary>
        public VeneerMinimapFrame MinimapFrame => _minimapFrame;

        /// <summary>
        /// Boss health frame component.
        /// </summary>
        public VeneerBossFrame BossFrame => _bossFrame;

        /// <summary>
        /// Hotbar component.
        /// </summary>
        public VeneerHotbar Hotbar => _hotbar;

        /// <summary>
        /// Initializes the Veneer HUD system.
        /// </summary>
        public static void Initialize(Transform parent, Hud vanillaHud)
        {
            if (_instance != null) return;

            var go = new GameObject("VeneerHud");

            // Add RectTransform and stretch to fill parent
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _instance = go.AddComponent<VeneerHud>();
            _instance._vanillaHud = vanillaHud;
            _instance.CreateHudElements();
        }

        private void CreateHudElements()
        {
            if (_initialized) return;

            Plugin.Log.LogInfo("Creating Veneer HUD elements");

            // Create player frame
            if (VeneerConfig.ReplaceHealthBar.Value || VeneerConfig.ReplaceStaminaBar.Value)
            {
                _playerFrame = VeneerUnitFrame.CreatePlayerFrame(transform);
                Plugin.Log.LogDebug("Created VeneerUnitFrame");
            }

            // Create food bar
            if (VeneerConfig.ReplaceFoodSlots.Value)
            {
                _foodBar = VeneerFoodBar.Create(transform);
                Plugin.Log.LogDebug("Created VeneerFoodBar");
            }

            // Create status bar
            if (VeneerConfig.ReplaceStatusEffects.Value)
            {
                _statusBar = VeneerStatusBar.Create(transform);
                Plugin.Log.LogDebug("Created VeneerStatusBar");
            }

            // Create minimap frame
            if (VeneerConfig.ReplaceMinimap.Value)
            {
                _minimapFrame = VeneerMinimapFrame.Create(transform);
                Plugin.Log.LogDebug("Created VeneerMinimapFrame");
            }

            // Create boss frame and boss group
            if (VeneerConfig.ReplaceBossHealth.Value)
            {
                _bossFrame = VeneerBossFrame.Create(transform);
                Plugin.Log.LogDebug("Created VeneerBossFrame");

                _bossGroup = VeneerBossGroup.Create(transform);
                Plugin.Log.LogDebug("Created VeneerBossGroup");
            }

            // Create hotbar only if enabled (both config AND API must allow it)
            // If HotbarEnabled is false, another mod is providing its own hotbar replacement
            if (VeneerConfig.ReplaceHotbar.Value && Core.VeneerAPI.HotbarEnabled)
            {
                _hotbar = VeneerHotbar.Create(transform);
                Plugin.Log.LogDebug("Created VeneerHotbar");
            }
            else if (!Core.VeneerAPI.HotbarEnabled)
            {
                Plugin.Log.LogInfo("VeneerHotbar NOT created - another mod is providing hotbar replacement");
            }

            _initialized = true;
            Plugin.Log.LogInfo("Veneer HUD elements created");
        }

        /// <summary>
        /// Hides vanilla HUD elements that are being replaced.
        /// </summary>
        public void HideVanillaElements()
        {
            if (_vanillaHud == null) return;

            // Health panel
            if (VeneerConfig.ReplaceHealthBar.Value && _vanillaHud.m_healthPanel != null)
            {
                _vanillaHud.m_healthPanel.gameObject.SetActive(false);
            }

            // Hide the entire hotkey bar (slots 1-8)
            if (VeneerConfig.ReplaceHotbar.Value)
            {
                var hotkeyBar = _vanillaHud.transform.Find("hudroot/HotKeyBar");
                if (hotkeyBar != null)
                {
                    hotkeyBar.gameObject.SetActive(false);
                }
            }

            // Food bar root - use reflection-safe approach to find food UI
            if (VeneerConfig.ReplaceFoodSlots.Value)
            {
                var foodBar = _vanillaHud.transform.Find("hudroot/FoodBar");
                if (foodBar != null)
                {
                    foodBar.gameObject.SetActive(false);
                }
            }

            // Status effects
            if (VeneerConfig.ReplaceStatusEffects.Value)
            {
                var statusRoot = _vanillaHud.transform.Find("hudroot/StatusEffects");
                if (statusRoot != null)
                {
                    statusRoot.gameObject.SetActive(false);
                }
            }

            // Guardian power icon
            var guardianPower = _vanillaHud.transform.Find("hudroot/GuardianPower");
            if (guardianPower != null)
            {
                guardianPower.gameObject.SetActive(false);
            }

            // Stagger bar
            var staggerBar = _vanillaHud.transform.Find("hudroot/staggerbar");
            if (staggerBar != null)
            {
                staggerBar.gameObject.SetActive(false);
            }

            // Crosshair - keep this visible
            // var crosshair = _vanillaHud.transform.Find("hudroot/crosshair");

            Plugin.Log.LogDebug("Vanilla HUD elements hidden");
        }

        /// <summary>
        /// Shows vanilla HUD elements (for when Veneer is disabled).
        /// </summary>
        public void ShowVanillaElements()
        {
            if (_vanillaHud == null) return;

            if (_vanillaHud.m_healthPanel != null)
                _vanillaHud.m_healthPanel.gameObject.SetActive(true);

            // Restore hotkey bar
            if (VeneerConfig.ReplaceHotbar.Value)
            {
                var hotkeyBar = _vanillaHud.transform.Find("hudroot/HotKeyBar");
                if (hotkeyBar != null)
                {
                    hotkeyBar.gameObject.SetActive(true);
                }
            }

            // Restore food elements
            var foodBar = _vanillaHud.transform.Find("hudroot/FoodBar");
            if (foodBar != null)
            {
                foodBar.gameObject.SetActive(true);
            }

            // Restore status effects
            var statusRoot = _vanillaHud.transform.Find("hudroot/StatusEffects");
            if (statusRoot != null)
            {
                statusRoot.gameObject.SetActive(true);
            }

            // Restore guardian power
            var guardianPower = _vanillaHud.transform.Find("hudroot/GuardianPower");
            if (guardianPower != null)
            {
                guardianPower.gameObject.SetActive(true);
            }

            // Restore stagger bar
            var staggerBar = _vanillaHud.transform.Find("hudroot/staggerbar");
            if (staggerBar != null)
            {
                staggerBar.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Adds a boss to track (will show closest in main frame, others in group).
        /// </summary>
        public void AddBoss(Character boss)
        {
            _bossFrame?.AddBoss(boss);
        }

        /// <summary>
        /// Removes a boss from tracking.
        /// </summary>
        public void RemoveBoss(Character boss)
        {
            _bossFrame?.RemoveBoss(boss);
        }

        /// <summary>
        /// Sets a boss to track (legacy - calls AddBoss).
        /// </summary>
        public void SetBoss(Character boss)
        {
            _bossFrame?.AddBoss(boss);
        }

        /// <summary>
        /// Clears the tracked boss.
        /// </summary>
        public void ClearBoss()
        {
            _bossFrame?.ClearBoss();
        }

        /// <summary>
        /// Clears all tracked bosses.
        /// </summary>
        public void ClearAllBosses()
        {
            _bossFrame?.ClearAllBosses();
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public static void Cleanup()
        {
            VeneerBossGroup.Cleanup();

            if (_instance != null)
            {
                _instance.ShowVanillaElements();
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
