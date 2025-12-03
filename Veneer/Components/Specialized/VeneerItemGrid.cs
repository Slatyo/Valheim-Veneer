using System;
using System.Collections.Generic;
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
    /// Item grid component for inventory displays.
    /// Manages a grid of VeneerItemSlots.
    /// </summary>
    public class VeneerItemGrid : VeneerElement, IDropHandler
    {
        private List<VeneerItemSlot> _slots = new List<VeneerItemSlot>();
        private Image _backgroundImage;
        private GridLayoutGroup _gridLayout;
        private Inventory _inventory;

        private int _width;
        private int _height;
        private float _slotSize;

        // Drag state
        private VeneerItemSlot _draggedSlot;
        private GameObject _dragIcon;
        private Image _dragIconImage;

        /// <summary>
        /// Number of columns.
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// Number of rows.
        /// </summary>
        public int Height => _height;

        /// <summary>
        /// The inventory being displayed.
        /// </summary>
        public Inventory GridInventory => _inventory;

        /// <summary>
        /// Called when an item is moved.
        /// </summary>
        public event Action<VeneerItemSlot, VeneerItemSlot> OnItemMoved;

        /// <summary>
        /// Called when a slot is clicked.
        /// </summary>
        public event Action<VeneerItemSlot, PointerEventData> OnSlotClicked;

        /// <summary>
        /// Creates an item grid.
        /// </summary>
        public static VeneerItemGrid Create(Transform parent, int width, int height, float slotSize = 40f)
        {
            var go = CreateUIObject("VeneerItemGrid", parent);
            var grid = go.AddComponent<VeneerItemGrid>();
            grid.Initialize(width, height, slotSize);
            return grid;
        }

        private void Initialize(int width, int height, float slotSize)
        {
            _width = width;
            _height = height;
            _slotSize = slotSize;

            float spacing = VeneerDimensions.Spacing;
            float padding = VeneerDimensions.Padding;

            float totalWidth = slotSize * width + spacing * (width - 1) + padding * 2;
            float totalHeight = slotSize * height + spacing * (height - 1) + padding * 2;

            SetSize(totalWidth, totalHeight);

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.BackgroundLight;

            // Border
            var borderGo = CreateUIObject("Border", transform);
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

            // Grid content
            var contentGo = CreateUIObject("Content", transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);

            _gridLayout = contentGo.AddComponent<GridLayoutGroup>();
            _gridLayout.cellSize = new Vector2(slotSize, slotSize);
            _gridLayout.spacing = new Vector2(spacing, spacing);
            _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            _gridLayout.childAlignment = TextAnchor.UpperLeft;
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = width;

            // Create slots
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var slot = VeneerItemSlot.Create(contentGo.transform, slotSize);
                    slot.SetGridPosition(x, y, null);
                    slot.OnSlotClick += HandleSlotClick;
                    slot.OnDragStart += HandleDragStart;
                    slot.OnDragEnd += HandleDragEnd;
                    _slots.Add(slot);
                }
            }

            // Create drag icon (hidden by default)
            CreateDragIcon();
        }

        private void CreateDragIcon()
        {
            // Create drag icon on the root canvas so it appears on top of everything
            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform.root;

            _dragIcon = CreateUIObject("DragIcon", parent);
            var dragRect = _dragIcon.GetComponent<RectTransform>();
            dragRect.sizeDelta = new Vector2(_slotSize, _slotSize);
            dragRect.pivot = new Vector2(0.5f, 0.5f);

            // Ensure drag icon renders on top - DragPreview layer
            var dragCanvas = _dragIcon.AddComponent<Canvas>();
            dragCanvas.overrideSorting = true;
            dragCanvas.sortingOrder = VeneerLayers.DragPreview;

            _dragIconImage = _dragIcon.AddComponent<Image>();
            _dragIconImage.raycastTarget = false;
            _dragIconImage.preserveAspect = true;

            var canvasGroup = _dragIcon.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.9f;

            // Add a slight background for visibility
            var bgGo = CreateUIObject("DragBg", _dragIcon.transform);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(-2, -2);
            bgRect.offsetMax = new Vector2(2, 2);
            bgRect.SetAsFirstSibling();

            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.5f);
            bgImage.raycastTarget = false;

            _dragIcon.SetActive(false);
        }

        /// <summary>
        /// Sets the inventory to display.
        /// </summary>
        public void SetInventory(Inventory inventory)
        {
            _inventory = inventory;

            // Update slot references
            foreach (var slot in _slots)
            {
                slot.SetGridPosition(slot.GridX, slot.GridY, inventory);
            }

            UpdateAllSlots();
        }

        /// <summary>
        /// Updates all slots from the inventory.
        /// </summary>
        public void UpdateAllSlots()
        {
            if (_inventory == null)
            {
                foreach (var slot in _slots)
                {
                    slot.Clear();
                }
                return;
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var item = _inventory.GetItemAt(x, y);
                    var slot = GetSlot(x, y);
                    slot?.SetItem(item);
                }
            }
        }

        /// <summary>
        /// Gets the slot at a grid position.
        /// </summary>
        public VeneerItemSlot GetSlot(int x, int y)
        {
            int index = y * _width + x;
            if (index >= 0 && index < _slots.Count)
            {
                return _slots[index];
            }
            return null;
        }

        /// <summary>
        /// Updates a specific slot.
        /// </summary>
        public void UpdateSlot(int x, int y)
        {
            var slot = GetSlot(x, y);
            if (slot != null && _inventory != null)
            {
                var item = _inventory.GetItemAt(x, y);
                slot.SetItem(item);
            }
        }

        private void HandleSlotClick(VeneerItemSlot slot, PointerEventData eventData)
        {
            OnSlotClicked?.Invoke(slot, eventData);

            // Handle default click behavior
            if (eventData.button == PointerEventData.InputButton.Right && slot.Item != null)
            {
                // Right click - use item
                var player = Player.m_localPlayer;
                if (player != null)
                {
                    player.UseItem(_inventory, slot.Item, true);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Left && slot.Item != null)
            {
                // Shift+Left click - split stack
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (slot.Item.m_stack > 1)
                    {
                        ShowSplitDialog(slot);
                    }
                }
            }
        }

        private void ShowSplitDialog(VeneerItemSlot sourceSlot)
        {
            var item = sourceSlot.Item;
            if (item == null || item.m_stack <= 1) return;

            VeneerSplitDialog.Show(item, item.m_stack, (splitAmount) =>
            {
                if (_inventory == null || splitAmount <= 0 || splitAmount >= item.m_stack) return;

                // Find an empty slot
                Vector2i emptySlot = FindEmptySlot();
                if (emptySlot.x >= 0)
                {
                    // Create a new item with the split amount
                    var newItem = item.Clone();
                    newItem.m_stack = splitAmount;
                    item.m_stack -= splitAmount;

                    // Add to inventory at empty slot
                    _inventory.AddItem(newItem, splitAmount, emptySlot.x, emptySlot.y);
                    UpdateAllSlots();
                }
            });
        }

        private Vector2i FindEmptySlot()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (_inventory.GetItemAt(x, y) == null)
                    {
                        return new Vector2i(x, y);
                    }
                }
            }
            return new Vector2i(-1, -1);
        }

        private void HandleDragStart(VeneerItemSlot slot)
        {
            if (slot.Item == null) return;

            _draggedSlot = slot;

            // Show drag icon
            if (slot.Item.m_shared.m_icons != null && slot.Item.m_shared.m_icons.Length > 0)
            {
                _dragIconImage.sprite = slot.Item.m_shared.m_icons[slot.Item.m_variant];
                _dragIcon.SetActive(true);
            }
        }

        private void HandleDragEnd(VeneerItemSlot slot, PointerEventData eventData)
        {
            _dragIcon.SetActive(false);

            if (_draggedSlot == null) return;

            // Find target slot
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            VeneerItemSlot targetSlot = null;
            foreach (var result in results)
            {
                targetSlot = result.gameObject.GetComponent<VeneerItemSlot>();
                if (targetSlot != null && targetSlot != _draggedSlot)
                {
                    break;
                }
            }

            if (targetSlot != null && targetSlot.SlotInventory == _inventory)
            {
                // Move item within same inventory
                var item = _draggedSlot.Item;
                if (item != null)
                {
                    _inventory.MoveItemToThis(_inventory, item, item.m_stack, targetSlot.GridX, targetSlot.GridY);
                    UpdateAllSlots();
                    OnItemMoved?.Invoke(_draggedSlot, targetSlot);
                }
            }

            _draggedSlot = null;
        }

        private void Update()
        {
            // Update drag icon position
            if (_dragIcon.activeSelf)
            {
                _dragIcon.transform.position = Input.mousePosition;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handle drops from external sources if needed
        }

        /// <summary>
        /// Resizes the grid.
        /// </summary>
        public void Resize(int width, int height)
        {
            if (width == _width && height == _height) return;

            // Clear existing slots
            foreach (var slot in _slots)
            {
                Destroy(slot.gameObject);
            }
            _slots.Clear();

            // Reinitialize
            _width = width;
            _height = height;
            _gridLayout.constraintCount = width;

            float spacing = VeneerDimensions.Spacing;
            float padding = VeneerDimensions.Padding;

            float totalWidth = _slotSize * width + spacing * (width - 1) + padding * 2;
            float totalHeight = _slotSize * height + spacing * (height - 1) + padding * 2;

            SetSize(totalWidth, totalHeight);

            // Create new slots
            var content = _gridLayout.transform;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var slot = VeneerItemSlot.Create(content, _slotSize);
                    slot.SetGridPosition(x, y, _inventory);
                    slot.OnSlotClick += HandleSlotClick;
                    slot.OnDragStart += HandleDragStart;
                    slot.OnDragEnd += HandleDragEnd;
                    _slots.Add(slot);
                }
            }

            UpdateAllSlots();
        }
    }
}
