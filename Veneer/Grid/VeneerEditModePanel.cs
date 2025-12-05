using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Grid
{
    /// <summary>
    /// Edit mode configuration panel with element visibility toggles.
    /// Groups elements by prefix and allows hiding/showing groups and individual elements.
    /// Visibility toggles only affect edit mode display - original window states are preserved.
    /// </summary>
    public class VeneerEditModePanel : VeneerElement
    {
        private static VeneerEditModePanel _instance;

        private Image _backgroundImage;
        private Text _titleText;
        private RectTransform _contentTransform;
        private ScrollRect _scrollRect;

        // Element visibility state during edit mode (does NOT affect actual window state)
        private Dictionary<string, bool> _editModeVisibility = new Dictionary<string, bool>();
        private Dictionary<string, Toggle> _elementToggles = new Dictionary<string, Toggle>();

        // Group visibility state
        private Dictionary<string, bool> _groupVisibility = new Dictionary<string, bool>();
        private Dictionary<string, Toggle> _groupToggles = new Dictionary<string, Toggle>();
        private Dictionary<string, GameObject> _groupContents = new Dictionary<string, GameObject>();
        private Dictionary<string, bool> _groupExpanded = new Dictionary<string, bool>();

        // Drag state
        private bool _isDragging;
        private Vector2 _dragOffset;
        private RectTransform _headerRect;

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static VeneerEditModePanel Instance => _instance;

        /// <summary>
        /// Creates the edit mode panel.
        /// </summary>
        public static VeneerEditModePanel Create(Transform parent)
        {
            if (_instance != null && _instance.gameObject != null)
            {
                Plugin.Log.LogDebug("VeneerEditModePanel.Create: Returning existing instance");
                return _instance;
            }

            _instance = null;

            if (parent == null)
            {
                Plugin.Log.LogError("VeneerEditModePanel.Create: parent is null!");
                return null;
            }

            try
            {
                var go = CreateUIObject("VeneerEditModePanel", parent);
                if (go == null)
                {
                    Plugin.Log.LogError("VeneerEditModePanel.Create: CreateUIObject returned null");
                    return null;
                }

                var panel = go.AddComponent<VeneerEditModePanel>();
                panel.Initialize();
                _instance = panel;
                Plugin.Log.LogInfo($"VeneerEditModePanel.Create: Panel created successfully");
                return panel;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"VeneerEditModePanel.Create: Exception - {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private void Initialize()
        {
            float width = 260f;
            float height = 420f;
            float padding = 6f;
            float headerHeight = 26f;

            SetSize(width, height);
            AnchorTo(AnchorPreset.TopLeft, new Vector2(10, -10));

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            _backgroundImage.raycastTarget = true;

            // Border
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Accent, Color.clear, 2);
            borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 2);
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;

            // Header
            var headerGo = CreateUIObject("Header", transform);
            _headerRect = headerGo.GetComponent<RectTransform>();
            _headerRect.anchorMin = new Vector2(0, 1);
            _headerRect.anchorMax = new Vector2(1, 1);
            _headerRect.pivot = new Vector2(0.5f, 1);
            _headerRect.anchoredPosition = Vector2.zero;
            _headerRect.sizeDelta = new Vector2(0, headerHeight);

            var headerBg = headerGo.AddComponent<Image>();
            headerBg.color = new Color(0.06f, 0.06f, 0.08f, 1f);
            headerBg.raycastTarget = true;

            var dragHandler = headerGo.AddComponent<EditModePanelDragHandler>();
            dragHandler.Target = this;

            // Title
            var titleGo = CreateUIObject("Title", headerGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(padding, 0);
            titleRect.offsetMax = new Vector2(-26, 0);

            _titleText = titleGo.AddComponent<Text>();
            _titleText.text = "Edit Mode";
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.fontSize = 13;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = VeneerColors.TextGold;
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.raycastTarget = false;

            // Close button
            var closeGo = CreateUIObject("CloseBtn", headerGo.transform);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-2, 0);
            closeRect.sizeDelta = new Vector2(22, 0);

            var closeBg = closeGo.AddComponent<Image>();
            closeBg.color = new Color(0.4f, 0.15f, 0.15f, 0.9f);
            closeBg.raycastTarget = true;

            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(() => Hide());

            var closeTextGo = CreateUIObject("X", closeGo.transform);
            var closeTextRect = closeTextGo.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            var closeText = closeTextGo.AddComponent<Text>();
            closeText.text = "X";
            closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            closeText.fontSize = 12;
            closeText.fontStyle = FontStyle.Bold;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.raycastTarget = false;

            // Scroll area
            var scrollGo = CreateUIObject("ScrollArea", transform);
            var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(padding, padding);
            scrollRectTransform.offsetMax = new Vector2(-padding, -headerHeight - 2);

            _scrollRect = scrollGo.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 25f;

            var viewportGo = CreateUIObject("Viewport", scrollGo.transform);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;

            viewportGo.AddComponent<RectMask2D>();

            _scrollRect.viewport = viewportRect;

            var contentGo = CreateUIObject("Content", viewportGo.transform);
            _contentTransform = contentGo.GetComponent<RectTransform>();
            _contentTransform.anchorMin = new Vector2(0, 1);
            _contentTransform.anchorMax = new Vector2(1, 1);
            _contentTransform.pivot = new Vector2(0.5f, 1);
            _contentTransform.anchoredPosition = Vector2.zero;
            _contentTransform.sizeDelta = new Vector2(0, 0);

            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 2f;
            contentLayout.padding = new RectOffset(2, 2, 2, 2);

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _contentTransform;

            // Canvas for rendering on top - EditModeUI layer
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = VeneerLayers.EditModeUI;

            var raycaster = gameObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            transform.SetAsLastSibling();
            gameObject.SetActive(false);

            // Subscribe to movers changed event to auto-refresh when new elements are added
            VeneerMover.OnMoversChanged += OnMoversListChanged;
        }


        /// <summary>
        /// Called when movers are added or removed. Refreshes the list if visible.
        /// </summary>
        private void OnMoversListChanged()
        {
            // Only refresh if we're visible
            if (gameObject.activeSelf)
            {
                RebuildElementList();
            }
        }

        /// <summary>
        /// Rebuilds the element list from all VeneerMovers, grouped by prefix.
        /// </summary>
        private void RebuildElementList()
        {
            // Clear existing content
            foreach (Transform child in _contentTransform)
            {
                Destroy(child.gameObject);
            }
            _elementToggles.Clear();
            _groupToggles.Clear();
            _groupContents.Clear();

            // Settings section
            CreateSettingsSection();

            // Separator
            CreateSeparator();

            // Get all movers directly - this includes disabled frames
            var movers = VeneerMover.AllMovers
                .Where(m => m != null && !string.IsNullOrEmpty(m.ElementId))
                .Where(m => m.ElementId != "VeneerEditModePanel")
                .OrderBy(m => m.ElementId)
                .ToList();

            // Group movers by prefix
            var groups = new Dictionary<string, List<VeneerMover>>();
            foreach (var mover in movers)
            {
                string prefix = GetGroupPrefix(mover.ElementId);
                if (!groups.ContainsKey(prefix))
                {
                    groups[prefix] = new List<VeneerMover>();
                }
                groups[prefix].Add(mover);
            }

            // Create UI Elements section header
            CreateSectionHeader($"UI Elements ({movers.Count} total)");

            // Create groups sorted alphabetically
            foreach (var group in groups.OrderBy(g => g.Key))
            {
                CreateGroup(group.Key, group.Value);
            }

            // Separator
            CreateSeparator();

            // Actions section
            CreateActionsSection();

            // Force layout update
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentTransform);
        }

        /// <summary>
        /// Gets the group prefix from an element ID.
        /// e.g., "Veneer_PlayerFrame" -> "Veneer", "Nexus_NetworkStats" -> "Nexus"
        /// </summary>
        private string GetGroupPrefix(string elementId)
        {
            int underscoreIndex = elementId.IndexOf('_');
            if (underscoreIndex > 0)
            {
                return elementId.Substring(0, underscoreIndex);
            }
            return "Other";
        }

        /// <summary>
        /// Creates a collapsible group for elements with the same prefix.
        /// </summary>
        private void CreateGroup(string groupName, List<VeneerMover> movers)
        {
            // Initialize group visibility if not set
            if (!_groupVisibility.ContainsKey(groupName))
            {
                _groupVisibility[groupName] = true;
            }
            if (!_groupExpanded.ContainsKey(groupName))
            {
                _groupExpanded[groupName] = true;
            }

            // Group header container
            var groupHeaderGo = CreateUIObject($"GroupHeader_{groupName}", _contentTransform);
            var groupHeaderLE = groupHeaderGo.AddComponent<LayoutElement>();
            groupHeaderLE.minHeight = 22f;
            groupHeaderLE.preferredHeight = 22f;

            var groupHeaderLayout = groupHeaderGo.AddComponent<HorizontalLayoutGroup>();
            groupHeaderLayout.childAlignment = TextAnchor.MiddleLeft;
            groupHeaderLayout.childControlWidth = true;
            groupHeaderLayout.childControlHeight = true;
            groupHeaderLayout.childForceExpandWidth = false;
            groupHeaderLayout.childForceExpandHeight = false;
            groupHeaderLayout.spacing = 4f;
            groupHeaderLayout.padding = new RectOffset(4, 4, 2, 2);

            var groupHeaderBg = groupHeaderGo.AddComponent<Image>();
            groupHeaderBg.color = new Color(0.18f, 0.18f, 0.2f, 1f);
            groupHeaderBg.raycastTarget = true;

            // Expand/collapse arrow button
            var arrowGo = CreateUIObject("Arrow", groupHeaderGo.transform);
            var arrowLE = arrowGo.AddComponent<LayoutElement>();
            arrowLE.minWidth = 16f;
            arrowLE.minHeight = 16f;
            arrowLE.preferredWidth = 16f;
            arrowLE.preferredHeight = 16f;
            arrowLE.flexibleWidth = 0f;

            var arrowText = arrowGo.AddComponent<Text>();
            arrowText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            arrowText.fontSize = 10;
            arrowText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.text = _groupExpanded[groupName] ? "▼" : "►";
            arrowText.raycastTarget = true;

            var arrowBtn = arrowGo.AddComponent<Button>();
            arrowBtn.targetGraphic = arrowText;
            arrowBtn.transition = Selectable.Transition.ColorTint;

            // Group checkbox
            var checkboxGo = CreateUIObject("Checkbox", groupHeaderGo.transform);
            var checkboxLE = checkboxGo.AddComponent<LayoutElement>();
            checkboxLE.minWidth = 14f;
            checkboxLE.minHeight = 14f;
            checkboxLE.preferredWidth = 14f;
            checkboxLE.preferredHeight = 14f;
            checkboxLE.flexibleWidth = 0f;

            var checkboxBg = checkboxGo.AddComponent<Image>();
            checkboxBg.color = new Color(0.25f, 0.25f, 0.28f, 1f);
            checkboxBg.raycastTarget = true;

            var checkmarkGo = CreateUIObject("Checkmark", checkboxGo.transform);
            var checkmarkRect = checkmarkGo.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;

            var checkmarkImage = checkmarkGo.AddComponent<Image>();
            checkmarkImage.color = VeneerColors.Accent;
            checkmarkImage.raycastTarget = false;
            checkmarkGo.SetActive(_groupVisibility[groupName]);

            var groupToggle = checkboxGo.AddComponent<Toggle>();
            groupToggle.isOn = _groupVisibility[groupName];
            groupToggle.graphic = checkmarkImage;
            groupToggle.targetGraphic = checkboxBg;

            _groupToggles[groupName] = groupToggle;

            // Group label
            var labelGo = CreateUIObject("Label", groupHeaderGo.transform);
            var labelLE = labelGo.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1f;
            labelLE.minHeight = 16f;

            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 11;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = VeneerColors.TextGold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = $"{groupName} ({movers.Count})";
            labelText.raycastTarget = false;

            // Group content container
            var groupContentGo = CreateUIObject($"GroupContent_{groupName}", _contentTransform);
            var groupContentLE = groupContentGo.AddComponent<LayoutElement>();
            groupContentLE.minHeight = 0f;

            var groupContentLayout = groupContentGo.AddComponent<VerticalLayoutGroup>();
            groupContentLayout.childAlignment = TextAnchor.UpperLeft;
            groupContentLayout.childControlWidth = true;
            groupContentLayout.childControlHeight = true;
            groupContentLayout.childForceExpandWidth = true;
            groupContentLayout.childForceExpandHeight = false;
            groupContentLayout.spacing = 1f;
            groupContentLayout.padding = new RectOffset(16, 2, 0, 2);

            var groupContentFitter = groupContentGo.AddComponent<ContentSizeFitter>();
            groupContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            groupContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _groupContents[groupName] = groupContentGo;
            groupContentGo.SetActive(_groupExpanded[groupName]);

            // Create element toggles within the group
            foreach (var mover in movers)
            {
                CreateElementToggle(mover.ElementId, GetFriendlyName(mover.ElementId), groupContentGo.transform);
            }

            // Wire up group toggle
            groupToggle.onValueChanged.AddListener(value =>
            {
                _groupVisibility[groupName] = value;
                checkmarkGo.SetActive(value);
                SetGroupVisible(groupName, value);
            });

            // Wire up arrow button for expand/collapse
            arrowBtn.onClick.AddListener(() =>
            {
                _groupExpanded[groupName] = !_groupExpanded[groupName];
                arrowText.text = _groupExpanded[groupName] ? "▼" : "►";
                groupContentGo.SetActive(_groupExpanded[groupName]);
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentTransform);
            });
        }

        /// <summary>
        /// Sets all elements in a group visible or hidden (in edit mode only).
        /// </summary>
        private void SetGroupVisible(string groupName, bool visible)
        {
            foreach (var mover in VeneerMover.AllMovers)
            {
                if (mover != null && !string.IsNullOrEmpty(mover.ElementId))
                {
                    if (GetGroupPrefix(mover.ElementId) == groupName)
                    {
                        SetElementVisibleInEditMode(mover.ElementId, visible);

                        // Update individual toggle if exists
                        if (_elementToggles.TryGetValue(mover.ElementId, out var toggle))
                        {
                            toggle.SetIsOnWithoutNotify(visible);
                            // Update checkmark
                            var checkmark = toggle.graphic?.gameObject;
                            if (checkmark != null) checkmark.SetActive(visible);
                        }
                    }
                }
            }
        }

        private void CreateSettingsSection()
        {
            CreateSectionHeader("Settings");

            // Grid lines toggle
            CreateConfigToggle("Show Grid", VeneerConfig.ShowGridLines, () => RefreshEditModeOverlay());

            // Element labels toggle
            CreateConfigToggle("Show Labels", VeneerConfig.ShowMoverTooltips, null);
        }

        private void CreateActionsSection()
        {
            CreateSectionHeader("Actions");

            // Show All button
            CreateActionButton("Show All", () =>
            {
                foreach (var kvp in _groupToggles)
                {
                    kvp.Value.isOn = true;
                }
            });

            // Hide All button
            CreateActionButton("Hide All", () =>
            {
                foreach (var kvp in _groupToggles)
                {
                    kvp.Value.isOn = false;
                }
            });

            // Expand All button
            CreateActionButton("Expand All Groups", () =>
            {
                foreach (var groupName in _groupContents.Keys.ToList())
                {
                    _groupExpanded[groupName] = true;
                    if (_groupContents.TryGetValue(groupName, out var content))
                    {
                        content.SetActive(true);
                    }
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentTransform);
            });

            // Collapse All button
            CreateActionButton("Collapse All Groups", () =>
            {
                foreach (var groupName in _groupContents.Keys.ToList())
                {
                    _groupExpanded[groupName] = false;
                    if (_groupContents.TryGetValue(groupName, out var content))
                    {
                        content.SetActive(false);
                    }
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentTransform);
            });

            // Reset Positions button
            CreateActionButton("Reset All Positions", () =>
            {
                VeneerAnchor.ResetAllToDefault();
                // Force refresh all movers to apply reset positions
                foreach (var mover in VeneerMover.AllMovers)
                {
                    if (mover != null && !string.IsNullOrEmpty(mover.ElementId))
                    {
                        var rect = mover.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            VeneerAnchor.ApplySavedLayout(rect, mover.ElementId);
                        }
                    }
                }
                Plugin.Log.LogInfo("VeneerEditModePanel: Reset all positions to defaults");
            });
        }

        private void CreateSectionHeader(string title)
        {
            var headerGo = CreateUIObject($"Header_{title}", _contentTransform);

            var headerLE = headerGo.AddComponent<LayoutElement>();
            headerLE.minHeight = 20f;
            headerLE.preferredHeight = 20f;

            var headerBg = headerGo.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.15f, 0.17f, 1f);
            headerBg.raycastTarget = false;

            var textGo = CreateUIObject("Text", headerGo.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6, 0);
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 11;
            text.fontStyle = FontStyle.Bold;
            text.color = VeneerColors.TextGold;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = title;
            text.raycastTarget = false;
        }

        private void CreateSeparator()
        {
            var sepGo = CreateUIObject("Separator", _contentTransform);

            var sepLE = sepGo.AddComponent<LayoutElement>();
            sepLE.minHeight = 6f;
            sepLE.preferredHeight = 6f;
        }

        private void CreateConfigToggle(string label, BepInEx.Configuration.ConfigEntry<bool> config, Action onChange)
        {
            var itemGo = CreateUIObject($"Config_{label}", _contentTransform);

            var itemLE = itemGo.AddComponent<LayoutElement>();
            itemLE.minHeight = 18f;
            itemLE.preferredHeight = 18f;

            var itemLayout = itemGo.AddComponent<HorizontalLayoutGroup>();
            itemLayout.childAlignment = TextAnchor.MiddleLeft;
            itemLayout.childControlWidth = true;
            itemLayout.childControlHeight = true;
            itemLayout.childForceExpandWidth = false;
            itemLayout.childForceExpandHeight = false;
            itemLayout.spacing = 6f;
            itemLayout.padding = new RectOffset(6, 4, 0, 0);

            // Checkbox
            var checkboxGo = CreateUIObject("Checkbox", itemGo.transform);

            var checkboxLE = checkboxGo.AddComponent<LayoutElement>();
            checkboxLE.minWidth = 14f;
            checkboxLE.minHeight = 14f;
            checkboxLE.preferredWidth = 14f;
            checkboxLE.preferredHeight = 14f;
            checkboxLE.flexibleWidth = 0f;

            var checkboxBg = checkboxGo.AddComponent<Image>();
            checkboxBg.color = new Color(0.2f, 0.2f, 0.22f, 1f);
            checkboxBg.raycastTarget = true;

            var checkmarkGo = CreateUIObject("Checkmark", checkboxGo.transform);
            var checkmarkRect = checkmarkGo.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;

            var checkmarkImage = checkmarkGo.AddComponent<Image>();
            checkmarkImage.color = VeneerColors.Accent;
            checkmarkImage.raycastTarget = false;
            checkmarkGo.SetActive(config.Value);

            var toggle = checkboxGo.AddComponent<Toggle>();
            toggle.isOn = config.Value;
            toggle.graphic = checkmarkImage;
            toggle.targetGraphic = checkboxBg;
            toggle.transition = Selectable.Transition.ColorTint;

            var colors = toggle.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.22f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.32f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.17f, 1f);
            toggle.colors = colors;

            toggle.onValueChanged.AddListener(value =>
            {
                config.Value = value;
                checkmarkGo.SetActive(value);
                onChange?.Invoke();
            });

            // Label
            var labelGo = CreateUIObject("Label", itemGo.transform);

            var labelLE = labelGo.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1f;
            labelLE.minHeight = 16f;

            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 11;
            labelText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = label;
            labelText.raycastTarget = false;
        }

        private void CreateElementToggle(string elementId, string label, Transform parent)
        {
            var itemGo = CreateUIObject($"Element_{elementId}", parent);

            var itemLE = itemGo.AddComponent<LayoutElement>();
            itemLE.minHeight = 16f;
            itemLE.preferredHeight = 16f;

            var itemLayout = itemGo.AddComponent<HorizontalLayoutGroup>();
            itemLayout.childAlignment = TextAnchor.MiddleLeft;
            itemLayout.childControlWidth = true;
            itemLayout.childControlHeight = true;
            itemLayout.childForceExpandWidth = false;
            itemLayout.childForceExpandHeight = false;
            itemLayout.spacing = 6f;
            itemLayout.padding = new RectOffset(4, 4, 0, 0);

            // Checkbox
            var checkboxGo = CreateUIObject("Checkbox", itemGo.transform);

            var checkboxLE = checkboxGo.AddComponent<LayoutElement>();
            checkboxLE.minWidth = 12f;
            checkboxLE.minHeight = 12f;
            checkboxLE.preferredWidth = 12f;
            checkboxLE.preferredHeight = 12f;
            checkboxLE.flexibleWidth = 0f;

            var checkboxBg = checkboxGo.AddComponent<Image>();
            checkboxBg.color = new Color(0.2f, 0.2f, 0.22f, 1f);
            checkboxBg.raycastTarget = true;

            var checkmarkGo = CreateUIObject("Checkmark", checkboxGo.transform);
            var checkmarkRect = checkmarkGo.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;

            var checkmarkImage = checkmarkGo.AddComponent<Image>();
            checkmarkImage.color = new Color(0.4f, 0.7f, 0.4f, 1f); // Green for visibility
            checkmarkImage.raycastTarget = false;

            // Initialize visibility state
            if (!_editModeVisibility.ContainsKey(elementId))
            {
                _editModeVisibility[elementId] = true;
            }
            bool isVisible = _editModeVisibility[elementId];
            checkmarkGo.SetActive(isVisible);

            var toggle = checkboxGo.AddComponent<Toggle>();
            toggle.isOn = isVisible;
            toggle.graphic = checkmarkImage;
            toggle.targetGraphic = checkboxBg;
            toggle.transition = Selectable.Transition.ColorTint;

            var colors = toggle.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.22f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.32f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.17f, 1f);
            toggle.colors = colors;

            toggle.onValueChanged.AddListener(value =>
            {
                _editModeVisibility[elementId] = value;
                checkmarkGo.SetActive(value);
                SetElementVisibleInEditMode(elementId, value);

                // Update group toggle state
                UpdateGroupToggleState(GetGroupPrefix(elementId));
            });

            _elementToggles[elementId] = toggle;

            // Label
            var labelGo = CreateUIObject("Label", itemGo.transform);

            var labelLE = labelGo.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1f;
            labelLE.minHeight = 14f;

            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 10;
            labelText.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = label;
            labelText.raycastTarget = false;
        }

        /// <summary>
        /// Updates the group toggle state based on child element states.
        /// </summary>
        private void UpdateGroupToggleState(string groupName)
        {
            if (!_groupToggles.TryGetValue(groupName, out var groupToggle))
                return;

            // Check if all elements in this group are visible
            bool allVisible = true;
            bool anyVisible = false;

            foreach (var mover in VeneerMover.AllMovers)
            {
                if (mover != null && !string.IsNullOrEmpty(mover.ElementId))
                {
                    if (GetGroupPrefix(mover.ElementId) == groupName)
                    {
                        bool isVisible = _editModeVisibility.TryGetValue(mover.ElementId, out var v) ? v : true;
                        if (isVisible) anyVisible = true;
                        else allVisible = false;
                    }
                }
            }

            // Update group toggle without triggering callback
            groupToggle.SetIsOnWithoutNotify(allVisible);
            _groupVisibility[groupName] = allVisible;

            // Update checkmark
            var checkmark = groupToggle.graphic?.gameObject;
            if (checkmark != null) checkmark.SetActive(anyVisible);
        }

        private void CreateActionButton(string label, Action onClick)
        {
            var btnGo = CreateUIObject($"Btn_{label}", _contentTransform);

            var btnLE = btnGo.AddComponent<LayoutElement>();
            btnLE.minHeight = 22f;
            btnLE.preferredHeight = 22f;

            var btnLayout = btnGo.AddComponent<HorizontalLayoutGroup>();
            btnLayout.padding = new RectOffset(6, 6, 2, 2);

            var btnBgGo = CreateUIObject("BtnBg", btnGo.transform);

            var btnBgLE = btnBgGo.AddComponent<LayoutElement>();
            btnBgLE.flexibleWidth = 1f;
            btnBgLE.minHeight = 18f;

            var btnBg = btnBgGo.AddComponent<Image>();
            btnBg.color = new Color(0.25f, 0.25f, 0.28f, 1f);
            btnBg.raycastTarget = true;

            var btn = btnBgGo.AddComponent<Button>();
            btn.targetGraphic = btnBg;

            var btnColors = btn.colors;
            btnColors.normalColor = new Color(0.25f, 0.25f, 0.28f, 1f);
            btnColors.highlightedColor = new Color(0.35f, 0.35f, 0.38f, 1f);
            btnColors.pressedColor = new Color(0.2f, 0.2f, 0.22f, 1f);
            btn.colors = btnColors;

            btn.onClick.AddListener(() => onClick?.Invoke());

            var textGo = CreateUIObject("Text", btnBgGo.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 10;
            text.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            text.raycastTarget = false;
        }

        private string GetFriendlyName(string elementId)
        {
            // Remove prefix (e.g., "Veneer_PlayerFrame" -> "PlayerFrame")
            string name = elementId;
            int underscoreIndex = name.IndexOf('_');
            if (underscoreIndex > 0 && underscoreIndex < name.Length - 1)
            {
                name = name.Substring(underscoreIndex + 1);
            }

            // Add spaces before capitals (e.g., "PlayerFrame" -> "Player Frame")
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(name[i]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Sets an element's visibility during edit mode.
        /// This only controls the edit mode overlay visibility - it doesn't change
        /// the actual window's open/closed state outside of edit mode.
        /// </summary>
        private void SetElementVisibleInEditMode(string elementId, bool visible)
        {
            foreach (var mover in VeneerMover.AllMovers)
            {
                if (mover != null && mover.ElementId == elementId)
                {
                    // Only control visibility during edit mode
                    // The mover handles restoring original state when edit mode ends
                    mover.gameObject.SetActive(visible);
                    Plugin.Log.LogDebug($"VeneerEditModePanel: Set {elementId} edit-mode visible={visible}");
                    return;
                }
            }
        }

        private void RefreshEditModeOverlay()
        {
            if (VeneerMover.EditModeEnabled)
            {
                VeneerAPI.ExitEditMode();
                VeneerAPI.EnterEditMode();
            }
        }

        /// <summary>
        /// Called when exiting edit mode - resets visibility tracking.
        /// The VeneerMover handles restoring actual window states.
        /// </summary>
        public void ResetEditModeState()
        {
            // Clear our tracking - VeneerMover.RestoreAfterEditMode handles actual state restoration
            _editModeVisibility.Clear();
            _groupVisibility.Clear();
        }

        public override void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
            }

            // Rebuild the element list when showing
            RebuildElementList();

            Plugin.Log.LogDebug($"VeneerEditModePanel.Show() - active={gameObject.activeSelf}");
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            VeneerMover.OnMoversChanged -= OnMoversListChanged;
            base.OnDestroy();
            if (_instance == this)
                _instance = null;
        }

        internal void OnBeginPanelDrag(PointerEventData eventData)
        {
            _isDragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);
            _dragOffset = RectTransform.anchoredPosition - localPoint;
        }

        internal void OnPanelDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            RectTransform.anchoredPosition = localPoint + _dragOffset;
        }

        internal void OnEndPanelDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// Drag handler for the edit mode panel header.
    /// </summary>
    public class EditModePanelDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public VeneerEditModePanel Target { get; set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Target?.OnBeginPanelDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Target?.OnPanelDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Target?.OnEndPanelDrag(eventData);
        }
    }
}
