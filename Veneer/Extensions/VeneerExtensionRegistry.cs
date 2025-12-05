using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Veneer.Extensions
{
    /// <summary>
    /// Central registry for all Veneer UI extensions.
    /// Other mods register their extensions here, and Veneer components
    /// query the registry to apply extensions.
    /// </summary>
    public static class VeneerExtensionRegistry
    {
        #region Storage

        private static readonly List<IQuickBarExtension> _quickBarExtensions = new List<IQuickBarExtension>();
        private static readonly List<IInventoryExtension> _inventoryExtensions = new List<IInventoryExtension>();
        private static readonly List<IWindowExtension> _windowExtensions = new List<IWindowExtension>();
        private static readonly List<IHotbarExtension> _hotbarExtensions = new List<IHotbarExtension>();
        private static readonly List<IHudExtension> _hudExtensions = new List<IHudExtension>();

        #endregion

        #region Registration - QuickBar

        /// <summary>
        /// Registers a QuickBar extension.
        /// </summary>
        public static void RegisterQuickBarExtension(IQuickBarExtension extension)
        {
            if (extension == null) return;
            InsertByPriority(_quickBarExtensions, extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Registered QuickBar extension: {extension.ExtensionId} (priority {extension.Priority})");
        }

        /// <summary>
        /// Unregisters a QuickBar extension.
        /// </summary>
        public static void UnregisterQuickBarExtension(IQuickBarExtension extension)
        {
            if (extension == null) return;
            _quickBarExtensions.Remove(extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Unregistered QuickBar extension: {extension.ExtensionId}");
        }

        /// <summary>
        /// Gets all registered QuickBar extensions.
        /// </summary>
        public static IReadOnlyList<IQuickBarExtension> GetQuickBarExtensions() => _quickBarExtensions;

        #endregion

        #region Registration - Inventory

        /// <summary>
        /// Registers an Inventory extension.
        /// </summary>
        public static void RegisterInventoryExtension(IInventoryExtension extension)
        {
            if (extension == null) return;
            InsertByPriority(_inventoryExtensions, extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Registered Inventory extension: {extension.ExtensionId} (priority {extension.Priority})");
        }

        /// <summary>
        /// Unregisters an Inventory extension.
        /// </summary>
        public static void UnregisterInventoryExtension(IInventoryExtension extension)
        {
            if (extension == null) return;
            _inventoryExtensions.Remove(extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Unregistered Inventory extension: {extension.ExtensionId}");
        }

        /// <summary>
        /// Gets all registered Inventory extensions.
        /// </summary>
        public static IReadOnlyList<IInventoryExtension> GetInventoryExtensions() => _inventoryExtensions;

        #endregion

        #region Registration - Window

        /// <summary>
        /// Registers a Window extension.
        /// </summary>
        public static void RegisterWindowExtension(IWindowExtension extension)
        {
            if (extension == null) return;
            InsertByPriority(_windowExtensions, extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Registered Window extension: {extension.ExtensionId} (priority {extension.Priority})");
        }

        /// <summary>
        /// Unregisters a Window extension.
        /// </summary>
        public static void UnregisterWindowExtension(IWindowExtension extension)
        {
            if (extension == null) return;
            _windowExtensions.Remove(extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Unregistered Window extension: {extension.ExtensionId}");
        }

        /// <summary>
        /// Gets all registered Window extensions.
        /// </summary>
        public static IReadOnlyList<IWindowExtension> GetWindowExtensions() => _windowExtensions;

        /// <summary>
        /// Gets Window extensions that apply to a specific window.
        /// </summary>
        public static IEnumerable<IWindowExtension> GetWindowExtensionsFor(string windowId)
        {
            return _windowExtensions.Where(e => e.AppliesTo(windowId));
        }

        #endregion

        #region Registration - Hotbar

        /// <summary>
        /// Registers a Hotbar extension.
        /// </summary>
        public static void RegisterHotbarExtension(IHotbarExtension extension)
        {
            if (extension == null) return;
            InsertByPriority(_hotbarExtensions, extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Registered Hotbar extension: {extension.ExtensionId} (priority {extension.Priority})");
        }

        /// <summary>
        /// Unregisters a Hotbar extension.
        /// </summary>
        public static void UnregisterHotbarExtension(IHotbarExtension extension)
        {
            if (extension == null) return;
            _hotbarExtensions.Remove(extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Unregistered Hotbar extension: {extension.ExtensionId}");
        }

        /// <summary>
        /// Gets all registered Hotbar extensions.
        /// </summary>
        public static IReadOnlyList<IHotbarExtension> GetHotbarExtensions() => _hotbarExtensions;

        #endregion

        #region Registration - HUD

        /// <summary>
        /// Registers a HUD extension.
        /// </summary>
        public static void RegisterHudExtension(IHudExtension extension)
        {
            if (extension == null) return;
            InsertByPriority(_hudExtensions, extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Registered HUD extension: {extension.ExtensionId} (priority {extension.Priority})");
        }

        /// <summary>
        /// Unregisters a HUD extension.
        /// </summary>
        public static void UnregisterHudExtension(IHudExtension extension)
        {
            if (extension == null) return;
            _hudExtensions.Remove(extension);
            Plugin.Log.LogInfo($"[VeneerExtensions] Unregistered HUD extension: {extension.ExtensionId}");
        }

        /// <summary>
        /// Gets all registered HUD extensions.
        /// </summary>
        public static IReadOnlyList<IHudExtension> GetHudExtensions() => _hudExtensions;

        #endregion

        #region Notification Methods (called by Veneer components)

        /// <summary>
        /// Notifies all QuickBar extensions that the QuickBar was created.
        /// Called by VeneerQuickBar.
        /// </summary>
        internal static void NotifyQuickBarCreated(QuickBarContext context)
        {
            foreach (var ext in _quickBarExtensions)
            {
                try
                {
                    ext.OnQuickBarCreated(context);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] QuickBar extension '{ext.ExtensionId}' threw exception in OnQuickBarCreated: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all QuickBar extensions that the QuickBar was destroyed.
        /// </summary>
        internal static void NotifyQuickBarDestroyed()
        {
            foreach (var ext in _quickBarExtensions)
            {
                try
                {
                    ext.OnQuickBarDestroyed();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] QuickBar extension '{ext.ExtensionId}' threw exception in OnQuickBarDestroyed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all Inventory extensions that the Inventory was created.
        /// </summary>
        internal static void NotifyInventoryCreated(InventoryContext context)
        {
            foreach (var ext in _inventoryExtensions)
            {
                try
                {
                    ext.OnInventoryCreated(context);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Inventory extension '{ext.ExtensionId}' threw exception in OnInventoryCreated: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all Inventory extensions that the Inventory was destroyed.
        /// </summary>
        internal static void NotifyInventoryDestroyed()
        {
            foreach (var ext in _inventoryExtensions)
            {
                try
                {
                    ext.OnInventoryDestroyed();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Inventory extension '{ext.ExtensionId}' threw exception in OnInventoryDestroyed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all Inventory extensions that the Inventory was shown.
        /// </summary>
        internal static void NotifyInventoryShown(InventoryContext context)
        {
            foreach (var ext in _inventoryExtensions)
            {
                try
                {
                    ext.OnInventoryShown(context);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Inventory extension '{ext.ExtensionId}' threw exception in OnInventoryShown: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all Inventory extensions that the Inventory was hidden.
        /// </summary>
        internal static void NotifyInventoryHidden()
        {
            foreach (var ext in _inventoryExtensions)
            {
                try
                {
                    ext.OnInventoryHidden();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Inventory extension '{ext.ExtensionId}' threw exception in OnInventoryHidden: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies matching Window extensions that a window was created.
        /// </summary>
        internal static void NotifyWindowCreated(WindowContext context)
        {
            foreach (var ext in _windowExtensions.Where(e => e.AppliesTo(context.WindowId)))
            {
                try
                {
                    ext.OnWindowCreated(context);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Window extension '{ext.ExtensionId}' threw exception in OnWindowCreated: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies matching Window extensions that a window was destroyed.
        /// </summary>
        internal static void NotifyWindowDestroyed(string windowId)
        {
            foreach (var ext in _windowExtensions.Where(e => e.AppliesTo(windowId)))
            {
                try
                {
                    ext.OnWindowDestroyed(windowId);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Window extension '{ext.ExtensionId}' threw exception in OnWindowDestroyed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies matching Window extensions that a window was shown.
        /// </summary>
        internal static void NotifyWindowShown(WindowContext context)
        {
            foreach (var ext in _windowExtensions.Where(e => e.AppliesTo(context.WindowId)))
            {
                try
                {
                    ext.OnWindowShown(context);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Window extension '{ext.ExtensionId}' threw exception in OnWindowShown: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies matching Window extensions that a window was hidden.
        /// </summary>
        internal static void NotifyWindowHidden(string windowId)
        {
            foreach (var ext in _windowExtensions.Where(e => e.AppliesTo(windowId)))
            {
                try
                {
                    ext.OnWindowHidden(windowId);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Window extension '{ext.ExtensionId}' threw exception in OnWindowHidden: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all Hotbar extensions that the Hotbar was created.
        /// </summary>
        internal static void NotifyHotbarCreated(HotbarContext context)
        {
            foreach (var ext in _hotbarExtensions)
            {
                try
                {
                    ext.OnHotbarCreated(context);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Hotbar extension '{ext.ExtensionId}' threw exception in OnHotbarCreated: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all Hotbar extensions that the Hotbar was destroyed.
        /// </summary>
        internal static void NotifyHotbarDestroyed()
        {
            foreach (var ext in _hotbarExtensions)
            {
                try
                {
                    ext.OnHotbarDestroyed();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] Hotbar extension '{ext.ExtensionId}' threw exception in OnHotbarDestroyed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all HUD extensions that the HUD was created.
        /// </summary>
        internal static void NotifyHudCreated(HudContext context)
        {
            foreach (var ext in _hudExtensions)
            {
                try
                {
                    ext.OnHudCreated(context);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] HUD extension '{ext.ExtensionId}' threw exception in OnHudCreated: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Notifies all HUD extensions that the HUD was destroyed.
        /// </summary>
        internal static void NotifyHudDestroyed()
        {
            foreach (var ext in _hudExtensions)
            {
                try
                {
                    ext.OnHudDestroyed();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerExtensions] HUD extension '{ext.ExtensionId}' threw exception in OnHudDestroyed: {ex.Message}");
                }
            }
        }

        #endregion

        #region Utilities

        private static void InsertByPriority<T>(List<T> list, T item) where T : IVeneerExtension
        {
            int index = 0;
            for (; index < list.Count; index++)
            {
                if (list[index].Priority > item.Priority)
                    break;
            }
            list.Insert(index, item);
        }

        /// <summary>
        /// Clears all registered extensions.
        /// Called on cleanup/logout.
        /// </summary>
        internal static void Clear()
        {
            _quickBarExtensions.Clear();
            _inventoryExtensions.Clear();
            _windowExtensions.Clear();
            _hotbarExtensions.Clear();
            _hudExtensions.Clear();
            Plugin.Log.LogDebug("[VeneerExtensions] All extensions cleared");
        }

        #endregion
    }
}
