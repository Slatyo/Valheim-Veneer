using System;
using System.Collections.Generic;
using UnityEngine;

namespace Veneer.Grid
{
    /// <summary>
    /// Screen anchor positions for the grid system.
    /// Elements can be anchored to these positions and offset from them.
    /// </summary>
    public enum ScreenAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    /// <summary>
    /// Manages screen anchor points for the grid system.
    /// Provides anchor positions and handles element registration.
    /// </summary>
    public static class VeneerAnchor
    {
        private static readonly Dictionary<string, AnchorData> _registeredElements = new Dictionary<string, AnchorData>();
        private static readonly Dictionary<string, (ScreenAnchor anchor, Vector2 offset, Vector2 size)> _pendingPositions = new Dictionary<string, (ScreenAnchor, Vector2, Vector2)>();

        /// <summary>
        /// Gets the anchor point position for a screen anchor.
        /// Returns normalized coordinates (0-1).
        /// </summary>
        public static Vector2 GetAnchorPosition(ScreenAnchor anchor)
        {
            return anchor switch
            {
                ScreenAnchor.TopLeft => new Vector2(0f, 1f),
                ScreenAnchor.TopCenter => new Vector2(0.5f, 1f),
                ScreenAnchor.TopRight => new Vector2(1f, 1f),
                ScreenAnchor.Left => new Vector2(0f, 0.5f),
                ScreenAnchor.Center => new Vector2(0.5f, 0.5f),
                ScreenAnchor.Right => new Vector2(1f, 0.5f),
                ScreenAnchor.BottomLeft => new Vector2(0f, 0f),
                ScreenAnchor.BottomCenter => new Vector2(0.5f, 0f),
                ScreenAnchor.BottomRight => new Vector2(1f, 0f),
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        /// <summary>
        /// Gets the pivot that should be used for a given anchor.
        /// </summary>
        public static Vector2 GetPivotForAnchor(ScreenAnchor anchor)
        {
            return anchor switch
            {
                ScreenAnchor.TopLeft => new Vector2(0f, 1f),
                ScreenAnchor.TopCenter => new Vector2(0.5f, 1f),
                ScreenAnchor.TopRight => new Vector2(1f, 1f),
                ScreenAnchor.Left => new Vector2(0f, 0.5f),
                ScreenAnchor.Center => new Vector2(0.5f, 0.5f),
                ScreenAnchor.Right => new Vector2(1f, 0.5f),
                ScreenAnchor.BottomLeft => new Vector2(0f, 0f),
                ScreenAnchor.BottomCenter => new Vector2(0.5f, 0f),
                ScreenAnchor.BottomRight => new Vector2(1f, 0f),
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        /// <summary>
        /// Applies an anchor configuration to a RectTransform.
        /// </summary>
        public static void ApplyAnchor(RectTransform rect, ScreenAnchor anchor, Vector2 offset)
        {
            var anchorPos = GetAnchorPosition(anchor);
            var pivot = GetPivotForAnchor(anchor);

            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.pivot = pivot;
            rect.anchoredPosition = offset;

            Plugin.Log.LogDebug($"VeneerAnchor.ApplyAnchor: {rect.name} -> anchor={anchor}, offset={offset}");
        }

        /// <summary>
        /// Minimum size for any element (prevents 0-sized windows).
        /// </summary>
        public const float MinElementSize = 50f;

        /// <summary>
        /// Applies saved position and size from anchor data to a RectTransform.
        /// Also registers the RectTransform for resolution change handling.
        /// </summary>
        public static void ApplySavedLayout(RectTransform rect, string elementId)
        {
            var data = GetAnchorData(elementId);
            if (data == null)
            {
                Plugin.Log.LogDebug($"VeneerAnchor.ApplySavedLayout: No data for {elementId}");
                return;
            }

            // Register the RectTransform for resolution change handling
            data.RectTransform = rect;

            // Apply anchor and position
            ApplyAnchor(rect, data.Anchor, data.Offset);

            // Apply size if saved and valid (enforce minimum size)
            if (data.Size.x >= MinElementSize && data.Size.y >= MinElementSize)
            {
                rect.sizeDelta = data.Size;
                Plugin.Log.LogDebug($"VeneerAnchor.ApplySavedLayout: Applied size {data.Size} to {elementId}");
            }
            else if (data.Size != Vector2.zero)
            {
                // Size was saved but is too small - use defaults or current size
                Plugin.Log.LogWarning($"VeneerAnchor.ApplySavedLayout: Size {data.Size} for {elementId} is too small, ignoring");
            }
        }

        /// <summary>
        /// Registers an element with the anchor system.
        /// If a saved position was loaded before registration, it will be applied.
        /// </summary>
        public static void Register(string elementId, ScreenAnchor defaultAnchor, Vector2 defaultOffset, Vector2 defaultSize = default)
        {
            // Check if we have a pending position from loaded layout
            ScreenAnchor anchor = defaultAnchor;
            Vector2 offset = defaultOffset;
            Vector2 size = defaultSize;

            if (_pendingPositions.TryGetValue(elementId, out var pending))
            {
                anchor = pending.anchor;
                offset = pending.offset;
                if (pending.size != Vector2.zero)
                {
                    size = pending.size;
                }
                _pendingPositions.Remove(elementId);
                Plugin.Log.LogInfo($"VeneerAnchor.Register: {elementId} - loaded saved position: anchor={anchor}, offset={offset}, size={size}");
            }
            else
            {
                Plugin.Log.LogDebug($"VeneerAnchor.Register: {elementId} - using default: anchor={anchor}, offset={offset}, size={size}");
            }

            _registeredElements[elementId] = new AnchorData
            {
                ElementId = elementId,
                Anchor = anchor,
                Offset = offset,
                Size = size,
                DefaultAnchor = defaultAnchor,
                DefaultOffset = defaultOffset,
                DefaultSize = defaultSize
            };
        }

        /// <summary>
        /// Updates the anchor data for an element.
        /// If element is not yet registered, stores as pending for when it registers.
        /// </summary>
        public static void UpdatePosition(string elementId, ScreenAnchor anchor, Vector2 offset, Vector2 size = default)
        {
            if (_registeredElements.TryGetValue(elementId, out var data))
            {
                data.Anchor = anchor;
                data.Offset = offset;
                // Only save size if it's valid (above minimum)
                if (size.x >= MinElementSize && size.y >= MinElementSize)
                {
                    data.Size = size;
                }
                Plugin.Log.LogDebug($"VeneerAnchor.UpdatePosition: {elementId} -> anchor={anchor}, offset={offset}, size={size}");
            }
            else
            {
                // Element not yet registered, store as pending
                _pendingPositions[elementId] = (anchor, offset, size);
                Plugin.Log.LogDebug($"VeneerAnchor.UpdatePosition (pending): {elementId} -> anchor={anchor}, offset={offset}, size={size}");
            }
        }

        /// <summary>
        /// Updates just the size for an element.
        /// </summary>
        public static void UpdateSize(string elementId, Vector2 size)
        {
            if (_registeredElements.TryGetValue(elementId, out var data))
            {
                // Only save size if it's valid (above minimum)
                if (size.x >= MinElementSize && size.y >= MinElementSize)
                {
                    data.Size = size;
                    Plugin.Log.LogDebug($"VeneerAnchor.UpdateSize: {elementId} -> size={size}");
                }
                else
                {
                    Plugin.Log.LogWarning($"VeneerAnchor.UpdateSize: Size {size} for {elementId} is too small, ignoring");
                }
            }
        }

        /// <summary>
        /// Gets the anchor data for an element.
        /// </summary>
        public static AnchorData GetAnchorData(string elementId)
        {
            return _registeredElements.TryGetValue(elementId, out var data) ? data : null;
        }

        /// <summary>
        /// Resets an element to its default position.
        /// </summary>
        public static void ResetToDefault(string elementId)
        {
            if (_registeredElements.TryGetValue(elementId, out var data))
            {
                data.Anchor = data.DefaultAnchor;
                data.Offset = data.DefaultOffset;
            }
        }

        /// <summary>
        /// Resets all elements to their default positions.
        /// </summary>
        public static void ResetAllToDefault()
        {
            foreach (var data in _registeredElements.Values)
            {
                data.Anchor = data.DefaultAnchor;
                data.Offset = data.DefaultOffset;
            }
        }

        /// <summary>
        /// Gets all registered element IDs.
        /// </summary>
        public static IEnumerable<string> GetRegisteredElements()
        {
            return _registeredElements.Keys;
        }

        /// <summary>
        /// Clears all registrations.
        /// </summary>
        public static void Clear()
        {
            _registeredElements.Clear();
            _pendingPositions.Clear();
        }

        /// <summary>
        /// Registers a RectTransform for an element so it can be re-positioned on resolution change.
        /// </summary>
        public static void RegisterRectTransform(string elementId, RectTransform rectTransform)
        {
            if (_registeredElements.TryGetValue(elementId, out var data))
            {
                data.RectTransform = rectTransform;
            }
        }

        /// <summary>
        /// Re-applies the stored position to the element's RectTransform.
        /// Called after resolution changes.
        /// </summary>
        public static void ReapplyToRectTransform(string elementId)
        {
            if (!_registeredElements.TryGetValue(elementId, out var data))
                return;

            if (data.RectTransform == null)
                return;

            // Check if RectTransform is still valid (not destroyed)
            if (!data.RectTransform)
            {
                data.RectTransform = null;
                return;
            }

            // Apply anchor and position
            ApplyAnchor(data.RectTransform, data.Anchor, data.Offset);

            // Apply size if valid
            if (data.Size.x >= MinElementSize && data.Size.y >= MinElementSize)
            {
                data.RectTransform.sizeDelta = data.Size;
            }

            // Force layout rebuild to ensure changes take effect
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(data.RectTransform);

            Plugin.Log.LogDebug($"VeneerAnchor.ReapplyToRectTransform: {elementId} -> offset={data.Offset}, anchor={data.Anchor}");
        }

        /// <summary>
        /// Re-applies positions to all registered RectTransforms.
        /// </summary>
        public static void ReapplyAllRectTransforms()
        {
            foreach (var elementId in _registeredElements.Keys)
            {
                ReapplyToRectTransform(elementId);
            }
        }

        /// <summary>
        /// Finds the nearest anchor point to a screen position.
        /// </summary>
        public static ScreenAnchor FindNearestAnchor(Vector2 screenPosition, Vector2 screenSize)
        {
            // Normalize position
            float x = screenPosition.x / screenSize.x;
            float y = screenPosition.y / screenSize.y;

            // Determine horizontal zone
            int hZone = x < 0.33f ? 0 : (x > 0.66f ? 2 : 1);

            // Determine vertical zone
            int vZone = y < 0.33f ? 0 : (y > 0.66f ? 2 : 1);

            // Map to anchor
            return (vZone * 3 + hZone) switch
            {
                0 => ScreenAnchor.BottomLeft,
                1 => ScreenAnchor.BottomCenter,
                2 => ScreenAnchor.BottomRight,
                3 => ScreenAnchor.Left,
                4 => ScreenAnchor.Center,
                5 => ScreenAnchor.Right,
                6 => ScreenAnchor.TopLeft,
                7 => ScreenAnchor.TopCenter,
                8 => ScreenAnchor.TopRight,
                _ => ScreenAnchor.Center
            };
        }
    }

    /// <summary>
    /// Data for a registered anchor element.
    /// </summary>
    public class AnchorData
    {
        public string ElementId { get; set; }
        public ScreenAnchor Anchor { get; set; }
        public Vector2 Offset { get; set; }
        public Vector2 Size { get; set; }
        public ScreenAnchor DefaultAnchor { get; set; }
        public Vector2 DefaultOffset { get; set; }
        public Vector2 DefaultSize { get; set; }

        /// <summary>
        /// Reference to the RectTransform for this element (if registered).
        /// Used for re-applying positions on resolution change.
        /// </summary>
        public RectTransform RectTransform { get; set; }
    }
}
