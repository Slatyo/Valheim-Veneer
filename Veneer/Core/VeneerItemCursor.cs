using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Theme;

namespace Veneer.Core
{
    /// <summary>
    /// Modifier keys for inventory operations.
    /// </summary>
    public enum ItemModifier
    {
        None = 0,
        Move = 1,
        Split = 2,
        Select = 3
    }

    /// <summary>
    /// Bridges Veneer's inventory UI with vanilla InventoryGui's drag system.
    /// Instead of reimplementing drag/drop, we hook into vanilla's m_dragItem system
    /// and call vanilla's OnSelectedItem for all placement logic.
    /// This ensures all vanilla features (equip, drop, stack, swap) work correctly.
    /// </summary>
    public static class VeneerItemCursor
    {
        // Reflection cache for vanilla InventoryGui private fields and methods
        private static FieldInfo _dragItemField;
        private static FieldInfo _dragInventoryField;
        private static FieldInfo _dragAmountField;
        private static FieldInfo _dragGoField;
        private static MethodInfo _setUpDragItemMethod;
        private static MethodInfo _onSelectedItemMethod;

        private static bool _initialized;

        /// <summary>
        /// The item currently held on cursor (from vanilla).
        /// </summary>
        public static ItemDrop.ItemData HeldItem => GetDragItem();

        /// <summary>
        /// The inventory the held item came from.
        /// </summary>
        public static Inventory SourceInventory => GetDragInventory();

        /// <summary>
        /// Amount of the item being held.
        /// </summary>
        public static int HeldAmount => GetDragAmount();

        /// <summary>
        /// Whether an item is currently held.
        /// </summary>
        public static bool IsHoldingItem => GetDragItem() != null;

        /// <summary>
        /// Called when an item is picked up to cursor.
        /// </summary>
        public static event Action<ItemDrop.ItemData, Inventory> OnItemPickedUp;

        /// <summary>
        /// Called when a held item is placed.
        /// </summary>
        public static event Action<ItemDrop.ItemData, Inventory> OnItemPlaced;

        /// <summary>
        /// Called when a held item is dropped to ground.
        /// </summary>
        public static event Action<ItemDrop.ItemData> OnItemDropped;

        private static void EnsureInitialized()
        {
            if (_initialized) return;

            var guiType = typeof(InventoryGui);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var publicFlags = BindingFlags.Public | BindingFlags.Instance;

            _dragItemField = guiType.GetField("m_dragItem", flags);
            _dragInventoryField = guiType.GetField("m_dragInventory", flags);
            _dragAmountField = guiType.GetField("m_dragAmount", flags);
            _dragGoField = guiType.GetField("m_dragGo", flags);
            _setUpDragItemMethod = guiType.GetMethod("SetupDragItem", flags);

            // OnSelectedItem is the method vanilla calls when a slot is clicked
            // Signature: void OnSelectedItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, Modifier mod)
            _onSelectedItemMethod = guiType.GetMethod("OnSelectedItem", flags)
                                    ?? guiType.GetMethod("OnSelectedItem", publicFlags);

            _initialized = true;
        }

        private static ItemDrop.ItemData GetDragItem()
        {
            if (InventoryGui.instance == null) return null;
            EnsureInitialized();
            return _dragItemField?.GetValue(InventoryGui.instance) as ItemDrop.ItemData;
        }

        private static Inventory GetDragInventory()
        {
            if (InventoryGui.instance == null) return null;
            EnsureInitialized();
            return _dragInventoryField?.GetValue(InventoryGui.instance) as Inventory;
        }

        private static int GetDragAmount()
        {
            if (InventoryGui.instance == null) return 0;
            EnsureInitialized();
            var val = _dragAmountField?.GetValue(InventoryGui.instance);
            return val is int i ? i : 0;
        }

        /// <summary>
        /// Simulates a slot click using vanilla's OnSelectedItem method.
        /// This handles ALL item operations: pickup, place, stack, swap, equip/unequip.
        /// </summary>
        /// <param name="grid">The vanilla InventoryGrid (or null to use player grid)</param>
        /// <param name="inventory">The inventory being clicked</param>
        /// <param name="item">The item at the clicked position (or null for empty slot)</param>
        /// <param name="pos">Grid position clicked</param>
        /// <param name="mod">Modifier key (Split for shift-click)</param>
        public static void SimulateSlotClick(InventoryGrid grid, Inventory inventory, ItemDrop.ItemData item, Vector2i pos, ItemModifier mod = ItemModifier.None)
        {
            if (InventoryGui.instance == null) return;
            EnsureInitialized();

            if (_onSelectedItemMethod != null && grid != null)
            {
                // Call vanilla's OnSelectedItem directly
                _onSelectedItemMethod.Invoke(InventoryGui.instance, new object[] { grid, item, pos, mod });
            }
            else
            {
                // Fallback: handle manually using SetupDragItem
                HandleSlotClickFallback(inventory, item, pos, mod);
            }
        }

