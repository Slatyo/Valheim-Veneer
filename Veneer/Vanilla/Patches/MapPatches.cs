using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Core;
using Veneer.Vanilla.Replacements;

namespace Veneer.Vanilla.Patches
{
    /// <summary>
    /// Patch to detect and debug map key presses.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), "Update")]
    public static class Minimap_Update_Debug
    {
        private static float _lastLogTime = 0f;

        [HarmonyPrefix]
        public static void Prefix(Minimap __instance)
        {
            // Only log once per second to avoid spam
            if (Time.time - _lastLogTime < 1f) return;

            // Check if M key (Map button) would be pressed
            if (ZInput.GetButtonDown("Map"))
            {
                _lastLogTime = Time.time;
                Plugin.Log.LogInfo($"Minimap.Update: Map button pressed! InventoryGui.IsVisible={InventoryGui.IsVisible()}, Hud.IsPieceSelectionVisible={Hud.IsPieceSelectionVisible()}");
            }
        }
    }

    /// <summary>
    /// Harmony patches for the vanilla map to use Veneer styling.
    /// - Keeps minimap visible when large map opens
    /// - Wraps large map in a moveable/resizable Veneer window
    /// </summary>
    [HarmonyPatch]
    public static class MapPatches
    {
        private static VeneerLargeMapFrame _largeMapFrame;

        /// <summary>
        /// Patch: Intercept SetMapMode to use our Veneer frame.
        /// </summary>
        // Track if we're currently showing the large map (internal for MapPanPatches access)
        internal static bool _isLargeMapShowing = false;

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
        [HarmonyPrefix]
        public static bool Minimap_SetMapMode_Prefix(Minimap __instance, Minimap.MapMode mode)
        {
            Plugin.Log.LogDebug($"MapPatches.SetMapMode_Prefix: mode={mode}, _isLargeMapShowing={_isLargeMapShowing}");

            if (!VeneerConfig.Enabled.Value) return true;
            if (!VeneerConfig.ReplaceMap.Value) return true;

            if (mode == Minimap.MapMode.Large)
            {
                // Toggle behavior - if already showing, close it instead
                if (_isLargeMapShowing && _largeMapFrame != null && _largeMapFrame.IsShowing)
                {
                    Plugin.Log.LogInfo("MapPatches: Map already open, closing it");
                    _largeMapFrame.Hide();
                    _largeMapFrame.gameObject.SetActive(false);
                    _isLargeMapShowing = false;

                    // Notify VeneerCursor that map was closed
                    VeneerCursor.OnMapStateChanged(false);

                    // Let vanilla close its internal state
                    __instance.SetMapMode(Minimap.MapMode.Small);
                    return false;
                }

                // Show our Veneer frame
                EnsureLargeMapFrame();
                if (_largeMapFrame != null)
                {
                    _largeMapFrame.gameObject.SetActive(true);
                    _largeMapFrame.Show();
                    _isLargeMapShowing = true;
                    Plugin.Log.LogInfo($"MapPatches: Large map opened, activeSelf={_largeMapFrame.gameObject.activeSelf}");

                    // Notify VeneerCursor that map was opened
                    VeneerCursor.OnMapStateChanged(true);
                }

                // LET vanilla run so it sets internal mode to Large (needed for map rendering)
                // But we'll hide its UI in the postfix
                return true;
            }
            else if (mode == Minimap.MapMode.Small || mode == Minimap.MapMode.None)
            {
                // Hide our frame when closing map
                if (_largeMapFrame != null && _largeMapFrame.IsShowing)
                {
                    Plugin.Log.LogInfo("MapPatches: Hiding large map frame");
                    _largeMapFrame.Hide();
                    _largeMapFrame.gameObject.SetActive(false);
                }

                bool wasOpen = _isLargeMapShowing;
                _isLargeMapShowing = false;

                // Notify VeneerCursor that map was closed
                if (wasOpen)
                {
                    VeneerCursor.OnMapStateChanged(false);
                }

                // Reset pan state
                MapPanPatches.Reset();

                // Let vanilla handle closing
                return true;
            }

            return true;
        }

        /// <summary>
        /// Patch: After SetMapMode, ensure minimap stays visible and hide vanilla large map UI.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
        [HarmonyPostfix]
        public static void Minimap_SetMapMode_Postfix(Minimap __instance, Minimap.MapMode mode)
        {
            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceMap.Value) return;

            if (mode == Minimap.MapMode.Large)
            {
                // Keep minimap visible even when large map is open
                if (__instance.m_smallRoot != null)
                {
                    __instance.m_smallRoot.SetActive(true);
                }

                // Hide vanilla's large map UI elements (we wrap them in our frame, but
                // vanilla might have its own background/overlay we need to hide)
                // The m_largeRoot is what we wrap, so its contents should be visible
                // but vanilla might show other elements
            }
        }

        /// <summary>
        /// Initialize the large map frame when Minimap starts.
        /// This ensures the frame is created early so it appears in Edit Mode.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "Start")]
        [HarmonyPostfix]
        public static void Minimap_Start_Postfix()
        {
            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceMap.Value) return;

