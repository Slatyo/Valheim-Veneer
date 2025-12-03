using UnityEngine;
using UnityEngine.UI;

namespace Veneer.Core
{
    /// <summary>
    /// Defines UI layer constants for z-ordering and provides utilities for layer management.
    /// Higher values render on top of lower values.
    /// </summary>
    public static class VeneerLayers
    {
        #region Layer Constants

        /// <summary>
        /// Background layer for permanent background elements.
        /// </summary>
        public const int Background = 0;

        /// <summary>
        /// HUD layer for always-visible elements (health, stamina, minimap, hotbar, food).
        /// </summary>
        public const int HUD = 100;

        /// <summary>
        /// HUD overlay layer for elements above base HUD (status effects, boss frame).
        /// </summary>
        public const int HUDOverlay = 150;

        /// <summary>
        /// Standard window layer for normal windows (inventory, crafting, skills).
        /// </summary>
        public const int Window = 1000;

        /// <summary>
        /// Focused window layer - the currently active/focused window gets this layer.
        /// </summary>
        public const int WindowFocused = 1100;

        /// <summary>
        /// Popup layer for larger overlay windows (world map, compendium).
        /// </summary>
        public const int Popup = 2000;

        /// <summary>
        /// QuickBar layer - above windows so buttons are always clickable.
        /// </summary>
        public const int QuickBar = 2500;

        /// <summary>
        /// Modal layer for dialogs that require user interaction before continuing.
        /// </summary>
        public const int Modal = 3000;

        /// <summary>
        /// Drag preview layer for items being dragged.
        /// </summary>
        public const int DragPreview = 4000;

        /// <summary>
        /// Edit mode background layer for grid overlay.
        /// </summary>
        public const int EditModeBackground = 5000;

        /// <summary>
        /// Edit mode overlay layer for mover overlays.
        /// </summary>
        public const int EditModeOverlay = 5100;

        /// <summary>
        /// Edit mode handles layer for resize handles.
        /// </summary>
        public const int EditModeHandles = 5200;

        /// <summary>
        /// Edit mode UI layer for the control panel.
        /// </summary>
        public const int EditModeUI = 5300;

        /// <summary>
        /// Tooltip layer - always topmost interactive element.
        /// </summary>
        public const int Tooltip = 6000;

        /// <summary>
        /// System overlay layer for loading screens and system messages.
        /// </summary>
        public const int SystemOverlay = 7000;

        #endregion

        #region Counter for Window Ordering

        // Used to give each window a unique sub-order within its layer
        private static int _windowCounter = 0;
        private static int _popupCounter = 0;

        /// <summary>
        /// Gets the next unique window order offset.
        /// </summary>
        public static int GetNextWindowOffset()
        {
            return _windowCounter++;
        }

        /// <summary>
        /// Gets the next unique popup order offset.
        /// </summary>
        public static int GetNextPopupOffset()
        {
            return _popupCounter++;
        }

        /// <summary>
        /// Resets counters (call on scene change).
        /// </summary>
        public static void ResetCounters()
        {
            _windowCounter = 0;
            _popupCounter = 0;
        }

        #endregion

        #region Layer Utilities

        /// <summary>
        /// Sets the sorting order for a GameObject by ensuring it has a Canvas component.
        /// </summary>
        /// <param name="go">The GameObject to set the layer on.</param>
        /// <param name="sortingOrder">The sorting order value.</param>
        /// <param name="addRaycaster">Whether to add a GraphicRaycaster for input.</param>
        public static void SetLayer(GameObject go, int sortingOrder, bool addRaycaster = true)
        {
            if (go == null) return;

            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (addRaycaster)
            {
                var raycaster = go.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = go.AddComponent<GraphicRaycaster>();
                }
                raycaster.ignoreReversedGraphics = true;
                raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }
        }

        /// <summary>
        /// Sets the sorting order for a Component's GameObject.
        /// </summary>
        public static void SetLayer(Component component, int sortingOrder, bool addRaycaster = true)
        {
            if (component != null)
            {
                SetLayer(component.gameObject, sortingOrder, addRaycaster);
            }
        }

        /// <summary>
        /// Gets the current sorting order of a GameObject.
        /// </summary>
        /// <param name="go">The GameObject to check.</param>
        /// <returns>The sorting order, or 0 if no Canvas is present.</returns>
        public static int GetLayer(GameObject go)
        {
            if (go == null) return 0;

            var canvas = go.GetComponent<Canvas>();
            if (canvas != null && canvas.overrideSorting)
            {
                return canvas.sortingOrder;
            }

            return 0;
        }

        /// <summary>
        /// Ensures a GameObject has a Canvas with the specified sorting order.
        /// If a Canvas already exists, updates its sorting order.
        /// </summary>
        public static Canvas EnsureCanvas(GameObject go, int sortingOrder, bool addRaycaster = true)
        {
            if (go == null) return null;

            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (addRaycaster)
            {
                var raycaster = go.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = go.AddComponent<GraphicRaycaster>();
                }
            }

            return canvas;
        }

        /// <summary>
        /// Removes the layer override from a GameObject.
        /// </summary>
        public static void RemoveLayerOverride(GameObject go)
        {
            if (go == null) return;

            var canvas = go.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = false;
            }
        }

        /// <summary>
        /// Gets the base layer value for a VeneerLayerType.
        /// </summary>
        public static int GetLayerValue(VeneerLayerType layerType)
        {
            return layerType switch
            {
                VeneerLayerType.Background => Background,
                VeneerLayerType.HUD => HUD,
                VeneerLayerType.HUDOverlay => HUDOverlay,
                VeneerLayerType.Window => Window,
                VeneerLayerType.Popup => Popup,
                VeneerLayerType.Modal => Modal,
                VeneerLayerType.Tooltip => Tooltip,
                VeneerLayerType.EditMode => EditModeUI,
                _ => 0
            };
        }

        #endregion
    }

    /// <summary>
    /// Enum for specifying layer types on VeneerElements.
    /// </summary>
    public enum VeneerLayerType
    {
        /// <summary>
        /// No explicit layer - inherits from parent canvas.
        /// </summary>
        None = 0,

        /// <summary>
        /// Background layer for permanent background elements.
        /// </summary>
        Background,

        /// <summary>
        /// HUD layer for always-visible elements.
        /// </summary>
        HUD,

        /// <summary>
        /// HUD overlay for elements above base HUD.
        /// </summary>
        HUDOverlay,

        /// <summary>
        /// Standard window layer.
        /// </summary>
        Window,

        /// <summary>
        /// Popup layer for larger overlay windows.
        /// </summary>
        Popup,

        /// <summary>
        /// Modal layer for dialogs.
        /// </summary>
        Modal,

        /// <summary>
        /// Tooltip layer.
        /// </summary>
        Tooltip,

        /// <summary>
        /// Edit mode UI layer.
        /// </summary>
        EditMode
    }
}
