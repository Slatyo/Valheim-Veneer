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
    /// </summary>
    public class VeneerInventoryPanel : VeneerElement
    {
        private const string ElementIdInventory = "Veneer_Inventory";

        private Image _backgroundImage;
        private Image _borderImage;
        private VeneerText _titleText;
        private VeneerText _weightText;

        // Equipment slots
        private Dictionary<string, VeneerItemSlot> _equipmentSlots = new Dictionary<string, VeneerItemSlot>();

        // Main inventory grid
        private VeneerItemGrid _inventoryGrid;

        // Container section (when chest is open) - Reserved for future implementation
#pragma warning disable CS0169, CS0649
        private GameObject _containerSection;
        private VeneerText _containerTitle;
        private VeneerItemGrid _containerGrid;
#pragma warning restore CS0169, CS0649

        // Quick slots
        private VeneerItemGrid _quickSlotGrid;

        private Player _player;
        private Container _openContainer;

        /// <summary>
        /// Main inventory grid.
        /// </summary>
        public VeneerItemGrid InventoryGrid => _inventoryGrid;

        /// <summary>
        /// Container grid (when chest open).
        /// </summary>
        public VeneerItemGrid ContainerGrid => _containerGrid;

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

            // Calculate sizes - 40% larger than default
            float slotSize = VeneerConfig.SlotSize.Value * 1.4f;
            int invWidth = 8;
            int invHeight = 4;
            float padding = VeneerDimensions.PaddingLarge;
            float spacing = VeneerDimensions.SpacingLarge;

            float gridWidth = slotSize * invWidth + VeneerDimensions.Spacing * (invWidth - 1) + VeneerDimensions.Padding * 2;
            float equipmentWidth = slotSize * 2 + VeneerDimensions.Spacing + VeneerDimensions.Padding * 2;
            float totalWidth = gridWidth + equipmentWidth + padding * 3;

            float gridHeight = slotSize * invHeight + VeneerDimensions.Spacing * (invHeight - 1) + VeneerDimensions.Padding * 2;
            float headerHeight = 30;
            float weightHeight = 20;
            float quickSlotHeight = slotSize + VeneerDimensions.Padding * 2;
            float totalHeight = headerHeight + gridHeight + quickSlotHeight + weightHeight + padding * 2 + spacing * 3;

            SetSize(totalWidth, totalHeight);
            AnchorTo(AnchorPreset.MiddleRight, new Vector2(-20, 0));

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.Background;

            // Border
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Border, Color.clear, 1);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Content area
            var content = CreateUIObject("Content", transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);

            // Title
            var titleGo = CreateUIObject("Title", content.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0, headerHeight);

            _titleText = titleGo.AddComponent<VeneerText>();
            _titleText.Content = "Inventory";
            _titleText.ApplyStyle(TextStyle.Header);
            _titleText.Alignment = TextAnchor.MiddleCenter;

            // Main section (equipment + inventory grid)
            var mainSection = CreateUIObject("MainSection", content.transform);
            var mainRect = mainSection.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0, 0);
            mainRect.anchorMax = new Vector2(1, 1);
            mainRect.offsetMin = new Vector2(0, quickSlotHeight + weightHeight + spacing * 2);
            mainRect.offsetMax = new Vector2(0, -headerHeight - spacing);

            var mainLayout = mainSection.AddComponent<HorizontalLayoutGroup>();
            mainLayout.childAlignment = TextAnchor.UpperLeft;
            mainLayout.childControlWidth = false;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = false;
            mainLayout.childForceExpandHeight = true;
            mainLayout.spacing = spacing;

            // Equipment section
            CreateEquipmentSection(mainSection.transform, equipmentWidth, slotSize);

            // Inventory grid with label
            var bagSection = CreateUIObject("BagSection", mainSection.transform);
            var bagLayout = bagSection.AddComponent<VerticalLayoutGroup>();
            bagLayout.childAlignment = TextAnchor.UpperCenter;
            bagLayout.childControlWidth = true;
            bagLayout.childControlHeight = false;
            bagLayout.childForceExpandWidth = true;
            bagLayout.childForceExpandHeight = false;
            bagLayout.spacing = 4f;

            var bagLE = bagSection.AddComponent<LayoutElement>();
            bagLE.flexibleWidth = 1f;

            CreateSectionLabel(bagSection.transform, "Bag", gridWidth);

            _inventoryGrid = VeneerItemGrid.Create(bagSection.transform, invWidth, invHeight, slotSize);

            // Quick slots at bottom with label
            float quickLabelHeight = 18f;
            var quickSection = CreateUIObject("QuickSlots", content.transform);
            var quickRect = quickSection.GetComponent<RectTransform>();
            quickRect.anchorMin = new Vector2(0, 0);
            quickRect.anchorMax = new Vector2(1, 0);
            quickRect.pivot = new Vector2(0.5f, 0);
            quickRect.anchoredPosition = new Vector2(0, weightHeight + spacing);
            quickRect.sizeDelta = new Vector2(0, quickSlotHeight + quickLabelHeight + 4);

            var quickLayout = quickSection.AddComponent<VerticalLayoutGroup>();
            quickLayout.childAlignment = TextAnchor.UpperCenter;
            quickLayout.childControlWidth = true;
            quickLayout.childControlHeight = false;
            quickLayout.childForceExpandWidth = true;
            quickLayout.childForceExpandHeight = false;
            quickLayout.spacing = 4f;

            CreateSectionLabel(quickSection.transform, "Hotbar", slotSize * 8 + VeneerDimensions.Spacing * 7);

            _quickSlotGrid = VeneerItemGrid.Create(quickSection.transform, 8, 1, slotSize);

            // Weight display
            var weightGo = CreateUIObject("Weight", content.transform);
            var weightRect = weightGo.GetComponent<RectTransform>();
            weightRect.anchorMin = new Vector2(0, 0);
            weightRect.anchorMax = new Vector2(1, 0);
            weightRect.pivot = new Vector2(0.5f, 0);
            weightRect.anchoredPosition = Vector2.zero;
            weightRect.sizeDelta = new Vector2(0, weightHeight);

            _weightText = weightGo.AddComponent<VeneerText>();
            _weightText.Content = "Weight: 0 / 300";
            _weightText.ApplyStyle(TextStyle.Caption);
            _weightText.Alignment = TextAnchor.MiddleRight;

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(400, 350);
            resizer.MaxSize = new Vector2(900, 800);

            // Start hidden - must register BEFORE SetActive(false) since Start() won't be called
            RegisterWithManager();
            gameObject.SetActive(false);

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }
        }

        private void CreateEquipmentSection(Transform parent, float width, float slotSize)
        {
            var equipGo = CreateUIObject("Equipment", parent);
            var equipRect = equipGo.GetComponent<RectTransform>();
            equipRect.sizeDelta = new Vector2(width, 0);

            var layoutElement = equipGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;

            // Use a vertical layout to stack label + grid
            var vertLayout = equipGo.AddComponent<VerticalLayoutGroup>();
            vertLayout.childAlignment = TextAnchor.UpperCenter;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = false;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;
            vertLayout.spacing = 4f;
            vertLayout.padding = new RectOffset(0, 0, 0, 0);

            // Section label - "Gear"
            CreateSectionLabel(equipGo.transform, "Gear", width);

            // Slots container with grid layout
            var slotsGo = CreateUIObject("Slots", equipGo.transform);
            var slotsLE = slotsGo.AddComponent<LayoutElement>();
            slotsLE.preferredWidth = width;
            slotsLE.preferredHeight = slotSize * 4 + VeneerDimensions.Spacing * 3 + VeneerDimensions.Padding * 2;

            var equipLayout = slotsGo.AddComponent<GridLayoutGroup>();
            equipLayout.cellSize = new Vector2(slotSize, slotSize);
            equipLayout.spacing = new Vector2(VeneerDimensions.Spacing, VeneerDimensions.Spacing);
            equipLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            equipLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            equipLayout.childAlignment = TextAnchor.UpperCenter;
            equipLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            equipLayout.constraintCount = 2;
            equipLayout.padding = new RectOffset(
                (int)VeneerDimensions.Padding,
                (int)VeneerDimensions.Padding,
                (int)VeneerDimensions.Padding,
                (int)VeneerDimensions.Padding
            );

            // Create equipment slots
            string[] slotNames = { "Head", "Chest", "Legs", "Shoulder", "Utility", "Weapon", "Shield", "Ammo" };
            foreach (var name in slotNames)
            {
                var slot = VeneerItemSlot.Create(slotsGo.transform, slotSize);
                _equipmentSlots[name] = slot;
            }
        }

        private void CreateSectionLabel(Transform parent, string text, float width)
        {
            var labelGo = CreateUIObject($"Label_{text}", parent);
            var labelLE = labelGo.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 18f;
            labelLE.preferredWidth = width;

            var labelBg = labelGo.AddComponent<Image>();
            labelBg.color = VeneerColors.BackgroundDark;

            // Text on child object
            var textGo = CreateUIObject("Text", labelGo.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6, 0);
            textRect.offsetMax = new Vector2(-6, 0);

            var labelText = textGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = VeneerConfig.GetScaledFontSize(10);
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = VeneerColors.TextGold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = text.ToUpper();
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

            base.Show(); // Fire OnShow event and set visibility
        }

        /// <summary>
        /// Hides the inventory panel.
        /// </summary>
        public override void Hide()
        {
            base.Hide(); // Fire OnHide event and set visibility
            _openContainer = null;
        }

        /// <summary>
        /// Opens a container alongside the inventory.
        /// </summary>
        public void OpenContainer(Container container)
        {
            _openContainer = container;
            // Container UI would be created/shown here
            // For now, the vanilla container system will handle it
        }

        /// <summary>
        /// Closes any open container.
        /// </summary>
        public void CloseContainer()
        {
            _openContainer = null;
        }

        private void Update()
        {
            if (!IsVisible || _player == null) return;

            _inventoryGrid.UpdateAllSlots();
            UpdateEquipmentSlots();
            UpdateWeight();
        }

        private void UpdateEquipmentSlots()
        {
            if (_player == null) return;

            var inventory = _player.GetInventory();
            var equippedItems = inventory.GetEquippedItems();

            // Update each equipment slot by finding equipped items of each type
            UpdateEquipSlot("Head", equippedItems.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet));
            UpdateEquipSlot("Chest", equippedItems.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest));
            UpdateEquipSlot("Legs", equippedItems.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs));
            UpdateEquipSlot("Shoulder", equippedItems.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder));
            UpdateEquipSlot("Utility", equippedItems.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility));

            // Weapons - find by weapon types (one-handed, two-handed, bows, etc.)
            var rightHand = equippedItems.Find(i =>
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool ||
                i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch);
            UpdateEquipSlot("Weapon", rightHand);

            // Shield
            var leftHand = equippedItems.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield);
            UpdateEquipSlot("Shield", leftHand);

            // Ammo
            var ammo = equippedItems.Find(i => i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo);
            UpdateEquipSlot("Ammo", ammo);
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
            {
                _weightText.TextColor = VeneerColors.Error;
            }
            else if (weight > maxWeight * 0.8f)
            {
                _weightText.TextColor = VeneerColors.Warning;
            }
            else
            {
                _weightText.TextColor = VeneerColors.TextMuted;
            }
        }
    }
}
