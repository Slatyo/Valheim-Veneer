using UnityEngine;
using Veneer.Components.Specialized;
using Veneer.Core;
using Veneer.Extensions;

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

        // Layout containers for HUD extensions
        private Transform _topLeftContainer;
        private Transform _topRightContainer;
        private Transform _bottomLeftContainer;
        private Transform _bottomRightContainer;
        private Transform _centerContainer;

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

            // Create layout containers for HUD extensions
            CreateLayoutContainers();

            // Notify HUD extensions that HUD is ready
            NotifyExtensions();
        }

        private void CreateLayoutContainers()
        {
            // Create containers for different screen regions
            // Extensions can parent their elements to these containers

            // Bottom-left container (where player frame typically goes)
            var bottomLeft = new GameObject("BottomLeftContainer");
            var blRect = bottomLeft.AddComponent<RectTransform>();
            blRect.SetParent(transform, false);
            blRect.anchorMin = Vector2.zero;
            blRect.anchorMax = new Vector2(0.3f, 0.3f);
            blRect.offsetMin = Vector2.zero;
            blRect.offsetMax = Vector2.zero;
            _bottomLeftContainer = blRect;

            // Top-left container
            var topLeft = new GameObject("TopLeftContainer");
            var tlRect = topLeft.AddComponent<RectTransform>();
            tlRect.SetParent(transform, false);
            tlRect.anchorMin = new Vector2(0, 0.7f);
            tlRect.anchorMax = new Vector2(0.3f, 1f);
            tlRect.offsetMin = Vector2.zero;
            tlRect.offsetMax = Vector2.zero;
            _topLeftContainer = tlRect;

            // Top-right container
            var topRight = new GameObject("TopRightContainer");
            var trRect = topRight.AddComponent<RectTransform>();
            trRect.SetParent(transform, false);
            trRect.anchorMin = new Vector2(0.7f, 0.7f);
            trRect.anchorMax = Vector2.one;
            trRect.offsetMin = Vector2.zero;
            trRect.offsetMax = Vector2.zero;
            _topRightContainer = trRect;

            // Bottom-right container
            var bottomRight = new GameObject("BottomRightContainer");
            var brRect = bottomRight.AddComponent<RectTransform>();
            brRect.SetParent(transform, false);
            brRect.anchorMin = new Vector2(0.7f, 0);
            brRect.anchorMax = new Vector2(1f, 0.3f);
            brRect.offsetMin = Vector2.zero;
            brRect.offsetMax = Vector2.zero;
            _bottomRightContainer = brRect;

            // Center container
            var center = new GameObject("CenterContainer");
            var cRect = center.AddComponent<RectTransform>();
            cRect.SetParent(transform, false);
            cRect.anchorMin = new Vector2(0.3f, 0.3f);
            cRect.anchorMax = new Vector2(0.7f, 0.7f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;
            _centerContainer = cRect;

            Plugin.Log.LogDebug("Created HUD layout containers");
        }

        private void NotifyExtensions()
        {
            var context = new HudContext
            {
                HudRoot = transform,
                TopLeftContainer = _topLeftContainer,
                TopRightContainer = _topRightContainer,
                BottomLeftContainer = _bottomLeftContainer,
                BottomRightContainer = _bottomRightContainer,
                CenterContainer = _centerContainer
            };

            VeneerExtensionRegistry.NotifyHudCreated(context);
            Plugin.Log.LogInfo($"Notified {VeneerExtensionRegistry.GetHudExtensions().Count} HUD extensions");
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

            // Notify extensions before destroying
            VeneerExtensionRegistry.NotifyHudDestroyed();

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
