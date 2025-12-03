using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Veneer.Components.Base;

namespace Veneer.Core
{
    /// <summary>
    /// Centralized manager for all Veneer windows.
    /// Handles window registration, focus management, and visibility control.
    /// </summary>
    public static class VeneerWindowManager
    {
        #region Fields

        // Registry of all windows by type
        private static readonly Dictionary<Type, VeneerElement> _windowsByType = new Dictionary<Type, VeneerElement>();

        // Registry of all windows by ID
        private static readonly Dictionary<string, VeneerElement> _windowsById = new Dictionary<string, VeneerElement>();

        // List of all registered windows for iteration
        private static readonly List<VeneerElement> _allWindows = new List<VeneerElement>();

        // Stack of focused windows (most recent at end)
        private static readonly List<VeneerElement> _focusStack = new List<VeneerElement>();

        // Currently focused window
        private static VeneerElement _focusedWindow;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the currently focused window.
        /// </summary>
        public static VeneerElement FocusedWindow => _focusedWindow;

        /// <summary>
        /// Gets all registered windows.
        /// </summary>
        public static IReadOnlyList<VeneerElement> AllWindows => _allWindows;

        /// <summary>
        /// Gets all currently visible windows.
        /// </summary>
        public static IEnumerable<VeneerElement> VisibleWindows => _allWindows.Where(w => w != null && w.IsVisible);

        #endregion

        #region Events

        /// <summary>
        /// Fired when a window is opened (shown).
        /// </summary>
        public static event Action<VeneerElement> OnWindowOpened;

        /// <summary>
        /// Fired when a window is closed (hidden).
        /// </summary>
        public static event Action<VeneerElement> OnWindowClosed;

        /// <summary>
        /// Fired when a window gains focus.
        /// </summary>
        public static event Action<VeneerElement> OnWindowFocused;

        /// <summary>
        /// Fired when a window loses focus.
        /// </summary>
        public static event Action<VeneerElement> OnWindowBlurred;

        /// <summary>
        /// Fired when a window is registered.
        /// </summary>
        public static event Action<VeneerElement> OnWindowRegistered;

        /// <summary>
        /// Fired when a window is unregistered.
        /// </summary>
        public static event Action<VeneerElement> OnWindowUnregistered;

        #endregion

        #region Registration

        /// <summary>
        /// Registers a window with the manager.
        /// </summary>
        /// <param name="window">The window to register.</param>
        public static void RegisterWindow(VeneerElement window)
        {
            if (window == null) return;

            var type = window.GetType();

            // Register by type (only one instance per type)
            if (!_windowsByType.ContainsKey(type))
            {
                _windowsByType[type] = window;
            }
            else
            {
                // Replace existing if it was destroyed
                if (_windowsByType[type] == null || _windowsByType[type].gameObject == null)
                {
                    _windowsByType[type] = window;
                }
            }

            // Register by ID if it has one
            if (!string.IsNullOrEmpty(window.ElementId))
            {
                _windowsById[window.ElementId] = window;
            }

            // Add to all windows list if not already there
            if (!_allWindows.Contains(window))
            {
                _allWindows.Add(window);
            }

            // Subscribe to window events
            window.OnShow += () => HandleWindowShown(window);
            window.OnHide += () => HandleWindowHidden(window);

            Plugin.Log.LogDebug($"VeneerWindowManager: Registered {type.Name} (ID: {window.ElementId ?? "none"})");
            OnWindowRegistered?.Invoke(window);
        }

        /// <summary>
        /// Unregisters a window from the manager.
        /// </summary>
        /// <param name="window">The window to unregister.</param>
        public static void UnregisterWindow(VeneerElement window)
        {
            if (window == null) return;

            var type = window.GetType();

            // Remove from type registry
            if (_windowsByType.TryGetValue(type, out var registered) && registered == window)
            {
                _windowsByType.Remove(type);
            }

            // Remove from ID registry
            if (!string.IsNullOrEmpty(window.ElementId) &&
                _windowsById.TryGetValue(window.ElementId, out var registeredById) &&
                registeredById == window)
            {
                _windowsById.Remove(window.ElementId);
            }

            // Remove from all windows list
            _allWindows.Remove(window);

            // Remove from focus stack
            _focusStack.Remove(window);

            // Clear focus if this was focused
            if (_focusedWindow == window)
            {
                _focusedWindow = _focusStack.LastOrDefault();
            }

            Plugin.Log.LogDebug($"VeneerWindowManager: Unregistered {type.Name}");
            OnWindowUnregistered?.Invoke(window);
        }