        /// <summary>
        /// Handles slot click when vanilla's OnSelectedItem isn't available.
        /// Uses SetupDragItem for pickup and MoveItemToThis for placement.
        /// </summary>
        private static void HandleSlotClickFallback(Inventory inventory, ItemDrop.ItemData item, Vector2i pos, ItemModifier mod)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            bool isHolding = IsHoldingItem;
            var heldItem = HeldItem;
            var sourceInv = SourceInventory;
            int heldAmount = HeldAmount;

            if (isHolding)
            {
                // Validate item still exists and has expected stack
                if (!sourceInv.ContainsItem(heldItem))
                {
                    Plugin.Log.LogWarning("[VeneerItemCursor] Held item no longer in source inventory");
                    ClearHold();
                    return;
                }

                // Placing held item
                if (item == null)
                {
                    // Empty slot - place item
                    if (heldAmount < heldItem.m_stack)
                    {
                        // Partial stack - create new item at target position
                        // Don't clone - create fresh to avoid reference issues
                        var splitItem = heldItem.Clone();
                        splitItem.m_stack = heldAmount;

                        // Use the overload that places at specific position
                        if (inventory.AddItem(splitItem, heldAmount, pos.x, pos.y))
                        {
                            heldItem.m_stack -= heldAmount;
                            sourceInv.Changed();
                            ClearHold();
                            OnItemPlaced?.Invoke(splitItem, inventory);
                        }
                        else
                        {
                            Plugin.Log.LogWarning("[VeneerItemCursor] Failed to add split item to target inventory");
                        }
                    }
                    else
                    {
                        // Full stack
                        inventory.MoveItemToThis(sourceInv, heldItem, heldAmount, pos.x, pos.y);
                        ClearHold();
                        OnItemPlaced?.Invoke(heldItem, inventory);
                    }
                }
                else if (CanStack(heldItem, item))
                {
                    // Stack onto existing
                    int space = item.m_shared.m_maxStackSize - item.m_stack;
                    int toAdd = Mathf.Min(heldAmount, space);
                    if (toAdd > 0)
                    {
                        item.m_stack += toAdd;
                        inventory.Changed();

                        if (heldAmount <= toAdd)
                        {
                            // All held amount was placed
                            if (heldAmount >= heldItem.m_stack)
                            {
                                // Placing entire item - remove from source
                                sourceInv.RemoveItem(heldItem);
                            }
                            else
                            {
                                // Placing partial - reduce source stack
                                heldItem.m_stack -= heldAmount;
                                sourceInv.Changed();
                            }
                            ClearHold();
                            OnItemPlaced?.Invoke(heldItem, inventory);
                        }
                        else
                        {
                            // Only partial amount fit - reduce source and update held amount
                            if (heldAmount >= heldItem.m_stack)
                            {
                                // Was holding full stack, reduce it
                                heldItem.m_stack -= toAdd;
                                sourceInv.Changed();
                            }
                            else
                            {
                                // Was holding partial, reduce source
                                heldItem.m_stack -= toAdd;
                                sourceInv.Changed();
                            }
                            _dragAmountField?.SetValue(InventoryGui.instance, heldAmount - toAdd);
                        }
                    }
                }
                else if (heldAmount >= heldItem.m_stack)
                {
                    // Swap items - only allowed when holding full stack
                    SwapItems(sourceInv, heldItem, inventory, item, pos);
                }
                else
                {
                    // Holding partial stack, can't swap - do nothing
                    Plugin.Log.LogDebug("[VeneerItemCursor] Can't swap with partial stack");
                }
            }
            else if (item != null)
            {
                // Picking up item - unequip if equipped
                if (item.m_equipped)
                {
                    player.UnequipItem(item, false);
                }

                int amount = item.m_stack;
                if (mod == ItemModifier.Split && item.m_stack > 1)
                {
                    amount = Mathf.CeilToInt(item.m_stack / 2f);
                }

                if (_setUpDragItemMethod != null)
                {
                    _setUpDragItemMethod.Invoke(InventoryGui.instance, new object[] { item, inventory, amount });
                }
                else
                {
                    _dragItemField?.SetValue(InventoryGui.instance, item);
                    _dragInventoryField?.SetValue(InventoryGui.instance, inventory);
                    _dragAmountField?.SetValue(InventoryGui.instance, amount);
                }

                // Ensure drag icon renders above Veneer UI
                EnsureDragIconOnTop();

                OnItemPickedUp?.Invoke(item, inventory);
            }
        }

