using System;
using UnityEngine;
using UnityEngine.UI;
using Jotunn.Managers;
using Veneer.Components.Base;
using Veneer.Components.Composite;
using Veneer.Components.Primitives;
using Veneer.Components.Specialized;
using Veneer.Extensions;
using Veneer.Grid;
using Veneer.Theme;
using Veneer.Vanilla.Patches;

namespace Veneer.Core
{
    /// <summary>
    /// Public API for other mods to create Veneer UI elements.
    /// </summary>
    public static class VeneerAPI
    {
        private static Transform _uiRoot;
        private static bool _initialized;
        private static GameObject _editModeOverlay;
        private static VeneerEditModePanel _editModePanel;

        /// <summary>
        /// Whether Veneer is initialized and ready.
        /// </summary>
        public static bool IsReady => _initialized;

        /// <summary>
        /// Event fired when Veneer is ready to use.
        /// </summary>
        public static event Action OnReady;

        /// <summary>
        /// Initializes the Veneer API.
        /// Called internally by the plugin.
        /// </summary>
        internal static void Initialize()
        {
            // Use Jotunn's custom GUI front as the root
            var newUiRoot = GUIManager.CustomGUIFront?.transform;

            if (newUiRoot == null)
            {
                Plugin.Log.LogError("Veneer: Failed to get UI root from GUIManager");
                return;
            }

            // Check if UI root changed (scene transition) - need to reinitialize
            bool uiRootChanged = _uiRoot != newUiRoot;
            if (_initialized && !uiRootChanged)
            {
                Plugin.Log.LogDebug("Veneer: Already initialized with same UI root, skipping");
                return;
            }

            if (uiRootChanged && _initialized)
            {
                Plugin.Log.LogInfo("Veneer: UI root changed, reinitializing...");
                // Clean up old instances that were destroyed with the old UI root
                // The GameObjects are gone, just reset the flags
                _initialized = false;
            }

            _uiRoot = newUiRoot;

            // Log scaling info - Valheim uses GuiScaler which sets Canvas.scaleFactor
            // based on the GUI Scale setting in options
            var canvas = _uiRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Plugin.Log.LogInfo($"[Veneer] Screen: {Screen.width}x{Screen.height}, Canvas.scaleFactor={canvas.scaleFactor}");

                // The scaleFactor is what Valheim's GuiScaler sets based on the in-game GUI Scale slider
                // If scaleFactor > 1, UI is scaled up (e.g., on 4K screens)
                // Our pixel values should automatically scale with this
            }

            // Initialize subsystems
            VeneerLayout.Initialize();
            VeneerTooltip.Initialize(_uiRoot);
            VeneerSplitDialog.Initialize(_uiRoot);
            VeneerFloatingText.Initialize();

            // Subscribe to edit mode changes for overlay (only once)
            if (!_initialized)
            {
                VeneerMover.OnEditModeChanged += OnEditModeChanged;
            }

            _initialized = true;
            Plugin.Log.LogInfo("Veneer API initialized");

            OnReady?.Invoke();
        }

        private static void OnEditModeChanged(bool enabled)
        {
            if (enabled)
            {
                ShowEditModeOverlay();
                ShowEditModePanel();
                // Unlock cursor for dragging
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Block player input
                GUIManager.BlockInput(true);
            }
            else
            {
                HideEditModeOverlay();
                HideEditModePanel();
                // Restore cursor state
                GUIManager.BlockInput(false);
            }
        }

        private static void ShowEditModePanel()
        {
            // Re-acquire UI root if it was destroyed (scene change)
            EnsureUIRoot();

            Plugin.Log.LogDebug($"ShowEditModePanel: _editModePanel={(_editModePanel != null ? "exists" : "null")}, _uiRoot={(_uiRoot != null ? "exists" : "null")}");

            // Always try to create if null or destroyed
            if (_editModePanel == null || _editModePanel.gameObject == null)
            {
                if (_uiRoot == null)
                {
                    Plugin.Log.LogError("VeneerAPI: Cannot create edit mode panel - _uiRoot is null even after re-acquire attempt");
                    return;
                }
                _editModePanel = VeneerEditModePanel.Create(_uiRoot);
            }

            if (_editModePanel != null)
            {
                _editModePanel.Show();
                Plugin.Log.LogInfo($"VeneerAPI: Edit mode panel shown, active={_editModePanel.gameObject.activeSelf}");
            }
            else
            {
                Plugin.Log.LogWarning("VeneerAPI: Edit mode panel is null after Create, cannot show");
            }
        }

