using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Veneer.Core;
using Veneer.Vanilla.Replacements;

namespace Veneer.Vanilla.Patches
{
    /// <summary>
    /// Harmony patches for the vanilla HUD to integrate Veneer replacements.
    /// </summary>
    [HarmonyPatch]
    public static class HudPatches
    {
        private static bool _hudInitialized;

        // Cached reflection for EnemyHud.m_huds
        private static FieldInfo _enemyHudHudsField;

        private static Dictionary<Character, EnemyHud.HudData> GetEnemyHuds(EnemyHud instance)
        {
            if (_enemyHudHudsField == null)
            {
                _enemyHudHudsField = typeof(EnemyHud).GetField("m_huds", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            return _enemyHudHudsField?.GetValue(instance) as Dictionary<Character, EnemyHud.HudData>;
        }

        /// <summary>
        /// Patch: Initialize Veneer HUD when vanilla HUD awakens.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        [HarmonyPostfix]
        public static void Hud_Awake_Postfix(Hud __instance)
        {
            if (!VeneerConfig.Enabled.Value) return;
            if (_hudInitialized) return;

            Plugin.Log.LogDebug("Hud.Awake - Initializing Veneer HUD");

            // Wait a frame for HUD to fully initialize
            __instance.StartCoroutine(InitializeVeneerHud(__instance));
        }

        private static System.Collections.IEnumerator InitializeVeneerHud(Hud hud)
        {
            yield return null; // Wait one frame

            if (VeneerAPI.IsReady)
            {
                // Create Veneer HUD as sibling to vanilla HUD root
                var hudRoot = hud.m_rootObject?.transform.parent ?? hud.transform;
                VeneerHud.Initialize(hudRoot, hud);
                VeneerHud.Instance?.HideVanillaElements();

                // Create QuickBar (permanently visible on HUD layer)
                CreateQuickBar();

                _hudInitialized = true;
            }
            else
            {
                // Wait for Veneer to be ready
                VeneerAPI.OnReady += () =>
                {
                    var hudRoot = hud.m_rootObject?.transform.parent ?? hud.transform;
                    VeneerHud.Initialize(hudRoot, hud);
                    VeneerHud.Instance?.HideVanillaElements();

                    // Create QuickBar (permanently visible on HUD layer)
                    CreateQuickBar();

                    _hudInitialized = true;
                };
            }
        }

        private static void CreateQuickBar()
        {
            // Create QuickBar under VeneerHud for consistent resolution handling
            var parent = VeneerHud.Instance?.transform;
            if (parent != null)
            {
                VeneerQuickBar.Create(parent);
            }
            else
            {
                Plugin.Log.LogWarning("HudPatches: Could not create QuickBar - VeneerHud not available");
            }
        }

        /// <summary>
        /// Patch: Prevent vanilla health bar updates when replaced.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateHealth))]
        [HarmonyPrefix]
        public static bool Hud_UpdateHealth_Prefix()
        {
            if (!VeneerConfig.Enabled.Value) return true;
            if (!VeneerConfig.ReplaceHealthBar.Value) return true;

            // Skip vanilla update - our VeneerUnitFrame handles it
            return false;
        }

        /// <summary>
        /// Patch: Prevent vanilla stamina bar updates when replaced.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateStamina))]
        [HarmonyPrefix]
        public static bool Hud_UpdateStamina_Prefix()
        {
            if (!VeneerConfig.Enabled.Value) return true;
            if (!VeneerConfig.ReplaceStaminaBar.Value) return true;

            // Skip vanilla update - our VeneerUnitFrame handles it
            return false;
        }

        /// <summary>
        /// Patch: Prevent vanilla eitr bar updates when replaced.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateEitr))]
        [HarmonyPrefix]
        public static bool Hud_UpdateEitr_Prefix()
        {
            if (!VeneerConfig.Enabled.Value) return true;
            if (!VeneerConfig.ReplaceHealthBar.Value) return true;

            // Skip vanilla update - our VeneerUnitFrame handles it
            return false;
        }

        /// <summary>
        /// Patch: Prevent vanilla food icon updates when replaced.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateFood))]
        [HarmonyPrefix]
        public static bool Hud_UpdateFood_Prefix()
        {
            if (!VeneerConfig.Enabled.Value) return true;
            if (!VeneerConfig.ReplaceFoodSlots.Value) return true;

            // Skip vanilla update - our VeneerFoodBar handles it
            return false;
        }

        /// <summary>
        /// Patch: Prevent vanilla status effect updates when replaced.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateStatusEffects))]
        [HarmonyPrefix]
        public static bool Hud_UpdateStatusEffects_Prefix()
        {
            if (!VeneerConfig.Enabled.Value) return true;
            if (!VeneerConfig.ReplaceStatusEffects.Value) return true;

            // Skip vanilla update - our VeneerStatusBar handles it
            return false;
        }

        /// <summary>
        /// Patch: Prevent vanilla boss HUD from being shown when Veneer replaces it.
        /// </summary>
        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
        [HarmonyPrefix]
        public static bool EnemyHud_ShowHud_Prefix(Character c, bool isMount)
        {
            if (!VeneerConfig.Enabled.Value) return true;
            if (!VeneerConfig.ReplaceBossHealth.Value) return true;
            if (isMount) return true;

            // For bosses, show our frame and skip vanilla
            if (c != null && c.IsBoss())
            {
                VeneerHud.Instance?.SetBoss(c);
                return false; // Skip vanilla boss HUD creation
            }

            return true; // Allow vanilla HUD for non-bosses
        }

        /// <summary>
        /// Patch: Hide vanilla boss HUD elements that may already exist.
        /// </summary>
        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
        [HarmonyPostfix]
        public static void EnemyHud_UpdateHuds_Postfix(EnemyHud __instance)
        {
            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceBossHealth.Value) return;

            // Hide any boss HUDs that might have been created (use reflection for private field)
            var huds = GetEnemyHuds(__instance);
            if (huds != null)
            {
                foreach (var kvp in huds)
                {
                    var character = kvp.Key;
                    var hudData = kvp.Value;

                    if (character != null && character.IsBoss() && hudData.m_gui != null)
                    {
                        hudData.m_gui.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Patch: Clear boss frame when boss dies or goes out of range.
        /// </summary>
        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.LateUpdate))]
        [HarmonyPostfix]
        public static void EnemyHud_LateUpdate_Postfix(EnemyHud __instance)
        {
            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceBossHealth.Value) return;

            var bossFrame = VeneerHud.Instance?.BossFrame;
            if (bossFrame == null || !bossFrame.IsActive) return;

            var boss = bossFrame.TrackedBoss;
            if (boss == null || boss.IsDead())
            {
                VeneerHud.Instance?.ClearBoss();
            }
        }

        /// <summary>
        /// Cleanup when HUD is destroyed.
        /// </summary>
        [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
        [HarmonyPostfix]
        public static void Hud_OnDestroy_Postfix()
        {
            VeneerHud.Cleanup();
            VeneerQuickBar.Cleanup();
            _hudInitialized = false;
        }
    }
}
