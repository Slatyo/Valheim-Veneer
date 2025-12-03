# Veneer

**UI Framework for Valheim Mods**

Veneer provides a complete, consistent UI component library for Valheim mod developers. Create beautiful panels, frames, bars, buttons, tooltips, and more with a unified dark aesthetic.

## Features

### For Players

- **Redesigned HUD Elements** - Health, stamina, and eitr bars with clean, modern styling
- **Boss Frame** - Track boss health with up to 20 bosses displayed simultaneously
- **Inventory Overhaul** - Redesigned inventory grid with item quality indicators
- **Minimap Enhancements** - Cleaner minimap frame with configurable styling
- **Edit Mode** - Press F8 to drag and reposition any Veneer UI element
- **Fully Configurable** - Adjust colors, sizes, fonts, and more via config file

### For Mod Developers

- **Complete Component Library** - Panels, frames, bars, buttons, text, tooltips, grids
- **Consistent Theming** - All components follow the same visual style
- **Easy API** - Simple factory methods for creating UI elements
- **Position Persistence** - Built-in save/load for element positions
- **Edit Mode Integration** - Your elements automatically become draggable in edit mode

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Install [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)
3. Place `Veneer.dll` in your `BepInEx/plugins` folder

## Configuration

Configuration file: `BepInEx/config/com.slatyo.veneer.cfg`

```ini
[General]
Enabled = true

[Colors]
AccentColor = Gold    # Gold, Blue, Green, Purple, Red, White

[Fonts]
FontScale = 1.0       # 0.8 to 1.5

[EditMode]
GridSnapSize = 10     # Snap to grid when dragging
ShowGridLines = true  # Show grid overlay in edit mode
```

## API Documentation

### Getting Started

Add Veneer as a dependency to your mod:

```csharp
[BepInPlugin("com.yourname.yourmod", "Your Mod", "1.0.0")]
[BepInDependency("com.slatyo.veneer")]
public class YourPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Wait for Veneer to initialize
        VeneerAPI.OnReady += OnVeneerReady;
    }

    private void OnVeneerReady()
    {
        // Now you can use the API
        CreateMyUI();
    }
}
```

### Creating Panels

```csharp
// Simple panel with size
var panel = VeneerAPI.CreatePanel(parent, width: 200, height: 150);

// Panel that stretches to fill parent
var stretched = VeneerAPI.CreateStretchedPanel(parent);

// Panel with custom border color
var rarityPanel = VeneerAPI.CreatePanelWithBorder(parent, VeneerAPI.GetRarityColor(3));

// Panel with thick border (boss frames, legendary items)
var bossPanel = VeneerAPI.CreatePanelWithThickBorder(parent, VeneerAPI.LegendaryColor, borderWidth: 2);

// Legendary-styled panel (gold border)
var legendary = VeneerAPI.CreateLegendaryPanel(parent, 100, 100);
```

### Creating Frames

Frames are standalone UI containers with position saving:

```csharp
// Simple frame
var frame = VeneerAPI.CreateFrame("mymod_frame", width: 300, height: 200,
    AnchorPreset.TopRight, offset: new Vector2(-20, -20));

// Full configuration
var config = new FrameConfig
{
    Id = "mymod_settings",
    Width = 400,
    Height = 300,
    Anchor = AnchorPreset.Center,
    Moveable = true,
    SavePosition = true,
    ShowInEditMode = true
};
var configuredFrame = VeneerAPI.CreateFrame(config);
```

### Creating Windows

Windows are draggable frames with title bars:

```csharp
var window = VeneerAPI.CreateWindow("mymod_window", "My Window", width: 400, height: 300);
window.Show();

// Or with full config
var windowConfig = new WindowConfig
{
    Id = "mymod_settings_window",
    Title = "Settings",
    Width = 500,
    Height = 400,
    Closeable = true,
    Draggable = true
};
var settingsWindow = VeneerAPI.CreateWindow(windowConfig);
```

### Creating Bars