        /// <summary>
        /// Ensures _uiRoot is valid, re-acquiring from GUIManager if destroyed.
        /// </summary>
        private static void EnsureUIRoot()
        {
            // Unity destroyed objects pass != null but fail when accessed
            // Use implicit bool conversion which properly checks for destroyed objects
            if (_uiRoot == null || !_uiRoot)
            {
                _uiRoot = GUIManager.CustomGUIFront?.transform;
                if (_uiRoot != null)
                {
                    Plugin.Log.LogDebug("VeneerAPI: Re-acquired _uiRoot from GUIManager");
                }
            }
        }

        private static void HideEditModePanel()
        {
            // Reset edit mode visibility state before hiding
            _editModePanel?.ResetEditModeState();
            _editModePanel?.Hide();
        }

        private static void ShowEditModeOverlay()
        {
            if (_editModeOverlay != null) return;

            // Re-acquire UI root if it was destroyed
            EnsureUIRoot();
            if (_uiRoot == null) return;

            _editModeOverlay = new GameObject("VeneerEditModeOverlay", typeof(RectTransform));
            _editModeOverlay.transform.SetParent(_uiRoot, false);
            _editModeOverlay.transform.SetAsLastSibling(); // On top so grid is visible

            var rect = _editModeOverlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Add CanvasGroup to allow clicks to pass through
            var canvasGroup = _editModeOverlay.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false; // Let clicks pass through to mover overlays
            canvasGroup.interactable = false;

            // Grid lines - drawn but don't block raycasts (optional)
            if (VeneerConfig.ShowGridLines.Value)
            {
                var grid = _editModeOverlay.AddComponent<VeneerEditModeGrid>();
                grid.GridSize = VeneerConfig.GridSnapSize.Value;
                grid.GridColor = new Color(1, 1, 1, 0.2f); // 20% opacity for subtlety
                grid.raycastTarget = false; // Don't block clicks
            }

            // Info text
            var infoGo = new GameObject("Info", typeof(RectTransform));
            infoGo.transform.SetParent(_editModeOverlay.transform, false);
            var infoRect = infoGo.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 1);
            infoRect.anchorMax = new Vector2(0.5f, 1);
            infoRect.pivot = new Vector2(0.5f, 1);
            infoRect.anchoredPosition = new Vector2(0, -20);
            infoRect.sizeDelta = new Vector2(400, 40);

            var infoText = infoGo.AddComponent<UnityEngine.UI.Text>();
            infoText.text = "EDIT MODE - Drag elements to reposition. Press F8 to exit.";
            infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            infoText.fontSize = 16;
            infoText.color = VeneerColors.TextGold;
            infoText.alignment = TextAnchor.MiddleCenter;
            infoText.raycastTarget = false;

