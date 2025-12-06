using System;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Extensions;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Quick access bar for opening windows and toggling settings.
    /// Permanently visible on the HUD layer (not tied to inventory).
    /// Acts as a button-only controller that uses VeneerWindowManager to toggle independent windows.
    /// Windows can be opened/closed independently - no mutual exclusivity is enforced.
    /// </summary>
    public class VeneerQuickBar : VeneerElement
    {
        private const string ElementIdQuickBar = "Veneer_QuickBar";

        private static VeneerQuickBar _instance;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static VeneerQuickBar Instance => _instance;

        private Image _backgroundImage;
        private Image _borderImage;
        private Transform _buttonContainer;

        // Quick access buttons
        private VeneerButton _inventoryTab;
        private VeneerButton _craftingTab;
        private VeneerButton _skillsTab;
        private VeneerButton _trophiesTab;
        private VeneerButton _compendiumTab;
        private VeneerButton _mapTab;
        private VeneerButton _pvpToggle;

        /// <summary>
        /// Event fired when a button is clicked.
        /// </summary>
        public event Action<string> OnButtonClicked;

        /// <summary>
        /// Creates the quick bar.
        /// </summary>
        public static VeneerQuickBar Create(Transform parent)
        {
            if (_instance != null)
            {
                Plugin.Log.LogWarning("VeneerQuickBar already exists, returning existing instance");
                return _instance;
            }

            var go = CreateUIObject("VeneerQuickBar", parent);
            var quickBar = go.AddComponent<VeneerQuickBar>();
            quickBar.Initialize();
            return quickBar;
        }

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            VeneerWindowManager.OnWindowOpened -= OnWindowStateChanged;
            VeneerWindowManager.OnWindowClosed -= OnWindowStateChanged;

            // Notify extensions
            VeneerExtensionRegistry.NotifyQuickBarDestroyed();

            if (_instance == this)
                _instance = null;
        }

        private void Initialize()
        {
            ElementId = ElementIdQuickBar;
            IsMoveable = true;
            SavePosition = true;
            // NOTE: We do NOT set LayerType here because we manually manage the canvas
            // Setting LayerType would cause ApplyLayer() in Start() to overwrite our sorting order
            // LayerType = VeneerLayerType.None is the default, which skips ApplyLayer()

            // Add Canvas with high sorting order (2500) to ensure clicks reach our buttons
            // This MUST be higher than vanilla Inventory_screen (sortingOrder=600)
            VeneerLayers.EnsureCanvas(gameObject, VeneerLayers.QuickBar, addRaycaster: true);

            // Register with anchor system - top right
            VeneerAnchor.Register(ElementId, ScreenAnchor.TopRight, new Vector2(-20, -20));

            // Button sizing constants
            float[] buttonWidths = { 70f, 70f, 55f, 70f, 90f, 45f, 50f };
            float buttonHeight = 28f;
            float spacing = 4f;
            float padding = 8f;

            // Add HorizontalLayoutGroup with padding to QuickBar for proper sizing
            var quickBarLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
            quickBarLayout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            quickBarLayout.childAlignment = TextAnchor.MiddleCenter;
            quickBarLayout.childControlWidth = true;
            quickBarLayout.childControlHeight = true;
            quickBarLayout.childForceExpandWidth = false;
            quickBarLayout.childForceExpandHeight = false;

            // Use ContentSizeFitter to auto-resize based on button content
            var sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AnchorTo(AnchorPreset.TopRight, new Vector2(-20, -20));

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.Background;

            // Border - ignored by layout, stretches to fill parent
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            // Ignore layout so border stretches over everything
            var borderLayout = borderGo.AddComponent<LayoutElement>();
            borderLayout.ignoreLayout = true;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Border, Color.clear, 1);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Content container with horizontal layout for buttons
            var content = CreateUIObject("Content", transform);
            _buttonContainer = content.transform;

            // Content needs LayoutElement and ContentSizeFitter so parent layout respects its size
            var contentLayoutElement = content.AddComponent<LayoutElement>();
            contentLayoutElement.minHeight = buttonHeight;
            contentLayoutElement.preferredHeight = buttonHeight;

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = spacing;

            // Create quick access buttons with proper widths
            int idx = 0;
            _inventoryTab = CreateButton(content.transform, "Inventory", buttonWidths[idx++], () => ToggleWindow("inventory"));
            _craftingTab = CreateButton(content.transform, "Crafting", buttonWidths[idx++], () => ToggleWindow("crafting"));
            _skillsTab = CreateButton(content.transform, "Skills", buttonWidths[idx++], () => ToggleWindow("skills"));
            _trophiesTab = CreateButton(content.transform, "Trophies", buttonWidths[idx++], () => ToggleWindow("trophies"));
            _compendiumTab = CreateButton(content.transform, "Compendium", buttonWidths[idx++], () => ToggleWindow("compendium"));
            _mapTab = CreateButton(content.transform, "Map", buttonWidths[idx++], () => ToggleWindow("map"));
            _pvpToggle = CreateButton(content.transform, "PvP", buttonWidths[idx++], () => TogglePvP());

            // Set initial button styles
            UpdateAllButtonStyles();

            // Add mover (no resizer - QuickBar auto-sizes based on buttons)
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }

            // Subscribe to window manager events to update button styles
            VeneerWindowManager.OnWindowOpened += OnWindowStateChanged;
            VeneerWindowManager.OnWindowClosed += OnWindowStateChanged;

            // Start visible - this is a permanent HUD element
            gameObject.SetActive(true);

            // Debug: Log full canvas hierarchy info
            var ownCanvas = GetComponent<Canvas>();
            var ownRaycaster = GetComponent<GraphicRaycaster>();
            var parentCanvas = GetComponentInParent<Canvas>();
            var rootCanvas = parentCanvas?.rootCanvas;

            Plugin.Log.LogInfo($"VeneerQuickBar: Canvas debug info:");
            Plugin.Log.LogInfo($"  - Own Canvas: {(ownCanvas != null ? $"sortingOrder={ownCanvas.sortingOrder}, overrideSorting={ownCanvas.overrideSorting}" : "null")}");
            Plugin.Log.LogInfo($"  - Own GraphicRaycaster: {(ownRaycaster != null ? $"enabled={ownRaycaster.enabled}" : "null")}");
            Plugin.Log.LogInfo($"  - Parent Canvas: {parentCanvas?.name ?? "null"}");
            Plugin.Log.LogInfo($"  - Root Canvas: {rootCanvas?.name ?? "null"}, sortingOrder={rootCanvas?.sortingOrder}");
            Plugin.Log.LogInfo($"  - Parent transform: {transform.parent?.name ?? "null"}");

            // Log button info
            Plugin.Log.LogInfo($"  - Background Image raycastTarget: {_backgroundImage?.raycastTarget}");

            // Check EventSystem
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            Plugin.Log.LogInfo($"  - EventSystem: {eventSystem?.name ?? "null"}");

            // Notify extensions that QuickBar is ready
            VeneerExtensionRegistry.NotifyQuickBarCreated(new QuickBarContext
            {
                ButtonContainer = _buttonContainer,
                QuickBar = this
            });
        }

        private void OnWindowStateChanged(VeneerElement window)
        {
            UpdateAllButtonStyles();
        }

        private VeneerButton CreateButton(Transform parent, string label, float width, Action onClick)
        {
            return CreateQuickBarButton(parent, label, width, onClick);
        }

        /// <summary>
        /// Creates a button styled for the QuickBar.
        /// Extensions can use this to add buttons that match the QuickBar's style.
        /// </summary>
        /// <param name="parent">Parent transform (use QuickBarContext.ButtonContainer)</param>
        /// <param name="label">Button label text</param>
        /// <param name="width">Button width in pixels</param>
        /// <param name="onClick">Click callback</param>
        /// <returns>The created button</returns>
        public static VeneerButton CreateQuickBarButton(Transform parent, string label, float width, Action onClick)
        {
            var button = VeneerButton.CreateTab(parent, label, () =>
            {
                Plugin.Log.LogInfo($"VeneerQuickBar: Button '{label}' clicked!");
                onClick?.Invoke();
            });
            button.SetButtonSize(ButtonSize.Small);

            var layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.minHeight = 28f;
            layoutElement.preferredHeight = 28f;

            Plugin.Log.LogDebug($"VeneerQuickBar: Created button '{label}' - raycastTarget={button.gameObject.GetComponent<UnityEngine.UI.Image>()?.raycastTarget}");

            // Force layout rebuild so QuickBar resizes to fit new button
            if (_instance != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_instance.RectTransform);
            }

            return button;
        }

        /// <summary>
        /// Shows the quick bar.
        /// </summary>
        public override void Show()
        {
            gameObject.SetActive(true);
            UpdatePvPButtonVisual();
            UpdateAllButtonStyles();
        }

        /// <summary>
        /// Hides the quick bar.
        /// Note: Generally should not be hidden as it's a permanent HUD element.
        /// </summary>
        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates all button styles based on current window visibility.
        /// </summary>
        private void UpdateAllButtonStyles()
        {
            _inventoryTab?.SetStyle(VeneerWindowManager.IsWindowVisible<VeneerInventoryPanel>() ? ButtonStyle.TabActive : ButtonStyle.Tab);
            _craftingTab?.SetStyle(VeneerWindowManager.IsWindowVisible<VeneerCraftingPanel>() ? ButtonStyle.TabActive : ButtonStyle.Tab);
            _skillsTab?.SetStyle(VeneerWindowManager.IsWindowVisible<VeneerSkillsPanel>() ? ButtonStyle.TabActive : ButtonStyle.Tab);
            _trophiesTab?.SetStyle(VeneerWindowManager.IsWindowVisible<VeneerTrophiesPanel>() ? ButtonStyle.TabActive : ButtonStyle.Tab);
            _compendiumTab?.SetStyle(VeneerWindowManager.IsWindowVisible<VeneerCompendiumPanel>() ? ButtonStyle.TabActive : ButtonStyle.Tab);
            _mapTab?.SetStyle(VeneerWindowManager.IsWindowVisible<VeneerLargeMapFrame>() ? ButtonStyle.TabActive : ButtonStyle.Tab);
            // PvP toggle uses its own style logic
        }

        /// <summary>
        /// Toggles a window using VeneerWindowManager.
        /// Windows can be independently opened - no mutual exclusivity.
        /// </summary>
        private void ToggleWindow(string windowName)
        {
            Plugin.Log.LogInfo($"VeneerQuickBar: ToggleWindow called for '{windowName}'");
            OnButtonClicked?.Invoke(windowName);

            switch (windowName)
            {
                case "inventory":
                    Plugin.Log.LogInfo($"VeneerQuickBar: Inventory registered={VeneerWindowManager.HasWindow<VeneerInventoryPanel>()}");
                    VeneerWindowManager.ToggleWindow<VeneerInventoryPanel>();
                    break;

                case "crafting":
                    Plugin.Log.LogInfo($"VeneerQuickBar: Crafting registered={VeneerWindowManager.HasWindow<VeneerCraftingPanel>()}");
                    VeneerWindowManager.ToggleWindow<VeneerCraftingPanel>();
                    break;

                case "skills":
                    Plugin.Log.LogInfo($"VeneerQuickBar: Skills registered={VeneerWindowManager.HasWindow<VeneerSkillsPanel>()}");
                    VeneerWindowManager.ToggleWindow<VeneerSkillsPanel>();
                    break;

                case "trophies":
                    Plugin.Log.LogInfo($"VeneerQuickBar: Trophies registered={VeneerWindowManager.HasWindow<VeneerTrophiesPanel>()}");
                    VeneerWindowManager.ToggleWindow<VeneerTrophiesPanel>();
                    break;

                case "compendium":
                    Plugin.Log.LogInfo($"VeneerQuickBar: Compendium registered={VeneerWindowManager.HasWindow<VeneerCompendiumPanel>()}");
                    VeneerWindowManager.ToggleWindow<VeneerCompendiumPanel>();
                    break;

                case "map":
                    // Map uses vanilla Minimap.SetMapMode which triggers our patches to create/show the frame
                    if (Minimap.instance != null)
                    {
                        var currentMode = Minimap.instance.m_mode;
                        Plugin.Log.LogInfo($"VeneerQuickBar: Map toggle - current mode={currentMode}");
                        if (currentMode == Minimap.MapMode.Large)
                        {
                            Minimap.instance.SetMapMode(Minimap.MapMode.Small);
                        }
                        else
                        {
                            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
                        }
                    }
                    break;
            }

            // Button styles will update via OnWindowStateChanged callback
        }

        #region PvP Toggle

        private bool _pvpState = false;
        private float _lastPvPToggleTime = 0f;
        private const float PVP_TOGGLE_COOLDOWN = 0.5f;

        private void TogglePvP()
        {
            // Prevent multiple rapid clicks
            if (Time.time - _lastPvPToggleTime < PVP_TOGGLE_COOLDOWN)
            {
                Plugin.Log.LogDebug("PvP toggle on cooldown");
                return;
            }
            _lastPvPToggleTime = Time.time;

            var player = Player.m_localPlayer;
            if (player != null)
            {
                // Get current state and toggle it
                bool currentState = player.IsPVPEnabled();
                bool newState = !currentState;

                Plugin.Log.LogInfo($"PvP: Current={currentState}, Attempting to set to {newState}");

                // Set the new state
                player.SetPVP(newState);

                // Read back the state to see if it actually changed
                bool actualState = player.IsPVPEnabled();
                _pvpState = actualState;

                Plugin.Log.LogInfo($"PvP: After SetPVP({newState}), actual state is now {actualState}");
                UpdatePvPButtonVisual();
            }
            else
            {
                Plugin.Log.LogWarning("PvP toggle: No local player found");
            }
        }

        private void UpdatePvPButtonVisual()
        {
            if (_pvpToggle == null) return;

            var player = Player.m_localPlayer;
            if (player != null)
            {
                // Sync our state with player state using proper accessor
                _pvpState = player.IsPVPEnabled();
            }

            // Update button appearance based on PvP state
            _pvpToggle.Label = _pvpState ? "PvP ON" : "PvP";

            // Change button style to indicate state
            // Use Danger (red) when ON, Tab style when OFF to match other buttons
            if (_pvpState)
            {
                _pvpToggle.SetStyle(ButtonStyle.Danger);
            }
            else
            {
                _pvpToggle.SetStyle(ButtonStyle.Tab);
            }
        }

        #endregion


        private void Update()
        {
            // Sync PvP state with player (in case changed elsewhere)
            var player = Player.m_localPlayer;
            if (player != null && player.IsPVPEnabled() != _pvpState)
            {
                _pvpState = player.IsPVPEnabled();
                UpdatePvPButtonVisual();
            }
        }

        /// <summary>
        /// Cleanup the quick bar.
        /// </summary>
        public static void Cleanup()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}