        private static void SwapItems(Inventory sourceInv, ItemDrop.ItemData heldItem, Inventory targetInv, ItemDrop.ItemData targetItem, Vector2i targetPos)
        {
            if (sourceInv == targetInv)
            {
                // Same inventory swap - just swap grid positions
                Vector2i srcPos = heldItem.m_gridPos;
                heldItem.m_gridPos = targetPos;
                targetItem.m_gridPos = srcPos;
                sourceInv.Changed();
            }
            else
            {
                // Cross-inventory swap
                Vector2i srcPos = heldItem.m_gridPos;
                sourceInv.RemoveItem(heldItem);
                targetInv.RemoveItem(targetItem);

                heldItem.m_gridPos = targetPos;
                targetItem.m_gridPos = srcPos;

                targetInv.AddItem(heldItem);
                sourceInv.AddItem(targetItem);
            }

            // Now holding the swapped item
            if (_setUpDragItemMethod != null)
            {
                _setUpDragItemMethod.Invoke(InventoryGui.instance, new object[] { targetItem, targetInv, targetItem.m_stack });
            }
            else
            {
                _dragItemField?.SetValue(InventoryGui.instance, targetItem);
                _dragInventoryField?.SetValue(InventoryGui.instance, targetInv);
                _dragAmountField?.SetValue(InventoryGui.instance, targetItem.m_stack);
            }

            OnItemPlaced?.Invoke(heldItem, targetInv);
        }

        /// <summary>
        /// Picks up an item to the cursor using vanilla's system.
        /// </summary>
        /// <param name="item">The item to pick up</param>
        /// <param name="inventory">The inventory containing the item</param>
        /// <param name="amount">Amount to pick up (-1 for full stack)</param>
        /// <param name="isSplit">If true, this is a split operation (partial stack pickup)</param>
        public static void PickupItem(ItemDrop.ItemData item, Inventory inventory, int amount = -1, bool isSplit = false)
        {
            if (item == null || inventory == null) return;
            if (InventoryGui.instance == null) return;

            // If already holding something, can't pickup
            if (IsHoldingItem) return;

            var player = Player.m_localPlayer;
            if (player == null) return;

            EnsureInitialized();

            // If item is equipped, unequip it first
            if (item.m_equipped)
            {
                player.UnequipItem(item, false);
            }

            int pickupAmount = amount > 0 ? Mathf.Min(amount, item.m_stack) : item.m_stack;

            // Use vanilla's SetupDragItem method
            if (_setUpDragItemMethod != null)
            {
                _setUpDragItemMethod.Invoke(InventoryGui.instance, new object[] { item, inventory, pickupAmount });
            }
            else
            {
                // Fallback: set fields directly
                _dragItemField?.SetValue(InventoryGui.instance, item);
                _dragInventoryField?.SetValue(InventoryGui.instance, inventory);
                _dragAmountField?.SetValue(InventoryGui.instance, pickupAmount);
            }

            // Ensure drag icon renders above Veneer UI
            EnsureDragIconOnTop();

            OnItemPickedUp?.Invoke(item, inventory);
        }

