using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Composite;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Crafting panel replacement.
    /// Layout: TopBar (search + sort buttons) → Tabs → Card Grid → Preview Panel
    /// Uses reusable Veneer components.
    /// </summary>
    public class VeneerCraftingPanel : VeneerElement
    {
        private const string ElementIdCrafting = "Veneer_Crafting";

        private VeneerFrame _frame;

        // Top bar components
        private VeneerSearchInput _searchInput;
        private VeneerToggleButton _sortNameButton;
        private VeneerToggleButton _craftableOnlyButton;

        // Tab bar
        private VeneerTabBar _tabBar;

        // Recipe grid
        private ScrollRect _recipeScrollRect;
        private RectTransform _recipeContent;
        private List<VeneerCard> _recipeCards = new List<VeneerCard>();

        // Preview panel
        private RectTransform _previewPanel;
        private Image _previewIcon;
        private VeneerText _previewName;
        private VeneerText _previewSubtitle;
        private VeneerText _previewStats;
        private RectTransform _requirementsContainer;
        private VeneerButton _craftButton;
        private List<VeneerRequirementRow> _requirementRows = new List<VeneerRequirementRow>();

        // State
        private Player _player;
        private CraftingStation _currentStation;
        private Recipe _selectedRecipe;
        private List<Recipe> _availableRecipes = new List<Recipe>();

        // Filter state
        private string _currentCategory = "All";
        private string _searchQuery = "";
        private SortMode _sortMode = SortMode.None;
        private bool _craftableOnly = false;

        private enum SortMode { None, Name }

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

            float width = 650f;
            float height = 650f;

            SetSize(width, height);
            AnchorTo(AnchorPreset.MiddleCenter);

            // Create VeneerFrame
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

            _frame.OnCloseClicked += Hide;

            var content = _frame.Content;

            // Layout from top to bottom:
            // 1. Top bar (search + buttons) - 36px
            // 2. Tab bar - 30px
            // 3. Recipe grid - flexible, ~55%
            // 4. Preview panel - ~180px

            float topBarHeight = 36f;
            float tabBarHeight = 30f;
            float previewHeight = 190f;
            float spacing = 8f;

            CreateTopBar(content, topBarHeight);
            CreateTabBar(content, topBarHeight + spacing, tabBarHeight);
            CreateRecipeGrid(content, topBarHeight + tabBarHeight + spacing * 2, previewHeight + spacing);
            CreatePreviewPanel(content, previewHeight);

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(500, 500);
            resizer.MaxSize = new Vector2(1000, 900);

            // Register and start hidden
            RegisterWithManager();
            gameObject.SetActive(false);
        }

        private void CreateTopBar(RectTransform parent, float height)
        {
            var topBar = CreateUIObject("TopBar", parent);
            var topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.anchoredPosition = Vector2.zero;
            topBarRect.sizeDelta = new Vector2(0, height);

            // Background
            var bgImage = topBar.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundDark;

            // Horizontal layout
            var layout = topBar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 4, 4);

            // Search input
            _searchInput = VeneerSearchInput.Create(topBar.transform, "Search recipes...");
            var searchLE = _searchInput.gameObject.AddComponent<LayoutElement>();
            searchLE.flexibleWidth = 1f;
            searchLE.minWidth = 150f;
            _searchInput.OnValueChanged += OnSearchChanged;

            // Sort A-Z button
            _sortNameButton = VeneerToggleButton.Create(topBar.transform, "Sort A-Z");
            var sortNameLE = _sortNameButton.gameObject.AddComponent<LayoutElement>();
            sortNameLE.preferredWidth = 70f;
            _sortNameButton.OnClick += OnSortNameClicked;

            // Craftable only button
            _craftableOnlyButton = VeneerToggleButton.Create(topBar.transform, "Craftable");
            var craftableLE = _craftableOnlyButton.gameObject.AddComponent<LayoutElement>();
            craftableLE.preferredWidth = 75f;
            _craftableOnlyButton.OnToggled += OnCraftableOnlyToggled;
        }

        private void CreateTabBar(RectTransform parent, float topOffset, float height)
        {
            _tabBar = VeneerTabBar.Create(parent, height);

            var tabBarRect = _tabBar.RectTransform;
            tabBarRect.anchorMin = new Vector2(0, 1);
            tabBarRect.anchorMax = new Vector2(1, 1);
            tabBarRect.pivot = new Vector2(0.5f, 1);
            tabBarRect.anchoredPosition = new Vector2(0, -topOffset);
            tabBarRect.sizeDelta = new Vector2(0, height);

            _tabBar.AddTabs(
                ("All", "All", 45f),
                ("Weapons", "Weapons", 70f),
                ("Armor", "Armor", 55f),
                ("Tools", "Tools", 50f),
                ("Food", "Food", 45f),
                ("Misc", "Misc", 45f)
            );

            _tabBar.OnTabSelected += OnCategoryChanged;
            _tabBar.SelectTab("All");
        }

        private void CreateRecipeGrid(RectTransform parent, float topOffset, float bottomOffset)
        {
            var gridContainer = CreateUIObject("GridContainer", parent);
            var gridRect = gridContainer.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 0);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.offsetMin = new Vector2(0, bottomOffset);
            gridRect.offsetMax = new Vector2(0, -topOffset);

            // Background
            var bgImage = gridContainer.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundDark;

            // Scroll view
            _recipeScrollRect = gridContainer.AddComponent<ScrollRect>();
            _recipeScrollRect.horizontal = false;
            _recipeScrollRect.vertical = true;
            _recipeScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _recipeScrollRect.scrollSensitivity = 30f;

            // Viewport
            var viewportGo = CreateUIObject("Viewport", gridContainer.transform);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8, 8);
            viewportRect.offsetMax = new Vector2(-8, -8);

            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var maskImage = viewportGo.AddComponent<Image>();
            maskImage.color = Color.white;

            _recipeScrollRect.viewport = viewportRect;

            // Content with grid layout
            var contentGo = CreateUIObject("Content", viewportGo.transform);
            _recipeContent = contentGo.GetComponent<RectTransform>();
            _recipeContent.anchorMin = new Vector2(0, 1);
            _recipeContent.anchorMax = new Vector2(1, 1);
            _recipeContent.pivot = new Vector2(0.5f, 1);
            _recipeContent.anchoredPosition = Vector2.zero;

            var gridLayout = contentGo.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(140, 120);
            gridLayout.spacing = new Vector2(12, 12);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            gridLayout.padding = new RectOffset(4, 4, 4, 4);

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _recipeScrollRect.content = _recipeContent;
        }

        private void CreatePreviewPanel(RectTransform parent, float height)
        {
            var previewGo = CreateUIObject("PreviewPanel", parent);
            _previewPanel = previewGo.GetComponent<RectTransform>();
            _previewPanel.anchorMin = new Vector2(0, 0);
            _previewPanel.anchorMax = new Vector2(1, 0);
            _previewPanel.pivot = new Vector2(0.5f, 0);
            _previewPanel.anchoredPosition = Vector2.zero;
            _previewPanel.sizeDelta = new Vector2(0, height);

            // Background
            var bgImage = previewGo.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundLight;

            // Border
            var borderGo = CreateUIObject("Border", previewGo.transform);
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

            float padding = 12f;

            // Left side: Icon + Name + Subtitle
            var iconGo = CreateUIObject("Icon", previewGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 1);
            iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 1);
            iconRect.anchoredPosition = new Vector2(padding, -padding);
            iconRect.sizeDelta = new Vector2(64, 64);

            var iconBg = iconGo.AddComponent<Image>();
            iconBg.color = VeneerColors.BackgroundDark;

            var iconInnerGo = CreateUIObject("IconImage", iconGo.transform);
            var iconInnerRect = iconInnerGo.GetComponent<RectTransform>();
            iconInnerRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconInnerRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconInnerRect.offsetMin = Vector2.zero;
            iconInnerRect.offsetMax = Vector2.zero;

            _previewIcon = iconInnerGo.AddComponent<Image>();
            _previewIcon.preserveAspect = true;

            // Name (next to icon)
            var nameGo = CreateUIObject("Name", previewGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(0.5f, 1);
            nameRect.pivot = new Vector2(0, 1);
            nameRect.offsetMin = new Vector2(padding + 72, -padding - 24);
            nameRect.offsetMax = new Vector2(0, -padding);

            _previewName = nameGo.AddComponent<VeneerText>();
            _previewName.Content = "Select an item";
            _previewName.ApplyStyle(TextStyle.Header);
            _previewName.TextColor = VeneerColors.Accent;
            _previewName.Alignment = TextAnchor.MiddleLeft;

            // Subtitle
            var subtitleGo = CreateUIObject("Subtitle", previewGo.transform);
            var subtitleRect = subtitleGo.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0, 1);
            subtitleRect.anchorMax = new Vector2(0.5f, 1);
            subtitleRect.pivot = new Vector2(0, 1);
            subtitleRect.offsetMin = new Vector2(padding + 72, -padding - 44);
            subtitleRect.offsetMax = new Vector2(0, -padding - 26);

            _previewSubtitle = subtitleGo.AddComponent<VeneerText>();
            _previewSubtitle.Content = "";
            _previewSubtitle.ApplyStyle(TextStyle.Caption);
            _previewSubtitle.Alignment = TextAnchor.MiddleLeft;

            // Stats (below icon, stretching down to bottom left)
            var statsGo = CreateUIObject("Stats", previewGo.transform);
            var statsRect = statsGo.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0);
            statsRect.anchorMax = new Vector2(0.5f, 1);
            statsRect.pivot = new Vector2(0, 1);
            // Start below the icon (icon is 64px + 12px padding from top = 76px down)
            statsRect.offsetMin = new Vector2(padding, padding);
            statsRect.offsetMax = new Vector2(-padding, -padding - 80);

            _previewStats = statsGo.AddComponent<VeneerText>();
            _previewStats.Content = "";
            _previewStats.ApplyStyle(TextStyle.Caption);
            _previewStats.Alignment = TextAnchor.UpperLeft;

            // Enable wrapping and rich text for stats
            var statsText = _previewStats.GetComponent<Text>();
            if (statsText != null)
            {
                statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
                statsText.verticalOverflow = VerticalWrapMode.Overflow;
                statsText.supportRichText = true;
            }
            var statsFitter = _previewStats.GetComponent<ContentSizeFitter>();
            if (statsFitter != null)
            {
                statsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                statsFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            // Requirements section (right side)
            var reqLabelGo = CreateUIObject("RecipeLabel", previewGo.transform);
            var reqLabelRect = reqLabelGo.GetComponent<RectTransform>();
            reqLabelRect.anchorMin = new Vector2(0.5f, 1);
            reqLabelRect.anchorMax = new Vector2(1, 1);
            reqLabelRect.pivot = new Vector2(0, 1);
            reqLabelRect.offsetMin = new Vector2(padding, -padding - 20);
            reqLabelRect.offsetMax = new Vector2(-padding, -padding);

            var reqLabel = reqLabelGo.AddComponent<VeneerText>();
            reqLabel.Content = "Recipe";
            reqLabel.ApplyStyle(TextStyle.Subheader);
            reqLabel.Alignment = TextAnchor.MiddleLeft;

            // Requirements container
            var reqContainerGo = CreateUIObject("Requirements", previewGo.transform);
            _requirementsContainer = reqContainerGo.GetComponent<RectTransform>();
            _requirementsContainer.anchorMin = new Vector2(0.5f, 0);
            _requirementsContainer.anchorMax = new Vector2(1, 1);
            _requirementsContainer.pivot = new Vector2(0.5f, 0.5f);
            _requirementsContainer.offsetMin = new Vector2(padding, padding + 45);
            _requirementsContainer.offsetMax = new Vector2(-padding, -padding - 24);

            var reqLayout = reqContainerGo.AddComponent<VerticalLayoutGroup>();
            reqLayout.childAlignment = TextAnchor.UpperLeft;
            reqLayout.childControlWidth = true;
            reqLayout.childControlHeight = false;
            reqLayout.childForceExpandWidth = true;
            reqLayout.childForceExpandHeight = false;
            reqLayout.spacing = 4f;

            // Craft button (bottom right)
            _craftButton = VeneerButton.Create(previewGo.transform, "Craft", OnCraftClicked);
            _craftButton.SetButtonSize(ButtonSize.Large);
            _craftButton.SetStyle(ButtonStyle.Primary);
            var craftRect = _craftButton.RectTransform;
            craftRect.anchorMin = new Vector2(1, 0);
            craftRect.anchorMax = new Vector2(1, 0);
            craftRect.pivot = new Vector2(1, 0);
            craftRect.anchoredPosition = new Vector2(-padding, padding);
            craftRect.sizeDelta = new Vector2(120, 36);
        }

        #region Event Handlers

        private void OnSearchChanged(string query)
        {
            _searchQuery = query.ToLowerInvariant();
            UpdateRecipeList();
        }

        private void OnSortNameClicked()
        {
            if (_sortMode == SortMode.Name)
            {
                _sortMode = SortMode.None;
                _sortNameButton.SetToggledSilent(false);
            }
            else
            {
                _sortMode = SortMode.Name;
                _sortNameButton.SetToggledSilent(true);
            }
            UpdateRecipeList();
        }

        private void OnCraftableOnlyToggled(bool toggled)
        {
            _craftableOnly = toggled;
            UpdateRecipeList();
        }

        private void OnCategoryChanged(string category)
        {
            _currentCategory = category;
            UpdateRecipeList();
        }

        #endregion

        #region Show/Hide

        public override void Show()
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            _currentStation = _player.GetCurrentCraftingStation();
            UpdateTitle();
            UpdateRecipeList();
            base.Show();
        }

        public void Show(CraftingStation station)
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            _currentStation = station;
            UpdateTitle();
            UpdateRecipeList();
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
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

        #endregion

        #region Recipe List

        private void UpdateRecipeList()
        {
            if (_player == null) return;

            // Clear existing cards
            ClearRecipeCards();

            // Get available recipes
            _availableRecipes.Clear();

            foreach (var recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe == null || recipe.m_item == null) continue;
                if (!_player.IsRecipeKnown(recipe.m_item.m_itemData.m_shared.m_name)) continue;

                // Station filter
                if (_currentStation != null)
                {
                    if (recipe.m_craftingStation != null &&
                        recipe.m_craftingStation.m_name != _currentStation.m_name)
                        continue;
                }
                else
                {
                    if (recipe.m_craftingStation != null) continue;
                }

                // Category filter
                if (_currentCategory != "All" && !MatchesCategory(recipe, _currentCategory))
                    continue;

                // Search filter
                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    string itemName = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name).ToLowerInvariant();
                    if (!itemName.Contains(_searchQuery))
                        continue;
                }

                // Craftable filter
                if (_craftableOnly && !CanCraftRecipe(recipe))
                    continue;

                _availableRecipes.Add(recipe);
            }

            // Sort - always alphabetical, toggle just controls whether it's applied explicitly
            _availableRecipes = _availableRecipes.OrderBy(r =>
                Localization.instance.Localize(r.m_item.m_itemData.m_shared.m_name)).ToList();

            // Create cards
            foreach (var recipe in _availableRecipes)
            {
                CreateRecipeCard(recipe);
            }

            // Auto-select first
            if (_selectedRecipe == null && _availableRecipes.Count > 0)
            {
                SelectRecipe(_availableRecipes[0]);
            }
            else if (_selectedRecipe != null && _availableRecipes.Contains(_selectedRecipe))
            {
                UpdatePreviewPanel();
                UpdateCardSelection();
            }
            else if (_availableRecipes.Count > 0)
            {
                SelectRecipe(_availableRecipes[0]);
            }
            else
            {
                _selectedRecipe = null;
                UpdatePreviewPanel();
            }
        }

        private void ClearRecipeCards()
        {
            foreach (var card in _recipeCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            _recipeCards.Clear();
        }

        private void CreateRecipeCard(Recipe recipe)
        {
            if (_recipeContent == null) return;

            var card = VeneerCard.Create(_recipeContent, 140f, 120f);
            card.UserData = recipe;

            var itemData = recipe.m_item.m_itemData;
            card.Icon = itemData.m_shared.m_icons[0];
            card.Title = Localization.instance.Localize(itemData.m_shared.m_name);

            // Subtitle: category + craft time
            string category = GetCategoryName(recipe);
            card.Subtitle = $"{category}";

            // Lock state based on craftability
            bool canCraft = CanCraftRecipe(recipe);
            card.IsLocked = !canCraft;

            // Click handler
            var capturedRecipe = recipe;
            card.OnClick += () => SelectRecipe(capturedRecipe);

            // Selection state
            card.IsSelected = recipe == _selectedRecipe;

            _recipeCards.Add(card);
        }

        private void SelectRecipe(Recipe recipe)
        {
            _selectedRecipe = recipe;
            UpdateCardSelection();
            UpdatePreviewPanel();
        }

        private void UpdateCardSelection()
        {
            foreach (var card in _recipeCards)
            {
                if (card != null && card.UserData is Recipe cardRecipe)
                {
                    card.IsSelected = cardRecipe == _selectedRecipe;
                }
            }
        }

        private string GetCategoryName(Recipe recipe)
        {
            if (recipe.m_item == null) return "Misc";

            var itemType = recipe.m_item.m_itemData.m_shared.m_itemType;

            if (itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft ||
                itemType == ItemDrop.ItemData.ItemType.Bow)
                return "Weapons";

            if (itemType == ItemDrop.ItemData.ItemType.Helmet ||
                itemType == ItemDrop.ItemData.ItemType.Chest ||
                itemType == ItemDrop.ItemData.ItemType.Legs ||
                itemType == ItemDrop.ItemData.ItemType.Shoulder ||
                itemType == ItemDrop.ItemData.ItemType.Shield)
                return "Armor";

            if (itemType == ItemDrop.ItemData.ItemType.Tool ||
                itemType == ItemDrop.ItemData.ItemType.Torch)
                return "Tools";

            if (itemType == ItemDrop.ItemData.ItemType.Consumable)
                return "Food";

            return "Misc";
        }

        private bool MatchesCategory(Recipe recipe, string category)
        {
            return GetCategoryName(recipe) == category;
        }

        #endregion

        #region Preview Panel

        private void UpdatePreviewPanel()
        {
            ClearRequirements();

            if (_selectedRecipe == null || _selectedRecipe.m_item == null)
            {
                _previewIcon.sprite = null;
                _previewName.Content = "Select an item";
                _previewSubtitle.Content = "";
                _previewStats.Content = "";
                _craftButton.Interactable = false;
                return;
            }

            var itemData = _selectedRecipe.m_item.m_itemData;
            var shared = itemData.m_shared;

            _previewIcon.sprite = shared.m_icons[0];
            _previewName.Content = Localization.instance.Localize(shared.m_name);
            _previewSubtitle.Content = $"{GetCategoryName(_selectedRecipe)} • Q{_selectedRecipe.m_amount}";

            // Stats
            _previewStats.Content = BuildItemStats(shared);

            // Requirements
            UpdateRequirements();

            // Craft button
            bool canCraft = CanCraftRecipe(_selectedRecipe);
            _craftButton.Interactable = canCraft;
            _craftButton.Label = canCraft ? "Craft" : "Missing";
            _craftButton.SetStyle(canCraft ? ButtonStyle.Primary : ButtonStyle.Default);
        }

        private string BuildItemStats(ItemDrop.ItemData.SharedData shared)
        {
            var lines = new List<string>();

            // Offensive stats
            var offensive = new List<string>();
            var damages = shared.m_damages;

            if (damages.m_damage > 0) offensive.Add($"Physical: {damages.m_damage:F0}");
            if (damages.m_slash > 0) offensive.Add($"Slash: {damages.m_slash:F0}");
            if (damages.m_pierce > 0) offensive.Add($"Pierce: {damages.m_pierce:F0}");
            if (damages.m_blunt > 0) offensive.Add($"Blunt: {damages.m_blunt:F0}");
            if (damages.m_fire > 0) offensive.Add($"Fire: {damages.m_fire:F0}");
            if (damages.m_frost > 0) offensive.Add($"Frost: {damages.m_frost:F0}");
            if (damages.m_lightning > 0) offensive.Add($"Lightning: {damages.m_lightning:F0}");
            if (damages.m_poison > 0) offensive.Add($"Poison: {damages.m_poison:F0}");
            if (damages.m_spirit > 0) offensive.Add($"Spirit: {damages.m_spirit:F0}");

            if (shared.m_attackForce > 0) offensive.Add($"Knockback: {shared.m_attackForce:F0}");
            if (shared.m_backstabBonus > 1) offensive.Add($"Backstab: {shared.m_backstabBonus:F1}x");

            if (offensive.Count > 0)
            {
                lines.Add("<color=#C9A227>Offensive</color>");
                lines.Add(string.Join(", ", offensive));
            }

            // Defensive stats
            var defensive = new List<string>();

            if (shared.m_armor > 0) defensive.Add($"Armor: {shared.m_armor:F0}");
            if (shared.m_blockPower > 0) defensive.Add($"Block: {shared.m_blockPower:F0}");
            if (shared.m_deflectionForce > 0) defensive.Add($"Parry: {shared.m_deflectionForce:F0}");
            if (shared.m_timedBlockBonus > 1) defensive.Add($"Parry Bonus: {shared.m_timedBlockBonus:F1}x");

            // Resistances from armor modifier (if any)
            if (shared.m_damageModifiers != null && shared.m_damageModifiers.Count > 0)
            {
                foreach (var mod in shared.m_damageModifiers)
                {
                    string modText = mod.m_modifier switch
                    {
                        HitData.DamageModifier.Resistant => "Resist",
                        HitData.DamageModifier.VeryResistant => "V.Resist",
                        HitData.DamageModifier.Weak => "Weak",
                        HitData.DamageModifier.VeryWeak => "V.Weak",
                        HitData.DamageModifier.Immune => "Immune",
                        _ => null
                    };

                    if (modText != null)
                    {
                        defensive.Add($"{mod.m_type}: {modText}");
                    }
                }
            }

            if (defensive.Count > 0)
            {
                if (lines.Count > 0) lines.Add("");
                lines.Add("<color=#C9A227>Defensive</color>");
                lines.Add(string.Join(", ", defensive));
            }

            // Utility/Other stats
            var utility = new List<string>();

            if (shared.m_durabilityPerLevel > 0) utility.Add($"Durability: {shared.m_maxDurability:F0}");
            if (shared.m_movementModifier != 0) utility.Add($"Movement: {shared.m_movementModifier * 100:F0}%");
            if (shared.m_eitrRegenModifier != 0) utility.Add($"Eitr Regen: {shared.m_eitrRegenModifier * 100:+0;-0}%");

            // Food stats
            if (shared.m_food > 0) utility.Add($"Health: +{shared.m_food:F0}");
            if (shared.m_foodStamina > 0) utility.Add($"Stamina: +{shared.m_foodStamina:F0}");
            if (shared.m_foodEitr > 0) utility.Add($"Eitr: +{shared.m_foodEitr:F0}");
            if (shared.m_foodBurnTime > 0) utility.Add($"Duration: {shared.m_foodBurnTime / 60:F0}m");
            if (shared.m_foodRegen > 0) utility.Add($"Regen: {shared.m_foodRegen:F1}/s");

            if (utility.Count > 0)
            {
                if (lines.Count > 0) lines.Add("");
                lines.Add("<color=#C9A227>Stats</color>");
                lines.Add(string.Join(", ", utility));
            }

            return string.Join("\n", lines);
        }

        private void ClearRequirements()
        {
            foreach (var row in _requirementRows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }
            _requirementRows.Clear();
        }

        private void UpdateRequirements()
        {
            if (_selectedRecipe == null || _player == null) return;

            var inventory = _player.GetInventory();

            foreach (var req in _selectedRecipe.m_resources)
            {
                if (req.m_resItem == null) continue;

                var row = VeneerRequirementRow.Create(_requirementsContainer);
                var rowLE = row.gameObject.AddComponent<LayoutElement>();
                rowLE.preferredHeight = 28f;

                int have = inventory.CountItems(req.m_resItem.m_itemData.m_shared.m_name);
                int need = req.m_amount;

                row.SetRequirement(
                    req.m_resItem.m_itemData.m_shared.m_icons[0],
                    Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name),
                    have,
                    need
                );

                _requirementRows.Add(row);
            }
        }

        #endregion

        #region Crafting

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
                _player.Message(MessageHud.MessageType.Center,
                    $"Crafted {Localization.instance.Localize(craftedItem.m_shared.m_name)}");

                _player.RaiseSkill(Skills.SkillType.All, 1f);
            }

            // Refresh UI
            UpdateRecipeList();
        }

        #endregion
    }
}
