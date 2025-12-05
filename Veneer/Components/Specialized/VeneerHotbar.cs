using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Components.Specialized
{
    /// <summary>
    /// Hotbar replacement showing slots 1-8 for quick item access.
    /// </summary>
    public class VeneerHotbar : VeneerElement
    {
        private const string ElementIdHotbar = "Veneer_Hotbar";
        private const int HotbarSlots = 8;

        private List<HotbarSlot> _slots = new List<HotbarSlot>();
        private Image _backgroundImage;
        private Image _borderImage;
        private Player _player;

        /// <summary>
        /// Creates the hotbar.
        /// </summary>
        public static VeneerHotbar Create(Transform parent)
        {
            var go = CreateUIObject("VeneerHotbar", parent);
            var hotbar = go.AddComponent<VeneerHotbar>();
            hotbar.Initialize();
            return hotbar;
        }

        private void Initialize()
        {
            ElementId = ElementIdHotbar;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.HUD;

            // Register with anchor system - bottom center with margin
            VeneerAnchor.Register(ElementId, ScreenAnchor.BottomCenter, new Vector2(0, 20));

            float slotSize = 40f;
            float spacing = 2f;
            float padding = 4f;

            float width = slotSize * HotbarSlots + spacing * (HotbarSlots - 1) + padding * 2;
            float height = slotSize + padding * 2;

            SetSize(width, height);
            AnchorTo(AnchorPreset.BottomCenter, new Vector2(0, 20));

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

            // Content with horizontal layout
            var content = CreateUIObject("Content", transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);

            var layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = spacing;

            // Create slots
            for (int i = 0; i < HotbarSlots; i++)
            {
                var slot = CreateHotbarSlot(content.transform, i, slotSize);
                _slots.Add(slot);
            }

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(200, 30);
            resizer.MaxSize = new Vector2(600, 100);

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }
        }

        private HotbarSlot CreateHotbarSlot(Transform parent, int index, float size)
        {
            var slotGo = CreateUIObject($"Slot_{index}", parent);
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(size, size);

            // Slot background
            var bgImage = slotGo.AddComponent<Image>();
            bgImage.sprite = VeneerTextures.CreateSlotSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = VeneerColors.SlotEmpty;

            // Selection border
            var selectGo = CreateUIObject("Selection", slotGo.transform);
            var selectRect = selectGo.GetComponent<RectTransform>();
            selectRect.anchorMin = Vector2.zero;
            selectRect.anchorMax = Vector2.one;
            selectRect.offsetMin = Vector2.zero;
            selectRect.offsetMax = Vector2.zero;

            var selectImage = selectGo.AddComponent<Image>();
            var selectTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Accent, Color.clear, 2);
            selectImage.sprite = VeneerTextures.CreateSlicedSprite(selectTex, 2);
            selectImage.type = Image.Type.Sliced;
            selectImage.raycastTarget = false;
            selectGo.SetActive(false);

            // Icon
            var iconGo = CreateUIObject("Icon", slotGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2, 2);
            iconRect.offsetMax = new Vector2(-2, -2);

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;

            // Stack count
            var stackGo = CreateUIObject("Stack", slotGo.transform);
            var stackRect = stackGo.GetComponent<RectTransform>();
            stackRect.anchorMin = new Vector2(1, 0);
            stackRect.anchorMax = new Vector2(1, 0);
            stackRect.pivot = new Vector2(1, 0);
            stackRect.anchoredPosition = new Vector2(-2, 2);
            stackRect.sizeDelta = new Vector2(20, 14);

            var stackText = stackGo.AddComponent<Text>();
            stackText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            stackText.fontSize = VeneerConfig.GetScaledFontSize(10);
            stackText.color = VeneerColors.Text;
            stackText.alignment = TextAnchor.LowerRight;
            stackText.raycastTarget = false;

            var stackOutline = stackGo.AddComponent<Outline>();
            stackOutline.effectColor = Color.black;
            stackOutline.effectDistance = new Vector2(1, -1);

            // Keybind number
            var keyGo = CreateUIObject("Key", slotGo.transform);
            var keyRect = keyGo.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0, 1);
            keyRect.anchorMax = new Vector2(0, 1);
            keyRect.pivot = new Vector2(0, 1);
            keyRect.anchoredPosition = new Vector2(2, -2);
            keyRect.sizeDelta = new Vector2(12, 12);

            var keyText = keyGo.AddComponent<Text>();
            keyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            keyText.fontSize = VeneerConfig.GetScaledFontSize(9);
            keyText.color = VeneerColors.TextMuted;
            keyText.alignment = TextAnchor.UpperLeft;
            keyText.text = (index + 1).ToString();
            keyText.raycastTarget = false;

            return new HotbarSlot
            {
                Root = slotGo,
                Background = bgImage,
                Selection = selectGo,
                Icon = iconImage,
                StackText = stackText,
                Index = index
            };
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = Player.m_localPlayer;
            }

            if (_player == null) return;

            UpdateSlots();
        }

        private void UpdateSlots()
        {
            var inventory = _player.GetInventory();
            if (inventory == null) return;

            // Get equipped items from inventory
            var equippedItems = inventory.GetEquippedItems();

            for (int i = 0; i < HotbarSlots; i++)
            {
                var slot = _slots[i];
                var item = inventory.GetItemAt(i, 0); // First row is hotbar

                if (item != null)
                {
                    // Show icon
                    if (item.m_shared?.m_icons != null && item.m_shared.m_icons.Length > 0)
                    {
                        slot.Icon.sprite = item.m_shared.m_icons[0];
                        slot.Icon.enabled = true;
                    }
                    else
                    {
                        slot.Icon.enabled = false;
                    }

                    // Show stack count
                    if (item.m_stack > 1)
                    {
                        slot.StackText.text = item.m_stack.ToString();
                        slot.StackText.enabled = true;
                    }
                    else
                    {
                        slot.StackText.enabled = false;
                    }

                    // Check if this item is equipped
                    bool isEquipped = item.m_equipped || equippedItems.Contains(item);
                    slot.Selection.SetActive(isEquipped);

                    slot.Background.color = VeneerColors.SlotFilled;
                }
                else
                {
                    slot.Icon.enabled = false;
                    slot.StackText.enabled = false;
                    slot.Selection.SetActive(false);
                    slot.Background.color = VeneerColors.SlotEmpty;
                }
            }
        }

        private class HotbarSlot
        {
            public GameObject Root;
            public Image Background;
            public GameObject Selection;
            public Image Icon;
            public Text StackText;
            public int Index;
        }
    }
}
