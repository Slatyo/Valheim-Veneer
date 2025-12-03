using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace Veneer.Grid
{
    /// <summary>
    /// Handles saving and loading of element positions.
    /// Persists layout to JSON in the BepInEx config folder.
    /// Supports resolution-independent positioning by storing the source resolution.
    /// </summary>
    public static class VeneerLayout
    {
        private const string LayoutFileName = "Veneer.layout.json";
        private const int LayoutVersion = 2; // v2 adds SavedAtWidth/Height

        private static string LayoutPath => Path.Combine(Paths.ConfigPath, LayoutFileName);

        private static LayoutData _currentLayout;
        private static bool _isDirty;

        /// <summary>
        /// Initializes the layout system and loads saved positions.
        /// </summary>
        public static void Initialize()
        {
            Load();
        }

        /// <summary>
        /// Loads the layout from disk.
        /// </summary>
        public static void Load()
        {
            _currentLayout = new LayoutData
            {
                Version = LayoutVersion,
                SavedAtWidth = Screen.width,
                SavedAtHeight = Screen.height
            };

            if (!File.Exists(LayoutPath))
            {
                Plugin.Log.LogDebug("Veneer: No layout file found, using defaults");
                return;
            }

            try
            {
                var json = File.ReadAllText(LayoutPath);
                _currentLayout = JsonConvert.DeserializeObject<LayoutData>(json);

                if (_currentLayout == null)
                {
                    Plugin.Log.LogWarning("Veneer: Failed to parse layout, using defaults");
                    _currentLayout = new LayoutData
                    {
                        Version = LayoutVersion,
                        SavedAtWidth = Screen.width,
                        SavedAtHeight = Screen.height
                    };
                    return;
                }

                // Migrate v1 layouts to v2
                if (_currentLayout.Version < 2)
                {
                    Plugin.Log.LogInfo("Veneer: Migrating layout from v1 to v2");
                    _currentLayout.SavedAtWidth = Screen.width;
                    _currentLayout.SavedAtHeight = Screen.height;
                    _currentLayout.Version = 2;
                    // Will save after applying positions
                }

                // Calculate scale factors for resolution adjustment
                float scaleX = (_currentLayout.SavedAtWidth > 0)
                    ? (float)Screen.width / _currentLayout.SavedAtWidth
                    : 1f;
                float scaleY = (_currentLayout.SavedAtHeight > 0)
                    ? (float)Screen.height / _currentLayout.SavedAtHeight
                    : 1f;

                bool needsScaling = Math.Abs(scaleX - 1f) > 0.01f || Math.Abs(scaleY - 1f) > 0.01f;
                if (needsScaling)
                {
                    Plugin.Log.LogInfo($"Veneer: Scaling layout from {_currentLayout.SavedAtWidth}x{_currentLayout.SavedAtHeight} to {Screen.width}x{Screen.height} (scale: {scaleX:F2}, {scaleY:F2})");
                }

                // Apply loaded positions to anchor system
                if (_currentLayout.Elements != null)
                {
                    foreach (var element in _currentLayout.Elements)
                    {
                        if (!string.IsNullOrEmpty(element.Id))
                        {
                            // Get anchor position (0, 0.5, or 1 for each axis)
                            var anchorPos = VeneerAnchor.GetAnchorPosition(element.Anchor);

                            // Scale offset based on anchor position:
                            // - Anchor at edge (0 or 1): offset is from edge, keep fixed
                            // - Anchor at center (0.5): offset is from center, scale it
                            float offsetX = element.OffsetX;
                            float offsetY = element.OffsetY;

                            // Scale X if anchor is horizontally centered
                            if (Math.Abs(anchorPos.x - 0.5f) < 0.01f)
                            {
                                offsetX = element.OffsetX * scaleX;
                            }

                            // Scale Y if anchor is vertically centered
                            if (Math.Abs(anchorPos.y - 0.5f) < 0.01f)
                            {
                                offsetY = element.OffsetY * scaleY;
                            }

                            // Sizes stay fixed (Valheim's GUI Scale handles size scaling)
                            float width = element.Width;
                            float height = element.Height;

                            VeneerAnchor.UpdatePosition(
                                element.Id,
                                element.Anchor,
                                new Vector2(offsetX, offsetY),
                                new Vector2(width, height)
                            );

                            if (needsScaling)
                            {
                                Plugin.Log.LogDebug($"Veneer: {element.Id} anchor={element.Anchor}: offset ({element.OffsetX}, {element.OffsetY}) -> ({offsetX:F0}, {offsetY:F0})");
                            }
                        }
                    }
                }

                Plugin.Log.LogInfo($"Veneer: Loaded layout with {_currentLayout.Elements?.Count ?? 0} elements at {Screen.width}x{Screen.height}");

                // Update saved resolution to current after scaling
                _currentLayout.SavedAtWidth = Screen.width;
                _currentLayout.SavedAtHeight = Screen.height;
                if (needsScaling)
                {
                    _isDirty = true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Veneer: Failed to load layout: {ex.Message}");
                _currentLayout = new LayoutData
                {
                    Version = LayoutVersion,
                    SavedAtWidth = Screen.width,
                    SavedAtHeight = Screen.height
                };
            }
        }

        /// <summary>
        /// Saves the current layout to disk.
        /// </summary>
        public static void Save()
        {
            if (_currentLayout == null)
            {
                _currentLayout = new LayoutData
                {
                    Version = LayoutVersion,
                    SavedAtWidth = Screen.width,
                    SavedAtHeight = Screen.height
                };
            }

            try
            {
                // Gather all registered elements
                var elementList = new List<ElementData>();
                var registeredIds = VeneerAnchor.GetRegisteredElements();

                foreach (var elementId in registeredIds)
                {
                    var data = VeneerAnchor.GetAnchorData(elementId);
                    if (data != null)
                    {
                        elementList.Add(new ElementData
                        {
                            Id = data.ElementId,
                            Anchor = data.Anchor,
                            OffsetX = data.Offset.x,
                            OffsetY = data.Offset.y,
                            Width = data.Size.x,
                            Height = data.Size.y
                        });
                    }
                }

                _currentLayout.Elements = elementList;
                _currentLayout.Version = LayoutVersion;
                _currentLayout.SavedAtWidth = Screen.width;
                _currentLayout.SavedAtHeight = Screen.height;

                var json = JsonConvert.SerializeObject(_currentLayout, Formatting.Indented);
                File.WriteAllText(LayoutPath, json);

                _isDirty = false;
                Plugin.Log.LogInfo($"Veneer: Saved layout with {elementList.Count} elements at {Screen.width}x{Screen.height} to {LayoutPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Veneer: Failed to save layout: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Marks the layout as dirty (needs saving).
        /// </summary>
        public static void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Returns true if there are unsaved changes.
        /// </summary>
        public static bool IsDirty => _isDirty;

        /// <summary>
        /// Gets the saved position for an element.
        /// </summary>
        public static ElementData GetElementPosition(string elementId)
        {
            if (_currentLayout?.Elements == null) return null;

            foreach (var element in _currentLayout.Elements)
            {
                if (element.Id == elementId)
                    return element;
            }

            return null;
        }

        /// <summary>
        /// Resets all positions to defaults and saves.
        /// </summary>
        public static void ResetAll()
        {
            VeneerAnchor.ResetAllToDefault();
            Save();
            Plugin.Log.LogInfo("Veneer: All positions reset to defaults");
        }

        /// <summary>
        /// Resets a specific element to default and saves.
        /// </summary>
        public static void ResetElement(string elementId)
        {
            VeneerAnchor.ResetToDefault(elementId);
            MarkDirty();
        }

        /// <summary>
        /// Deletes the layout file.
        /// </summary>
        public static void DeleteLayoutFile()
        {
            if (File.Exists(LayoutPath))
            {
                try
                {
                    File.Delete(LayoutPath);
                    Plugin.Log.LogInfo("Veneer: Layout file deleted");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Veneer: Failed to delete layout file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Re-applies all positions after a resolution change.
        /// Scaling depends on anchor position:
        /// - Offsets from screen edges (anchor at 0 or 1) stay fixed
        /// - Offsets from screen center (anchor at 0.5) scale proportionally
        /// </summary>
        public static void ReapplyPositions()
        {
            if (_currentLayout?.Elements == null) return;

            // Calculate scale factors
            float scaleX = (_currentLayout.SavedAtWidth > 0)
                ? (float)Screen.width / _currentLayout.SavedAtWidth
                : 1f;
            float scaleY = (_currentLayout.SavedAtHeight > 0)
                ? (float)Screen.height / _currentLayout.SavedAtHeight
                : 1f;

            Plugin.Log.LogInfo($"Veneer: Reapplying positions from {_currentLayout.SavedAtWidth}x{_currentLayout.SavedAtHeight} to {Screen.width}x{Screen.height} (scale: {scaleX:F2}, {scaleY:F2})");

            foreach (var element in _currentLayout.Elements)
            {
                if (string.IsNullOrEmpty(element.Id)) continue;

                // Get anchor position (0, 0.5, or 1 for each axis)
                var anchorPos = VeneerAnchor.GetAnchorPosition(element.Anchor);

                // Scale offset based on anchor position:
                // - Anchor at edge (0 or 1): offset is from edge, keep fixed
                // - Anchor at center (0.5): offset is from center, scale it
                float offsetX = element.OffsetX;
                float offsetY = element.OffsetY;

                // Scale X if anchor is horizontally centered
                if (Math.Abs(anchorPos.x - 0.5f) < 0.01f)
                {
                    offsetX = element.OffsetX * scaleX;
                }

                // Scale Y if anchor is vertically centered
                if (Math.Abs(anchorPos.y - 0.5f) < 0.01f)
                {
                    offsetY = element.OffsetY * scaleY;
                }

                Plugin.Log.LogDebug($"Veneer: {element.Id} anchor={element.Anchor} ({anchorPos.x}, {anchorPos.y}): offset ({element.OffsetX}, {element.OffsetY}) -> ({offsetX:F0}, {offsetY:F0})");

                // Update anchor system
                VeneerAnchor.UpdatePosition(
                    element.Id,
                    element.Anchor,
                    new Vector2(offsetX, offsetY),
                    new Vector2(element.Width, element.Height)
                );

                // Re-apply to actual RectTransform if element is registered
                VeneerAnchor.ReapplyToRectTransform(element.Id);
            }

            // Update saved resolution to current
            _currentLayout.SavedAtWidth = Screen.width;
            _currentLayout.SavedAtHeight = Screen.height;
            _isDirty = true;
        }
    }

    /// <summary>
    /// Root layout data structure for JSON serialization.
    /// </summary>
    public class LayoutData
    {
        public int Version { get; set; }

        /// <summary>
        /// Screen width when this layout was saved.
        /// Used to scale positions when loading at different resolutions.
        /// </summary>
        public int SavedAtWidth { get; set; }

        /// <summary>
        /// Screen height when this layout was saved.
        /// Used to scale positions when loading at different resolutions.
        /// </summary>
        public int SavedAtHeight { get; set; }

        public List<ElementData> Elements { get; set; } = new List<ElementData>();
    }

    /// <summary>
    /// Individual element position data.
    /// </summary>
    public class ElementData
    {
        public string Id { get; set; }
        public ScreenAnchor Anchor { get; set; }
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }
}
