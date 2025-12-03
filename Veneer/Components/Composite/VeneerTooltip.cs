using System;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Composite
{
    /// <summary>
    /// Tooltip component with rich formatting and positioning.
    /// Follows mouse cursor and auto-positions to stay on screen.
    /// </summary>
    public class VeneerTooltip : VeneerElement
    {
        private static VeneerTooltip _instance;

        /// <summary>
        /// Singleton instance. Returns null if not initialized.
        /// </summary>
        public static VeneerTooltip Instance => _instance;

        /// <summary>
        /// Whether the tooltip is initialized.
        /// </summary>
        public static bool IsInitialized => _instance != null;

        private Image _backgroundImage;
        private Image _borderImage;
        private VerticalLayoutGroup _contentLayout;
        private RectTransform _contentRect;
        private Canvas _canvas;

        private Text _titleText;
        private Text _subtitleText;
        private Text _bodyText;
        private GameObject _divider;

        private bool _isShowing;
        private TooltipData _currentData;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            if (_isShowing)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// Initializes the tooltip system.
        /// </summary>
        public static void Initialize(Transform parent)
        {
            if (_instance != null) return;

            var go = CreateUIObject("VeneerTooltip", parent);
            var tooltip = go.AddComponent<VeneerTooltip>();
            tooltip.Setup();
            tooltip.HideInternal();
        }

        private void Setup()
        {
            _canvas = GetComponentInParent<Canvas>();

            // Container setup
            SetPivot(0, 1); // Top-left pivot

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.BackgroundSolid;

            // Border
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTexture = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Border, Color.clear, 1);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTexture, 1);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Content container
            var contentGo = CreateUIObject("Content", transform);
            _contentRect = contentGo.GetComponent<RectTransform>();
            _contentRect.anchorMin = Vector2.zero;
            _contentRect.anchorMax = Vector2.one;
            _contentRect.offsetMin = new Vector2(VeneerDimensions.TooltipPadding, VeneerDimensions.TooltipPadding);
            _contentRect.offsetMax = new Vector2(-VeneerDimensions.TooltipPadding, -VeneerDimensions.TooltipPadding);

            _contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            _contentLayout.childAlignment = TextAnchor.UpperLeft;
            _contentLayout.childControlWidth = true;
            _contentLayout.childControlHeight = true;
            _contentLayout.childForceExpandWidth = true;
            _contentLayout.childForceExpandHeight = false;
            _contentLayout.spacing = VeneerDimensions.Spacing;
            _contentLayout.padding = new RectOffset(0, 0, 0, 0);

            // Content size fitter
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Also add to main object
            var mainFitter = gameObject.AddComponent<ContentSizeFitter>();
            mainFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            mainFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create text elements
            CreateTextElements();

            // Ensure tooltip renders on top of everything - Tooltip layer
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = VeneerLayers.Tooltip;
            gameObject.AddComponent<GraphicRaycaster>();

            var canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void CreateTextElements()
        {
            // Title
            var titleGo = CreateUIObject("Title", _contentRect);
            _titleText = titleGo.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeMedium);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = VeneerColors.Text;
            _titleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var titleLayout = titleGo.AddComponent<LayoutElement>();
            titleLayout.preferredWidth = VeneerDimensions.TooltipMaxWidth;

            // Subtitle
            var subtitleGo = CreateUIObject("Subtitle", _contentRect);
            _subtitleText = subtitleGo.AddComponent<Text>();
            _subtitleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _subtitleText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
            _subtitleText.color = VeneerColors.TextMuted;
            _subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var subtitleLayout = subtitleGo.AddComponent<LayoutElement>();
            subtitleLayout.preferredWidth = VeneerDimensions.TooltipMaxWidth;

            // Divider
            _divider = CreateUIObject("Divider", _contentRect);
            var dividerImage = _divider.AddComponent<Image>();
            dividerImage.color = VeneerColors.Border;
            var dividerLayout = _divider.AddComponent<LayoutElement>();
            dividerLayout.preferredHeight = 1;
            dividerLayout.flexibleWidth = 1;

            // Body
            var bodyGo = CreateUIObject("Body", _contentRect);
            _bodyText = bodyGo.AddComponent<Text>();
            _bodyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _bodyText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
            _bodyText.color = VeneerColors.Text;
            _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var bodyLayout = bodyGo.AddComponent<LayoutElement>();
            bodyLayout.preferredWidth = VeneerDimensions.TooltipMaxWidth;
        }

        /// <summary>
        /// Shows the tooltip with simple text.
        /// </summary>
        public static void Show(string text)
        {
            Show(new TooltipData { Body = text });
        }

        /// <summary>
        /// Shows the tooltip with title and body.
        /// </summary>
        public static void Show(string title, string body)
        {
            Show(new TooltipData { Title = title, Body = body });
        }

        /// <summary>
        /// Shows the tooltip with full data.
        /// </summary>
        public static void Show(TooltipData data)
        {
            if (Instance == null) return;
            Instance.ShowInternal(data);
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public new static void Hide()
        {
            if (_instance != null)
                _instance.HideInternal();
        }

        private void ShowInternal(TooltipData data)
        {
            _currentData = data;
            _isShowing = true;

            // Update title
            if (!string.IsNullOrEmpty(data.Title))
            {
                _titleText.text = data.Title;
                _titleText.color = data.TitleColor ?? VeneerColors.Text;
                _titleText.gameObject.SetActive(true);
            }
            else
            {
                _titleText.gameObject.SetActive(false);
            }

            // Update subtitle
            if (!string.IsNullOrEmpty(data.Subtitle))
            {
                _subtitleText.text = data.Subtitle;
                _subtitleText.color = data.SubtitleColor ?? VeneerColors.TextMuted;
                _subtitleText.gameObject.SetActive(true);
            }
            else
            {
                _subtitleText.gameObject.SetActive(false);
            }

            // Update divider
            _divider.SetActive(!string.IsNullOrEmpty(data.Title) && !string.IsNullOrEmpty(data.Body));

            // Update body
            if (!string.IsNullOrEmpty(data.Body))
            {
                _bodyText.text = data.Body;
                _bodyText.gameObject.SetActive(true);
            }
            else
            {
                _bodyText.gameObject.SetActive(false);
            }

            // Update border color (for rarity)
            if (data.RarityTier.HasValue)
            {
                _borderImage.color = VeneerColors.GetRarityColor(data.RarityTier.Value);
                _titleText.color = VeneerColors.GetRarityColor(data.RarityTier.Value);
            }
            else
            {
                _borderImage.color = VeneerColors.Border;
            }

            gameObject.SetActive(true);

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);

            // Ensure tooltip is on top
            transform.SetAsLastSibling();

            UpdatePosition();
        }

        private void HideInternal()
        {
            _isShowing = false;
            _currentData = null;
            gameObject.SetActive(false);
        }

        private void UpdatePosition()
        {
            Vector2 mousePos = Input.mousePosition;
            float offset = 15f;

            // Force layout update to get accurate size
            Canvas.ForceUpdateCanvases();
            Vector2 tooltipSize = RectTransform.sizeDelta;

            // Handle case where size isn't calculated yet
            if (tooltipSize.x <= 0 || tooltipSize.y <= 0)
            {
                tooltipSize = new Vector2(200, 100); // Fallback size
            }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Default position: below and right of cursor
            Vector2 position = new Vector2(
                mousePos.x + offset,
                mousePos.y - offset
            );

            // If tooltip goes off right edge, flip to left of cursor
            if (position.x + tooltipSize.x > screenWidth)
            {
                position.x = mousePos.x - tooltipSize.x - offset;
            }

            // If tooltip goes off bottom, flip to above cursor
            if (position.y - tooltipSize.y < 0)
            {
                position.y = mousePos.y + tooltipSize.y + offset;
            }

            // Final clamp
            position.x = Mathf.Clamp(position.x, 0, screenWidth - tooltipSize.x);
            position.y = Mathf.Clamp(position.y, tooltipSize.y, screenHeight);

            RectTransform.position = position;
        }

        /// <summary>
        /// Cleans up the tooltip system.
        /// </summary>
        public static void Cleanup()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }

    /// <summary>
    /// Data for tooltip display.
    /// </summary>
    public class TooltipData
    {
        /// <summary>
        /// Main title text.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Title color override.
        /// </summary>
        public Color? TitleColor { get; set; }

        /// <summary>
        /// Subtitle text (below title).
        /// </summary>
        public string Subtitle { get; set; }

        /// <summary>
        /// Subtitle color override.
        /// </summary>
        public Color? SubtitleColor { get; set; }

        /// <summary>
        /// Body text.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Rarity tier for border/title color.
        /// </summary>
        public int? RarityTier { get; set; }
    }
}
