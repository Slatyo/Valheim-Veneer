using UnityEngine;

namespace Veneer.Core
{
    /// <summary>
    /// Manages cursor visibility by leveraging the vanilla InventoryGui system.
    ///
    /// The vanilla inventory cursor is the ONLY reliable way to get proper cursor
    /// behavior in Valheim. This class wraps InventoryGui.Show()/Hide() to provide
    /// independent cursor control while hiding the vanilla inventory UI.
    ///
    /// Keybind: Alt+Q (configurable)
    /// </summary>
    public static class VeneerCursor
    {
        // Did WE (VeneerCursor) open the inventory just for cursor?
        private static bool _weOpenedInventory = false;

        // Is cursor mode logically active (user toggled it on)?
        private static bool _cursorModeActive = false;

        /// <summary>
        /// Flag to indicate VeneerCursor is triggering InventoryGui.Show() for cursor only.
        /// InventoryPatches checks this to skip showing the inventory panel.
        /// </summary>
        public static bool IsCursorOnlyMode { get; private set; } = false;

        /// <summary>
        /// Whether cursor mode is currently active.
        /// </summary>
        public static bool IsActive => _cursorModeActive;

        /// <summary>
        /// Event fired when cursor mode changes.
        /// </summary>
        public static event System.Action<bool> OnCursorModeChanged;

        /// <summary>
        /// Toggles cursor mode on/off.
        /// When UI is open, this toggles between "cursor active" and "gameplay mode"
        /// while keeping the UI visible.
        /// </summary>
        public static void Toggle()
        {
            Plugin.Log.LogDebug($"VeneerCursor.Toggle: _cursorModeActive={_cursorModeActive}, _weOpenedInventory={_weOpenedInventory}, _forcedHidden={_forcedHidden}");

            // Case 1: We force-hid the cursor while UI was open - restore it
            if (_forcedHidden)
            {
                RestoreCursor();
                return;
            }

            // Case 2: Check if external UI (map, inventory opened by user, menu) is providing cursor
            bool externalUIOpen = IsExternalUIOpen();

            if (externalUIOpen)
            {
                // UI is open - force hide cursor but keep UI visible
                ForceHideCursor();
                return;
            }

            // Case 3: No external UI - normal toggle between cursor mode on/off
            if (_cursorModeActive || _weOpenedInventory)
            {
                // We enabled cursor mode - disable it
                Disable();
            }
            else
            {
                // Enable cursor mode
                Enable();
            }
        }

        /// <summary>
        /// Checks if an external UI (not opened by us) is providing cursor.
        /// </summary>
        private static bool IsExternalUIOpen()
        {
            // Map is open
            if (Minimap.instance != null && Minimap.instance.m_mode == Minimap.MapMode.Large)
                return true;

            // Inventory opened by user (not by us for cursor)
            if (InventoryGui.IsVisible() && !_weOpenedInventory)
                return true;

            // Menu, store, text input
            if (Menu.IsVisible()) return true;
            if (TextInput.IsVisible()) return true;
            if (StoreGui.IsVisible()) return true;

            return false;
        }

        // Track if we've force-hidden the cursor while UI is open
        private static bool _forcedHidden = false;

        /// <summary>
        /// Whether cursor is force-hidden (UI open but cursor disabled for gameplay).
        /// Used by GameCameraPatches to override vanilla cursor control.
        /// </summary>
        public static bool IsForcedHidden => _forcedHidden;

        /// <summary>
        /// Force hides cursor and unlocks player controls, even while UI is open.
        /// </summary>
        private static void ForceHideCursor()
        {
            _forcedHidden = true;
            _cursorModeActive = false;

            // Hide cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            Plugin.Log.LogInfo("VeneerCursor: Force hidden cursor (UI still open)");
            OnCursorModeChanged?.Invoke(false);
        }

        /// <summary>
        /// Restores cursor after force-hide.
        /// </summary>
        private static void RestoreCursor()
        {
            _forcedHidden = false;
            _cursorModeActive = true;

            // Show cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Plugin.Log.LogInfo("VeneerCursor: Restored cursor");
            OnCursorModeChanged?.Invoke(true);
        }

