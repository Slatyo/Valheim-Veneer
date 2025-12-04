using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Crafting panel replacement.
    /// Features a recipe grid on the left and crafting details on the right.
    /// Uses VeneerFrame for consistent header/dragging/close button.
    /// </summary>
    public class VeneerCraftingPanel : VeneerElement
    {
        private const string ElementIdCrafting = "Veneer_Crafting";

        private VeneerFrame _frame;

        // Recipe list (left side)
        private RectTransform _recipeContent;
        private ScrollRect _recipeScrollRect;
        private List<RecipeCard> _recipeCards = new List<RecipeCard>();

        // Crafting details (right side)
        private GameObject _detailsPanel;
        private Image _selectedItemIcon;
        private VeneerText _selectedItemName;
        private VeneerText _selectedItemDescription;
        private VeneerText _itemStatsText;
        private RectTransform _requirementsContent;
        private VeneerButton _craftButton;
        private VeneerText _stationText;

        // State
        private Player _player;
        private CraftingStation _currentStation;
        private Recipe _selectedRecipe;
        private List<Recipe> _availableRecipes = new List<Recipe>();

        // Category filter
        private string _currentCategory = "All";
        private Dictionary<string, VeneerButton> _categoryButtons = new Dictionary<string, VeneerButton>();

        /// <summary>
        /// Creates the crafting panel.
        /// </summary>
        public static VeneerCraftingPanel Create(Transform parent)
        {
            var go = CreateUIObject("VeneerCraftingPanel", parent);
            var panel = go.AddComponent<VeneerCraftingPanel>();
            panel.Initialize();
            return panel;
        }

        private void Initialize()
        {
            ElementId = ElementIdCrafting;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.Window;
            AutoRegisterWithManager = true;

            float width = 700f;
            float height = 550f;

            SetSize(width, height);
            AnchorTo(AnchorPreset.MiddleCenter);

            // Create VeneerFrame with header, close button, and dragging
            _frame = VeneerFrame.Create(transform, new FrameConfig
            {
                Id = ElementIdCrafting,
                Name = "CraftingFrame",
                Title = "Crafting",
                Width = width,
                Height = height,
                HasHeader = true,
                HasCloseButton = true,
                IsDraggable = true,
                Moveable = true,
                SavePosition = true,
                Anchor = AnchorPreset.MiddleCenter
            });

            // Fill parent
            _frame.RectTransform.anchorMin = Vector2.zero;
            _frame.RectTransform.anchorMax = Vector2.one;
            _frame.RectTransform.offsetMin = Vector2.zero;
            _frame.RectTransform.offsetMax = Vector2.zero;

            // Connect close event
            _frame.OnCloseClicked += Hide;

            // Use frame's content area for all inner content
            var content = _frame.Content;

            // Category bar at the top of content
            CreateCategoryBar(content);

            // Main content area (below categories) - use percentage-based layout
            float categoryHeight = 32f;

            // Left panel - Recipe list (40% width) - percentage based
            CreateRecipeList(content, categoryHeight, 0.40f);

            // Right panel - Details (60% width) - percentage based
            CreateDetailsPanel(content, categoryHeight, 0.40f, 0.60f);

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(550, 400);
            resizer.MaxSize = new Vector2(1000, 800);

            // Start hidden - must register BEFORE SetActive(false) since Start() won't be called
            RegisterWithManager();
            gameObject.SetActive(false);
        }

        private void CreateCategoryBar(RectTransform parent)
        {
            var categoryBar = CreateUIObject("CategoryBar", parent);
            var categoryRect = categoryBar.GetComponent<RectTransform>();
            categoryRect.anchorMin = new Vector2(0, 1);
            categoryRect.anchorMax = new Vector2(1, 1);
            categoryRect.pivot = new Vector2(0.5f, 1);
            categoryRect.anchoredPosition = Vector2.zero;
            categoryRect.sizeDelta = new Vector2(0, 28f);

            var layout = categoryBar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 4f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            // Category buttons
            string[] categories = { "All", "Weapons", "Armor", "Tools", "Building", "Food", "Misc" };
            foreach (var cat in categories)
            {
                var btn = VeneerButton.Create(categoryBar.transform, cat, () => FilterByCategory(cat));
                btn.SetButtonSize(ButtonSize.Small);
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = cat == "Building" ? 65f : 55f;
                _categoryButtons[cat] = btn;
            }

            // Highlight "All" by default
            if (_categoryButtons.TryGetValue("All", out var allBtn))
            {
                allBtn.SetStyle(ButtonStyle.Primary);
            }
        }

        private void CreateRecipeList(RectTransform parent, float topOffset, float widthPercent)
        {
            // Container - use percentage-based anchors so it scales with parent
            var container = CreateUIObject("RecipeListContainer", parent);
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(widthPercent, 1);
            containerRect.pivot = new Vector2(0, 0.5f);
            // Use offsets from anchors
            containerRect.offsetMin = new Vector2(0, 0); // left, bottom
            containerRect.offsetMax = new Vector2(-6, -topOffset - 4); // right margin, top offset

            // Background for recipe list
            var bgImage = container.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundDark;

            // Add RectMask2D to clip overflow
            container.AddComponent<RectMask2D>();

            // Scroll view - stretches to fill container
            var scrollGo = CreateUIObject("ScrollView", container.transform);
            var scrollViewRect = scrollGo.GetComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.offsetMin = new Vector2(4, 4);
            scrollViewRect.offsetMax = new Vector2(-4, -4);

            _recipeScrollRect = scrollGo.AddComponent<ScrollRect>();
            _recipeScrollRect.horizontal = false;
            _recipeScrollRect.vertical = true;
            _recipeScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _recipeScrollRect.scrollSensitivity = 50f;

            // Viewport
            var viewportGo = CreateUIObject("Viewport", scrollGo.transform);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportMask = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.white;

            _recipeScrollRect.viewport = viewportRect;

            // Content - use VerticalLayoutGroup instead of GridLayout for better scaling
            var contentGo = CreateUIObject("Content", viewportGo.transform);
            _recipeContent = contentGo.GetComponent<RectTransform>();
            _recipeContent.anchorMin = new Vector2(0, 1);
            _recipeContent.anchorMax = new Vector2(1, 1);
            _recipeContent.pivot = new Vector2(0.5f, 1);
            _recipeContent.anchoredPosition = Vector2.zero;

            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 4f;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _recipeScrollRect.content = _recipeContent;
        }

        private void CreateDetailsPanel(RectTransform parent, float topOffset, float leftPercent, float widthPercent)
        {
            // Use percentage-based anchors so it scales with parent
            _detailsPanel = CreateUIObject("DetailsPanel", parent);
            var detailsRect = _detailsPanel.GetComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(leftPercent, 0);
            detailsRect.anchorMax = new Vector2(leftPercent + widthPercent, 1);
            detailsRect.pivot = new Vector2(0.5f, 0.5f);
            // Use offsets from anchors
            detailsRect.offsetMin = new Vector2(6, 0); // left margin, bottom
            detailsRect.offsetMax = new Vector2(0, -topOffset - 4); // right, top offset

            // Background
            var bgImage = _detailsPanel.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundLight;

            // Add RectMask2D to clip overflow
            _detailsPanel.AddComponent<RectMask2D>();

            // Border - stretches with parent
            var borderGo = CreateUIObject("Border", _detailsPanel.transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(8, VeneerColors.Border, Color.clear, 1);
            borderImg.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;

            float innerPadding = 12f;

            // Item icon (top left)
            var iconContainer = CreateUIObject("IconContainer", _detailsPanel.transform);
            var iconContainerRect = iconContainer.GetComponent<RectTransform>();
            iconContainerRect.anchorMin = new Vector2(0, 1);
            iconContainerRect.anchorMax = new Vector2(0, 1);
            iconContainerRect.pivot = new Vector2(0, 1);
            iconContainerRect.anchoredPosition = new Vector2(innerPadding, -innerPadding);
            iconContainerRect.sizeDelta = new Vector2(64, 64);

            var iconBg = iconContainer.AddComponent<Image>();
            iconBg.color = VeneerColors.BackgroundDark;

            var iconGo = CreateUIObject("Icon", iconContainer.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            _selectedItemIcon = iconGo.AddComponent<Image>();
            _selectedItemIcon.preserveAspect = true;

            // Item name (next to icon) - stretches horizontally
            var nameGo = CreateUIObject("ItemName", _detailsPanel.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0, 1);
            nameRect.offsetMin = new Vector2(innerPadding + 72, -innerPadding - 28);
            nameRect.offsetMax = new Vector2(-innerPadding, -innerPadding);

            _selectedItemName = nameGo.AddComponent<VeneerText>();
            _selectedItemName.Content = "Select a recipe";
            _selectedItemName.ApplyStyle(TextStyle.Header);
            _selectedItemName.Alignment = TextAnchor.MiddleLeft;
            _selectedItemName.TextColor = VeneerColors.Accent;

            // Item description - stretches horizontally
            var descGo = CreateUIObject("ItemDescription", _detailsPanel.transform);
            var descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0, 1);
            descRect.offsetMin = new Vector2(innerPadding + 72, -innerPadding - 68);
            descRect.offsetMax = new Vector2(-innerPadding, -innerPadding - 32);

            _selectedItemDescription = descGo.AddComponent<VeneerText>();
            _selectedItemDescription.Content = "";
            _selectedItemDescription.ApplyStyle(TextStyle.Body);
            _selectedItemDescription.Alignment = TextAnchor.UpperLeft;

            // Item stats (damage, armor, etc)
            var statsGo = CreateUIObject("ItemStats", _detailsPanel.transform);
            var statsRect = statsGo.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1);
            statsRect.anchorMax = new Vector2(1, 1);
            statsRect.pivot = new Vector2(0, 1);
            statsRect.offsetMin = new Vector2(innerPadding, -innerPadding - 110);
            statsRect.offsetMax = new Vector2(-innerPadding, -innerPadding - 72);

            _itemStatsText = statsGo.AddComponent<VeneerText>();
            _itemStatsText.Content = "";
            _itemStatsText.ApplyStyle(TextStyle.Value);
            _itemStatsText.Alignment = TextAnchor.UpperLeft;
            _itemStatsText.TextColor = VeneerColors.TextGold;

            // Requirements label - stretches horizontally
            var reqLabelGo = CreateUIObject("RequirementsLabel", _detailsPanel.transform);
            var reqLabelRect = reqLabelGo.GetComponent<RectTransform>();
            reqLabelRect.anchorMin = new Vector2(0, 1);
            reqLabelRect.anchorMax = new Vector2(1, 1);
            reqLabelRect.pivot = new Vector2(0, 1);
            reqLabelRect.offsetMin = new Vector2(innerPadding, -innerPadding - 135);
            reqLabelRect.offsetMax = new Vector2(-innerPadding, -innerPadding - 115);

            var reqLabel = reqLabelGo.AddComponent<VeneerText>();
            reqLabel.Content = "Requirements";
            reqLabel.ApplyStyle(TextStyle.Subheader);
            reqLabel.Alignment = TextAnchor.MiddleLeft;

            // Requirements content area - stretches both ways
            var reqContainer = CreateUIObject("RequirementsContainer", _detailsPanel.transform);
            var reqContainerRect = reqContainer.GetComponent<RectTransform>();
            reqContainerRect.anchorMin = new Vector2(0, 0);
            reqContainerRect.anchorMax = new Vector2(1, 1);
            reqContainerRect.pivot = new Vector2(0.5f, 0.5f);
            reqContainerRect.offsetMin = new Vector2(innerPadding, 70); // Bottom padding for button/station
            reqContainerRect.offsetMax = new Vector2(-innerPadding, -innerPadding - 140); // Top offset

            _requirementsContent = reqContainerRect;

            // Use VerticalLayoutGroup for requirements - shows icon + name for each
            var reqLayout = reqContainer.AddComponent<VerticalLayoutGroup>();
            reqLayout.childAlignment = TextAnchor.UpperLeft;
            reqLayout.childControlWidth = true;
            reqLayout.childControlHeight = false;
            reqLayout.childForceExpandWidth = true;
            reqLayout.childForceExpandHeight = false;
            reqLayout.spacing = 6f;
            reqLayout.padding = new RectOffset(4, 4, 4, 4);

            // Station text - at bottom, stretches horizontally
            var stationGo = CreateUIObject("StationText", _detailsPanel.transform);
            var stationRect = stationGo.GetComponent<RectTransform>();
            stationRect.anchorMin = new Vector2(0, 0);
            stationRect.anchorMax = new Vector2(1, 0);
            stationRect.pivot = new Vector2(0.5f, 0);
            stationRect.offsetMin = new Vector2(innerPadding, innerPadding + 45);
            stationRect.offsetMax = new Vector2(-innerPadding, innerPadding + 65);

            _stationText = stationGo.AddComponent<VeneerText>();
            _stationText.Content = "";
            _stationText.ApplyStyle(TextStyle.Caption);
            _stationText.Alignment = TextAnchor.MiddleCenter;

            // Craft button - anchored to bottom center
            _craftButton = VeneerButton.Create(_detailsPanel.transform, "Craft", OnCraftClicked);
            _craftButton.SetButtonSize(ButtonSize.Large);
            _craftButton.SetStyle(ButtonStyle.Primary);
            var craftRect = _craftButton.RectTransform;
            craftRect.anchorMin = new Vector2(0.5f, 0);
            craftRect.anchorMax = new Vector2(0.5f, 0);
            craftRect.pivot = new Vector2(0.5f, 0);
            craftRect.anchoredPosition = new Vector2(0, innerPadding);
            craftRect.sizeDelta = new Vector2(150, 36);
        }

        private void FilterByCategory(string category)
        {
            _currentCategory = category;

            // Update button styles
            foreach (var kvp in _categoryButtons)
            {
                kvp.Value.SetStyle(kvp.Key == category ? ButtonStyle.Primary : ButtonStyle.Default);
            }

            UpdateRecipeList();
        }

        /// <summary>
        /// Shows the crafting panel.
        /// </summary>
        public override void Show()
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            _currentStation = _player.GetCurrentCraftingStation();
            UpdateTitle();
            UpdateRecipeList();
            base.Show(); // Fire OnShow event and set visibility
        }

        /// <summary>
        /// Shows the crafting panel with a specific station.
        /// </summary>
        public void Show(CraftingStation station)
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            _currentStation = station;
            UpdateTitle();
            UpdateRecipeList();
            base.Show(); // Fire OnShow event and set visibility
        }

        /// <summary>
        /// Hides the crafting panel.
        /// </summary>
        public override void Hide()
        {
            base.Hide(); // Fire OnHide event and set visibility
        }

        private void UpdateTitle()
        {
            if (_currentStation != null)
            {
                string stationName = Localization.instance.Localize(_currentStation.m_name);
                _frame.Title = $"Crafting - {stationName}";
            }
            else
            {
                _frame.Title = "Crafting";
            }
        }

        private void UpdateRecipeList()
        {
            if (_player == null) return;

            // Clear existing cards
            foreach (var card in _recipeCards)
            {
                Destroy(card.Root);
            }
            _recipeCards.Clear();

            // Get available recipes
            _availableRecipes.Clear();

            // Iterate through all recipes and check if player knows them
            foreach (var recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe == null || recipe.m_item == null) continue;

                // Check if player knows this recipe
                if (!_player.IsRecipeKnown(recipe.m_item.m_itemData.m_shared.m_name)) continue;

                // Check if we can craft at current station (or hand-craft)
                if (_currentStation != null)
                {
                    if (recipe.m_craftingStation != null &&
                        recipe.m_craftingStation.m_name != _currentStation.m_name)
                        continue;
                }
                else
                {
                    // Hand crafting - only recipes with no station requirement
                    if (recipe.m_craftingStation != null) continue;
                }

                // Filter by category
                if (_currentCategory != "All" && !MatchesCategory(recipe, _currentCategory))
                    continue;

                _availableRecipes.Add(recipe);
            }

            // Sort by name
            _availableRecipes = _availableRecipes.OrderBy(r =>
                Localization.instance.Localize(r.m_item.m_itemData.m_shared.m_name)).ToList();

            // Create cards
            foreach (var recipe in _availableRecipes)
            {
                var card = CreateRecipeCard(recipe);
                _recipeCards.Add(card);
            }

            // Select first recipe if none selected
            if (_selectedRecipe == null && _availableRecipes.Count > 0)
            {
                SelectRecipe(_availableRecipes[0]);
            }
            else if (_selectedRecipe != null)
            {
                UpdateDetailsPanel();
            }
        }

        private bool MatchesCategory(Recipe recipe, string category)
        {
            if (recipe.m_item == null) return false;

            var itemData = recipe.m_item.m_itemData;
            var itemType = itemData.m_shared.m_itemType;

            return category switch
            {
                "Weapons" => itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                             itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                             itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
                             itemType == ItemDrop.ItemData.ItemType.Bow,
                "Armor" => itemType == ItemDrop.ItemData.ItemType.Helmet ||
                           itemType == ItemDrop.ItemData.ItemType.Chest ||
                           itemType == ItemDrop.ItemData.ItemType.Legs ||
                           itemType == ItemDrop.ItemData.ItemType.Shoulder ||
                           itemType == ItemDrop.ItemData.ItemType.Shield,
                "Tools" => itemType == ItemDrop.ItemData.ItemType.Tool ||
                           itemType == ItemDrop.ItemData.ItemType.Torch,
                "Building" => itemType == ItemDrop.ItemData.ItemType.Material &&
                              itemData.m_shared.m_name.Contains("$item_") == false,
                "Food" => itemType == ItemDrop.ItemData.ItemType.Consumable,
                "Misc" => itemType == ItemDrop.ItemData.ItemType.Material ||
                          itemType == ItemDrop.ItemData.ItemType.Ammo ||
                          itemType == ItemDrop.ItemData.ItemType.Utility,
                _ => true
            };
        }

        private RecipeCard CreateRecipeCard(Recipe recipe)
        {
            var cardGo = CreateUIObject("RecipeCard", _recipeContent);

            // LayoutElement for vertical layout group
            var layoutElement = cardGo.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 52f;
            layoutElement.flexibleWidth = 1f;

            // Card background
            var bgImage = cardGo.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundLight;

            // Make clickable
            var button = cardGo.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = VeneerColors.BackgroundLight;
            colors.highlightedColor = VeneerColors.BackgroundLight * 1.3f;
            colors.pressedColor = VeneerColors.BackgroundLight * 0.8f;
            colors.selectedColor = VeneerColors.Accent * 0.5f;
            button.colors = colors;

            var capturedRecipe = recipe;
            button.onClick.AddListener(() => SelectRecipe(capturedRecipe));

            // Icon
            var iconGo = CreateUIObject("Icon", cardGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(6, 0);
            iconRect.sizeDelta = new Vector2(40, 40);

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = recipe.m_item.m_itemData.m_shared.m_icons[0];
            iconImage.preserveAspect = true;

            // Name
            var nameGo = CreateUIObject("Name", cardGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 0.5f);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.anchoredPosition = new Vector2(52, 5);
            nameRect.sizeDelta = new Vector2(-60, 20);

            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = VeneerConfig.GetScaledFontSize(12);
            nameText.color = VeneerColors.Text;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name);

            // Craftable indicator
            bool canCraft = CanCraftRecipe(recipe);
            var indicatorGo = CreateUIObject("Indicator", cardGo.transform);
            var indicatorRect = indicatorGo.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0, 0.5f);
            indicatorRect.anchorMax = new Vector2(1, 0.5f);
            indicatorRect.pivot = new Vector2(0, 0.5f);
            indicatorRect.anchoredPosition = new Vector2(52, -10);
            indicatorRect.sizeDelta = new Vector2(-60, 16);

            var indicatorText = indicatorGo.AddComponent<Text>();
            indicatorText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            indicatorText.fontSize = VeneerConfig.GetScaledFontSize(10);
            indicatorText.color = canCraft ? VeneerColors.Success : VeneerColors.TextMuted;
            indicatorText.alignment = TextAnchor.MiddleLeft;
            indicatorText.text = canCraft ? "Ready to craft" : "Missing materials";

            return new RecipeCard
            {
                Root = cardGo,
                Recipe = recipe,
                Icon = iconImage,
                NameText = nameText,
                IndicatorText = indicatorText,
                Button = button
            };
        }

        private void SelectRecipe(Recipe recipe)
        {
            _selectedRecipe = recipe;

            // Update card visuals
            foreach (var card in _recipeCards)
            {
                var isSelected = card.Recipe == recipe;
                var bgImage = card.Root.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = isSelected ? VeneerColors.Accent * 0.3f : VeneerColors.BackgroundLight;
                }
            }

            UpdateDetailsPanel();
        }

        private void UpdateDetailsPanel()
        {
            if (_selectedRecipe == null || _selectedRecipe.m_item == null)
            {
                _selectedItemName.Content = "Select a recipe";
                _selectedItemDescription.Content = "";
                if (_itemStatsText != null) _itemStatsText.Content = "";
                _selectedItemIcon.sprite = null;
                _craftButton.Interactable = false;
                ClearRequirements();
                return;
            }

            var itemData = _selectedRecipe.m_item.m_itemData;
            var shared = itemData.m_shared;

            _selectedItemIcon.sprite = shared.m_icons[0];
            _selectedItemName.Content = Localization.instance.Localize(shared.m_name);
            _selectedItemDescription.Content = Localization.instance.Localize(shared.m_description);

            // Build stats string based on item type
            string stats = BuildItemStats(shared);
            if (_itemStatsText != null)
            {
                _itemStatsText.Content = stats;
            }

            // Station requirement
            if (_selectedRecipe.m_craftingStation != null)
            {
                string stationName = Localization.instance.Localize(_selectedRecipe.m_craftingStation.m_name);
                int minLevel = _selectedRecipe.m_minStationLevel;
                _stationText.Content = minLevel > 1 ? $"Requires {stationName} (Level {minLevel})" : $"Requires {stationName}";
            }
            else
            {
                _stationText.Content = "Can be crafted anywhere";
            }

            // Update requirements
            UpdateRequirements();

            // Update craft button
            bool canCraft = CanCraftRecipe(_selectedRecipe);
            _craftButton.Interactable = canCraft;
            _craftButton.SetStyle(canCraft ? ButtonStyle.Primary : ButtonStyle.Default);
        }

        private string BuildItemStats(ItemDrop.ItemData.SharedData shared)
        {
            var statParts = new List<string>();

            // Damage stats for weapons
            if (shared.m_damages.GetTotalDamage() > 0)
            {
                var dmg = shared.m_damages;
                if (dmg.m_damage > 0) statParts.Add($"Damage: {dmg.m_damage}");
                if (dmg.m_slash > 0) statParts.Add($"Slash: {dmg.m_slash}");
                if (dmg.m_pierce > 0) statParts.Add($"Pierce: {dmg.m_pierce}");
                if (dmg.m_blunt > 0) statParts.Add($"Blunt: {dmg.m_blunt}");
                if (dmg.m_fire > 0) statParts.Add($"Fire: {dmg.m_fire}");
                if (dmg.m_frost > 0) statParts.Add($"Frost: {dmg.m_frost}");
                if (dmg.m_lightning > 0) statParts.Add($"Lightning: {dmg.m_lightning}");
                if (dmg.m_poison > 0) statParts.Add($"Poison: {dmg.m_poison}");
                if (dmg.m_spirit > 0) statParts.Add($"Spirit: {dmg.m_spirit}");
            }

            // Armor for armor pieces
            if (shared.m_armor > 0)
            {
                statParts.Add($"Armor: {shared.m_armor}");
            }

            // Block power for shields
            if (shared.m_blockPower > 0)
            {
                statParts.Add($"Block: {shared.m_blockPower}");
            }

            // Durability
            if (shared.m_maxDurability > 0)
            {
                statParts.Add($"Durability: {shared.m_maxDurability}");
            }

            // Weight
            if (shared.m_weight > 0)
            {
                statParts.Add($"Weight: {shared.m_weight:F1}");
            }

            return string.Join("  |  ", statParts);
        }

        private void ClearRequirements()
        {
            foreach (Transform child in _requirementsContent)
            {
                Destroy(child.gameObject);
            }
        }

        private void UpdateRequirements()
        {
            ClearRequirements();

            if (_selectedRecipe == null || _player == null) return;

            var inventory = _player.GetInventory();

            foreach (var req in _selectedRecipe.m_resources)
            {
                if (req.m_resItem == null) continue;

                CreateRequirementSlot(req, inventory);
            }
        }

        private void CreateRequirementSlot(Piece.Requirement req, Inventory inventory)
        {
            // Horizontal layout: [Icon] [Material Name] [Count]
            var slotGo = CreateUIObject("RequirementSlot", _requirementsContent);

            // LayoutElement for vertical layout group
            var layoutElement = slotGo.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 32f;

            // Background
            var bgImage = slotGo.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundDark;

            // Horizontal layout
            var layout = slotGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 8f;
            layout.padding = new RectOffset(6, 6, 4, 4);

            // Icon
            var iconGo = CreateUIObject("Icon", slotGo.transform);
            var iconLE = iconGo.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 24f;
            iconLE.preferredHeight = 24f;

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = req.m_resItem.m_itemData.m_shared.m_icons[0];
            iconImage.preserveAspect = true;

            // Material name
            string matName = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
            var nameGo = CreateUIObject("Name", slotGo.transform);
            var nameLE = nameGo.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1f;

            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = VeneerConfig.GetScaledFontSize(12);
            nameText.color = VeneerColors.Text;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = matName;

            // Count text (have/need)
            int have = inventory.CountItems(req.m_resItem.m_itemData.m_shared.m_name);
            int need = req.m_amount;
            bool hasEnough = have >= need;

            var countGo = CreateUIObject("Count", slotGo.transform);
            var countLE = countGo.AddComponent<LayoutElement>();
            countLE.preferredWidth = 60f;

            var countText = countGo.AddComponent<Text>();
            countText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            countText.fontSize = VeneerConfig.GetScaledFontSize(12);
            countText.fontStyle = FontStyle.Bold;
            countText.color = hasEnough ? VeneerColors.Success : VeneerColors.Error;
            countText.alignment = TextAnchor.MiddleRight;
            countText.text = $"{have}/{need}";

            // Left border color indicator
            var indicatorGo = CreateUIObject("Indicator", slotGo.transform);
            indicatorGo.transform.SetAsFirstSibling(); // Move to front
            var indicatorLE = indicatorGo.AddComponent<LayoutElement>();
            indicatorLE.preferredWidth = 4f;

            var indicatorImg = indicatorGo.AddComponent<Image>();
            indicatorImg.color = hasEnough ? VeneerColors.Success : VeneerColors.Error;
        }

        private bool CanCraftRecipe(Recipe recipe)
        {
            if (_player == null || recipe == null) return false;

            var inventory = _player.GetInventory();

            // Check station level
            if (recipe.m_craftingStation != null && _currentStation != null)
            {
                if (_currentStation.GetLevel() < recipe.m_minStationLevel)
                    return false;
            }

            // Check resources
            foreach (var req in recipe.m_resources)
            {
                if (req.m_resItem == null) continue;

                int have = inventory.CountItems(req.m_resItem.m_itemData.m_shared.m_name);
                if (have < req.m_amount) return false;
            }

            return true;
        }

        private void OnCraftClicked()
        {
            if (_selectedRecipe == null || _player == null || !CanCraftRecipe(_selectedRecipe))
                return;

            // Attempt to craft
            var inventory = _player.GetInventory();

            // Remove resources
            foreach (var req in _selectedRecipe.m_resources)
            {
                if (req.m_resItem == null) continue;
                inventory.RemoveItem(req.m_resItem.m_itemData.m_shared.m_name, req.m_amount);
            }

            // Add crafted item
            var craftedItem = _selectedRecipe.m_item.m_itemData.Clone();
            craftedItem.m_quality = 1;
            craftedItem.m_stack = _selectedRecipe.m_amount;

            if (inventory.AddItem(craftedItem))
            {
                // Play craft effect
                _player.Message(MessageHud.MessageType.Center,
                    $"Crafted {Localization.instance.Localize(craftedItem.m_shared.m_name)}");

                // Raise skill if applicable
                _player.RaiseSkill(Skills.SkillType.All, 1f);
            }

            // Refresh UI
            UpdateRecipeList();
            UpdateDetailsPanel();
        }

        private void Update()
        {
            // Refresh craftable status periodically
            if (_player != null && gameObject.activeSelf)
            {
                // Could add periodic refresh here if needed
            }
        }

        private class RecipeCard
        {
            public GameObject Root;
            public Recipe Recipe;
            public Image Icon;
            public Text NameText;
            public Text IndicatorText;
            public Button Button;
        }
    }
}
