using HarmonyLib;
using UnityEngine;
using Veneer.Core;
using Veneer.Vanilla.Replacements;

namespace Veneer.Vanilla.Patches
{
    /// <summary>
    /// Harmony patches for the vanilla inventory system.
    /// </summary>
    [HarmonyPatch]
    public static class InventoryPatches
    {
        private static VeneerInventoryPanel _inventoryPanel;
        private static VeneerSkillsPanel _skillsPanel;
        private static VeneerTrophiesPanel _trophiesPanel;
        private static VeneerCraftingPanel _craftingPanel;
        private static VeneerCompendiumPanel _compendiumPanel;
        private static bool _initialized;

        /// <summary>
        /// Patch: Initialize inventory panel when InventoryGui awakens.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        [HarmonyPostfix]
        public static void InventoryGui_Awake_Postfix(InventoryGui __instance)
        {
            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceInventory.Value) return;
            if (_initialized) return;

            Plugin.Log.LogDebug("InventoryGui.Awake - Initializing Veneer Inventory");

            __instance.StartCoroutine(InitializeInventoryPanel(__instance));
        }

        private static System.Collections.IEnumerator InitializeInventoryPanel(InventoryGui gui)
        {
            yield return null; // Wait one frame

            if (VeneerAPI.IsReady)
            {
                CreateInventoryPanel(gui);
            }
            else
            {
                VeneerAPI.OnReady += () => CreateInventoryPanel(gui);
            }
        }

        private static void CreateInventoryPanel(InventoryGui gui)
        {
            if (_initialized) return;

            // Use VeneerHud for consistent resolution handling
            var parent = VeneerHud.Instance?.transform ?? gui.transform;
            _inventoryPanel = VeneerInventoryPanel.Create(parent);

            // Create standalone window panels (decoupled from tabs - can be opened independently)
            CreateStandaloneWindowPanels(parent);

            _initialized = true;

            Plugin.Log.LogInfo("Veneer Inventory Panel and Window Panels created");
        }

        private static void CreateStandaloneWindowPanels(Transform parent)
        {
            // Create Skills panel (standalone window)
            _skillsPanel = VeneerSkillsPanel.Create(parent);
            Plugin.Log.LogDebug("Created VeneerSkillsPanel (standalone)");

            // Create Trophies panel (standalone window)
            _trophiesPanel = VeneerTrophiesPanel.Create(parent);
            Plugin.Log.LogDebug("Created VeneerTrophiesPanel (standalone)");

            // Create Crafting panel (standalone window)
            _craftingPanel = VeneerCraftingPanel.Create(parent);
            Plugin.Log.LogDebug("Created VeneerCraftingPanel (standalone)");

            // Create Compendium panel (standalone popup)
            _compendiumPanel = VeneerCompendiumPanel.Create(parent);
            Plugin.Log.LogDebug("Created VeneerCompendiumPanel (standalone)");
        }

        /// <summary>
        /// Patch: Show Veneer inventory instead of vanilla.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        [HarmonyPostfix]
        public static void InventoryGui_Show_Postfix(InventoryGui __instance, Container container, int activeGroup)
        {
            // If VeneerCursor is just enabling cursor mode, don't show inventory panel
            if (VeneerCursor.IsCursorOnlyMode)
            {
                // Still hide vanilla inventory UI
                HideVanillaInventory(__instance);
                return;
            }

            // Notify VeneerCursor that inventory was opened (always, regardless of Veneer settings)
            // This tracks external inventory opens (Tab key, etc.)
            VeneerCursor.OnInventoryOpened();

            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceInventory.Value) return;

