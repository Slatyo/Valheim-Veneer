using System;
using System.Collections.Generic;
using UnityEngine;

namespace Veneer.Extensions
{
    /// <summary>
    /// Base interface for all Veneer UI extensions.
    /// Extensions allow other mods to hook into Veneer's UI components.
    /// </summary>
    public interface IVeneerExtension
    {
        /// <summary>
        /// Priority for ordering extensions. Lower values run first.
        /// Default extensions are 0. Use negative values to run before defaults.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Unique identifier for this extension (e.g., "mymod.quickbar.teleport").
        /// Used for logging and debugging.
        /// </summary>
        string ExtensionId { get; }
    }

    #region QuickBar Extensions

    /// <summary>
    /// Context for QuickBar extensions.
    /// </summary>
    public class QuickBarContext
    {
        /// <summary>
        /// The QuickBar's content transform where buttons are added.
        /// </summary>
        public Transform ButtonContainer { get; set; }

        /// <summary>
        /// Reference to the QuickBar instance.
        /// </summary>
        public Component QuickBar { get; set; }
    }

    /// <summary>
    /// Interface for adding buttons to the QuickBar.
    /// </summary>
    public interface IQuickBarExtension : IVeneerExtension
    {
        /// <summary>
        /// Called when the QuickBar is initialized.
        /// Add your buttons here.
        /// </summary>
        void OnQuickBarCreated(QuickBarContext context);

        /// <summary>
        /// Called when the QuickBar is destroyed.
        /// Clean up any references.
        /// </summary>
        void OnQuickBarDestroyed();
    }

    #endregion

    #region Inventory Extensions

    /// <summary>
    /// Position where inventory extensions can be placed.
    /// </summary>
    public enum InventoryExtensionPosition
    {
        /// <summary>Above the inventory grid.</summary>
        AboveGrid,
        /// <summary>Below the inventory grid.</summary>
        BelowGrid,
        /// <summary>Left of the inventory panel.</summary>
        LeftPanel,
        /// <summary>Right of the inventory panel.</summary>
        RightPanel,
        /// <summary>In the header/toolbar area.</summary>
        Toolbar
    }

    /// <summary>
    /// Context for Inventory extensions.
    /// </summary>
    public class InventoryContext
    {
        /// <summary>
        /// The main inventory panel.
        /// </summary>
        public Component InventoryPanel { get; set; }

        /// <summary>
        /// Container for extensions above the grid.
        /// </summary>
        public Transform AboveGridContainer { get; set; }

        /// <summary>
        /// Container for extensions below the grid.
        /// </summary>
        public Transform BelowGridContainer { get; set; }

        /// <summary>
        /// Container for extensions on the left.
        /// </summary>
        public Transform LeftContainer { get; set; }

        /// <summary>
        /// Container for extensions on the right.
        /// </summary>
        public Transform RightContainer { get; set; }

        /// <summary>
        /// Container for toolbar extensions.
        /// </summary>
        public Transform ToolbarContainer { get; set; }

        /// <summary>
        /// Gets the container for the specified position.
        /// </summary>
        public Transform GetContainer(InventoryExtensionPosition position)
        {
            return position switch
            {
                InventoryExtensionPosition.AboveGrid => AboveGridContainer,
                InventoryExtensionPosition.BelowGrid => BelowGridContainer,
                InventoryExtensionPosition.LeftPanel => LeftContainer,
                InventoryExtensionPosition.RightPanel => RightContainer,
                InventoryExtensionPosition.Toolbar => ToolbarContainer,
                _ => null
            };
        }
    }

    /// <summary>
    /// Interface for extending the Inventory panel.
    /// </summary>
    public interface IInventoryExtension : IVeneerExtension
    {
        /// <summary>
        /// Called when the Inventory panel is created.
        /// </summary>
        void OnInventoryCreated(InventoryContext context);

        /// <summary>
        /// Called when the Inventory panel is destroyed.
        /// </summary>
        void OnInventoryDestroyed();

        /// <summary>
        /// Called when the Inventory is shown.
        /// </summary>
        void OnInventoryShown(InventoryContext context);