            // Create frame eagerly so it's available in Edit Mode
            EnsureLargeMapFrame();
        }

        /// <summary>
        /// Creates the Veneer large map frame if needed.
        /// </summary>
        private static void EnsureLargeMapFrame()
        {
            if (_largeMapFrame != null && _largeMapFrame.gameObject != null)
            {
                return;
            }

            _largeMapFrame = null;

            if (!VeneerAPI.IsReady)
            {
                Plugin.Log.LogWarning("MapPatches: VeneerAPI not ready, cannot create large map frame");
                return;
            }

            _largeMapFrame = VeneerLargeMapFrame.Create(VeneerAPI.UIRoot);
            Plugin.Log.LogInfo("MapPatches: VeneerLargeMapFrame created");
        }

        /// <summary>
        /// Cleanup when Minimap is destroyed.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnDestroy))]
        [HarmonyPostfix]
        public static void Minimap_OnDestroy_Postfix()
        {
            Cleanup();
        }

        /// <summary>
        /// Cleans up the map frame.
        /// </summary>
        public static void Cleanup()
        {
            if (_largeMapFrame != null)
            {
                Object.Destroy(_largeMapFrame.gameObject);
                _largeMapFrame = null;
            }
            Plugin.Log.LogDebug("MapPatches: Cleanup complete");
        }

        /// <summary>
        /// Patch: Return true when our Veneer large map is showing.
        /// This is CRITICAL for input routing - vanilla uses IsOpen() to decide
        /// whether scroll should go to map zoom vs camera zoom.
        /// Also returns false when cursor is force-hidden to allow player input.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.IsOpen))]
        [HarmonyPostfix]
        public static void Minimap_IsOpen_Postfix(ref bool __result)
        {
            // When cursor is force-hidden, pretend map is not open
            // so the game allows player input (WASD, mouse look, combat, etc.)
            if (VeneerCursor.IsForcedHidden && __result)
            {
                __result = false;
                return;
            }

            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceMap.Value) return;

            // Return true when our large map is showing - this routes scroll to map, not camera
            if (_isLargeMapShowing && _largeMapFrame != null && _largeMapFrame.IsShowing)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Custom map panning for Veneer's reparented map.
    ///
    /// The problem: Vanilla's pan uses ScreenPointToLocalPointInRectangle which breaks
    /// when reparented because coordinates are calculated relative to the wrong parent.
    ///
    /// Solution: We directly modify m_mapOffset when dragging. We track dragging ourselves
    /// and apply delta movements to the offset. Vanilla's other systems (pins, coordinates)
    /// will use the modified offset correctly.
    /// </summary>
    [HarmonyPatch]
    public static class MapPanPatches
    {
        private static bool _isDragging;
        private static Vector3 _lastMousePosition;

        // Reflection for private fields
        private static FieldInfo _mapOffsetField;
        private static FieldInfo _largeZoomField;

        private static Vector3 GetMapOffset(Minimap minimap)
        {
            if (_mapOffsetField == null)
            {
                _mapOffsetField = typeof(Minimap).GetField("m_mapOffset", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            return (Vector3)_mapOffsetField.GetValue(minimap);
        }

        private static void SetMapOffset(Minimap minimap, Vector3 value)
        {
            if (_mapOffsetField == null)
            {
                _mapOffsetField = typeof(Minimap).GetField("m_mapOffset", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            _mapOffsetField.SetValue(minimap, value);
        }

        private static float GetLargeZoom(Minimap minimap)
        {
            if (_largeZoomField == null)
            {
                _largeZoomField = typeof(Minimap).GetField("m_largeZoom", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            return (float)_largeZoomField.GetValue(minimap);
        }

        [HarmonyPatch(typeof(Minimap), "Update")]
        [HarmonyPostfix]
        public static void Minimap_Update_Postfix(Minimap __instance)
        {
            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceMap.Value) return;
            if (__instance.m_mode != Minimap.MapMode.Large) return;
            if (!MapPatches._isLargeMapShowing) return;

            HandlePanInput(__instance);
        }

        private static void HandlePanInput(Minimap minimap)
        {
            bool leftMouseDown = Input.GetMouseButton(0);
            bool leftMouseJustPressed = Input.GetMouseButtonDown(0);
            Vector3 mousePos = Input.mousePosition;

            // Start drag
            if (leftMouseJustPressed && !_isDragging)
            {
                if (IsMouseOverMap(minimap))
                {
                    _isDragging = true;
                    _lastMousePosition = mousePos;
                }
            }
            // End drag
            else if (!leftMouseDown)
            {
                _isDragging = false;
            }

            // During drag - apply delta to m_mapOffset
            if (_isDragging)
            {
                Vector3 delta = mousePos - _lastMousePosition;
                _lastMousePosition = mousePos;

                if (delta.sqrMagnitude > 0.1f)
                {
                    // Get current offset and modify it
                    Vector3 currentOffset = GetMapOffset(minimap);
                    float zoom = GetLargeZoom(minimap);

                    // Speed scaling based on user feedback:
                    // - Zoom 0.03 = speed 1.0 (feels great)
                    // - Zoom 0.08 = speed ~2.0 (needs doubling)
                    // - Zoom 0.29 = speed ~4.0 (needs 4x)
                    // Formula: scale proportionally to zoom, with 0.03 as baseline
                    float baselineZoom = 0.03f;
                    float speed = zoom / baselineZoom;

                    currentOffset.x -= delta.x * speed;
                    currentOffset.z -= delta.y * speed;

                    SetMapOffset(minimap, currentOffset);
                }
            }
        }

        private static bool IsMouseOverMap(Minimap minimap)
        {
            RawImage mapImage = minimap.m_mapImageLarge;
            if (mapImage == null) return false;

            // Get the map image's screen rect
            RectTransform rt = mapImage.rectTransform;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector2 mousePos = Input.mousePosition;

            // Check if mouse is within the corners (screen space)
            float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

            return mousePos.x >= minX && mousePos.x <= maxX &&
                   mousePos.y >= minY && mousePos.y <= maxY;
        }

        internal static void Reset()
        {
            _isDragging = false;
        }
    }
}
