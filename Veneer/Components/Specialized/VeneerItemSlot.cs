using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Composite;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Specialized
{
    /// <summary>
    /// Item slot component for inventory grids.
    /// Handles item display, interaction, and tooltips.
    /// </summary>
    public class VeneerItemSlot : VeneerElement, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Image _backgroundImage;
        private Image _borderImage;
        private Image _iconImage;
        private Text _amountText;
        private Image _durabilityBar;

        // Reserved for future durability text display
#pragma warning disable CS0169
        private Text _durabilityText;
#pragma warning restore CS0169
        private Image _equippedIndicator;

        private ItemDrop.ItemData _item;
        private int _gridX;
        private int _gridY;
        private Inventory _inventory;
        private bool _isHovered;
        private bool _isDragging;

        /// <summary>
        /// The item in this slot.
        /// </summary>
        public ItemDrop.ItemData Item => _item;

        /// <summary>
        /// Grid X position.
        /// </summary>
        public int GridX => _gridX;

        /// <summary>
        /// Grid Y position.
        /// </summary>
        public int GridY => _gridY;

        /// <summary>
        /// The inventory this slot belongs to.
        /// </summary>
        public Inventory SlotInventory => _inventory;

        /// <summary>
        /// Called when the slot is clicked.
        /// </summary>
        public event Action<VeneerItemSlot, PointerEventData> OnSlotClick;

        /// <summary>
        /// Called when drag starts.
        /// </summary>
        public event Action<VeneerItemSlot> OnDragStart;

        /// <summary>
        /// Called when drag ends.
        /// </summary>
        public event Action<VeneerItemSlot, PointerEventData> OnDragEnd;

        /// <summary>
        /// Creates an item slot.
        /// </summary>
        public static VeneerItemSlot Create(Transform parent, float size = 40f)
        {
            var go = CreateUIObject("ItemSlot", parent);
            var slot = go.AddComponent<VeneerItemSlot>();
            slot.Initialize(size);
            return slot;
        }

        private void Initialize(float size)
        {
            SetSize(size, size);

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreateSlotSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.SlotEmpty;

            // Border - more visible for slot distinction
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.SlotBorder, Color.clear, 2);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 2);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Item icon
            var iconGo = CreateUIObject("Icon", transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2, 2);
            iconRect.offsetMax = new Vector2(-2, -2);

            _iconImage = iconGo.AddComponent<Image>();
            _iconImage.preserveAspect = true;
            _iconImage.raycastTarget = false;
            _iconImage.enabled = false;

            // Amount text (bottom right)
            var amountGo = CreateUIObject("Amount", transform);
            var amountRect = amountGo.GetComponent<RectTransform>();
            amountRect.anchorMin = Vector2.zero;
            amountRect.anchorMax = Vector2.one;
            amountRect.offsetMin = new Vector2(2, 2);
            amountRect.offsetMax = new Vector2(-2, -2);

            _amountText = amountGo.AddComponent<Text>();
            _amountText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _amountText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
            _amountText.color = VeneerColors.Text;
            _amountText.alignment = TextAnchor.LowerRight;
            _amountText.raycastTarget = false;

            var amountOutline = amountGo.AddComponent<Outline>();
            amountOutline.effectColor = Color.black;
            amountOutline.effectDistance = new Vector2(1, -1);

            // Durability bar (bottom)
            var durabilityGo = CreateUIObject("Durability", transform);
            var durabilityRect = durabilityGo.GetComponent<RectTransform>();
            durabilityRect.anchorMin = new Vector2(0, 0);
            durabilityRect.anchorMax = new Vector2(1, 0);
            durabilityRect.pivot = new Vector2(0.5f, 0);
            durabilityRect.offsetMin = new Vector2(2, 2);
            durabilityRect.offsetMax = new Vector2(-2, 5);

            var durabilityBg = durabilityGo.AddComponent<Image>();
            durabilityBg.color = VeneerColors.BackgroundDark;

            var durabilityFillGo = CreateUIObject("Fill", durabilityGo.transform);
            var durabilityFillRect = durabilityFillGo.GetComponent<RectTransform>();
            durabilityFillRect.anchorMin = Vector2.zero;
            durabilityFillRect.anchorMax = Vector2.one;
            durabilityFillRect.offsetMin = Vector2.zero;
            durabilityFillRect.offsetMax = Vector2.zero;

            _durabilityBar = durabilityFillGo.AddComponent<Image>();
            _durabilityBar.color = VeneerColors.Success;
            _durabilityBar.type = Image.Type.Filled;
            _durabilityBar.fillMethod = Image.FillMethod.Horizontal;

            durabilityGo.SetActive(false);

            // Equipped indicator (small dot in corner)
            var equippedGo = CreateUIObject("Equipped", transform);
            var equippedRect = equippedGo.GetComponent<RectTransform>();
            equippedRect.anchorMin = new Vector2(0, 1);
            equippedRect.anchorMax = new Vector2(0, 1);
            equippedRect.pivot = new Vector2(0, 1);
            equippedRect.anchoredPosition = new Vector2(2, -2);
            equippedRect.sizeDelta = new Vector2(8, 8);

            _equippedIndicator = equippedGo.AddComponent<Image>();
            _equippedIndicator.color = VeneerColors.Success;
            equippedGo.SetActive(false);
        }

        /// <summary>
        /// Sets the slot position in the grid.
        /// </summary>
        public void SetGridPosition(int x, int y, Inventory inventory)
        {
            _gridX = x;
            _gridY = y;
            _inventory = inventory;
        }

        /// <summary>
        /// Updates the slot with an item.
        /// </summary>
        public void SetItem(ItemDrop.ItemData item)
        {
            _item = item;
            UpdateVisuals();
        }

        /// <summary>
        /// Clears the slot.
        /// </summary>
        public void Clear()
        {
            _item = null;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_item == null)
            {
                _iconImage.enabled = false;
                _amountText.text = "";
                _durabilityBar.transform.parent.gameObject.SetActive(false);
                _equippedIndicator.gameObject.SetActive(false);
                _borderImage.color = VeneerColors.SlotBorder;
                _backgroundImage.color = VeneerColors.SlotEmpty;
                return;
            }

            // Icon
            if (_item.m_shared.m_icons != null && _item.m_shared.m_icons.Length > 0)
            {
                _iconImage.sprite = _item.m_shared.m_icons[_item.m_variant];
                _iconImage.enabled = true;
            }

            // Amount
            if (_item.m_shared.m_maxStackSize > 1)
            {
                _amountText.text = _item.m_stack.ToString();
            }
            else
            {
                _amountText.text = "";
            }

            // Durability
            if (_item.m_shared.m_useDurability && _item.m_shared.m_maxDurability > 0)
            {
                float durability = _item.m_durability / _item.GetMaxDurability();
                _durabilityBar.fillAmount = durability;
                _durabilityBar.color = durability < 0.25f ? VeneerColors.Error :
                                        durability < 0.5f ? VeneerColors.Warning : VeneerColors.Success;
                _durabilityBar.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                _durabilityBar.transform.parent.gameObject.SetActive(false);
            }

            // Equipped indicator
            bool isEquipped = _item.m_equipped;
            _equippedIndicator.gameObject.SetActive(isEquipped);

            // Quality/rarity border
            int quality = _item.m_quality;
            if (quality > 1)
            {
                _borderImage.color = VeneerColors.GetRarityColor(Mathf.Min(quality, 5));
            }
            else
            {
                _borderImage.color = VeneerColors.SlotBorder;
            }

            _backgroundImage.color = _isHovered ? VeneerColors.SlotHover : VeneerColors.SlotEmpty;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _backgroundImage.color = VeneerColors.SlotHover;

            if (_item != null)
            {
                ShowItemTooltip();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _backgroundImage.color = VeneerColors.SlotEmpty;
            VeneerTooltip.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSlotClick?.Invoke(this, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_item == null) return;
            _isDragging = true;
            OnDragStart?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Drag visual handled by VeneerItemGrid
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;
            OnDragEnd?.Invoke(this, eventData);
        }

        private void ShowItemTooltip()
        {
            if (_item == null) return;

            var tooltip = new TooltipData
            {
                Title = Localization.instance.Localize(_item.m_shared.m_name),
                Subtitle = GetItemSubtitle(),
                Body = Localization.instance.Localize(_item.m_shared.m_description),
                RarityTier = _item.m_quality > 1 ? Mathf.Min(_item.m_quality, 5) : (int?)null
            };

            // Use ShowForItem to allow tooltip providers to modify the tooltip
            VeneerTooltip.ShowForItem(_item, tooltip);
        }

        private string GetItemSubtitle()
        {
            var parts = new System.Collections.Generic.List<string>();

            // Type
            var itemType = _item.m_shared.m_itemType;
            parts.Add(itemType.ToString());

            // Quality
            if (_item.m_quality > 1)
            {
                parts.Add($"Quality {_item.m_quality}");
            }

            // Weight
            float weight = _item.GetWeight();
            if (weight > 0)
            {
                parts.Add($"{weight:F1} weight");
            }

            return string.Join(" | ", parts);
        }
    }
}
