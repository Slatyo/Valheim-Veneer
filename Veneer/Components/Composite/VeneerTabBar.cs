using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Composite
{
    /// <summary>
    /// Tab bar component for filtering/navigation.
    /// Displays horizontal tabs with an accent underline indicator for the active tab.
    /// </summary>
    public class VeneerTabBar : VeneerElement
    {
        private HorizontalLayoutGroup _layout;
        private List<TabButton> _tabs = new List<TabButton>();
        private string _activeTab;
        private float _tabHeight = 28f;

        /// <summary>
        /// The currently active tab key.
        /// </summary>
        public string ActiveTab => _activeTab;

        /// <summary>
        /// Fired when a tab is selected. Parameter is the tab key.
        /// </summary>
        public event Action<string> OnTabSelected;

        /// <summary>
        /// Creates a new VeneerTabBar.
        /// </summary>
        public static VeneerTabBar Create(Transform parent, float height = 28f)
        {
            var go = CreateUIObject("VeneerTabBar", parent);
            var tabBar = go.AddComponent<VeneerTabBar>();
            tabBar.Initialize(height);
            return tabBar;
        }

        private void Initialize(float height)
        {
            _tabHeight = height;

            // Size to stretch horizontally
            var rect = RectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, height);
            rect.anchoredPosition = Vector2.zero;

            // Horizontal layout
            _layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            _layout.childAlignment = TextAnchor.MiddleLeft;
            _layout.childControlWidth = false;
            _layout.childControlHeight = true;
            _layout.childForceExpandWidth = false;
            _layout.childForceExpandHeight = true;
            _layout.spacing = 2f;
            _layout.padding = new RectOffset(0, 0, 0, 0);
        }

        /// <summary>
        /// Adds a tab to the bar.
        /// </summary>
        /// <param name="key">Unique identifier for this tab.</param>
        /// <param name="label">Display text.</param>
        /// <param name="width">Tab width (0 = auto-size based on text).</param>
        public void AddTab(string key, string label, float width = 0)
        {
            var tabGo = CreateUIObject($"Tab_{key}", transform);

            // Layout element for width
            var layoutElement = tabGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width > 0 ? width : EstimateTextWidth(label) + 20f;
            layoutElement.preferredHeight = _tabHeight;

            // Background
            var bg = tabGo.AddComponent<Image>();
            bg.color = VeneerColors.BackgroundDark;

            // Label
            var labelGo = CreateUIObject("Label", tabGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6, 0);
            labelRect.offsetMax = new Vector2(-6, -3); // Leave room for indicator

            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.text = label;
            labelText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
            labelText.color = VeneerColors.TextMuted;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;

            // Bottom indicator bar (accent color when active)
            var indicatorGo = CreateUIObject("Indicator", tabGo.transform);
            var indicatorRect = indicatorGo.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0, 0);
            indicatorRect.anchorMax = new Vector2(1, 0);
            indicatorRect.pivot = new Vector2(0.5f, 0);
            indicatorRect.sizeDelta = new Vector2(0, 3);
            indicatorRect.anchoredPosition = Vector2.zero;

            var indicator = indicatorGo.AddComponent<Image>();
            indicator.color = Color.clear; // Hidden by default

            // Button behavior
            var button = tabGo.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;

            string capturedKey = key;
            button.onClick.AddListener(() => SelectTab(capturedKey));

            // Hover handler
            var hoverHandler = tabGo.AddComponent<TabHoverHandler>();
            hoverHandler.Initialize(bg, labelText, indicator, () => _activeTab == key);

            var tabButton = new TabButton
            {
                Key = key,
                GameObject = tabGo,
                Background = bg,
                Label = labelText,
                Indicator = indicator,
                Button = button,
                HoverHandler = hoverHandler
            };

            _tabs.Add(tabButton);
        }

        /// <summary>
        /// Adds multiple tabs at once.
        /// </summary>
        public void AddTabs(params (string key, string label)[] tabs)
        {
            foreach (var (key, label) in tabs)
            {
                AddTab(key, label);
            }
        }

        /// <summary>
        /// Adds multiple tabs with custom widths.
        /// </summary>
        public void AddTabs(params (string key, string label, float width)[] tabs)
        {
            foreach (var (key, label, width) in tabs)
            {
                AddTab(key, label, width);
            }
        }

        /// <summary>
        /// Selects a tab by key.
        /// </summary>
        public void SelectTab(string key)
        {
            if (_activeTab == key) return;

            _activeTab = key;

            // Update all tabs
            foreach (var tab in _tabs)
            {
                bool isActive = tab.Key == key;
                UpdateTabVisuals(tab, isActive);
            }

            OnTabSelected?.Invoke(key);
        }

        /// <summary>
        /// Selects the first tab.
        /// </summary>
        public void SelectFirst()
        {
            if (_tabs.Count > 0)
            {
                SelectTab(_tabs[0].Key);
            }
        }

        private void UpdateTabVisuals(TabButton tab, bool isActive)
        {
            if (isActive)
            {
                tab.Background.color = VeneerColors.BackgroundLight;
                tab.Label.color = VeneerColors.TextGold;
                tab.Indicator.color = VeneerColors.Accent;
            }
            else
            {
                tab.Background.color = VeneerColors.BackgroundDark;
                tab.Label.color = VeneerColors.TextMuted;
                tab.Indicator.color = Color.clear;
            }

            tab.HoverHandler.SetActive(isActive);
        }

        private float EstimateTextWidth(string text)
        {
            // Rough estimate: 7 pixels per character at small font size
            return text.Length * 7f;
        }

        /// <summary>
        /// Clears all tabs.
        /// </summary>
        public void ClearTabs()
        {
            foreach (var tab in _tabs)
            {
                if (tab.GameObject != null)
                {
                    Destroy(tab.GameObject);
                }
            }
            _tabs.Clear();
            _activeTab = null;
        }

        private class TabButton
        {
            public string Key;
            public GameObject GameObject;
            public Image Background;
            public Text Label;
            public Image Indicator;
            public Button Button;
            public TabHoverHandler HoverHandler;
        }
    }

    /// <summary>
    /// Handles hover effects for tab buttons.
    /// </summary>
    public class TabHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Image _background;
        private Text _label;
        private Image _indicator;
        private Func<bool> _isActiveCheck;
        private bool _isActive;

        public void Initialize(Image bg, Text label, Image indicator, Func<bool> isActiveCheck)
        {
            _background = bg;
            _label = label;
            _indicator = indicator;
            _isActiveCheck = isActiveCheck;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isActive) return;

            _background.color = VeneerColors.BackgroundLight;
            _label.color = VeneerColors.Text;
            _indicator.color = VeneerColors.BorderLight;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isActive) return;

            _background.color = VeneerColors.BackgroundDark;
            _label.color = VeneerColors.TextMuted;
            _indicator.color = Color.clear;
        }
    }
}