```csharp
// Health bar
var health = VeneerAPI.CreateHealthBar("mymod_health", width: 200);
health.SetValue(0.75f); // 75%
health.SetText("75/100");

// Stamina bar
var stamina = VeneerAPI.CreateStaminaBar("mymod_stamina", width: 200);

// Custom colored bar
var custom = VeneerAPI.CreateBar("mymod_xp", Color.cyan, width: 250, height: 16);
custom.SetValue(0.5f);
custom.SetText("Level 5 - 50%");

// Progress/cast bar
var progress = VeneerAPI.CreateProgressBar("mymod_cast", width: 300, height: 24);
```

### Creating Text

```csharp
// Basic text
var text = VeneerAPI.CreateText(parent, "Hello World");

// Styled text
var header = VeneerAPI.CreateText(parent, "Title", TextStyle.Header);
var body = VeneerAPI.CreateText(parent, "Description", TextStyle.Body);
var caption = VeneerAPI.CreateText(parent, "Small text", TextStyle.Caption);
var gold = VeneerAPI.CreateText(parent, "Important!", TextStyle.Gold);

// Text with outline (for readability over game world)
var outlined = VeneerAPI.CreateText(parent, "Visible Text");
outlined.WithOutline();           // Black outline
outlined.WithOutline(Color.red);  // Custom color outline
```

Available text styles:
- `Header` - Large, bold, gold (20px)
- `Subheader` - Medium, bold (16px)
- `Body` - Standard text (14px)
- `Caption` - Small, muted (12px)
- `Value` - Centered, for numbers
- `Muted` - Dimmed text
- `Gold` - Gold colored
- `Error` - Red
- `Success` - Green

### Creating Buttons

```csharp
// Basic button
var button = VeneerAPI.CreateButton(parent, "Click Me", () => {
    Debug.Log("Clicked!");
});

// Primary styled button
var primary = VeneerAPI.CreatePrimaryButton(parent, "Save", OnSave);
```

### Inventory Components

```csharp
// Single item slot
var slot = VeneerAPI.CreateItemSlot(parent, size: 50);
slot.SetItem(itemData);

// Item grid (custom size)
var grid = VeneerAPI.CreateItemGrid(parent, columns: 5, rows: 3, slotSize: 45);

// Player inventory grid (8x4)
var inventory = VeneerAPI.CreatePlayerInventoryGrid(parent);

// Hotbar grid (8x1)
var hotbar = VeneerAPI.CreateHotbarGrid(parent);
```

### Tooltips

```csharp
// Simple tooltip
VeneerAPI.ShowTooltip("Hover text");

// Tooltip with title
VeneerAPI.ShowTooltip("Item Name", "Item description goes here");

// Full tooltip configuration
VeneerAPI.ShowTooltip(new TooltipData
{
    Title = "Legendary Sword",
    Body = "A powerful weapon",
    Rarity = 4,
    Stats = new[] { "+50 Damage", "+10% Crit" }
});

// Hide tooltip
VeneerAPI.HideTooltip();
```

### Edit Mode & Positioning

```csharp
// Enter/exit edit mode
VeneerAPI.EnterEditMode();
VeneerAPI.ExitEditMode();
VeneerAPI.ToggleEditMode();

// Check edit mode state
if (VeneerAPI.IsEditModeEnabled) { }

// Reset positions to defaults
VeneerAPI.ResetAllPositions();

// Manual save
VeneerAPI.SaveLayout();

// Register custom element for positioning
VeneerAPI.RegisterElement("mymod_element", ScreenAnchor.TopRight, new Vector2(-20, -20));
```

### Colors

