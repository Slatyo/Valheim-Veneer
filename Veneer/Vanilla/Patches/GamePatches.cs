using HarmonyLib;
using UnityEngine;
using Veneer.Core;
using Veneer.Grid;

namespace Veneer.Vanilla.Patches
{
    /// <summary>
    /// Harmony patches for game lifecycle events.
    /// Handles cleanup when player logs out.
    /// </summary>
    [HarmonyPatch]
    public static class GamePatches
    {
        /// <summary>
        /// Patch: Clean up when player logs out.
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
        [HarmonyPrefix]
        public static void Game_Logout_Prefix()
        {
            Plugin.Log.LogInfo("Game.Logout - Saving layout and cleaning up Veneer");

            // Save layout before logout
            VeneerLayout.Save();

            // Clean up UI systems so they re-initialize on next login
            VeneerAPI.Cleanup();
        }
    }

    /// <summary>
    /// Patches for GameCamera to allow cursor control override.
    /// When VeneerCursor has force-hidden the cursor, we prevent the game from showing it.
    /// </summary>
    [HarmonyPatch]
    public static class GameCameraPatches
    {
        /// <summary>
        /// Patch: After UpdateMouseCapture, re-hide cursor if VeneerCursor has force-hidden it.
        /// </summary>
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
        [HarmonyPostfix]
        public static void UpdateMouseCapture_Postfix(GameCamera __instance)
        {
            // If VeneerCursor has force-hidden the cursor, override the game's decision
            if (VeneerCursor.IsForcedHidden)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