        /// <summary>
        /// Cleans up destroyed windows from the registry.
        /// </summary>
        public static void CleanupDestroyed()
        {
            // Find destroyed windows
            var destroyed = _allWindows.Where(w => w == null || w.gameObject == null).ToList();

            foreach (var window in destroyed)
            {
                UnregisterWindow(window);
            }

            // Clean up type registry
            var typesToRemove = _windowsByType.Where(kvp => kvp.Value == null || kvp.Value.gameObject == null)
                                              .Select(kvp => kvp.Key)
                                              .ToList();
            foreach (var type in typesToRemove)
            {
                _windowsByType.Remove(type);
            }

            // Clean up ID registry
            var idsToRemove = _windowsById.Where(kvp => kvp.Value == null || kvp.Value.gameObject == null)
                                          .Select(kvp => kvp.Key)
                                          .ToList();
            foreach (var id in idsToRemove)
            {
                _windowsById.Remove(id);
            }
        }

        /// <summary>
        /// Clears all registrations (call on scene unload).
        /// </summary>
        public static void Clear()
        {
            _windowsByType.Clear();
            _windowsById.Clear();
            _allWindows.Clear();
            _focusStack.Clear();
            _focusedWindow = null;
            VeneerLayers.ResetCounters();
            Plugin.Log.LogDebug("VeneerWindowManager: Cleared all registrations");
        }

        #endregion

        #region Window Access

        /// <summary>
        /// Gets a window by type.
        /// </summary>
        /// <typeparam name="T">The window type.</typeparam>
        /// <returns>The window instance, or null if not registered.</returns>
        public static T GetWindow<T>() where T : VeneerElement
        {
            if (_windowsByType.TryGetValue(typeof(T), out var window))
            {
                return window as T;
            }
            return null;
        }

        /// <summary>
        /// Gets a window by ID.
        /// </summary>
        /// <param name="elementId">The element ID.</param>
        /// <returns>The window instance, or null if not found.</returns>
        public static VeneerElement GetWindow(string elementId)
        {
            if (string.IsNullOrEmpty(elementId)) return null;

            _windowsById.TryGetValue(elementId, out var window);
            return window;
        }

        /// <summary>
        /// Checks if a window type is registered.
        /// </summary>
        public static bool HasWindow<T>() where T : VeneerElement
        {
            return _windowsByType.ContainsKey(typeof(T)) && _windowsByType[typeof(T)] != null;
        }

        /// <summary>
        /// Checks if a window type is currently visible.
        /// </summary>
        public static bool IsWindowVisible<T>() where T : VeneerElement
        {
            var window = GetWindow<T>();
            return window != null && window.IsVisible;
        }

        #endregion

        #region Show/Hide/Toggle

        /// <summary>
        /// Shows a window by type.
        /// </summary>
        public static void ShowWindow<T>() where T : VeneerElement
        {
            var window = GetWindow<T>();
            if (window != null)
            {
                window.Show();
            }
            else
            {
                Plugin.Log.LogWarning($"VeneerWindowManager: Cannot show {typeof(T).Name} - not registered");
            }
        }

        /// <summary>
        /// Shows a window by ID.
        /// </summary>
        public static void ShowWindow(string elementId)
        {
            var window = GetWindow(elementId);
            if (window != null)
            {
                window.Show();
            }
        }

        /// <summary>
        /// Hides a window by type.
        /// </summary>
        public static void HideWindow<T>() where T : VeneerElement
        {
            var window = GetWindow<T>();
            if (window != null)
            {
                window.Hide();
            }
        }

        /// <summary>
        /// Hides a window by ID.
        /// </summary>
        public static void HideWindow(string elementId)
        {
            var window = GetWindow(elementId);
            if (window != null)
            {
                window.Hide();
            }
        }

        /// <summary>
        /// Toggles a window by type.
        /// </summary>
        public static void ToggleWindow<T>() where T : VeneerElement
        {
            var window = GetWindow<T>();
            Plugin.Log.LogInfo($"VeneerWindowManager.ToggleWindow<{typeof(T).Name}>: window={window}, IsVisible={window?.IsVisible}, gameObject.activeSelf={window?.gameObject?.activeSelf}");
            if (window != null)
            {
                if (window.IsVisible)
                {
                    Plugin.Log.LogInfo($"VeneerWindowManager: Hiding {typeof(T).Name}");
                    window.Hide();
                }
                else
                {
                    Plugin.Log.LogInfo($"VeneerWindowManager: Showing {typeof(T).Name}");
                    window.Show();
                }
            }
            else
            {
                Plugin.Log.LogWarning($"VeneerWindowManager.ToggleWindow<{typeof(T).Name}>: Window not found!");
            }
        }

        /// <summary>
        /// Toggles a window by ID.
        /// </summary>
        public static void ToggleWindow(string elementId)
        {
            var window = GetWindow(elementId);
            if (window != null)
            {
                if (window.IsVisible)
                {
                    window.Hide();
                }
                else
                {
                    window.Show();
                }
            }
        }

        /// <summary>
        /// Closes all visible windows.
        /// </summary>
        public static void CloseAllWindows()
        {
            foreach (var window in _allWindows.ToList())
            {
                if (window != null && window.IsVisible)
                {
                    window.Hide();
                }
            }
        }

