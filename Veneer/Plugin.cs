using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer
{
    /// <summary>
    /// Veneer - UI framework for Valheim.
    /// Provides consistent, themeable UI components with a grid-based positioning system.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.slatyo.veneer";
        public const string PluginName = "Veneer";
        public const string PluginVersion = "1.0.0";

        /// <summary>
        /// Logger instance for Veneer.
        /// </summary>
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        // Resolution tracking for responsive repositioning
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private bool _resolutionChangeScheduled;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{PluginVersion} loading...");

            // Initialize configuration
            VeneerConfig.Initialize(Config);

            // Initialize Harmony
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            // Initialize Veneer systems when GUI is ready
            GUIManager.OnCustomGUIAvailable += OnGUIAvailable;

            Log.LogInfo($"{PluginName} loaded successfully");
        }

        private void OnGUIAvailable()
        {
            Log.LogInfo("OnGUIAvailable fired - attempting to initialize Veneer");

            // Initialize the Veneer API (can be called multiple times - it's idempotent)
            VeneerAPI.Initialize();

            Log.LogDebug("Veneer UI systems initialized");

            // Don't unsubscribe - we need this to fire again after logout/login
            // VeneerAPI.Initialize() handles the idempotency check internally
        }

        private void Update()
        {
            if (!VeneerAPI.IsReady) return;

            // Check for resolution changes (user changed in Options menu)
            CheckResolutionChange();

            // Check for edit mode toggle keybind
            if (VeneerConfig.EditModeKey.Value.IsDown())
            {
                VeneerMover.ToggleEditMode();
            }

            // Check for cursor mode toggle keybind (Alt+Q)
            if (VeneerConfig.CursorModeKey.Value.IsDown())
            {
                VeneerCursor.Toggle();
            }
        }

        private void CheckResolutionChange()
        {
            int currentWidth = Screen.width;
            int currentHeight = Screen.height;

            // Initialize on first check
            if (_lastScreenWidth == 0 || _lastScreenHeight == 0)
            {
                _lastScreenWidth = currentWidth;
                _lastScreenHeight = currentHeight;
                return;
            }

            // Check if resolution changed
            if (currentWidth != _lastScreenWidth || currentHeight != _lastScreenHeight)
            {
                // Schedule the reapply with a short delay to let Unity finish applying resolution
                if (!_resolutionChangeScheduled)
                {
                    _resolutionChangeScheduled = true;
                    StartCoroutine(DelayedResolutionChange(_lastScreenWidth, _lastScreenHeight));
                }

                _lastScreenWidth = currentWidth;
                _lastScreenHeight = currentHeight;
            }
        }

        private IEnumerator DelayedResolutionChange(int oldWidth, int oldHeight)
        {
            // Wait a few frames for Unity to fully apply the resolution change
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            Log.LogInfo($"[Veneer] Resolution changed from {oldWidth}x{oldHeight} to {Screen.width}x{Screen.height}");

            // Reapply all positions
            VeneerLayout.ReapplyPositions();

            _resolutionChangeScheduled = false;
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            VeneerAPI.Cleanup();
        }
    }
}