            if (_inventoryPanel != null)
            {
                _inventoryPanel.Show();

                if (container != null)
                {
                    _inventoryPanel.OpenContainer(container);
                }

                // Hide vanilla inventory elements
                HideVanillaInventory(__instance);
            }
        }

        /// <summary>
        /// Patch: Hide Veneer inventory when vanilla hides.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        [HarmonyPostfix]
        public static void InventoryGui_Hide_Postfix(InventoryGui __instance)
        {
            // Notify VeneerCursor that inventory was closed (always, regardless of Veneer settings)
            VeneerCursor.OnInventoryClosed();

            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceInventory.Value) return;

            if (_inventoryPanel != null)
            {
                _inventoryPanel.Hide();
                _inventoryPanel.CloseContainer();
            }
        }

        /// <summary>
        /// Patch: Sync inventory updates.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
        [HarmonyPrefix]
        public static bool InventoryGui_UpdateInventory_Prefix(InventoryGui __instance)
        {
            if (!VeneerConfig.Enabled.Value) return true;
            if (!VeneerConfig.ReplaceInventory.Value) return true;

            // Let Veneer handle inventory updates
            // We still run vanilla for compatibility, but hide the UI
            return true;
        }

        /// <summary>
        /// Patch: Keep vanilla container hidden during updates.
        /// The game re-enables the container UI each frame, so we need to hide it again.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
        [HarmonyPostfix]
        public static void InventoryGui_UpdateContainer_Postfix(InventoryGui __instance)
        {
            if (!VeneerConfig.Enabled.Value) return;
            if (!VeneerConfig.ReplaceInventory.Value) return;

            // Keep the vanilla container hidden
            if (__instance.m_container != null)
            {
                __instance.m_container.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Patch: Override IsVisible to return false when cursor is force-hidden.
        /// This allows player movement/combat while keeping the UI visible on screen.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsVisible))]
        [HarmonyPostfix]
        public static void InventoryGui_IsVisible_Postfix(ref bool __result)
        {
            // When cursor is force-hidden, pretend inventory is not visible
            // so the game allows player input (WASD, mouse look, combat, etc.)
            if (VeneerCursor.IsForcedHidden && __result)
            {
                __result = false;
            }
        }

        private static void HideVanillaInventory(InventoryGui gui)
        {
            // Hide the entire root panel which contains all vanilla inventory UI
            var root = gui.transform.Find("root");
            if (root != null)
            {
                root.gameObject.SetActive(false);
                Plugin.Log.LogDebug("Hid vanilla inventory root");
            }

            // Also try to hide individual sections as fallback
            if (gui.m_player != null)
            {
                gui.m_player.gameObject.SetActive(false);
            }

            if (gui.m_container != null)
            {
                gui.m_container.gameObject.SetActive(false);
            }

            // Hide crafting
            var craftingRoot = gui.transform.Find("root/Crafting");
            if (craftingRoot != null)
            {
                craftingRoot.gameObject.SetActive(false);
            }

            // Hide info panel
            var infoPanel = gui.transform.Find("root/Info");
            if (infoPanel != null)
            {
                infoPanel.gameObject.SetActive(false);
            }

            // Hide trophies panel
            var trophiesPanel = gui.transform.Find("root/Trophies");
            if (trophiesPanel != null)
            {
                trophiesPanel.gameObject.SetActive(false);
            }

            // Hide skills panel
            var skillsPanel = gui.transform.Find("root/Skills");
            if (skillsPanel != null)
            {
                skillsPanel.gameObject.SetActive(false);
            }

            // Hide text and split dialog
            var textInput = gui.transform.Find("root/TextInput");
            if (textInput != null)
            {
                textInput.gameObject.SetActive(false);
            }

            var splitPanel = gui.transform.Find("root/SplitPanel");
            if (splitPanel != null)
            {
                splitPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Show vanilla inventory elements (for when Veneer is disabled).
        /// </summary>
        public static void ShowVanillaInventory(InventoryGui gui)
        {
            var root = gui.transform.Find("root");
            if (root != null)
            {
                root.gameObject.SetActive(true);
            }

            if (gui.m_player != null)
            {
                gui.m_player.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public static void Cleanup()
        {
            if (_inventoryPanel != null)
            {
                Object.Destroy(_inventoryPanel.gameObject);
                _inventoryPanel = null;
            }
            if (_skillsPanel != null)
            {
                Object.Destroy(_skillsPanel.gameObject);
                _skillsPanel = null;
            }
            if (_trophiesPanel != null)
            {
                Object.Destroy(_trophiesPanel.gameObject);
                _trophiesPanel = null;
            }
            if (_craftingPanel != null)
            {
                Object.Destroy(_craftingPanel.gameObject);
                _craftingPanel = null;
            }
            if (_compendiumPanel != null)
            {
                Object.Destroy(_compendiumPanel.gameObject);
                _compendiumPanel = null;
            }

            // Clear window manager registrations
            VeneerWindowManager.Clear();

            _initialized = false;
        }
    }
}