        /// <summary>
        /// Checks if cursor is currently visible.
        /// </summary>
        private static bool IsCursorCurrentlyVisible()
        {
            return Cursor.visible || Cursor.lockState == CursorLockMode.None;
        }

        /// <summary>
        /// Enables cursor mode by showing the vanilla inventory (then hiding its UI).
        /// This gives us the exact same cursor behavior as opening inventory with Tab.
        /// </summary>
        public static void Enable()
        {
            if (_cursorModeActive) return;
            if (!IsInGame()) return;

            var inventoryGui = InventoryGui.instance;
            if (inventoryGui == null)
            {
                Plugin.Log.LogWarning("VeneerCursor: InventoryGui.instance is null");
                return;
            }

            // Check if cursor is already available (inventory/map is open)
            bool cursorAlreadyAvailable = IsCursorAvailable();

            if (!cursorAlreadyAvailable)
            {
                // We need to open inventory to get cursor
                IsCursorOnlyMode = true;
                inventoryGui.Show(null);
                IsCursorOnlyMode = false;
                _weOpenedInventory = true;
                Plugin.Log.LogInfo("VeneerCursor: Enabled via InventoryGui.Show()");
            }
            else
            {
                // Cursor already available from inventory/map, just mark as active
                _weOpenedInventory = false;
                Plugin.Log.LogDebug("VeneerCursor: Cursor already available, just activating mode");
            }

            _cursorModeActive = true;
            OnCursorModeChanged?.Invoke(true);
        }

        /// <summary>
        /// Disables cursor mode by hiding the vanilla inventory (if we opened it).
        /// </summary>
        public static void Disable()
        {
            if (!_cursorModeActive) return;

            Plugin.Log.LogDebug($"VeneerCursor.Disable: _weOpenedInventory={_weOpenedInventory}");

            // Only hide inventory if WE were the ones who opened it for cursor
            if (_weOpenedInventory)
            {
                var inventoryGui = InventoryGui.instance;
                if (inventoryGui != null)
                {
                    inventoryGui.Hide();
                    Plugin.Log.LogInfo("VeneerCursor: Disabled via InventoryGui.Hide()");
                }
                _weOpenedInventory = false;
            }
            else
            {
                // We didn't open inventory, so don't close it
                // Just deactivate cursor mode - cursor will naturally go away when user closes inventory/map
                Plugin.Log.LogDebug("VeneerCursor: Deactivating mode (inventory/map still open)");
            }

            _cursorModeActive = false;
            OnCursorModeChanged?.Invoke(false);
        }

        /// <summary>
        /// Checks if cursor is currently available (any UI that provides cursor is open).
        /// </summary>
        private static bool IsCursorAvailable()
        {
            // Check inventory
            if (InventoryGui.IsVisible()) return true;

            // Check map
            if (Minimap.instance != null && Minimap.instance.m_mode == Minimap.MapMode.Large) return true;

            // Check other UIs that give cursor
            if (Menu.IsVisible()) return true;
            if (TextInput.IsVisible()) return true;
            if (StoreGui.IsVisible()) return true;

            return false;
        }

        /// <summary>
        /// Checks if we're in actual gameplay (not menu/loading).
        /// </summary>
        private static bool IsInGame()
        {
            return Player.m_localPlayer != null && Game.instance != null;
        }

        /// <summary>
        /// Called when inventory is opened externally (Tab key).
        /// </summary>
        public static void OnInventoryOpened()
        {
            // If we had opened inventory for cursor, user is now taking over
            if (_weOpenedInventory)
            {
                _weOpenedInventory = false;
            }

            // Cursor is now active due to inventory
            if (!_cursorModeActive)
            {
                _cursorModeActive = true;
                OnCursorModeChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// Called when inventory is closed externally.
        /// </summary>
        public static void OnInventoryClosed()
        {
            // Check if cursor is still available from other sources (map, menu, etc.)
            bool cursorStillAvailable = IsCursorAvailable();

            if (!cursorStillAvailable && _cursorModeActive)
            {
                _cursorModeActive = false;
                _weOpenedInventory = false;
                OnCursorModeChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Resets state (call on scene unload).
        /// </summary>
        public static void Reset()
        {
            _cursorModeActive = false;
            _weOpenedInventory = false;
            _forcedHidden = false;
        }
    }
}