        /// <summary>
        /// Ensures vanilla's drag icon renders above Veneer UI elements.
        /// </summary>
        private static void EnsureDragIconOnTop()
        {
            var dragGo = _dragGoField?.GetValue(InventoryGui.instance) as GameObject;
            if (dragGo == null) return;

            // Add/update Canvas component to override sorting order
            var canvas = dragGo.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = dragGo.AddComponent<Canvas>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = VeneerLayers.DragPreview;

            // Ensure it doesn't block raycasts
            var canvasGroup = dragGo.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dragGo.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Places held item into a target slot/inventory.
        /// Uses SimulateSlotClick for proper vanilla behavior.
        /// </summary>
        public static bool PlaceItem(Inventory targetInventory, int x, int y)
        {
            if (!IsHoldingItem || targetInventory == null) return false;
            if (InventoryGui.instance == null) return false;

            var item = HeldItem;
            var targetItem = targetInventory.GetItemAt(x, y);
            var pos = new Vector2i(x, y);

            // Use fallback since we don't have the InventoryGrid reference
            HandleSlotClickFallback(targetInventory, targetItem, pos, ItemModifier.None);

            return !IsHoldingItem; // Returns true if item was fully placed
        }

        /// <summary>
        /// Drops the held item on the ground.
        /// </summary>
        public static void DropHeldItem()
        {
            if (!IsHoldingItem) return;

            var player = Player.m_localPlayer;
            if (player == null) return;

            var item = HeldItem;
            var sourceInv = SourceInventory;
            int amount = HeldAmount;

            if (item == null) return;

            // Check if item is still in inventory
            bool itemInInventory = sourceInv != null && sourceInv.ContainsItem(item);

            if (itemInInventory)
            {
                // Item still in inventory - use vanilla drop
                player.DropItem(sourceInv, item, amount);
            }
            else
            {
                // Item was already removed - create drop manually
                CreateItemDrop(item, amount, player);
            }

            OnItemDropped?.Invoke(item);
            ClearHold();
        }

        private static void CreateItemDrop(ItemDrop.ItemData item, int amount, Player player)
        {
            Vector3 dropPos = player.transform.position + player.transform.forward * 1.5f + Vector3.up * 0.5f;

            // Find the prefab
            string prefabName = item.m_dropPrefab?.name;
            if (string.IsNullOrEmpty(prefabName))
            {
                prefabName = item.m_shared.m_name;
            }

            GameObject prefab = ObjectDB.instance?.GetItemPrefab(prefabName);
            if (prefab == null && item.m_dropPrefab != null)
            {
                prefab = item.m_dropPrefab;
            }

            if (prefab != null)
            {
                GameObject dropObj = UnityEngine.Object.Instantiate(prefab, dropPos, Quaternion.identity);
                ItemDrop itemDrop = dropObj.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    itemDrop.m_itemData = item.Clone();
                    itemDrop.m_itemData.m_stack = amount;

#pragma warning disable CS0618 // Suppress velocity obsolete warning
                    Rigidbody rb = dropObj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = (player.transform.forward + Vector3.up * 0.5f).normalized * 4f;
                    }
#pragma warning restore CS0618
                }
            }
        }

        /// <summary>
        /// Cancels the current hold, returning item to source.
        /// </summary>
        public static void CancelHold()
        {
            if (!IsHoldingItem) return;

            var item = HeldItem;
            var sourceInv = SourceInventory;
            int amount = HeldAmount;

            if (item != null && sourceInv != null)
            {
                // Check if item still exists in inventory
                if (!sourceInv.ContainsItem(item))
                {
                    // Item was removed - add it back
                    var returnItem = item.Clone();
                    returnItem.m_stack = amount;
                    sourceInv.AddItem(returnItem);
                }
                else if (item.m_stack < amount)
                {
                    // Stack was split - restore amount
                    item.m_stack += amount;
                    sourceInv.Changed();
                }
            }

            ClearHold();
        }

        /// <summary>
        /// Clears the held item without returning it.
        /// </summary>
        public static void ClearHold()
        {
            if (InventoryGui.instance == null) return;
            EnsureInitialized();

            _dragItemField?.SetValue(InventoryGui.instance, null);
            _dragInventoryField?.SetValue(InventoryGui.instance, null);
            _dragAmountField?.SetValue(InventoryGui.instance, 0);

            // Hide vanilla drag icon
            var dragGo = _dragGoField?.GetValue(InventoryGui.instance) as GameObject;
            if (dragGo != null)
            {
                dragGo.SetActive(false);
            }
        }

        private static bool CanStack(ItemDrop.ItemData a, ItemDrop.ItemData b)
        {
            if (a == null || b == null) return false;
            return a.m_shared.m_name == b.m_shared.m_name &&
                   a.m_quality == b.m_quality &&
                   a.m_shared.m_maxStackSize > 1;
        }

        /// <summary>
        /// Updates the drag visual. Call from Update if showing custom cursor.
        /// </summary>
        public static void UpdateDragVisual()
        {
            if (!IsHoldingItem) return;
            if (InventoryGui.instance == null) return;

            // Let vanilla handle the drag visual via UpdateItemDrag
        }
    }
}
