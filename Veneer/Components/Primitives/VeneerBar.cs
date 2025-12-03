using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Primitives
{
    /// <summary>
    /// Progress/status bar component.
    /// Used for health, stamina, experience, cast bars, etc.
    /// </summary>
    public class VeneerBar : VeneerElement
    {
        private Image _backgroundImage;
        private Image _fillImage;
        private Image _borderImage;
        private Text _valueText;
        private RectTransform _fillRect;

        private float _currentValue;
        private float _maxValue = 100f;
        private bool _showText = true;
        private string _textFormat = "{0:F0}/{1:F0}";

        /// <summary>
        /// Current bar value.
        /// </summary>
        public float Value
        {
            get => _currentValue;
            set => SetValue(value);
        }

        /// <summary>
        /// Maximum bar value.
        /// </summary>
        public float MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = Mathf.Max(0.001f, value);
                UpdateBar();
            }
        }

        /// <summary>
        /// Fill percentage (0-1).
        /// </summary>
        public float FillAmount => _maxValue > 0 ? Mathf.Clamp01(_currentValue / _maxValue) : 0f;

        /// <summary>
        /// Fill color.
        /// </summary>
        public Color FillColor
        {
            get => _fillImage != null ? _fillImage.color : VeneerColors.Accent;
            set
            {
                if (_fillImage != null)
                    _fillImage.color = value;
            }
        }

        /// <summary>
        /// Background color.
        /// </summary>
        public Color BackgroundColor
        {
            get => _backgroundImage != null ? _backgroundImage.color : VeneerColors.BackgroundDark;
            set
            {
                if (_backgroundImage != null)
                    _backgroundImage.color = value;
            }
        }

        /// <summary>
        /// Whether to show the value text.
        /// </summary>
        public bool ShowText
        {
            get => _showText;
            set
            {
                _showText = value;
                if (_valueText != null)
                    _valueText.enabled = value;
            }
        }

        /// <summary>
        /// Text format string. Use {0} for current, {1} for max, {2} for percentage.
        /// </summary>
        public string TextFormat
        {
            get => _textFormat;
            set
            {
                _textFormat = value;
                UpdateBar();
            }
        }

        /// <summary>
        /// Creates a new VeneerBar.
        /// </summary>
        public static VeneerBar Create(Transform parent, string name = "VeneerBar", float width = 200, float height = 20)
        {
            var go = CreateUIObject(name, parent);
            var bar = go.AddComponent<VeneerBar>();
            bar.Initialize(width, height);
            return bar;
        }

        /// <summary>
        /// Creates a health bar with default styling.
        /// </summary>
        public static VeneerBar CreateHealthBar(Transform parent, string name = "HealthBar")
        {
            var bar = Create(parent, name, 200, VeneerDimensions.BarHeightLarge);
            bar.FillColor = VeneerColors.Health;
            bar.BackgroundColor = VeneerColors.HealthBackground;
            return bar;
        }

        /// <summary>
        /// Creates a stamina bar with default styling.
        /// </summary>
        public static VeneerBar CreateStaminaBar(Transform parent, string name = "StaminaBar")
        {
            var bar = Create(parent, name, 200, VeneerDimensions.BarHeightLarge);
            bar.FillColor = VeneerColors.Stamina;
            bar.BackgroundColor = VeneerColors.StaminaBackground;
            return bar;
        }

        /// <summary>
        /// Creates an eitr bar with default styling.
        /// </summary>
        public static VeneerBar CreateEitrBar(Transform parent, string name = "EitrBar")
        {
            var bar = Create(parent, name, 200, VeneerDimensions.BarHeightLarge);
            bar.FillColor = VeneerColors.Eitr;
            bar.BackgroundColor = VeneerColors.EitrBackground;
            return bar;
        }

        /// <summary>
        /// Creates an experience bar.
        /// </summary>
        public static VeneerBar CreateExperienceBar(Transform parent, string name = "ExperienceBar")
        {
            var bar = Create(parent, name, 300, VeneerDimensions.BarHeightSmall);
            bar.FillColor = VeneerColors.Epic;
            bar.ShowText = false;
            return bar;
        }

        /// <summary>
        /// Creates a cast/progress bar.
        /// </summary>
        public static VeneerBar CreateCastBar(Transform parent, string name = "CastBar")
        {
            var bar = Create(parent, name, 250, VeneerDimensions.BarHeight);
            bar.FillColor = VeneerColors.Accent;
            bar.TextFormat = "{2:F0}%";
            return bar;
        }

        private void Initialize(float width, float height)
        {
            SetSize(width, height);

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.color = VeneerColors.BackgroundDark;

            // Fill container (for clipping)
            var fillContainer = CreateUIObject("FillContainer", transform);
            var containerRect = fillContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.one; // 1px padding for border
            containerRect.offsetMax = -Vector2.one;

            // Fill bar
            var fillGo = CreateUIObject("Fill", fillContainer.transform);
            _fillRect = fillGo.GetComponent<RectTransform>();
            _fillRect.anchorMin = Vector2.zero;
            _fillRect.anchorMax = new Vector2(0, 1);
            _fillRect.pivot = new Vector2(0, 0.5f);
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;

            _fillImage = fillGo.AddComponent<Image>();
            _fillImage.color = VeneerColors.Accent;

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

            // Value text
            var textGo = CreateUIObject("ValueText", transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(VeneerDimensions.Padding, 0);
            textRect.offsetMax = new Vector2(-VeneerDimensions.Padding, 0);

            _valueText = textGo.AddComponent<Text>();
            _valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _valueText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
            _valueText.color = VeneerColors.Text;
            _valueText.alignment = TextAnchor.MiddleCenter;
            _valueText.raycastTarget = false;

            // Add outline for readability
            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = VeneerColors.BackgroundSolid;
            outline.effectDistance = new Vector2(1, -1);

            UpdateBar();
        }

        /// <summary>
        /// Sets the bar value.
        /// </summary>
        public void SetValue(float value)
        {
            _currentValue = Mathf.Max(0, value);
            UpdateBar();
        }

        /// <summary>
        /// Sets both current and max values.
        /// </summary>
        public void SetValues(float current, float max)
        {
            _maxValue = Mathf.Max(0.001f, max);
            _currentValue = Mathf.Clamp(current, 0, _maxValue);
            UpdateBar();
        }

        /// <summary>
        /// Sets the fill amount directly (0-1).
        /// </summary>
        public void SetFillAmount(float amount)
        {
            amount = Mathf.Clamp01(amount);
            _currentValue = amount * _maxValue;
            UpdateBar();
        }

        private void UpdateBar()
        {
            if (_fillRect == null) return;

            float fill = FillAmount;

            // Update fill width
            _fillRect.anchorMax = new Vector2(fill, 1);

            // Update text
            if (_valueText != null && _showText)
            {
                float percentage = fill * 100f;
                _valueText.text = string.Format(_textFormat, _currentValue, _maxValue, percentage);
            }
        }

        /// <summary>
        /// Smoothly animates to a new value.
        /// </summary>
        public void AnimateToValue(float targetValue, float duration = 0.3f)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateCoroutine(targetValue, duration));
        }

        private System.Collections.IEnumerator AnimateCoroutine(float target, float duration)
        {
            float start = _currentValue;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t); // Smooth step
                SetValue(Mathf.Lerp(start, target, t));
                yield return null;
            }

            SetValue(target);
        }

        /// <summary>
        /// Sets fill color based on percentage thresholds.
        /// </summary>
        public void SetThresholdColors(Color high, Color medium, Color low, float mediumThreshold = 0.5f, float lowThreshold = 0.25f)
        {
            float fill = FillAmount;
            if (fill <= lowThreshold)
                FillColor = low;
            else if (fill <= mediumThreshold)
                FillColor = medium;
            else
                FillColor = high;
        }
    }
}
