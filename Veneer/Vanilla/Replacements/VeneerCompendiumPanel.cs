using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Compendium panel replacement.
    /// Shows discovered lore texts, runestones, and tutorials.
    /// Uses VeneerFrame for consistent header/dragging/close button.
    /// </summary>
    public class VeneerCompendiumPanel : VeneerElement
    {
        private const string ElementIdCompendium = "Veneer_Compendium";

        private VeneerFrame _frame;
        private VeneerText _countText;

        // Entry list (left side)
        private RectTransform _listContainer;
        private ScrollRect _listScrollRect;
        private RectTransform _listContent;
        private List<CompendiumEntry> _entryButtons = new List<CompendiumEntry>();

        // Text display (right side)
        private RectTransform _textContainer;
        private VeneerText _entryTitle;
        private ScrollRect _textScrollRect;
        private Text _entryText;

        // State
        private Player _player;
        private string _selectedKey;
        private List<KeyValuePair<string, string>> _discoveredTexts = new List<KeyValuePair<string, string>>();

        // Category filter
        private string _currentCategory = "All";
        private Dictionary<string, VeneerButton> _categoryButtons = new Dictionary<string, VeneerButton>();

        /// <summary>
        /// Creates the compendium panel.
        /// </summary>
        public static VeneerCompendiumPanel Create(Transform parent)
        {
            var go = CreateUIObject("VeneerCompendiumPanel", parent);
            var panel = go.AddComponent<VeneerCompendiumPanel>();
            panel.Initialize();
            return panel;
        }

        private void Initialize()
        {
            ElementId = ElementIdCompendium;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.Popup;
            AutoRegisterWithManager = true;

            float width = 700f;
            float height = 500f;

            SetSize(width, height);
            AnchorTo(AnchorPreset.MiddleCenter);

            // Create VeneerFrame with header, close button, and dragging
            _frame = VeneerFrame.Create(transform, new FrameConfig
            {
                Id = ElementIdCompendium,
                Name = "CompendiumFrame",
                Title = "Compendium",
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

            // Add count text to the header area
            AddCountTextToHeader();

            // Use frame's content area for all inner content
            var content = _frame.Content;

            // Category bar at the top of content
            float categoryHeight = 30f;
            CreateCategoryBar(content, categoryHeight);

            // Main content area - use percentage-based layout for resize scaling
            // Left panel takes 35%, right panel takes 65%
            CreateEntryList(content, categoryHeight, 0.35f);
            CreateTextPanel(content, categoryHeight, 0.35f, 0.65f);

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(550, 400);
            resizer.MaxSize = new Vector2(1000, 800);

            // Start hidden - must register BEFORE SetActive(false) since Start() won't be called
            RegisterWithManager();
            gameObject.SetActive(false);
        }

        private void AddCountTextToHeader()
        {
            // Find header in frame
            var header = _frame.transform.Find("Header");
            if (header == null) return;

            // Count text (right side of header, before close button)
            var countGo = CreateUIObject("Count", header);
            var countRect = countGo.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0);
            countRect.anchorMax = new Vector2(1, 1);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = new Vector2(-40, 0); // Leave room for close button

            _countText = countGo.AddComponent<VeneerText>();
            _countText.Content = "0 entries";
            _countText.ApplyStyle(TextStyle.Muted);
            _countText.Alignment = TextAnchor.MiddleRight;
        }

        private void CreateCategoryBar(RectTransform parent, float height)
        {
            var categoryBar = CreateUIObject("CategoryBar", parent);
            var categoryRect = categoryBar.GetComponent<RectTransform>();
            categoryRect.anchorMin = new Vector2(0, 1);
            categoryRect.anchorMax = new Vector2(1, 1);
            categoryRect.pivot = new Vector2(0.5f, 1);
            categoryRect.anchoredPosition = Vector2.zero;
            categoryRect.sizeDelta = new Vector2(0, height);

            var layout = categoryBar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 4f;
            layout.padding = new RectOffset(0, 0, 2, 2);

            string[] categories = { "All", "Lore", "Runestones", "Tutorials", "Biomes" };
            float[] widths = { 40f, 45f, 80f, 70f, 55f };

            for (int i = 0; i < categories.Length; i++)
            {
                var cat = categories[i];
                var btn = VeneerButton.Create(categoryBar.transform, cat, () => FilterByCategory(cat));
                btn.SetButtonSize(ButtonSize.Small);
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = widths[i];
                _categoryButtons[cat] = btn;
            }

            if (_categoryButtons.TryGetValue("All", out var allBtn))
            {
                allBtn.SetStyle(ButtonStyle.Primary);
            }
        }

        private void CreateEntryList(RectTransform parent, float topOffset, float widthPercent)
        {
            // Use percentage-based anchors so content scales with parent resize
            _listContainer = CreateUIObject("EntryListContainer", parent).GetComponent<RectTransform>();
            _listContainer.anchorMin = new Vector2(0, 0);
            _listContainer.anchorMax = new Vector2(widthPercent, 1);
            _listContainer.pivot = new Vector2(0, 0.5f);
            _listContainer.offsetMin = new Vector2(0, 0); // left, bottom padding
            _listContainer.offsetMax = new Vector2(-6, -topOffset - 4); // right margin, top offset

            var bgImage = _listContainer.gameObject.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundDark;

            // Add RectMask2D to clip overflow
            _listContainer.gameObject.AddComponent<RectMask2D>();

            // Scroll view - stretches to fill container
            var scrollGo = CreateUIObject("ScrollView", _listContainer);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(4, 4);
            scrollRect.offsetMax = new Vector2(-4, -4);

            _listScrollRect = scrollGo.AddComponent<ScrollRect>();
            _listScrollRect.horizontal = false;
            _listScrollRect.vertical = true;
            _listScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _listScrollRect.scrollSensitivity = 50f;

            // Viewport with mask
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
            viewportImage.raycastTarget = true;

            _listScrollRect.viewport = viewportRect;

            // Content
            var contentGo = CreateUIObject("Content", viewportGo.transform);
            _listContent = contentGo.GetComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.anchoredPosition = Vector2.zero;
            _listContent.sizeDelta = new Vector2(0, 0);

            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 2f;
            contentLayout.padding = new RectOffset(2, 2, 2, 2);

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _listScrollRect.content = _listContent;
        }

        private void CreateTextPanel(RectTransform parent, float topOffset, float leftPercent, float widthPercent)
        {
            // Use percentage-based anchors so content scales with parent resize
            _textContainer = CreateUIObject("TextPanel", parent).GetComponent<RectTransform>();
            _textContainer.anchorMin = new Vector2(leftPercent, 0);
            _textContainer.anchorMax = new Vector2(leftPercent + widthPercent, 1);
            _textContainer.pivot = new Vector2(0.5f, 0.5f);
            _textContainer.offsetMin = new Vector2(6, 0); // left margin, bottom
            _textContainer.offsetMax = new Vector2(0, -topOffset - 4); // right, top offset

            var bgImage = _textContainer.gameObject.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundLight;

            // Add RectMask2D to clip overflow
            _textContainer.gameObject.AddComponent<RectMask2D>();

            float innerPadding = 12f;

            // Entry title - uses stretch anchors
            var titleGo = CreateUIObject("EntryTitle", _textContainer);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.offsetMin = new Vector2(innerPadding, -innerPadding - 26);
            titleRect.offsetMax = new Vector2(-innerPadding, -innerPadding);

            _entryTitle = titleGo.AddComponent<VeneerText>();
            _entryTitle.Content = "Select an entry";
            _entryTitle.ApplyStyle(TextStyle.Header);
            _entryTitle.TextColor = VeneerColors.Accent;
            _entryTitle.Alignment = TextAnchor.MiddleCenter;

            // Separator line - uses percentage anchors
            var lineGo = CreateUIObject("Line", _textContainer);
            var lineRect = lineGo.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.1f, 1);
            lineRect.anchorMax = new Vector2(0.9f, 1);
            lineRect.pivot = new Vector2(0.5f, 1);
            lineRect.offsetMin = new Vector2(0, -innerPadding - 32);
            lineRect.offsetMax = new Vector2(0, -innerPadding - 30);
            var lineImage = lineGo.AddComponent<Image>();
            lineImage.color = VeneerColors.Border;

            // Scroll view for text - stretches to fill remaining space
            var scrollGo = CreateUIObject("TextScrollView", _textContainer);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(innerPadding, innerPadding);
            scrollRect.offsetMax = new Vector2(-innerPadding, -innerPadding - 38);

            _textScrollRect = scrollGo.AddComponent<ScrollRect>();
            _textScrollRect.horizontal = false;
            _textScrollRect.vertical = true;
            _textScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _textScrollRect.scrollSensitivity = 50f;

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
            viewportImage.color = Color.clear;

            _textScrollRect.viewport = viewportRect;

            // Content
            var contentGo = CreateUIObject("Content", viewportGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;

            _entryText = contentGo.AddComponent<Text>();
            _entryText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _entryText.fontSize = VeneerConfig.GetScaledFontSize(13);
            _entryText.color = VeneerColors.Text;
            _entryText.alignment = TextAnchor.UpperLeft;
            _entryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _entryText.verticalOverflow = VerticalWrapMode.Overflow;
            _entryText.lineSpacing = 1.3f;
            _entryText.text = "Select an entry from the list to read its contents.";

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _textScrollRect.content = contentRect;
        }

        private void FilterByCategory(string category)
        {
            _currentCategory = category;

            foreach (var kvp in _categoryButtons)
            {
                kvp.Value.SetStyle(kvp.Key == category ? ButtonStyle.Primary : ButtonStyle.Default);
            }

            UpdateEntryList();
        }

        public override void Show()
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            LoadDiscoveredTexts();
            UpdateEntryList();
            base.Show(); // Fire OnShow event and set visibility
        }

        public override void Hide()
        {
            base.Hide(); // Fire OnHide event and set visibility
        }

        private void LoadDiscoveredTexts()
        {
            _discoveredTexts.Clear();

            if (_player == null) return;

            try
            {
                var knownTextsField = typeof(Player).GetField("m_knownTexts", BindingFlags.NonPublic | BindingFlags.Instance);
                if (knownTextsField != null)
                {
                    var knownTexts = knownTextsField.GetValue(_player) as Dictionary<string, string>;
                    if (knownTexts != null)
                    {
                        foreach (var kvp in knownTexts)
                        {
                            _discoveredTexts.Add(kvp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to load known texts: {ex.Message}");
            }

            _discoveredTexts = _discoveredTexts.OrderBy(x => GetDisplayTitle(x.Key)).ToList();
        }

        private void UpdateEntryList()
        {
            // Clear existing
            foreach (var entry in _entryButtons)
            {
                Destroy(entry.GameObject);
            }
            _entryButtons.Clear();

            var filteredTexts = _currentCategory == "All"
                ? _discoveredTexts
                : _discoveredTexts.Where(t => MatchesCategory(t.Key, _currentCategory)).ToList();

            if (_countText != null)
            {
                _countText.Content = $"{filteredTexts.Count} {(filteredTexts.Count == 1 ? "entry" : "entries")}";
            }

            foreach (var text in filteredTexts)
            {
                CreateEntryButton(text.Key, text.Value);
            }

            // Auto-select first entry if none selected
            if (string.IsNullOrEmpty(_selectedKey) && filteredTexts.Count > 0)
            {
                SelectEntry(filteredTexts[0].Key, filteredTexts[0].Value);
            }
            else if (!string.IsNullOrEmpty(_selectedKey))
            {
                // Re-highlight selected entry
                foreach (var entry in _entryButtons)
                {
                    entry.SetSelected(entry.Key == _selectedKey);
                }
            }
        }

        private void CreateEntryButton(string key, string text)
        {
            var entryGo = CreateUIObject("Entry_" + key, _listContent);

            var le = entryGo.AddComponent<LayoutElement>();
            le.preferredHeight = 32f;
            le.flexibleWidth = 1f;

            var bgImage = entryGo.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundLight;

            // Make it a button
            var button = entryGo.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = VeneerColors.BackgroundLight;
            colors.highlightedColor = VeneerColors.SlotHover;
            colors.pressedColor = VeneerColors.SlotSelected;
            colors.selectedColor = VeneerColors.Accent * 0.4f;
            button.colors = colors;
            button.targetGraphic = bgImage;

            string capturedKey = key;
            string capturedText = text;
            button.onClick.AddListener(() => SelectEntry(capturedKey, capturedText));

            // Icon
            var iconGo = CreateUIObject("Icon", entryGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(8, 0);
            iconRect.sizeDelta = new Vector2(20, 20);

            var iconText = iconGo.AddComponent<Text>();
            iconText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            iconText.fontSize = VeneerConfig.GetScaledFontSize(12);
            iconText.color = VeneerColors.Accent;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.text = GetCategoryIcon(key);

            // Title
            var titleGo = CreateUIObject("Title", entryGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(32, 2);
            titleRect.offsetMax = new Vector2(-8, -2);

            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = VeneerConfig.GetScaledFontSize(11);
            titleText.color = VeneerColors.Text;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = Localization.instance.Localize(GetDisplayTitle(key));
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;

            _entryButtons.Add(new CompendiumEntry
            {
                GameObject = entryGo,
                Key = key,
                Text = text,
                Background = bgImage,
                Button = button
            });
        }

        private void SelectEntry(string key, string text)
        {
            _selectedKey = key;

            foreach (var entry in _entryButtons)
            {
                entry.SetSelected(entry.Key == key);
            }

            _entryTitle.Content = Localization.instance.Localize(GetDisplayTitle(key));
            _entryText.text = Localization.instance.Localize(text);
            _textScrollRect.verticalNormalizedPosition = 1f;
        }

        private bool MatchesCategory(string key, string category)
        {
            string lowerKey = key.ToLower();
            return category switch
            {
                "Lore" => lowerKey.Contains("lore") || lowerKey.Contains("story") || lowerKey.Contains("legend") || lowerKey.Contains("tale"),
                "Runestones" => lowerKey.Contains("runestone") || lowerKey.Contains("rune_") || lowerKey.Contains("stone_"),
                "Tutorials" => lowerKey.Contains("tutorial") || lowerKey.Contains("guide") || lowerKey.Contains("tip") || lowerKey.Contains("hugin"),
                "Biomes" => lowerKey.Contains("meadow") || lowerKey.Contains("forest") || lowerKey.Contains("swamp") || lowerKey.Contains("mountain") || lowerKey.Contains("plain") || lowerKey.Contains("ocean") || lowerKey.Contains("mistland") || lowerKey.Contains("ashland"),
                _ => true
            };
        }

        private string GetDisplayTitle(string key)
        {
            string localized = Localization.instance.Localize("$" + key);
            if (!localized.StartsWith("$"))
                return localized;

            string title = key;
            string[] prefixes = { "lore_", "runestone_", "tutorial_", "text_", "dreams_" };
            foreach (var prefix in prefixes)
            {
                if (title.ToLower().StartsWith(prefix))
                {
                    title = title.Substring(prefix.Length);
                    break;
                }
            }

            title = title.Replace("_", " ");
            if (title.Length > 0)
                title = char.ToUpper(title[0]) + title.Substring(1);

            return title;
        }

        private string GetCategoryIcon(string key)
        {
            string lowerKey = key.ToLower();
            if (lowerKey.Contains("runestone") || lowerKey.Contains("rune_")) return "R";
            if (lowerKey.Contains("tutorial") || lowerKey.Contains("hugin")) return "?";
            if (lowerKey.Contains("dream")) return "D";
            return "L";
        }

        private class CompendiumEntry
        {
            public GameObject GameObject;
            public string Key;
            public string Text;
            public Image Background;
            public Button Button;

            public void SetSelected(bool selected)
            {
                Background.color = selected ? VeneerColors.Accent * 0.35f : VeneerColors.BackgroundLight;
            }
        }
    }
}