        /// <summary>
        /// Closes all visible windows except the specified types.
        /// </summary>
        public static void CloseAllExcept(params Type[] exceptions)
        {
            var exceptionSet = new HashSet<Type>(exceptions);

            foreach (var window in _allWindows.ToList())
            {
                if (window != null && window.IsVisible && !exceptionSet.Contains(window.GetType()))
                {
                    window.Hide();
                }
            }
        }

        /// <summary>
        /// Closes all visible windows except HUD elements.
        /// </summary>
        public static void CloseAllNonHUD()
        {
            foreach (var window in _allWindows.ToList())
            {
                if (window != null && window.IsVisible)
                {
                    var layer = window.LayerType;
                    if (layer != VeneerLayerType.HUD && layer != VeneerLayerType.HUDOverlay)
                    {
                        window.Hide();
                    }
                }
            }
        }

        #endregion

        #region Focus Management

        /// <summary>
        /// Focuses a window, bringing it to the front of its layer.
        /// </summary>
        public static void FocusWindow(VeneerElement window)
        {
            if (window == null) return;
            if (!window.IsVisible) return;

            // Don't refocus if already focused
            if (_focusedWindow == window) return;

            // Blur previous window
            var previousFocus = _focusedWindow;
            if (previousFocus != null)
            {
                OnWindowBlurred?.Invoke(previousFocus);
            }

            // Update focus
            _focusedWindow = window;

            // Update focus stack
            _focusStack.Remove(window);
            _focusStack.Add(window);

            // Bring to front
            window.BringToFront();

            Plugin.Log.LogDebug($"VeneerWindowManager: Focused {window.GetType().Name}");
            OnWindowFocused?.Invoke(window);
        }

        /// <summary>
        /// Focuses a window by type.
        /// </summary>
        public static void FocusWindow<T>() where T : VeneerElement
        {
            var window = GetWindow<T>();
            if (window != null)
            {
                FocusWindow(window);
            }
        }

        /// <summary>
        /// Removes focus from a window.
        /// </summary>
        public static void BlurWindow(VeneerElement window)
        {
            if (window == null) return;
            if (_focusedWindow != window) return;

            OnWindowBlurred?.Invoke(window);
            _focusStack.Remove(window);
            _focusedWindow = _focusStack.LastOrDefault();

            if (_focusedWindow != null)
            {
                OnWindowFocused?.Invoke(_focusedWindow);
            }
        }

        #endregion

        #region Event Handlers

        private static void HandleWindowShown(VeneerElement window)
        {
            if (window == null) return;

            // Auto-focus window-type elements when shown
            if (window.LayerType == VeneerLayerType.Window ||
                window.LayerType == VeneerLayerType.Popup ||
                window.LayerType == VeneerLayerType.Modal)
            {
                FocusWindow(window);
            }

            OnWindowOpened?.Invoke(window);
        }

        private static void HandleWindowHidden(VeneerElement window)
        {
            if (window == null) return;

            // Remove from focus if hidden
            if (_focusedWindow == window)
            {
                BlurWindow(window);
            }

            _focusStack.Remove(window);

            OnWindowClosed?.Invoke(window);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets all windows of a specific layer type.
        /// </summary>
        public static IEnumerable<VeneerElement> GetWindowsByLayer(VeneerLayerType layerType)
        {
            return _allWindows.Where(w => w != null && w.LayerType == layerType);
        }

        /// <summary>
        /// Gets all visible windows of a specific layer type.
        /// </summary>
        public static IEnumerable<VeneerElement> GetVisibleWindowsByLayer(VeneerLayerType layerType)
        {
            return _allWindows.Where(w => w != null && w.IsVisible && w.LayerType == layerType);
        }

        /// <summary>
        /// Checks if any modal window is currently visible.
        /// </summary>
        public static bool IsModalActive()
        {
            return _allWindows.Any(w => w != null && w.IsVisible && w.LayerType == VeneerLayerType.Modal);
        }

        /// <summary>
        /// Gets debug information about registered windows.
        /// </summary>
        public static string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== VeneerWindowManager Debug ===");
            sb.AppendLine($"Registered windows: {_allWindows.Count}");
            sb.AppendLine($"Focused: {_focusedWindow?.GetType().Name ?? "none"}");
            sb.AppendLine();
            sb.AppendLine("Windows:");

            foreach (var window in _allWindows)
            {
                if (window == null) continue;
                var status = window.IsVisible ? "VISIBLE" : "hidden";
                var focused = window == _focusedWindow ? " [FOCUSED]" : "";
                sb.AppendLine($"  - {window.GetType().Name} ({window.ElementId ?? "no-id"}): {window.LayerType} - {status}{focused}");
            }

            return sb.ToString();
        }

        #endregion
    }
}
