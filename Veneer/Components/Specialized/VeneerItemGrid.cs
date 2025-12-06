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
    /// Supports click-to-pickup/drop and drag-and-drop.
    /// </summary>
    public class VeneerItemGrid : VeneerElement, IDropHandler, IPointerClickHandler
    {
        private List<VeneerItemSlot> _slots = new List<VeneerItemSlot>();
        private Image _backgroundImage;
        private GridLayoutGroup _gridLayout;
        private Inventory _inventory;

        private int _width;
        private int _height;
        private float _slotSize;
        private bool _hideEquippedItems;

        // Drag state (for drag-and-drop within same grid)
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
        /// When true, equipped items will not be displayed in the grid.
        /// The slot will appear empty but the item remains in the inventory.
        /// </summary>
        public bool HideEquippedItems
        {
            get => _hideEquippedItems;
            set => _hideEquippedItems = value;
        }

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

                    // Hide equipped items if enabled
                    if (_hideEquippedItems && item != null && item.m_equipped)
                    {
                        slot?.Clear();
                    }
                    else
                    {
                        slot?.SetItem(item);
                    }
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
            // Block interactions while split dialog is open
            if (VeneerSplitDialog.IsShowing)
            {
                return;
            }

            OnSlotClicked?.Invoke(slot, eventData);

            var player = Player.m_localPlayer;
            if (player == null) return;

            // Check if we're holding an item on cursor
            if (VeneerItemCursor.IsHoldingItem)
            {
                // Left click - place held item
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (VeneerItemCursor.PlaceItem(_inventory, slot.GridX, slot.GridY))
                    {
                        UpdateAllSlots();
                        // Also update source grid if different
                        if (VeneerItemCursor.SourceInventory != null && VeneerItemCursor.SourceInventory != _inventory)
                        {
                            // Source grid will update on its own in Update()
                        }
                    }
                }
                return;
            }

            // Not holding item - handle normal clicks
            if (eventData.button == PointerEventData.InputButton.Left && slot.Item != null)
            {
                // Shift+Left click - split stack
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (slot.Item.m_stack > 1)
                    {
                        ShowSplitDialog(slot);
                    }
                }
                // Ctrl+Left click - quick move to other inventory (container/player)
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    TryQuickMove(slot);
                }
                // Plain left click - pickup item to cursor
                else
                {
                    VeneerItemCursor.PickupItem(slot.Item, _inventory);
                    UpdateAllSlots();
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right && slot.Item != null)
            {
                // Right click - use item
                player.UseItem(_inventory, slot.Item, true);
            }
        }

        private void TryQuickMove(VeneerItemSlot slot)
        {
            if (slot.Item == null) return;

            // Find target inventory
            var player = Player.m_localPlayer;
            if (player == null) return;

            var playerInventory = player.GetInventory();
            Inventory targetInventory = null;

            // If this is player inventory, move to open container
            // If this is container, move to player inventory
            if (_inventory == playerInventory)
            {
                // Try to find open container
                var containerGrid = FindOpenContainerGrid();
                if (containerGrid != null)
                {
                    targetInventory = containerGrid.GridInventory;
                }
            }
            else
            {
                // This is a container - move to player
                targetInventory = playerInventory;
            }

            if (targetInventory == null) return;

            var item = slot.Item;

            // Clone item and try to add to target inventory
            var clonedItem = item.Clone();
            if (targetInventory.AddItem(clonedItem))
            {
                _inventory.RemoveItem(item);
                UpdateAllSlots();
            }
        }

        private VeneerItemGrid FindOpenContainerGrid()
        {
            // Find VeneerInventoryPanel and get its container grid
#pragma warning disable CS0618 // Using deprecated method for Unity version compatibility
            var panel = FindObjectOfType<Veneer.Vanilla.Replacements.VeneerInventoryPanel>();
#pragma warning restore CS0618
            return panel?.ContainerGrid;
        }

        private void ShowSplitDialog(VeneerItemSlot sourceSlot)
        {
            var item = sourceSlot.Item;
            if (item == null || item.m_stack <= 1) return;

            // Store original stack count for validation
            int originalStack = item.m_stack;

            VeneerSplitDialog.Show(item, item.m_stack, (splitAmount) =>
            {
                // Validate inventory still exists
                if (_inventory == null)
                {
                    Plugin.Log.LogWarning("[VeneerItemGrid] Inventory is null during split callback");
                    return;
                }

                // Validate split amount
                if (splitAmount <= 0 || splitAmount >= item.m_stack)
                {
                    Plugin.Log.LogDebug($"[VeneerItemGrid] Invalid split amount: {splitAmount} (stack: {item.m_stack})");
                    return;
                }

                // Validate item still exists in inventory with expected stack
                if (!_inventory.ContainsItem(item))
                {
                    Plugin.Log.LogWarning("[VeneerItemGrid] Item no longer in inventory during split");
                    UpdateAllSlots();
                    return;
                }

                // Check if stack was modified while dialog was open
                if (item.m_stack != originalStack)
                {
                    Plugin.Log.LogWarning($"[VeneerItemGrid] Item stack changed during split dialog: {originalStack} -> {item.m_stack}");
                    UpdateAllSlots();
                    return;
                }

                // All validation passed - pick up split amount to cursor
                VeneerItemCursor.PickupItem(item, _inventory, splitAmount, true);
                UpdateAllSlots();
            },
            () =>
            {
                // On cancel - just refresh the display
                UpdateAllSlots();
            });
        }

        private void HandleDragStart(VeneerItemSlot slot)
        {
            if (slot.Item == null) return;

            // If already holding on cursor, don't start drag
            if (VeneerItemCursor.IsHoldingItem) return;

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

            // Find what's under the cursor
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            VeneerItemSlot targetSlot = null;
            VeneerItemGrid targetGrid = null;

            foreach (var result in results)
            {
                // Check for slot first
                targetSlot = result.gameObject.GetComponent<VeneerItemSlot>();
                if (targetSlot != null && targetSlot != _draggedSlot)
                {
                    targetGrid = targetSlot.GetComponentInParent<VeneerItemGrid>();
                    break;
                }

                // Check for grid (dropping on empty area of grid)
                var grid = result.gameObject.GetComponent<VeneerItemGrid>();
                if (grid != null)
                {
                    targetGrid = grid;
                    // No specific slot - will need to find empty slot
                }
            }

            var item = _draggedSlot.Item;
            if (item == null)
            {
                _draggedSlot = null;
                return;
            }

            // Dropped on a slot
            if (targetSlot != null && targetGrid != null)
            {
                var targetInventory = targetGrid.GridInventory;
                var sourceInventory = _inventory;

                if (targetInventory != null && sourceInventory != null)
                {
                    // Same inventory - move/swap
                    if (targetInventory == sourceInventory)
                    {
                        sourceInventory.MoveItemToThis(sourceInventory, item, item.m_stack, targetSlot.GridX, targetSlot.GridY);
                    }
                    // Different inventory - transfer
                    else
                    {
                        var targetItem = targetInventory.GetItemAt(targetSlot.GridX, targetSlot.GridY);
                        if (targetItem == null)
                        {
                            // Empty slot - just move
                            targetInventory.MoveItemToThis(sourceInventory, item, item.m_stack, targetSlot.GridX, targetSlot.GridY);
                        }
                        else if (CanStack(item, targetItem))
                        {
                            // Stack
                            int space = targetItem.m_shared.m_maxStackSize - targetItem.m_stack;
                            int toMove = Mathf.Min(item.m_stack, space);
                            if (toMove > 0)
                            {
                                targetItem.m_stack += toMove;
                                if (toMove >= item.m_stack)
                                {
                                    sourceInventory.RemoveItem(item);
                                }
                                else
                                {
                                    item.m_stack -= toMove;
                                }
                                targetInventory.Changed();
                                sourceInventory.Changed();
                            }
                        }
                        else
                        {
                            // Swap across inventories
                            sourceInventory.RemoveItem(item);
                            targetInventory.RemoveItem(targetItem);
                            targetInventory.AddItem(item, item.m_stack, targetSlot.GridX, targetSlot.GridY);
                            sourceInventory.AddItem(targetItem);
                        }
                    }

                    UpdateAllSlots();
                    if (targetGrid != this)
                    {
                        targetGrid.UpdateAllSlots();
                    }
                    OnItemMoved?.Invoke(_draggedSlot, targetSlot);
                }
            }
            // Dropped on grid but not on a slot - find empty slot
            else if (targetGrid != null && targetSlot == null)
            {
                var emptyPos = targetGrid.FindEmptySlot();
                if (emptyPos.x >= 0)
                {
                    targetGrid.GridInventory.MoveItemToThis(_inventory, item, item.m_stack, emptyPos.x, emptyPos.y);
                    UpdateAllSlots();
                    targetGrid.UpdateAllSlots();
                }
            }
            // Dropped outside any grid - drop on ground
            else if (targetGrid == null)
            {
                DropItemToGround(item);
            }

            _draggedSlot = null;
        }

        private void DropItemToGround(ItemDrop.ItemData item)
        {
            if (item == null) return;

            var player = Player.m_localPlayer;
            if (player == null) return;

            player.DropItem(_inventory, item, item.m_stack);
            UpdateAllSlots();
        }

        private static bool CanStack(ItemDrop.ItemData a, ItemDrop.ItemData b)
        {
            if (a == null || b == null) return false;
            return a.m_shared.m_name == b.m_shared.m_name &&
                   a.m_quality == b.m_quality &&
                   a.m_shared.m_maxStackSize > 1;
        }

        /// <summary>
        /// Finds an empty slot in the grid.
        /// </summary>
        public Vector2i FindEmptySlot()
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
            // Handle drops from cursor (when clicking on the grid background)
            if (VeneerItemCursor.IsHoldingItem)
            {
                var emptyPos = FindEmptySlot();
                if (emptyPos.x >= 0)
                {
                    if (VeneerItemCursor.PlaceItem(_inventory, emptyPos.x, emptyPos.y))
                    {
                        UpdateAllSlots();
                    }
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Handle click on grid background (not on a slot)
            if (VeneerItemCursor.IsHoldingItem && eventData.button == PointerEventData.InputButton.Left)
            {
                // Check if click was on a slot
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                foreach (var result in results)
                {
                    if (result.gameObject.GetComponent<VeneerItemSlot>() != null)
                    {
                        return; // Click was on slot, handled by slot
                    }
                }

                // Click was on grid background - find empty slot
                var emptyPos = FindEmptySlot();
                if (emptyPos.x >= 0)
                {
                    if (VeneerItemCursor.PlaceItem(_inventory, emptyPos.x, emptyPos.y))
                    {
                        UpdateAllSlots();
                    }
                }
            }
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