            var outline = infoGo.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
        }

        private static void HideEditModeOverlay()
        {
            if (_editModeOverlay != null)
            {
                UnityEngine.Object.Destroy(_editModeOverlay);
                _editModeOverlay = null;
            }
        }

        /// <summary>
        /// Cleans up the Veneer API.
        /// </summary>
        internal static void Cleanup()
        {
            VeneerMover.OnEditModeChanged -= OnEditModeChanged;
            HideEditModeOverlay();
            VeneerTooltip.Cleanup();
            VeneerSplitDialog.Cleanup();
            VeneerFloatingText.Cleanup();
            VeneerTextures.Cleanup();
            VeneerAnchor.Clear();
            VeneerWindowManager.Clear();
            InventoryPatches.Cleanup();
            MapPatches.Cleanup();
            _initialized = false;
        }

        #region Window Creation

        /// <summary>
        /// Creates a new window (frame with header, close button, draggable).
        /// </summary>
        public static VeneerFrame CreateWindow(string id, string title, float width = 400, float height = 300)
        {
            EnsureInitialized();
            return VeneerFrame.Create(_uiRoot, new FrameConfig
            {
                Id = id,
                Title = title,
                Width = width,
                Height = height,
                HasHeader = true,
                HasCloseButton = true,
                IsDraggable = true,
                SavePosition = true
            });
        }

        #endregion

        #region Frame/Panel Creation

        /// <summary>
        /// Creates a styled frame container.
        /// </summary>
        public static VeneerFrame CreateFrame(FrameConfig config)
        {
            EnsureInitialized();
            return VeneerFrame.Create(_uiRoot, config);
        }

        /// <summary>
        /// Creates a styled frame container.
        /// </summary>
        public static VeneerFrame CreateFrame(string id, float width, float height, AnchorPreset anchor, Vector2 offset = default)
        {
            return CreateFrame(new FrameConfig
            {
                Id = id,
                Width = width,
                Height = height,
                Anchor = anchor,
                Offset = offset,
                SavePosition = true
            });
        }

        /// <summary>
        /// Creates a panel with background and border.
        /// </summary>
        public static VeneerPanel CreatePanel(Transform parent, float width, float height)
        {
            return VeneerPanel.Create(parent ?? _uiRoot, "Panel", width, height);
        }

        /// <summary>
        /// Creates a panel that stretches to fill its parent.
        /// </summary>
        public static VeneerPanel CreateStretchedPanel(Transform parent, string name = "Panel")
        {
            return VeneerPanel.CreateStretched(parent ?? _uiRoot, name);
        }

        /// <summary>
        /// Creates a panel with a custom border color.
        /// </summary>
        public static VeneerPanel CreatePanelWithBorder(Transform parent, Color borderColor, float width = 100, float height = 100)
        {
            return VeneerPanel.CreateWithBorder(parent ?? _uiRoot, borderColor, "Panel", width, height);
        }

        /// <summary>
        /// Creates a panel with a thick border (e.g., for boss frames).
        /// </summary>
        public static VeneerPanel CreatePanelWithThickBorder(Transform parent, Color borderColor, int borderWidth = 2, float width = 100, float height = 100)
        {
            return VeneerPanel.CreateWithThickBorder(parent ?? _uiRoot, borderColor, borderWidth, "Panel", width, height);
        }

        /// <summary>
        /// Creates a panel with the legendary/boss gold border.
        /// </summary>
        public static VeneerPanel CreateLegendaryPanel(Transform parent, float width = 100, float height = 100)
        {
            var panel = VeneerPanel.CreateWithThickBorder(parent ?? _uiRoot, VeneerColors.Legendary, 2, "LegendaryPanel", width, height);
            return panel;
        }

        #endregion

        #region Inventory/Slot Components

        /// <summary>
        /// Creates an item slot for displaying inventory items.
        /// </summary>
        /// <param name="parent">Parent transform.</param>
        /// <param name="size">Slot size in pixels (default: 40).</param>
        /// <returns>A new VeneerItemSlot.</returns>
        public static VeneerItemSlot CreateItemSlot(Transform parent, float size = 40f)
        {
            return VeneerItemSlot.Create(parent ?? _uiRoot, size);
        }

        /// <summary>
        /// Creates an item grid for displaying multiple inventory items.
        /// </summary>
        /// <param name="parent">Parent transform.</param>
        /// <param name="width">Number of columns.</param>
        /// <param name="height">Number of rows.</param>
        /// <param name="slotSize">Size of each slot in pixels (default: 40).</param>
        /// <returns>A new VeneerItemGrid.</returns>
        public static VeneerItemGrid CreateItemGrid(Transform parent, int width, int height, float slotSize = 40f)
        {
            return VeneerItemGrid.Create(parent ?? _uiRoot, width, height, slotSize);
        }

        /// <summary>
        /// Creates an item grid sized for a player inventory (8x4 = 32 slots).
        /// </summary>
        /// <param name="parent">Parent transform.</param>
        /// <param name="slotSize">Size of each slot in pixels (default: 40).</param>
        /// <returns>A new VeneerItemGrid configured for player inventory.</returns>
        public static VeneerItemGrid CreatePlayerInventoryGrid(Transform parent, float slotSize = 40f)
        {
            return VeneerItemGrid.Create(parent ?? _uiRoot, 8, 4, slotSize);
        }

        /// <summary>
        /// Creates an item grid sized for a hotbar (8x1 = 8 slots).
        /// </summary>
        /// <param name="parent">Parent transform.</param>
        /// <param name="slotSize">Size of each slot in pixels (default: 40).</param>
        /// <returns>A new VeneerItemGrid configured for hotbar.</returns>
        public static VeneerItemGrid CreateHotbarGrid(Transform parent, float slotSize = 40f)
        {
            return VeneerItemGrid.Create(parent ?? _uiRoot, 8, 1, slotSize);
        }

        #endregion

        #region HUD Elements

        /// <summary>
        /// Creates a health bar styled element.
        /// </summary>
        public static VeneerBar CreateHealthBar(string id, float width = 200)
        {
            EnsureInitialized();
            var bar = VeneerBar.CreateHealthBar(_uiRoot, id);
            bar.SetSize(width, VeneerDimensions.BarHeightLarge);
            bar.ElementId = id;
            return bar;
        }

        /// <summary>
        /// Creates a stamina bar styled element.
        /// </summary>
        public static VeneerBar CreateStaminaBar(string id, float width = 200)
        {
            EnsureInitialized();
            var bar = VeneerBar.CreateStaminaBar(_uiRoot, id);
            bar.SetSize(width, VeneerDimensions.BarHeightLarge);
            bar.ElementId = id;
            return bar;
        }

        /// <summary>
        /// Creates a progress/cast bar.
        /// </summary>
        public static VeneerBar CreateProgressBar(string id, float width = 250, float height = 20)
        {
            EnsureInitialized();
            var bar = VeneerBar.CreateCastBar(_uiRoot, id);
            bar.SetSize(width, height);
            bar.ElementId = id;
            return bar;
        }

        /// <summary>
        /// Creates a custom bar.
        /// </summary>
        public static VeneerBar CreateBar(string id, Color fillColor, float width = 200, float height = 20)
        {
            EnsureInitialized();
            var bar = VeneerBar.Create(_uiRoot, id, width, height);
            bar.FillColor = fillColor;
            bar.ElementId = id;
            return bar;
        }

        #endregion

        #region Text/Button

        /// <summary>
        /// Creates a text element.
        /// </summary>
        public static VeneerText CreateText(Transform parent, string content, TextStyle style = TextStyle.Body)
        {
            var text = VeneerText.Create(parent ?? _uiRoot, content);
            text.ApplyStyle(style);
            return text;
        }

        /// <summary>
        /// Creates a button.
        /// </summary>
        public static VeneerButton CreateButton(Transform parent, string label, Action onClick)
        {
            return VeneerButton.Create(parent ?? _uiRoot, label, onClick);
        }

        /// <summary>
        /// Creates a primary styled button.
        /// </summary>
        public static VeneerButton CreatePrimaryButton(Transform parent, string label, Action onClick)
        {
            return VeneerButton.CreatePrimary(parent ?? _uiRoot, label, onClick);
        }

        #endregion

        #region Tooltip

        /// <summary>
        /// Shows a simple tooltip.
        /// </summary>
        public static void ShowTooltip(string text)
        {
            VeneerTooltip.Show(text);
        }

        /// <summary>
        /// Shows a tooltip with title and body.
        /// </summary>
        public static void ShowTooltip(string title, string body)
        {
            VeneerTooltip.Show(title, body);
        }

        /// <summary>
        /// Shows a tooltip with full configuration.
        /// </summary>
        public static void ShowTooltip(TooltipData data)
        {
            VeneerTooltip.Show(data);
        }

        /// <summary>
        /// Shows a tooltip for an item, allowing registered providers to modify it.
        /// </summary>
        public static void ShowItemTooltip(ItemDrop.ItemData item, TooltipData baseTooltip)
        {
            VeneerTooltip.ShowForItem(item, baseTooltip);
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public static void HideTooltip()
        {
            VeneerTooltip.Hide();
        }

        /// <summary>
        /// Registers a tooltip provider that can modify item tooltips.
        /// Providers are called in order of priority (lowest first).
        /// </summary>
        public static void RegisterTooltipProvider(IItemTooltipProvider provider)
        {
            VeneerTooltip.RegisterProvider(provider);
        }

        /// <summary>
        /// Unregisters a tooltip provider.
        /// </summary>
        public static void UnregisterTooltipProvider(IItemTooltipProvider provider)
        {
            VeneerTooltip.UnregisterProvider(provider);
        }

        /// <summary>
        /// Registers a visual provider that can modify item slot appearance (border color, etc).
        /// Providers are called in order of priority (lowest first).
        /// </summary>
        public static void RegisterSlotVisualProvider(IItemSlotVisualProvider provider)
        {
            VeneerItemSlot.RegisterVisualProvider(provider);
        }

        /// <summary>
        /// Unregisters a slot visual provider.
        /// </summary>
        public static void UnregisterSlotVisualProvider(IItemSlotVisualProvider provider)
        {
            VeneerItemSlot.UnregisterVisualProvider(provider);
        }

        #endregion

        #region Grid System

        /// <summary>
        /// Enters edit mode for repositioning elements.
        /// </summary>
        public static void EnterEditMode()
        {
            VeneerMover.EnterEditMode();
        }

        /// <summary>
        /// Exits edit mode and saves layout.
        /// </summary>
        public static void ExitEditMode()
        {
            VeneerMover.ExitEditMode();
        }

        /// <summary>
        /// Toggles edit mode.
        /// </summary>
        public static void ToggleEditMode()
        {
            VeneerMover.ToggleEditMode();
        }

        /// <summary>
        /// Whether edit mode is enabled.
        /// </summary>
        public static bool IsEditModeEnabled => VeneerMover.EditModeEnabled;

        /// <summary>
        /// Resets all element positions to defaults.
        /// </summary>
        public static void ResetAllPositions()
        {
            VeneerLayout.ResetAll();
        }

        /// <summary>
        /// Saves the current layout.
        /// </summary>
        public static void SaveLayout()
        {
            VeneerLayout.Save();
        }

        /// <summary>
        /// Registers an element with the anchor system.
        /// </summary>
        public static void RegisterElement(string elementId, ScreenAnchor defaultAnchor, Vector2 defaultOffset)
        {
            VeneerAnchor.Register(elementId, defaultAnchor, defaultOffset);
        }

        #endregion

        #region Theme

        /// <summary>
        /// Gets a rarity color.
        /// </summary>
        public static Color GetRarityColor(int tier)
        {
            return VeneerColors.GetRarityColor(tier);
        }

        /// <summary>
        /// Gets a color with modified alpha.
        /// </summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            return VeneerColors.WithAlpha(color, alpha);
        }

        /// <summary>
        /// Current accent color from config.
        /// </summary>
        public static Color AccentColor => VeneerConfig.GetAccentColor();

        #endregion

        #region Layer System

        /// <summary>
        /// Sets the layer type on a game object.
        /// </summary>
        public static void SetLayer(GameObject go, int sortingOrder, bool addRaycaster = true)
        {
            VeneerLayers.SetLayer(go, sortingOrder, addRaycaster);
        }

        /// <summary>
        /// Gets the sorting order of a game object.
        /// </summary>
        public static int GetLayer(GameObject go)
        {
            return VeneerLayers.GetLayer(go);
        }

        /// <summary>
        /// Gets the base sorting order for a layer type.
        /// </summary>
        public static int GetLayerValue(VeneerLayerType layerType)
        {
            return VeneerLayers.GetLayerValue(layerType);
        }

        #endregion

        #region Window Manager

        /// <summary>
        /// Gets a registered window by type.
        /// </summary>
        public static T GetWindow<T>() where T : VeneerElement
        {
            return VeneerWindowManager.GetWindow<T>();
        }

        /// <summary>
        /// Shows a window by type.
        /// </summary>
        public static void ShowWindow<T>() where T : VeneerElement
        {
            VeneerWindowManager.ShowWindow<T>();
        }

        /// <summary>
        /// Hides a window by type.
        /// </summary>
        public static void HideWindow<T>() where T : VeneerElement
        {
            VeneerWindowManager.HideWindow<T>();
        }

        /// <summary>
        /// Toggles a window by type.
        /// </summary>
        public static void ToggleWindow<T>() where T : VeneerElement
        {
            VeneerWindowManager.ToggleWindow<T>();
        }

        /// <summary>
        /// Checks if a window type is currently visible.
        /// </summary>
        public static bool IsWindowVisible<T>() where T : VeneerElement
        {
            return VeneerWindowManager.IsWindowVisible<T>();
        }

        /// <summary>
        /// Focuses a window, bringing it to the front.
        /// </summary>
        public static void FocusWindow(VeneerElement window)
        {
            VeneerWindowManager.FocusWindow(window);
        }

        /// <summary>
        /// Closes all visible windows.
        /// </summary>
        public static void CloseAllWindows()
        {
            VeneerWindowManager.CloseAllWindows();
        }

        /// <summary>
        /// Gets the currently focused window.
        /// </summary>
        public static VeneerElement FocusedWindow => VeneerWindowManager.FocusedWindow;

        #endregion

        #region Utility

        /// <summary>
        /// Creates a UI game object as a child of the specified parent.
        /// </summary>
        public static GameObject CreateUIObject(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent ?? _uiRoot, false);
            return go;
        }

        /// <summary>
        /// Gets the main UI root transform.
        /// </summary>
        public static Transform UIRoot
        {
            get
            {
                EnsureInitialized();
                EnsureUIRoot(); // Re-acquire if destroyed
                return _uiRoot;
            }
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Veneer is not initialized. Wait for VeneerAPI.OnReady or check VeneerAPI.IsReady.");
            }
        }

        #endregion

        #region Textures/Sprites

        /// <summary>
        /// Creates a panel background sprite.
        /// </summary>
        public static Sprite CreatePanelSprite()
        {
            return VeneerTextures.CreatePanelSprite();
        }

        /// <summary>
        /// Creates a button background sprite.
        /// </summary>
        public static Sprite CreateButtonSprite()
        {
            return VeneerTextures.CreateButtonSprite();
        }

        /// <summary>
        /// Creates a slot background sprite.
        /// </summary>
        public static Sprite CreateSlotSprite()
        {
            return VeneerTextures.CreateSlotSprite();
        }

        /// <summary>
        /// Creates a sliced border sprite with custom color and thickness.
        /// </summary>
        /// <param name="borderColor">The border color.</param>
        /// <param name="borderWidth">Border thickness in pixels (default: 1).</param>
        /// <returns>A 9-sliced border sprite.</returns>
        public static Sprite CreateBorderSprite(Color borderColor, int borderWidth = 1)
        {
            var texture = VeneerTextures.CreateSlicedBorderTexture(16, borderColor, Color.clear, borderWidth);
            return VeneerTextures.CreateSlicedSprite(texture, borderWidth);
        }

        #endregion

        #region Colors Reference

        /// <summary>
        /// Standard background color.
        /// </summary>
        public static Color BackgroundColor => VeneerColors.Background;

        /// <summary>
        /// Standard border color.
        /// </summary>
        public static Color BorderColor => VeneerColors.Border;

        /// <summary>
        /// Legendary/boss gold color.
        /// </summary>
        public static Color LegendaryColor => VeneerColors.Legendary;

        /// <summary>
        /// Health bar color.
        /// </summary>
        public static Color HealthColor => VeneerColors.Health;

        /// <summary>
        /// Stamina bar color.
        /// </summary>
        public static Color StaminaColor => VeneerColors.Stamina;

        /// <summary>
        /// Eitr/magic bar color.
        /// </summary>
        public static Color EitrColor => VeneerColors.Eitr;

        /// <summary>
        /// Error/danger color.
        /// </summary>
        public static Color ErrorColor => VeneerColors.Error;

        /// <summary>
        /// Success/positive color.
        /// </summary>
        public static Color SuccessColor => VeneerColors.Success;

        /// <summary>
        /// Warning color.
        /// </summary>
        public static Color WarningColor => VeneerColors.Warning;

        #endregion

        #region Dimensions Reference

        /// <summary>
        /// Standard padding value.
        /// </summary>
        public static float Padding => VeneerDimensions.Padding;

        /// <summary>
        /// Standard spacing value.
        /// </summary>
        public static float Spacing => VeneerDimensions.Spacing;

        /// <summary>
        /// Standard slot size.
        /// </summary>
        public static float SlotSize => VeneerDimensions.SlotSize;

        /// <summary>
        /// Gets a font size scaled by the user's font scale setting.
        /// </summary>
        public static int GetScaledFontSize(int baseSize)
        {
            return VeneerConfig.GetScaledFontSize(baseSize);
        }

        #endregion

        #region Extensions

        /// <summary>
        /// Registers a QuickBar extension.
        /// Extensions can add buttons to the QuickBar.
        /// </summary>
        public static void RegisterQuickBarExtension(IQuickBarExtension extension)
        {
            VeneerExtensionRegistry.RegisterQuickBarExtension(extension);
        }

        /// <summary>
        /// Unregisters a QuickBar extension.
        /// </summary>
        public static void UnregisterQuickBarExtension(IQuickBarExtension extension)
        {
            VeneerExtensionRegistry.UnregisterQuickBarExtension(extension);
        }

        /// <summary>
        /// Registers an Inventory extension.
        /// Extensions can add UI elements to the inventory panel.
        /// </summary>
        public static void RegisterInventoryExtension(IInventoryExtension extension)
        {
            VeneerExtensionRegistry.RegisterInventoryExtension(extension);
        }

        /// <summary>
        /// Unregisters an Inventory extension.
        /// </summary>
        public static void UnregisterInventoryExtension(IInventoryExtension extension)
        {
            VeneerExtensionRegistry.UnregisterInventoryExtension(extension);
        }

        /// <summary>
        /// Registers a Window extension.
        /// Extensions can hook into window lifecycle events.
        /// </summary>
        public static void RegisterWindowExtension(IWindowExtension extension)
        {
            VeneerExtensionRegistry.RegisterWindowExtension(extension);
        }

        /// <summary>
        /// Unregisters a Window extension.
        /// </summary>
        public static void UnregisterWindowExtension(IWindowExtension extension)
        {
            VeneerExtensionRegistry.UnregisterWindowExtension(extension);
        }

        /// <summary>
        /// Registers a Hotbar extension.
        /// Extensions can add elements next to the hotbar.
        /// </summary>
        public static void RegisterHotbarExtension(IHotbarExtension extension)
        {
            VeneerExtensionRegistry.RegisterHotbarExtension(extension);
        }

        /// <summary>
        /// Unregisters a Hotbar extension.
        /// </summary>
        public static void UnregisterHotbarExtension(IHotbarExtension extension)
        {
            VeneerExtensionRegistry.UnregisterHotbarExtension(extension);
        }

        /// <summary>
        /// Registers a HUD extension.
        /// Extensions can add custom HUD elements.
        /// </summary>
        public static void RegisterHudExtension(IHudExtension extension)
        {
            VeneerExtensionRegistry.RegisterHudExtension(extension);
        }

        /// <summary>
        /// Unregisters a HUD extension.
        /// </summary>
        public static void UnregisterHudExtension(IHudExtension extension)
        {
            VeneerExtensionRegistry.UnregisterHudExtension(extension);
        }

        #endregion

        #region Floating Text

        /// <summary>
        /// Shows floating damage text at a world position.
        /// </summary>
        /// <param name="damage">Damage amount</param>
        /// <param name="worldPosition">World position to display at</param>
        /// <param name="isCritical">Is this a critical hit?</param>
        /// <param name="damageType">Optional damage type for coloring (fire, frost, lightning, poison)</param>
        /// <param name="isDamageTaken">True if this is damage the local player received (shows in red)</param>
        public static void ShowDamageText(float damage, Vector3 worldPosition, bool isCritical = false, string damageType = null, bool isDamageTaken = false)
        {
            VeneerFloatingText.ShowDamage(damage, worldPosition, isCritical, damageType, isDamageTaken);
        }

        /// <summary>
        /// Shows floating healing text at a world position.
        /// </summary>
        public static void ShowHealText(float amount, Vector3 worldPosition)
        {
            VeneerFloatingText.ShowHeal(amount, worldPosition);
        }

        /// <summary>
        /// Shows floating XP gain text at a world position.
        /// </summary>
        public static void ShowXPText(long amount, Vector3 worldPosition)
        {
            VeneerFloatingText.ShowXP(amount, worldPosition);
        }

        /// <summary>
        /// Shows "Miss" text at a world position.
        /// </summary>
        public static void ShowMissText(Vector3 worldPosition)
        {
            VeneerFloatingText.ShowMiss(worldPosition);
        }

        /// <summary>
        /// Shows "Blocked" text at a world position.
        /// </summary>
        public static void ShowBlockedText(float amount, Vector3 worldPosition)
        {
            VeneerFloatingText.ShowBlocked(amount, worldPosition);
        }

        /// <summary>
        /// Shows custom floating text at a world position.
        /// </summary>
        public static void ShowFloatingText(string text, Vector3 worldPosition, VeneerFloatingText.TextStyle style = VeneerFloatingText.TextStyle.Normal, float duration = 1.5f)
        {
            VeneerFloatingText.Show(text, worldPosition, style, duration);
        }

        #endregion
    }
}
