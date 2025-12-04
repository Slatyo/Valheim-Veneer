using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Composite
{
    /// <summary>
    /// A card component displaying an icon, title, and optional subtitle.
    /// Features hover lift animation and click support.
    /// </summary>
    public class VeneerCard : VeneerElement, IPointerClickHandler
    {
        private Image _backgroundImage;
        private Image _borderImage;
        private Image _iconImage;
        private Text _titleText;
        private Text _subtitleText;
        private VeneerAnimatable _animatable;
        private bool _isSelected;
        private bool _isLocked;

        /// <summary>
        /// Card icon sprite.
        /// </summary>
        public Sprite Icon
        {
            get => _iconImage != null ? _iconImage.sprite : null;
            set
            {
                if (_iconImage != null)
                    _iconImage.sprite = value;
            }
        }

        /// <summary>
        /// Card title text.
        /// </summary>
        public string Title
        {
            get => _titleText != null ? _titleText.text : string.Empty;
            set
            {
                if (_titleText != null)
                    _titleText.text = value;
            }
        }

        /// <summary>
        /// Card subtitle text.
        /// </summary>
        public string Subtitle
        {
            get => _subtitleText != null ? _subtitleText.text : string.Empty;
            set
            {
                if (_subtitleText != null)
                    _subtitleText.text = value;
            }
        }

        /// <summary>
        /// Whether this card is selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Whether this card is locked/disabled (reduces opacity).
        /// </summary>
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                _isLocked = value;
                if (_animatable != null)
                    _animatable.IsDisabled = value;
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Fired when the card is clicked.
        /// </summary>
        public event Action OnClick;

        /// <summary>
        /// User data attached to this card.
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Creates a new VeneerCard.
        /// </summary>
        public static VeneerCard Create(Transform parent, float width = 140f, float height = 120f, string name = "VeneerCard")
        {
            var go = CreateUIObject(name, parent);
            var card = go.AddComponent<VeneerCard>();
            card.Initialize(width, height);
            return card;
        }

        private void Initialize(float width, float height)
        {
            SetSize(width, height);

            // Add animatable for hover lift effect
            _animatable = VeneerAnimatable.Setup(gameObject, hoverLift: true, pressScale: true, liftAmount: 4f);

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreateButtonSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.BackgroundLight;

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

            // Icon container (centered at top)
            float iconSize = Mathf.Min(width * 0.5f, 72f);
            var iconContainer = CreateUIObject("IconContainer", transform);
            var iconContainerRect = iconContainer.GetComponent<RectTransform>();
            iconContainerRect.anchorMin = new Vector2(0.5f, 1f);
            iconContainerRect.anchorMax = new Vector2(0.5f, 1f);
            iconContainerRect.pivot = new Vector2(0.5f, 1f);
            iconContainerRect.anchoredPosition = new Vector2(0, -10f);
            iconContainerRect.sizeDelta = new Vector2(iconSize, iconSize);

            // Icon background
            var iconBg = iconContainer.AddComponent<Image>();
            iconBg.color = VeneerColors.BackgroundDark;
            iconBg.sprite = VeneerTextures.CreateButtonSprite();
            iconBg.type = Image.Type.Sliced;

            // Icon
            var iconGo = CreateUIObject("Icon", iconContainer.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            _iconImage = iconGo.AddComponent<Image>();
            _iconImage.preserveAspect = true;
            _iconImage.raycastTarget = false;

            // Title (below icon)
            var titleGo = CreateUIObject("Title", transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 0);
            titleRect.pivot = new Vector2(0.5f, 0);
            titleRect.anchoredPosition = new Vector2(0, 24f);
            titleRect.sizeDelta = new Vector2(-12f, 20f);

            _titleText = titleGo.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = VeneerColors.Text;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _titleText.raycastTarget = false;

            // Subtitle (at bottom)
            var subtitleGo = CreateUIObject("Subtitle", transform);
            var subtitleRect = subtitleGo.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0, 0);
            subtitleRect.anchorMax = new Vector2(1, 0);
            subtitleRect.pivot = new Vector2(0.5f, 0);
            subtitleRect.anchoredPosition = new Vector2(0, 8f);
            subtitleRect.sizeDelta = new Vector2(-12f, 16f);

            _subtitleText = subtitleGo.AddComponent<Text>();
            _subtitleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _subtitleText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall - 1);
            _subtitleText.color = VeneerColors.TextMuted;
            _subtitleText.alignment = TextAnchor.MiddleCenter;
            _subtitleText.raycastTarget = false;

            UpdateVisuals();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Always allow click - locked just affects visuals, not interactivity
            OnClick?.Invoke();
        }

        private void UpdateVisuals()
        {
            if (_backgroundImage == null || _borderImage == null) return;

            if (_isSelected)
            {
                _backgroundImage.color = VeneerColors.WithAlpha(VeneerColors.Accent, 0.25f);
                _borderImage.color = VeneerColors.Accent;
            }
            else
            {
                _backgroundImage.color = VeneerColors.BackgroundLight;
                _borderImage.color = VeneerColors.Border;
            }
        }

        /// <summary>
        /// Sets icon by emoji text (fallback for non-sprite icons).
        /// Creates a text-based icon display.
        /// </summary>
        public void SetIconText(string emoji, int fontSize = 26)
        {
            if (_iconImage == null) return;

            // Hide the image
            _iconImage.enabled = false;

            // Add or get text component on same object
            var iconText = _iconImage.GetComponent<Text>();
            if (iconText == null)
            {
                iconText = _iconImage.gameObject.AddComponent<Text>();
            }

            iconText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            iconText.fontSize = VeneerConfig.GetScaledFontSize(fontSize);
            iconText.fontStyle = FontStyle.Bold;
            iconText.color = VeneerColors.Text;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.text = emoji;
        }
    }
}
