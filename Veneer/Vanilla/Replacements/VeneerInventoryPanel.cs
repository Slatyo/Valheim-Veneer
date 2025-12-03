using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Composite;
using Veneer.Components.Primitives;
using Veneer.Components.Specialized;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Complete inventory panel replacement.
    /// Clean layout with proper sizing and no overlapping elements.
    /// </summary>
    public class VeneerInventoryPanel : VeneerElement
    {
        private const string ElementIdInventory = "Veneer_Inventory";
        private const string ElementIdContainer = "Veneer_Container";

        // UI References
        private VeneerText _titleText;
        private VeneerText _weightText;
        private Dictionary<string, VeneerItemSlot> _equipmentSlots = new Dictionary<string, VeneerItemSlot>();
        private VeneerItemGrid _inventoryGrid;
        private VeneerItemGrid _quickSlotGrid;

        // Container panel
        private GameObject _containerPanel;
        private VeneerText _containerTitle;
        private VeneerItemGrid _containerGrid;

        // State
        private Player _player;
        private Container _openContainer;
        private float _slotSize;

        // Layout constants
        private const int INV_COLS = 8;
        private const int INV_ROWS = 4;
        private const int EQUIP_COLS = 2;
        private const int EQUIP_ROWS = 4;
        private const int QUICKSLOT_COLS = 8;
        private const float HEADER_HEIGHT = 28f;
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
            panel.Initialize();
            return panel;
        }

        private void Initialize()
        {
            ElementId = ElementIdInventory;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.Window;
            AutoRegisterWithManager = true;

            VeneerAnchor.Register(ElementId, ScreenAnchor.Right, new Vector2(-20, 0));

            // Calculate slot size (scaled up from base)
            _slotSize = VeneerConfig.SlotSize.Value * 1.3f;
            float padding = 12f;
            float spacing = 8f;
            float innerSpacing = 4f;

            // Calculate section dimensions
            float equipWidth = _slotSize * EQUIP_COLS + innerSpacing * (EQUIP_COLS - 1);
            float equipHeight = _slotSize * EQUIP_ROWS + innerSpacing * (EQUIP_ROWS - 1);

            float bagWidth = _slotSize * INV_COLS + innerSpacing * (INV_COLS - 1);
            float bagHeight = _slotSize * INV_ROWS + innerSpacing * (INV_ROWS - 1);

            float quickWidth = _slotSize * QUICKSLOT_COLS + innerSpacing * (QUICKSLOT_COLS - 1);
            float quickHeight = _slotSize;

            // Calculate total panel size
            float contentWidth = equipWidth + spacing + bagWidth;
            float mainHeight = Mathf.Max(equipHeight + LABEL_HEIGHT + innerSpacing, bagHeight + LABEL_HEIGHT + innerSpacing);
            float totalWidth = contentWidth + padding * 2;
            float totalHeight = HEADER_HEIGHT + mainHeight + spacing + LABEL_HEIGHT + innerSpacing + quickHeight + spacing + WEIGHT_HEIGHT + padding * 2;

            SetSize(totalWidth, totalHeight);
            AnchorTo(AnchorPreset.MiddleRight, new Vector2(-20, 0));

            // Background
            var bg = gameObject.AddComponent<Image>();
            bg.sprite = VeneerTextures.CreatePanelSprite();
            bg.type = Image.Type.Sliced;
            bg.color = VeneerColors.Background;

            // Border
            CreateBorder(transform);

            // Build layout from top to bottom
            float yPos = -padding;

            // Header
            yPos = CreateHeader(transform, padding, yPos, contentWidth);

            // Main section (Equipment + Bag side by side)
            yPos -= spacing;
            yPos = CreateMainSection(transform, padding, yPos, equipWidth, equipHeight, bagWidth, bagHeight, spacing, innerSpacing);

            // Quickslots
            yPos -= spacing;
            yPos = CreateQuickSlots(transform, padding, yPos, quickWidth, quickHeight, innerSpacing);

            // Weight
            yPos -= spacing;
            CreateWeight(transform, padding, yPos, contentWidth);

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Create container panel (hidden by default)
            CreateContainerPanel();

            // Start hidden
            RegisterWithManager();
            gameObject.SetActive(false);

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }
        }

        private void CreateBorder(Transform parent)
        {
            var borderGo = CreateUIObject("Border", parent);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            var borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Border, Color.clear, 1);
            borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;
        }

        private float CreateHeader(Transform parent, float padding, float yPos, float width)
        {
            var headerGo = CreateUIObject("Header", parent);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(0, 1);
            headerRect.pivot = new Vector2(0, 1);
            headerRect.anchoredPosition = new Vector2(padding, yPos);
            headerRect.sizeDelta = new Vector2(width, HEADER_HEIGHT);

            _titleText = headerGo.AddComponent<VeneerText>();
            _titleText.Content = "Inventory";
            _titleText.ApplyStyle(TextStyle.Header);
            _titleText.Alignment = TextAnchor.MiddleCenter;

            return yPos - HEADER_HEIGHT;
        }

        private float CreateMainSection(Transform parent, float padding, float yPos, float equipWidth, float equipHeight, float bagWidth, float bagHeight, float spacing, float innerSpacing)
        {
            float xPos = padding;

            // Equipment section
            CreateSectionWithLabel(parent, "GEAR", xPos, yPos, equipWidth, LABEL_HEIGHT);
            CreateEquipmentGrid(parent, xPos, yPos - LABEL_HEIGHT - innerSpacing, equipWidth, equipHeight, innerSpacing);

            xPos += equipWidth + spacing;

            // Bag section
            CreateSectionWithLabel(parent, "BAG", xPos, yPos, bagWidth, LABEL_HEIGHT);
            _inventoryGrid = CreateItemGrid(parent, "InventoryGrid", xPos, yPos - LABEL_HEIGHT - innerSpacing, INV_COLS, INV_ROWS, innerSpacing);

            float sectionHeight = Mathf.Max(LABEL_HEIGHT + innerSpacing + equipHeight, LABEL_HEIGHT + innerSpacing + bagHeight);
            return yPos - sectionHeight;
        }

        private void CreateSectionWithLabel(Transform parent, string text, float x, float y, float width, float height)
        {
            var labelGo = CreateUIObject($"Label_{text}", parent);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(x, y);
            labelRect.sizeDelta = new Vector2(width, height);

            var labelBg = labelGo.AddComponent<Image>();
            labelBg.color = VeneerColors.BackgroundDark;

            var textGo = CreateUIObject("Text", labelGo.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 0);
            textRect.offsetMax = new Vector2(-4, 0);

            var labelText = textGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = VeneerConfig.GetScaledFontSize(10);
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = VeneerColors.TextGold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = text;
        }

        private void CreateEquipmentGrid(Transform parent, float x, float y, float width, float height, float spacing)
        {
            var containerGo = CreateUIObject("Equipment", parent);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = new Vector2(x, y);
            containerRect.sizeDelta = new Vector2(width, height);

            // Background
            var bg = containerGo.AddComponent<Image>();
            bg.color = VeneerColors.BackgroundLight;

            var gridLayout = containerGo.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(_slotSize, _slotSize);
            gridLayout.spacing = new Vector2(spacing, spacing);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = EQUIP_COLS;

            string[] slotNames = { "Head", "Chest", "Legs", "Shoulder", "Utility", "Weapon", "Shield", "Ammo" };
            foreach (var name in slotNames)
            {
                var slot = VeneerItemSlot.Create(containerGo.transform, _slotSize);
                _equipmentSlots[name] = slot;
            }
        }

        private VeneerItemGrid CreateItemGrid(Transform parent, string name, float x, float y, int cols, int rows, float spacing)
        {
            float gridWidth = _slotSize * cols + spacing * (cols - 1);
            float gridHeight = _slotSize * rows + spacing * (rows - 1);

            var containerGo = CreateUIObject(name, parent);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = new Vector2(x, y);
            containerRect.sizeDelta = new Vector2(gridWidth, gridHeight);

            var grid = VeneerItemGrid.Create(containerGo.transform, cols, rows, _slotSize);

            // Position grid at origin of container
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 1);
            gridRect.anchorMax = new Vector2(0, 1);
            gridRect.pivot = new Vector2(0, 1);
            gridRect.anchoredPosition = Vector2.zero;

            return grid;
        }

        private float CreateQuickSlots(Transform parent, float padding, float yPos, float width, float height, float spacing)
        {
            float totalWidth = width;
            float xPos = padding;

            CreateSectionWithLabel(parent, "HOTBAR", xPos, yPos, totalWidth, LABEL_HEIGHT);
            _quickSlotGrid = CreateItemGrid(parent, "QuickSlots", xPos, yPos - LABEL_HEIGHT - spacing, QUICKSLOT_COLS, 1, spacing);

            return yPos - LABEL_HEIGHT - spacing - height;
        }

        private void CreateWeight(Transform parent, float padding, float yPos, float width)
        {
            var weightGo = CreateUIObject("Weight", parent);
            var weightRect = weightGo.GetComponent<RectTransform>();
            weightRect.anchorMin = new Vector2(0, 1);
            weightRect.anchorMax = new Vector2(0, 1);
            weightRect.pivot = new Vector2(0, 1);
            weightRect.anchoredPosition = new Vector2(padding, yPos);
            weightRect.sizeDelta = new Vector2(width, WEIGHT_HEIGHT);

            _weightText = weightGo.AddComponent<VeneerText>();
            _weightText.Content = "Weight: 0 / 300";
            _weightText.ApplyStyle(TextStyle.Caption);
            _weightText.Alignment = TextAnchor.MiddleRight;
        }

        private void CreateContainerPanel()
        {
            float padding = 12f;
            float spacing = 4f;
            int containerCols = 8;
            int containerRows = 4;

            float gridWidth = _slotSize * containerCols + spacing * (containerCols - 1);
            float gridHeight = _slotSize * containerRows + spacing * (containerRows - 1);
            float totalWidth = gridWidth + padding * 2;
            float totalHeight = HEADER_HEIGHT + spacing + LABEL_HEIGHT + spacing + gridHeight + padding * 2;

            _containerPanel = CreateUIObject("ContainerPanel", transform.parent);
            var containerRect = _containerPanel.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(totalWidth, totalHeight);

            // Background
            var bg = _containerPanel.AddComponent<Image>();
            bg.sprite = VeneerTextures.CreatePanelSprite();
            bg.type = Image.Type.Sliced;
            bg.color = VeneerColors.Background;

            // Border
            CreateBorder(_containerPanel.transform);

            float yPos = -padding;

            // Header
            var headerGo = CreateUIObject("Header", _containerPanel.transform);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(0, 1);
            headerRect.pivot = new Vector2(0, 1);
            headerRect.anchoredPosition = new Vector2(padding, yPos);
            headerRect.sizeDelta = new Vector2(gridWidth, HEADER_HEIGHT);

            _containerTitle = headerGo.AddComponent<VeneerText>();
            _containerTitle.Content = "Container";
            _containerTitle.ApplyStyle(TextStyle.Header);
            _containerTitle.Alignment = TextAnchor.MiddleCenter;

            yPos -= HEADER_HEIGHT + spacing;

            // Label
            CreateSectionWithLabel(_containerPanel.transform, "CONTENTS", padding, yPos, gridWidth, LABEL_HEIGHT);
            yPos -= LABEL_HEIGHT + spacing;

            // Grid
            _containerGrid = CreateItemGrid(_containerPanel.transform, "ContainerGrid", padding, yPos, containerCols, containerRows, spacing);

            // Mover
            var mover = _containerPanel.AddComponent<VeneerMover>();
            mover.ElementId = ElementIdContainer;

            VeneerAnchor.Register(ElementIdContainer, ScreenAnchor.Left, new Vector2(20, 0));

            _containerPanel.SetActive(false);
        }

        /// <summary>
        /// Shows the inventory panel.
        /// </summary>
        public override void Show()
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            _inventoryGrid.SetInventory(_player.GetInventory());
            UpdateEquipmentSlots();
            UpdateWeight();

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
            _containerTitle.Content = !string.IsNullOrEmpty(containerName)
                ? Localization.instance.Localize(containerName)
                : "Container";

            // Resize grid if needed
            int cols = containerInventory.GetWidth();
            int rows = containerInventory.GetHeight();

            if (cols != _containerGrid.Width || rows != _containerGrid.Height)
            {
                _containerGrid.Resize(cols, rows);

                // Resize panel
                float padding = 12f;
                float spacing = 4f;
                float gridWidth = _slotSize * cols + spacing * (cols - 1);
                float gridHeight = _slotSize * rows + spacing * (rows - 1);
                float totalWidth = gridWidth + padding * 2;
                float totalHeight = HEADER_HEIGHT + spacing + LABEL_HEIGHT + spacing + gridHeight + padding * 2;

                var containerRect = _containerPanel.GetComponent<RectTransform>();
                containerRect.sizeDelta = new Vector2(totalWidth, totalHeight);
            }

            _containerGrid.SetInventory(containerInventory);
            PositionContainerPanel();
            _containerPanel.SetActive(true);
        }

        private void PositionContainerPanel()
        {
            if (_containerPanel == null) return;

            var containerRect = _containerPanel.GetComponent<RectTransform>();

            var savedData = VeneerAnchor.GetAnchorData(ElementIdContainer);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(containerRect, savedData.Anchor, savedData.Offset);
            }
            else
            {
                containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                containerRect.anchorMax = new Vector2(0.5f, 0.5f);
                containerRect.pivot = new Vector2(1, 0.5f);

                Vector3[] corners = new Vector3[4];
                RectTransform.GetWorldCorners(corners);
                containerRect.position = new Vector3(corners[0].x - 20, RectTransform.position.y, 0);
            }
        }

        /// <summary>
        /// Closes any open container.
        /// </summary>
        public void CloseContainer()
        {
            _openContainer = null;
            if (_containerPanel != null) _containerPanel.SetActive(false);
            if (_containerGrid != null) _containerGrid.SetInventory(null);
        }

        private void Update()
        {
            if (!IsVisible || _player == null) return;

            _inventoryGrid.UpdateAllSlots();
            UpdateEquipmentSlots();
            UpdateWeight();

            if (_openContainer != null && _containerGrid != null && _containerPanel.activeSelf)
            {
                _containerGrid.UpdateAllSlots();
            }
        }

        private void UpdateEquipmentSlots()
        {
            if (_player == null) return;

            var inventory = _player.GetInventory();
            var equipped = inventory.GetEquippedItems();

            UpdateEquipSlot("Head", equipped.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet));
            UpdateEquipSlot("Chest", equipped.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest));
            UpdateEquipSlot("Legs", equipped.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs));
            UpdateEquipSlot("Shoulder", equipped.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder));
            UpdateEquipSlot("Utility", equipped.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility));

            var weapon = equipped.Find(i =>
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch);
            UpdateEquipSlot("Weapon", weapon);

            UpdateEquipSlot("Shield", equipped.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield));
            UpdateEquipSlot("Ammo", equipped.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo));
        }

        private void UpdateEquipSlot(string slotName, ItemDrop.ItemData item)
        {
            if (_equipmentSlots.TryGetValue(slotName, out var slot))
            {
                slot.SetItem(item);
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
            if (_containerPanel != null)
            {
                Destroy(_containerPanel);
            }
            base.OnDestroy();
        }
    }
}
