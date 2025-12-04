using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Primitives
{
    /// <summary>
    /// Styled text component with consistent formatting.
    /// </summary>
    public class VeneerText : VeneerElement
    {
        private Text _text;

        /// <summary>
        /// The text content.
        /// </summary>
        public string Content
        {
            get => _text != null ? _text.text : string.Empty;
            set
            {
                if (_text != null)
                    _text.text = value;
            }
        }

        /// <summary>
        /// Text color.
        /// </summary>
        public Color TextColor
        {
            get => _text != null ? _text.color : VeneerColors.Text;
            set
            {
                if (_text != null)
                    _text.color = value;
            }
        }

        /// <summary>
        /// Font size.
        /// </summary>
        public int FontSize
        {
            get => _text != null ? _text.fontSize : VeneerDimensions.FontSizeNormal;
            set
            {
                if (_text != null)
                    _text.fontSize = value;
            }
        }

        /// <summary>
        /// Text alignment.
        /// </summary>
        public TextAnchor Alignment
        {
            get => _text != null ? _text.alignment : TextAnchor.MiddleLeft;
            set
            {
                if (_text != null)
                    _text.alignment = value;
            }
        }

        /// <summary>
        /// Font style (normal, bold, italic).
        /// </summary>
        public FontStyle Style
        {
            get => _text != null ? _text.fontStyle : FontStyle.Normal;
            set
            {
                if (_text != null)
                    _text.fontStyle = value;
            }
        }

        /// <summary>
        /// Creates a new VeneerText.
        /// </summary>
        public static VeneerText Create(Transform parent, string content, string name = "VeneerText")
        {
            var go = CreateUIObject(name, parent);
            var text = go.AddComponent<VeneerText>();
            // Awake already initialized, just set the content
            text.Content = content;
            return text;
        }

        /// <summary>
        /// Creates a header-styled text.
        /// </summary>
        public static VeneerText CreateHeader(Transform parent, string content, string name = "Header")
        {
            var text = Create(parent, content, name);
            text.ApplyStyle(TextStyle.Header);
            return text;
        }

        /// <summary>
        /// Creates a subheader-styled text.
        /// </summary>
        public static VeneerText CreateSubheader(Transform parent, string content, string name = "Subheader")
        {
            var text = Create(parent, content, name);
            text.ApplyStyle(TextStyle.Subheader);
            return text;
        }

        /// <summary>
        /// Creates body text.
        /// </summary>
        public static VeneerText CreateBody(Transform parent, string content, string name = "Body")
        {
            var text = Create(parent, content, name);
            text.ApplyStyle(TextStyle.Body);
            return text;
        }

        /// <summary>
        /// Creates caption (small) text.
        /// </summary>
        public static VeneerText CreateCaption(Transform parent, string content, string name = "Caption")
        {
            var text = Create(parent, content, name);
            text.ApplyStyle(TextStyle.Caption);
            return text;
        }

        /// <summary>
        /// Creates a value display (like "100/100").
        /// </summary>
        public static VeneerText CreateValue(Transform parent, string content, string name = "Value")
        {
            var text = Create(parent, content, name);
            text.ApplyStyle(TextStyle.Value);
            return text;
        }

        protected override void Awake()
        {
            base.Awake();
            // Auto-initialize if added via AddComponent instead of factory
            if (_text == null)
            {
                Initialize("");
            }
        }

        private void Initialize(string content)
        {
            _text = gameObject.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.text = content;
            _text.fontSize = VeneerDimensions.FontSizeNormal;
            _text.color = VeneerColors.Text;
            _text.alignment = TextAnchor.MiddleLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;

            // Use ContentSizeFitter for auto-sizing
            var fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// Applies a predefined text style.
        /// </summary>
        public void ApplyStyle(TextStyle style)
        {
            switch (style)
            {
                case TextStyle.Header:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeHeader);
                    TextColor = VeneerColors.TextGold;
                    Style = FontStyle.Bold;
                    break;

                case TextStyle.Subheader:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeLarge);
                    TextColor = VeneerColors.Text;
                    Style = FontStyle.Bold;
                    break;

                case TextStyle.Body:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
                    TextColor = VeneerColors.Text;
                    Style = FontStyle.Normal;
                    break;

                case TextStyle.Caption:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
                    TextColor = VeneerColors.TextMuted;
                    Style = FontStyle.Normal;
                    break;

                case TextStyle.Value:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
                    TextColor = VeneerColors.Text;
                    Style = FontStyle.Normal;
                    Alignment = TextAnchor.MiddleCenter;
                    break;

                case TextStyle.Muted:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
                    TextColor = VeneerColors.TextMuted;
                    Style = FontStyle.Normal;
                    break;

                case TextStyle.Gold:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
                    TextColor = VeneerColors.TextGold;
                    Style = FontStyle.Normal;
                    break;

                case TextStyle.Error:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
                    TextColor = VeneerColors.Error;
                    Style = FontStyle.Normal;
                    break;

                case TextStyle.Success:
                    FontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
                    TextColor = VeneerColors.Success;
                    Style = FontStyle.Normal;
                    break;
            }
        }

        /// <summary>
        /// Sets the text color to a rarity color.
        /// </summary>
        public void SetRarityColor(int rarityTier)
        {
            TextColor = VeneerColors.GetRarityColor(rarityTier);
        }

        /// <summary>
        /// Enables text wrapping with a max width.
        /// </summary>
        public void EnableWrapping(float maxWidth)
        {
            if (_text != null)
            {
                _text.horizontalOverflow = HorizontalWrapMode.Wrap;
                RectTransform.sizeDelta = new Vector2(maxWidth, RectTransform.sizeDelta.y);

                var fitter = GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
            }
        }

        /// <summary>
        /// Disables text wrapping.
        /// </summary>
        public void DisableWrapping()
        {
            if (_text != null)
            {
                _text.horizontalOverflow = HorizontalWrapMode.Overflow;

                var fitter = GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }
        }

        /// <summary>
        /// Adds a black outline effect to the text for better readability.
        /// </summary>
        /// <returns>This VeneerText instance for chaining.</returns>
        public VeneerText WithOutline()
        {
            return WithOutline(Color.black);
        }

        /// <summary>
        /// Adds an outline effect to the text with a custom color.
        /// </summary>
        /// <param name="color">The outline color.</param>
        /// <param name="distance">The outline distance (default: 1, -1).</param>
        /// <returns>This VeneerText instance for chaining.</returns>
        public VeneerText WithOutline(Color color, Vector2? distance = null)
        {
            var outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance ?? new Vector2(1, -1);
            return this;
        }

        /// <summary>
        /// Removes the outline effect from the text.
        /// </summary>
        /// <returns>This VeneerText instance for chaining.</returns>
        public VeneerText WithoutOutline()
        {
            var outline = GetComponent<Outline>();
            if (outline != null)
            {
                Destroy(outline);
            }
            return this;
        }

        /// <summary>
        /// Adds a shadow effect to the text.
        /// </summary>
        /// <param name="color">The shadow color (default: black with 50% alpha).</param>
        /// <param name="distance">The shadow distance (default: 1, -1).</param>
        /// <returns>This VeneerText instance for chaining.</returns>
        public VeneerText WithShadow(Color? color = null, Vector2? distance = null)
        {
            var shadow = GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = color ?? new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = distance ?? new Vector2(1, -1);
            return this;
        }
    }

    /// <summary>
    /// Predefined text styles.
    /// </summary>
    public enum TextStyle
    {
        Header,
        Subheader,
        Body,
        Caption,
        Value,
        Muted,
        Gold,
        Error,
        Success
    }
}
