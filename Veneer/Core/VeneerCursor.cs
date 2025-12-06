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
    ///
    /// Design: This class uses a simple state machine with explicit state tracking.
    /// Toggle() always works reliably by checking actual game state.
    /// </summary>
    public static class VeneerCursor
    {
        /// <summary>
        /// Our internal state - what WE think the cursor state is.
        /// </summary>
        private enum CursorState
        {
            /// <summary>No cursor control active - vanilla handles everything.</summary>
            Inactive,

            /// <summary>We opened inventory just for cursor (cursor-only mode).</summary>
            CursorOnlyMode,

            /// <summary>Cursor force-hidden while UI is open (gameplay mode with UI visible).</summary>
            ForcedHidden
        }

        private static CursorState _state = CursorState.Inactive;

        /// <summary>
        /// Flag to indicate VeneerCursor is triggering InventoryGui.Show() for cursor only.
        /// InventoryPatches checks this to skip showing the inventory panel.
        /// </summary>
        public static bool IsCursorOnlyMode => _state == CursorState.CursorOnlyMode;

        /// <summary>
        /// Whether cursor mode is currently active (cursor visible due to our control).
        /// </summary>
        public static bool IsActive => _state == CursorState.CursorOnlyMode;

        /// <summary>
        /// Whether cursor is force-hidden (UI open but cursor disabled for gameplay).
        /// Used by GameCameraPatches to override vanilla cursor control.
        /// </summary>
        public static bool IsForcedHidden => _state == CursorState.ForcedHidden;

        /// <summary>
        /// Event fired when cursor mode changes.
        /// </summary>
        public static event System.Action<bool> OnCursorModeChanged;

        /// <summary>
        /// Toggles cursor mode on/off.
        /// This is the main entry point - it always works by checking actual state.
        /// </summary>
        public static void Toggle()
        {
            // First, sync our state with reality (in case vanilla changed something)
            SyncWithVanillaState();

            bool cursorCurrentlyVisible = IsCursorVisible();
            bool externalUIOpen = IsExternalUIOpen();

            Plugin.Log.LogDebug($"VeneerCursor.Toggle: state={_state}, cursorVisible={cursorCurrentlyVisible}, externalUI={externalUIOpen}");

            // Simple toggle logic based on current actual state
            if (cursorCurrentlyVisible)
            {
                // Cursor is visible - hide it
                HideCursor(externalUIOpen);
            }
            else
            {
                // Cursor is hidden - show it
                ShowCursor();
            }
        }

        /// <summary>
        /// Shows the cursor by opening inventory in cursor-only mode.
        /// </summary>
        private static void ShowCursor()
        {
            if (!IsInGame()) return;

            var inventoryGui = InventoryGui.instance;
            if (inventoryGui == null)
            {
                Plugin.Log.LogWarning("VeneerCursor: InventoryGui.instance is null");
                return;
            }

            // If cursor is already available from external UI, just clear our state
            if (IsCursorVisible())
            {
                _state = CursorState.Inactive;
                Plugin.Log.LogDebug("VeneerCursor: Cursor already visible, clearing state");
                OnCursorModeChanged?.Invoke(true);
                return;
            }

            // Open inventory for cursor only
            _state = CursorState.CursorOnlyMode;
            inventoryGui.Show(null);

            Plugin.Log.LogInfo("VeneerCursor: Enabled via InventoryGui.Show()");
            OnCursorModeChanged?.Invoke(true);
        }

        /// <summary>
        /// Hides the cursor.
        /// </summary>
        /// <param name="keepUIOpen">If true, UI stays visible but cursor is hidden (force-hide mode).</param>
        private static void HideCursor(bool keepUIOpen)
        {
            if (keepUIOpen)
            {
                // External UI is open - force hide cursor but keep UI visible
                _state = CursorState.ForcedHidden;

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                Plugin.Log.LogInfo("VeneerCursor: Force hidden cursor (UI still open)");
                OnCursorModeChanged?.Invoke(false);
            }
            else if (_state == CursorState.CursorOnlyMode)
            {
                // We opened inventory for cursor - close it
                var inventoryGui = InventoryGui.instance;
                if (inventoryGui != null)
                {
                    inventoryGui.Hide();
                    Plugin.Log.LogInfo("VeneerCursor: Disabled via InventoryGui.Hide()");
                }

                _state = CursorState.Inactive;
                OnCursorModeChanged?.Invoke(false);
            }
            else if (_state == CursorState.ForcedHidden)
            {
                // Already force-hidden, toggle back to showing cursor
                _state = CursorState.Inactive;

                // Restore cursor (vanilla UI will handle it since UI is still open)
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                Plugin.Log.LogInfo("VeneerCursor: Restored cursor from force-hidden");
                OnCursorModeChanged?.Invoke(true);
            }
            else
            {
                // Cursor visible from external source (inventory/map opened by user)
                // Force hide it
                _state = CursorState.ForcedHidden;

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                Plugin.Log.LogInfo("VeneerCursor: Force hidden cursor (external UI)");
                OnCursorModeChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Syncs our internal state with vanilla game state.
        /// Called at the start of Toggle() to ensure consistency.
        /// </summary>
        private static void SyncWithVanillaState()
        {
            bool vanillaCursorVisible = IsCursorVisible();
            bool externalUIOpen = IsExternalUIOpen();

            // If we think we're in CursorOnlyMode but inventory isn't actually visible from us
            if (_state == CursorState.CursorOnlyMode)
            {
                // Check if inventory is still open and it's actually from our cursor-only mode
                if (!InventoryGui.IsVisible())
                {
                    // Inventory was closed externally - reset our state
                    Plugin.Log.LogDebug("VeneerCursor.Sync: CursorOnlyMode but inventory closed - resetting");
                    _state = CursorState.Inactive;
                }
            }

            // If we think cursor is force-hidden but no UI is open anymore
            if (_state == CursorState.ForcedHidden)
            {
                if (!externalUIOpen && !InventoryGui.IsVisible())
                {
                    // UI was closed - reset our state
                    Plugin.Log.LogDebug("VeneerCursor.Sync: ForcedHidden but no UI open - resetting");
                    _state = CursorState.Inactive;

                    // Cursor should already be hidden by vanilla, but ensure it
                    if (vanillaCursorVisible)
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                }
            }

            // If we think we're inactive but cursor is force-hidden state is still applied
            if (_state == CursorState.Inactive && !vanillaCursorVisible && !externalUIOpen && !InventoryGui.IsVisible())
            {
                // This is the expected state - game has no UI open and cursor is hidden
            }
        }

        /// <summary>
        /// Checks if cursor is currently visible.
        /// </summary>
        private static bool IsCursorVisible()
        {
            return Cursor.visible && Cursor.lockState != CursorLockMode.Locked;
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
            if (InventoryGui.IsVisible() && _state != CursorState.CursorOnlyMode)
                return true;

            // Menu, store, text input
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
            Plugin.Log.LogDebug($"VeneerCursor.OnInventoryOpened: state={_state}");

            // If we were in cursor-only mode and user opened inventory properly,
            // transition to inactive (vanilla now controls cursor)
            if (_state == CursorState.CursorOnlyMode)
            {
                _state = CursorState.Inactive;
                Plugin.Log.LogDebug("VeneerCursor: User opened inventory, transitioning from CursorOnlyMode to Inactive");
            }

            // If we were force-hiding, stay in that state - user wants gameplay mode
            // They can toggle again to restore cursor
        }

        /// <summary>
        /// Called when inventory is closed externally.
        /// </summary>
        public static void OnInventoryClosed()
        {
            Plugin.Log.LogDebug($"VeneerCursor.OnInventoryClosed: state={_state}");

            // Check if cursor is still available from other sources
            bool cursorStillAvailable = false;
            if (Minimap.instance != null && Minimap.instance.m_mode == Minimap.MapMode.Large)
                cursorStillAvailable = true;
            if (Menu.IsVisible() || TextInput.IsVisible() || StoreGui.IsVisible())
                cursorStillAvailable = true;

            if (_state == CursorState.CursorOnlyMode)
            {
                // Our cursor-only mode inventory was closed
                _state = CursorState.Inactive;
                OnCursorModeChanged?.Invoke(false);
            }
            else if (_state == CursorState.ForcedHidden && !cursorStillAvailable)
            {
                // UI that we were force-hiding cursor for is now closed
                _state = CursorState.Inactive;
                // Cursor is already hidden, no need to do anything
            }
        }

        /// <summary>
        /// Called when map is opened/closed.
        /// </summary>
        public static void OnMapStateChanged(bool isOpen)
        {
            Plugin.Log.LogDebug($"VeneerCursor.OnMapStateChanged: isOpen={isOpen}, state={_state}");

            if (!isOpen && _state == CursorState.ForcedHidden)
            {
                // Map was closed while we were force-hiding cursor
                // Check if any other UI is still open
                bool otherUIOpen = InventoryGui.IsVisible() || Menu.IsVisible() ||
                                   TextInput.IsVisible() || StoreGui.IsVisible();

                if (!otherUIOpen)
                {
                    _state = CursorState.Inactive;
                    Plugin.Log.LogDebug("VeneerCursor: Map closed, no other UI - resetting to Inactive");
                }
            }
        }

        /// <summary>
        /// Resets state (call on scene unload).
        /// </summary>
        public static void Reset()
        {
            _state = CursorState.Inactive;
            Plugin.Log.LogDebug("VeneerCursor: State reset");
        }

        /// <summary>
        /// Enables cursor mode directly (for programmatic use).
        /// Prefer using Toggle() for user input.
        /// </summary>
        public static void Enable()
        {
            if (_state != CursorState.Inactive) return;
            ShowCursor();
        }

        /// <summary>
        /// Disables cursor mode directly (for programmatic use).
        /// Prefer using Toggle() for user input.
        /// </summary>
        public static void Disable()
        {
            if (_state == CursorState.Inactive) return;
            HideCursor(IsExternalUIOpen());
        }
    }
}
