using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Components.Specialized;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Complete inventory panel replacement using proper Veneer components.
    /// Uses VeneerFrame for both inventory and container panels.
    /// </summary>
    public class VeneerInventoryPanel : VeneerElement
    {
        private const string ElementIdInventory = "Veneer_Inventory";
        private const string ElementIdContainer = "Veneer_Container";

        // Main inventory frame
        private VeneerFrame _inventoryFrame;

        // UI References
        private VeneerText _weightText;
        private VeneerItemGrid _inventoryGrid;
        private VeneerItemGrid _quickSlotGrid;

        // Container frame (separate VeneerFrame)
        private VeneerFrame _containerFrame;
        private VeneerItemGrid _containerGrid;

        // State
        private Player _player;
        private Container _openContainer;
        private float _slotSize;

        // Layout constants
        private const int INV_COLS = 8;
        private const int INV_ROWS = 4;
        private const int QUICKSLOT_COLS = 8;
        private const float LABEL_HEIGHT = 18f;
        private const float WEIGHT_HEIGHT = 20f;

        /// <summary>
        /// Main inventory grid.
        /// </summary>
        public VeneerItemGrid InventoryGrid => _inventoryGrid;

        /// <summary>
        /// Container grid (when chest open).
        /// </summary>
        public VeneerItemGrid ContainerGrid => _containerGrid;

        /// <summary>
        /// Currently open container.
        /// </summary>
        public Container CurrentContainer => _openContainer;

        /// <summary>
        /// Creates the inventory panel.
        /// </summary>
        public static VeneerInventoryPanel Create(Transform parent)
        {
            var go = CreateUIObject("VeneerInventoryPanel", parent);
            var panel = go.AddComponent<VeneerInventoryPanel>();
            panel.Initialize(parent);
            return panel;
        }

        private void Initialize(Transform parent)
        {
            ElementId = ElementIdInventory;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.Window;
            AutoRegisterWithManager = true;

            // Calculate slot size (scaled up from base)
            _slotSize = VeneerConfig.SlotSize.Value * 1.3f;
            float padding = VeneerDimensions.Padding;
            float spacing = 8f;
            float innerSpacing = 4f;

            // Calculate section dimensions (bags only - equipment moved to Viking Character Window)
            float bagWidth = _slotSize * INV_COLS + innerSpacing * (INV_COLS - 1);
            float bagHeight = _slotSize * INV_ROWS + innerSpacing * (INV_ROWS - 1);

            float quickWidth = _slotSize * QUICKSLOT_COLS + innerSpacing * (QUICKSLOT_COLS - 1);
            float quickHeight = _slotSize;

            // Calculate total panel size (bags + hotbar + weight)
            float contentWidth = bagWidth;
            float totalWidth = contentWidth + padding * 2;
            float totalHeight = VeneerDimensions.WindowTitleHeight + LABEL_HEIGHT + innerSpacing + bagHeight + spacing + LABEL_HEIGHT + innerSpacing + quickHeight + spacing + WEIGHT_HEIGHT + padding * 2;

            // Create main inventory frame using VeneerFrame
            _inventoryFrame = VeneerFrame.Create(parent, new FrameConfig
            {
                Id = ElementIdInventory,
                Name = "InventoryFrame",
                Title = "Inventory",
                Width = totalWidth,
                Height = totalHeight,
                HasHeader = true,
                HasCloseButton = true,
                IsDraggable = true,
                SavePosition = true,
                Anchor = AnchorPreset.MiddleRight,
                Offset = new Vector2(-20, 0)
            });

            _inventoryFrame.OnCloseClicked += OnInventoryWindowClosed;

            // Build content inside the frame's content area
            BuildInventoryContent(_inventoryFrame.Content, contentWidth, bagWidth, bagHeight, quickWidth, quickHeight, spacing, innerSpacing);

            // Create container frame (hidden by default)
            CreateContainerFrame(parent);

            // Register anchor
            VeneerAnchor.Register(ElementIdInventory, ScreenAnchor.Right, new Vector2(-20, 0));

            // Start hidden
            RegisterWithManager();
            _inventoryFrame.Hide();
        }

        private void BuildInventoryContent(RectTransform content, float contentWidth, float bagWidth, float bagHeight, float quickWidth, float quickHeight, float spacing, float innerSpacing)
        {
            float yPos = 0;

            // Bag section (equipment moved to Viking Character Window)
            CreateSectionLabel(content, "BAG", 0, yPos, bagWidth);
            yPos -= LABEL_HEIGHT + innerSpacing;
            _inventoryGrid = CreateItemGrid(content, "InventoryGrid", 0, yPos, INV_COLS, INV_ROWS, innerSpacing);
            _inventoryGrid.HideEquippedItems = true; // Equipment shown in Character Window
            yPos -= bagHeight + spacing;

            // Quickslots section
            CreateSectionLabel(content, "HOTBAR", 0, yPos, quickWidth);
            yPos -= LABEL_HEIGHT + innerSpacing;
            _quickSlotGrid = CreateItemGrid(content, "QuickSlots", 0, yPos, QUICKSLOT_COLS, 1, innerSpacing);
            yPos -= quickHeight + spacing;

            // Weight display
            CreateWeightDisplay(content, yPos, contentWidth);
        }

        private void CreateSectionLabel(RectTransform parent, string text, float x, float y, float width)
        {
            var panel = VeneerPanel.Create(parent, $"Label_{text}", width, LABEL_HEIGHT);
            panel.BackgroundColor = VeneerColors.BackgroundDark;
            panel.ShowBorder = false;

            var panelRect = panel.RectTransform;
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(x, y);

            var labelText = VeneerText.Create(panel.transform, text);
            labelText.ApplyStyle(TextStyle.Caption);
            labelText.TextColor = VeneerColors.TextGold;
            labelText.Alignment = TextAnchor.MiddleCenter;
            labelText.Style = FontStyle.Bold;
            labelText.StretchToFill();
        }

        private VeneerItemGrid CreateItemGrid(RectTransform parent, string name, float x, float y, int cols, int rows, float spacing)
        {
            float gridWidth = _slotSize * cols + spacing * (cols - 1);
            float gridHeight = _slotSize * rows + spacing * (rows - 1);

            var container = VeneerPanel.Create(parent, name, gridWidth, gridHeight);
            container.ShowBorder = false;
            container.BackgroundColor = Color.clear;

            var containerRect = container.RectTransform;
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = new Vector2(x, y);

            var grid = VeneerItemGrid.Create(container.transform, cols, rows, _slotSize);

            // Position grid at origin of container
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 1);
            gridRect.anchorMax = new Vector2(0, 1);
            gridRect.pivot = new Vector2(0, 1);
            gridRect.anchoredPosition = Vector2.zero;

            return grid;
        }

        private void CreateWeightDisplay(RectTransform parent, float yPos, float width)
        {
            _weightText = VeneerText.Create(parent, "Weight: 0 / 300");
            _weightText.ApplyStyle(TextStyle.Caption);
            _weightText.Alignment = TextAnchor.MiddleRight;

            var rect = _weightText.RectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(0, yPos);
            rect.sizeDelta = new Vector2(width, WEIGHT_HEIGHT);
        }

        private void CreateContainerFrame(Transform parent)
        {
            float padding = VeneerDimensions.Padding;
            float spacing = 4f;
            int containerCols = 8;
            int containerRows = 4;

            float gridWidth = _slotSize * containerCols + spacing * (containerCols - 1);
            float gridHeight = _slotSize * containerRows + spacing * (containerRows - 1);
            float totalWidth = gridWidth + padding * 2;
            // No CONTENTS label needed - header shows container name
            float totalHeight = VeneerDimensions.WindowTitleHeight + gridHeight + padding * 2;

            // Create container frame using VeneerFrame
            _containerFrame = VeneerFrame.Create(parent, new FrameConfig
            {
                Id = ElementIdContainer,
                Name = "ContainerFrame",
                Title = "Container",
                Width = totalWidth,
                Height = totalHeight,
                HasHeader = true,
                HasCloseButton = true,
                IsDraggable = true,
                SavePosition = true,
                Anchor = AnchorPreset.MiddleLeft,
                Offset = new Vector2(20, 0)
            });

            _containerFrame.OnCloseClicked += OnContainerWindowClosed;

            // Build container content - grid directly in content area
            var content = _containerFrame.Content;

            // Grid starts at top of content
            _containerGrid = CreateItemGrid(content, "ContainerGrid", 0, 0, containerCols, containerRows, spacing);

            // Register anchor
            VeneerAnchor.Register(ElementIdContainer, ScreenAnchor.Left, new Vector2(20, 0));

            _containerFrame.Hide();
        }

        private void OnInventoryWindowClosed()
        {
            // Close via vanilla to ensure proper cleanup
            if (InventoryGui.instance != null && InventoryGui.IsVisible())
            {
                InventoryGui.instance.Hide();
            }
        }

        private void OnContainerWindowClosed()
        {
            CloseContainer();
        }

        /// <summary>
        /// Shows the inventory panel.
        /// </summary>
        public override void Show()
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            _inventoryGrid.SetInventory(_player.GetInventory());
            UpdateWeight();

            _inventoryFrame.Show();
            base.Show();
        }

        /// <summary>
        /// Hides the inventory panel.
        /// </summary>
        public override void Hide()
        {
            if (VeneerItemCursor.IsHoldingItem)
            {
                VeneerItemCursor.CancelHold();
            }

            CloseContainer();
            _inventoryFrame.Hide();
            base.Hide();
        }

        /// <summary>
        /// Opens a container alongside the inventory.
        /// </summary>
        public void OpenContainer(Container container)
        {
            if (container == null) return;

            _openContainer = container;

            var containerInventory = container.GetInventory();
            if (containerInventory == null) return;

            // Update title
            string containerName = container.m_name;
            _containerFrame.Title = !string.IsNullOrEmpty(containerName)
                ? Localization.instance.Localize(containerName)
                : "Container";

            // Resize grid if needed
            int cols = containerInventory.GetWidth();
            int rows = containerInventory.GetHeight();

            if (cols != _containerGrid.Width || rows != _containerGrid.Height)
            {
                _containerGrid.Resize(cols, rows);

                // Resize frame
                float padding = VeneerDimensions.Padding;
                float spacing = 4f;
                float gridWidth = _slotSize * cols + spacing * (cols - 1);
                float gridHeight = _slotSize * rows + spacing * (rows - 1);
                float totalWidth = gridWidth + padding * 2;
                // No CONTENTS label - header shows container name
                float totalHeight = VeneerDimensions.WindowTitleHeight + gridHeight + padding * 2;

                _containerFrame.SetSize(totalWidth, totalHeight);
            }

            _containerGrid.SetInventory(containerInventory);
            _containerFrame.Show();
        }

        /// <summary>
        /// Closes any open container.
        /// </summary>
        public void CloseContainer()
        {
            _openContainer = null;
            if (_containerFrame != null) _containerFrame.Hide();
            if (_containerGrid != null) _containerGrid.SetInventory(null);
        }

        private void Update()
        {
            if (!IsVisible || _player == null) return;

            _inventoryGrid?.UpdateAllSlots();
            UpdateWeight();

            if (_openContainer != null && _containerGrid != null && _containerFrame.IsVisible)
            {
                _containerGrid.UpdateAllSlots();
            }
        }

        private void UpdateWeight()
        {
            if (_player == null || _weightText == null) return;

            var inventory = _player.GetInventory();
            float weight = inventory.GetTotalWeight();
            float maxWeight = _player.GetMaxCarryWeight();

            _weightText.Content = $"Weight: {weight:F0} / {maxWeight:F0}";

            if (weight > maxWeight)
                _weightText.TextColor = VeneerColors.Error;
            else if (weight > maxWeight * 0.8f)
                _weightText.TextColor = VeneerColors.Warning;
            else
                _weightText.TextColor = VeneerColors.TextMuted;
        }

        protected override void OnDestroy()
        {
            if (_containerFrame != null)
            {
                Destroy(_containerFrame.gameObject);
            }
            if (_inventoryFrame != null)
            {
                Destroy(_inventoryFrame.gameObject);
            }
            base.OnDestroy();
        }
    }
}