        /// <summary>
        /// Called when the Inventory is hidden.
        /// </summary>
        void OnInventoryHidden();
    }

    #endregion

    #region Window Extensions

    /// <summary>
    /// Context for generic window extensions.
    /// </summary>
    public class WindowContext
    {
        /// <summary>
        /// The window's unique ID (e.g., "Veneer_Skills", "Veneer_Crafting").
        /// </summary>
        public string WindowId { get; set; }

        /// <summary>
        /// The window's content transform.
        /// </summary>
        public Transform ContentArea { get; set; }

        /// <summary>
        /// Reference to the window component.
        /// </summary>
        public Component Window { get; set; }

        /// <summary>
        /// Container for header/toolbar extensions (if available).
        /// </summary>
        public Transform ToolbarContainer { get; set; }

        /// <summary>
        /// Container for footer extensions (if available).
        /// </summary>
        public Transform FooterContainer { get; set; }
    }

    /// <summary>
    /// Interface for extending any Veneer window.
    /// Use WindowId to filter which windows to extend.
    /// </summary>
    public interface IWindowExtension : IVeneerExtension
    {
        /// <summary>
        /// Returns true if this extension should apply to the given window.
        /// </summary>
        bool AppliesTo(string windowId);

        /// <summary>
        /// Called when a matching window is created.
        /// </summary>
        void OnWindowCreated(WindowContext context);

        /// <summary>
        /// Called when a matching window is destroyed.
        /// </summary>
        void OnWindowDestroyed(string windowId);

        /// <summary>
        /// Called when a matching window is shown.
        /// </summary>
        void OnWindowShown(WindowContext context);

        /// <summary>
        /// Called when a matching window is hidden.
        /// </summary>
        void OnWindowHidden(string windowId);
    }

    #endregion

    #region Hotbar Extensions

    /// <summary>
    /// Context for Hotbar extensions.
    /// </summary>
    public class HotbarContext
    {
        /// <summary>
        /// The Hotbar's container transform.
        /// </summary>
        public Transform Container { get; set; }

        /// <summary>
        /// Reference to the Hotbar component.
        /// </summary>
        public Component Hotbar { get; set; }

        /// <summary>
        /// Container for extensions to the left of the hotbar.
        /// </summary>
        public Transform LeftContainer { get; set; }

        /// <summary>
        /// Container for extensions to the right of the hotbar.
        /// </summary>
        public Transform RightContainer { get; set; }
    }

    /// <summary>
    /// Interface for extending the Hotbar.
    /// </summary>
    public interface IHotbarExtension : IVeneerExtension
    {
        /// <summary>
        /// Called when the Hotbar is created.
        /// </summary>
        void OnHotbarCreated(HotbarContext context);

        /// <summary>
        /// Called when the Hotbar is destroyed.
        /// </summary>
        void OnHotbarDestroyed();
    }

    #endregion

    #region HUD Extensions

    /// <summary>
    /// Context for HUD extensions (global UI additions).
    /// </summary>
    public class HudContext
    {
        /// <summary>
        /// The HUD root transform.
        /// </summary>
        public Transform HudRoot { get; set; }

        /// <summary>
        /// Container for top-left HUD elements.
        /// </summary>
        public Transform TopLeftContainer { get; set; }

        /// <summary>
        /// Container for top-right HUD elements.
        /// </summary>
        public Transform TopRightContainer { get; set; }

        /// <summary>
        /// Container for bottom-left HUD elements.
        /// </summary>
        public Transform BottomLeftContainer { get; set; }

        /// <summary>
        /// Container for bottom-right HUD elements.
        /// </summary>
        public Transform BottomRightContainer { get; set; }

        /// <summary>
        /// Container for center-screen elements (notifications, etc.).
        /// </summary>
        public Transform CenterContainer { get; set; }
    }

    /// <summary>
    /// Interface for adding custom HUD elements.
    /// </summary>
    public interface IHudExtension : IVeneerExtension
    {
        /// <summary>
        /// Called when the HUD is initialized.
        /// </summary>
        void OnHudCreated(HudContext context);

        /// <summary>
        /// Called when the HUD is destroyed.
        /// </summary>
        void OnHudDestroyed();
    }

    #endregion
}