```csharp
// Theme colors
Color bg = VeneerAPI.BackgroundColor;
Color border = VeneerAPI.BorderColor;
Color legendary = VeneerAPI.LegendaryColor;

// Resource colors
Color hp = VeneerAPI.HealthColor;
Color stam = VeneerAPI.StaminaColor;
Color eitr = VeneerAPI.EitrColor;

// Status colors
Color error = VeneerAPI.ErrorColor;
Color success = VeneerAPI.SuccessColor;
Color warning = VeneerAPI.WarningColor;

// Rarity colors (0-4: Common, Uncommon, Rare, Epic, Legendary)
Color rare = VeneerAPI.GetRarityColor(2);

// Accent color from config
Color accent = VeneerAPI.AccentColor;

// Modify alpha
Color faded = VeneerAPI.WithAlpha(VeneerAPI.HealthColor, 0.5f);
```

### Dimensions

```csharp
float padding = VeneerAPI.Padding;   // Standard padding (4px)
float spacing = VeneerAPI.Spacing;   // Standard spacing (2px)
float slotSize = VeneerAPI.SlotSize; // Item slot size (40px)

// Get font size scaled by user's font scale setting
int fontSize = VeneerAPI.GetScaledFontSize(14);
```

### Textures & Sprites

```csharp
// Create themed sprites for custom UI
Sprite panelBg = VeneerAPI.CreatePanelSprite();
Sprite buttonBg = VeneerAPI.CreateButtonSprite();
Sprite slotBg = VeneerAPI.CreateSlotSprite();
Sprite border = VeneerAPI.CreateBorderSprite(Color.gold, borderWidth: 2);
```

### Window Management

```csharp
// Get registered window by type
var settings = VeneerAPI.GetWindow<MySettingsWindow>();

// Show/hide/toggle windows
VeneerAPI.ShowWindow<MySettingsWindow>();
VeneerAPI.HideWindow<MySettingsWindow>();
VeneerAPI.ToggleWindow<MySettingsWindow>();

// Check visibility
if (VeneerAPI.IsWindowVisible<MySettingsWindow>()) { }

// Focus a window (bring to front)
VeneerAPI.FocusWindow(window);

// Close all windows
VeneerAPI.CloseAllWindows();

// Get currently focused window
var focused = VeneerAPI.FocusedWindow;
```

### Layer System

Control rendering order:

```csharp
VeneerAPI.SetLayer(gameObject, sortingOrder: 100);
int layer = VeneerAPI.GetLayer(gameObject);
int tooltipLayer = VeneerAPI.GetLayerValue(VeneerLayerType.Tooltip);
```

### Utility

```csharp
// Create a raw UI GameObject
var go = VeneerAPI.CreateUIObject("MyElement", parent);

// Get the UI root transform
Transform root = VeneerAPI.UIRoot;

// Check if Veneer is ready
if (VeneerAPI.IsReady) { }
```

## Components Overview

| Component | Description |
|-----------|-------------|
| `VeneerPanel` | Basic panel with background and border |
| `VeneerFrame` | Standalone container with positioning |
| `VeneerWindow` | Draggable window with title bar |
| `VeneerBar` | Progress/resource bar |
| `VeneerText` | Styled text element |
| `VeneerButton` | Interactive button |
| `VeneerItemSlot` | Inventory slot display |
| `VeneerItemGrid` | Grid of inventory slots |
| `VeneerTooltip` | Mouse-following tooltip |

## Rarity Colors

| Tier | Name | Color |
|------|------|-------|
| 0 | Common | White |
| 1 | Uncommon | Green |
| 2 | Rare | Blue |
| 3 | Epic | Purple |
| 4 | Legendary | Orange/Gold |

## Changelog

### 1.0.0
- Initial release
- Core components: Panel, Frame, Window, Bar, Text, Button
- Inventory components: ItemSlot, ItemGrid
- HUD replacements: Health, Stamina, Eitr bars
- Boss frame with multi-boss support
- Edit mode with drag positioning
- Full theming system
- Complete API for mod developers

## Credits

- **Slatyo** - Development
- **Jotunn Team** - Modding framework

## License

MIT License - Feel free to use in your own mods!

## Support

- [GitHub Issues](https://github.com/Slatyo/Valheim-Veneer/issues)
- [Thunderstore](https://thunderstore.io/c/valheim/p/Slatyo/Veneer/)
