using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Primitives
{
    /// <summary>
    /// Styled button component with rounded corners and smooth transitions.
    /// </summary>
    public class VeneerButton : VeneerElement, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Image _backgroundImage;
        private Image _borderImage;
        private Text _label;
        private Button _button;
        private ButtonStyle _style = ButtonStyle.Default;
        private bool _isHovered;
        private bool _isPressed;

        // Smooth transition support
        private Coroutine _colorTransition;
        private Color _targetBgColor;
        private Color _targetBorderColor;

        /// <summary>
        /// Button label text.
        /// </summary>
        public string Label
        {
            get => _label != null ? _label.text : string.Empty;
            set
            {
                if (_label != null)
                    _label.text = value;
            }
        }

        /// <summary>
        /// Whether the button is interactable.
        /// </summary>
        public new bool Interactable
        {
            get => _button != null && _button.interactable;
            set
            {
                if (_button != null)
                {
                    _button.interactable = value;
                    UpdateVisuals();
                }
            }
        }

        /// <summary>
        /// Click event.
        /// </summary>
        public event Action OnClick;

        /// <summary>
        /// Creates a new VeneerButton.
        /// </summary>
        public static VeneerButton Create(Transform parent, string label, Action onClick = null, string name = "VeneerButton")
        {
            var go = CreateUIObject(name, parent);
            var button = go.AddComponent<VeneerButton>();
            button.Initialize(label, onClick);
            return button;
        }

        /// <summary>
        /// Creates a primary-styled button.
        /// </summary>
        public static VeneerButton CreatePrimary(Transform parent, string label, Action onClick = null)
        {
            var button = Create(parent, label, onClick, "PrimaryButton");
            button.SetStyle(ButtonStyle.Primary);
            return button;
        }

        /// <summary>
        /// Creates a danger-styled button.
        /// </summary>
        public static VeneerButton CreateDanger(Transform parent, string label, Action onClick = null)
        {
            var button = Create(parent, label, onClick, "DangerButton");
            button.SetStyle(ButtonStyle.Danger);
            return button;
        }

        /// <summary>
        /// Creates a ghost button (transparent background).
        /// </summary>
        public static VeneerButton CreateGhost(Transform parent, string label, Action onClick = null)
        {
            var button = Create(parent, label, onClick, "GhostButton");
            button.SetStyle(ButtonStyle.Ghost);
            return button;
        }

        /// <summary>
        /// Creates a tab-styled button.
        /// </summary>
        public static VeneerButton CreateTab(Transform parent, string label, Action onClick = null, bool isActive = false)
        {
            var button = Create(parent, label, onClick, "TabButton");
            button.SetStyle(isActive ? ButtonStyle.TabActive : ButtonStyle.Tab);
            return button;
        }

        private void Initialize(string label, Action onClick)
        {
            SetSize(VeneerDimensions.ButtonWidthMin, VeneerDimensions.ButtonHeight);

            int cornerRadius = (int)VeneerDimensions.CornerRadiusSmall;

            // Background with rounded corners
            _backgroundImage = gameObject.AddComponent<Image>();
            var bgTexture = VeneerTextures.CreateRoundedRectTexture(
                32,
                Color.white,
                Color.clear,
                cornerRadius,
                0
            );
            _backgroundImage.sprite = VeneerTextures.CreateRoundedSprite(bgTexture, cornerRadius);
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.ButtonNormal;

            // Border with rounded corners
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTexture = VeneerTextures.CreateRoundedRectTexture(
                32,
                Color.clear,
                Color.white,
                cornerRadius,
                1
            );
            _borderImage.sprite = VeneerTextures.CreateRoundedSprite(borderTexture, cornerRadius);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.color = VeneerColors.Border;
            _borderImage.raycastTarget = false;

            // Label
            var labelGo = CreateUIObject("Label", transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(VeneerDimensions.Padding, 0);
            labelRect.offsetMax = new Vector2(-VeneerDimensions.Padding, 0);

            _label = labelGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.text = label;
            _label.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
            _label.color = VeneerColors.Text;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.raycastTarget = false;

            // Button component
            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = _backgroundImage;
            _button.transition = Selectable.Transition.None; // We handle visuals ourselves
            _button.onClick.AddListener(() => OnClick?.Invoke());

            if (onClick != null)
                OnClick += onClick;

            // Initialize target colors
            _targetBgColor = VeneerColors.ButtonNormal;
            _targetBorderColor = VeneerColors.Border;

            UpdateVisuals();
        }

        /// <summary>
        /// Sets the button style.
        /// </summary>
        public void SetStyle(ButtonStyle style)
        {
            _style = style;
            UpdateVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateVisuals();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            UpdateVisuals();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_backgroundImage == null || _borderImage == null || _label == null) return;

            bool interactable = _button == null || _button.interactable;

            Color bgColor;
            Color borderColor;
            Color textColor = VeneerColors.Text;

            if (!interactable)
            {
                bgColor = VeneerColors.BackgroundDark;
                borderColor = VeneerColors.BorderDark;
                textColor = VeneerColors.TextMuted;
            }
            else if (_isPressed)
            {
                bgColor = GetPressedColor();
                borderColor = GetBorderColor(true);
            }
            else if (_isHovered)
            {
                bgColor = GetHoverColor();
                borderColor = GetBorderColor(true);
            }
            else
            {
                bgColor = GetNormalColor();
                borderColor = GetBorderColor(false);
            }

            // Smooth color transition (only if active, otherwise apply immediately)
            _targetBgColor = bgColor;
            _targetBorderColor = borderColor;

            if (_colorTransition != null)
            {
                StopCoroutine(_colorTransition);
                _colorTransition = null;
            }

            if (gameObject.activeInHierarchy)
            {
                _colorTransition = StartCoroutine(TransitionColors(VeneerTheme.ButtonHoverDuration));
            }
            else
            {
                // Apply immediately if inactive
                _backgroundImage.color = bgColor;
                _borderImage.color = borderColor;
            }

            _label.color = interactable ? GetTextColor() : textColor;
        }

        private IEnumerator TransitionColors(float duration)
        {
            Color startBg = _backgroundImage.color;
            Color startBorder = _borderImage.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = VeneerAnimation.SmoothStep(t);

                _backgroundImage.color = Color.Lerp(startBg, _targetBgColor, eased);
                _borderImage.color = Color.Lerp(startBorder, _targetBorderColor, eased);

                yield return null;
            }

            _backgroundImage.color = _targetBgColor;
            _borderImage.color = _targetBorderColor;
            _colorTransition = null;
        }

        private Color GetNormalColor()
        {
            return _style switch
            {
                ButtonStyle.Primary => VeneerColors.Accent,
                ButtonStyle.Danger => VeneerColors.Darken(VeneerColors.Error, 0.3f),
                ButtonStyle.Ghost => Color.clear,
                ButtonStyle.Tab => VeneerColors.BackgroundDark,
                ButtonStyle.TabActive => VeneerColors.BackgroundLight, // Slightly lighter to indicate active
                _ => VeneerColors.ButtonNormal
            };
        }

        private Color GetHoverColor()
        {
            return _style switch
            {
                ButtonStyle.Primary => VeneerColors.AccentHover,
                ButtonStyle.Danger => VeneerColors.Error,
                ButtonStyle.Ghost => VeneerColors.WithAlpha(VeneerColors.BackgroundLight, 0.5f),
                ButtonStyle.Tab => VeneerColors.BackgroundLight,
                ButtonStyle.TabActive => VeneerColors.ButtonHover,
                _ => VeneerColors.ButtonHover
            };
        }

        private Color GetPressedColor()
        {
            return _style switch
            {
                ButtonStyle.Primary => VeneerColors.AccentPressed,
                ButtonStyle.Danger => VeneerColors.Darken(VeneerColors.Error, 0.2f),
                ButtonStyle.Ghost => VeneerColors.WithAlpha(VeneerColors.BackgroundLight, 0.7f),
                ButtonStyle.Tab => VeneerColors.BackgroundSolid,
                ButtonStyle.TabActive => VeneerColors.BackgroundSolid,
                _ => VeneerColors.ButtonPressed
            };
        }

        private Color GetBorderColor(bool highlighted)
        {
            if (_style == ButtonStyle.Ghost)
                return highlighted ? VeneerColors.BorderLight : Color.clear;

            if (_style == ButtonStyle.Tab)
                return highlighted ? VeneerColors.Border : VeneerColors.BorderDark;

            if (_style == ButtonStyle.TabActive)
                // Subtle gold tint border - brighter on hover
                return highlighted ? VeneerColors.AccentSubtle : VeneerColors.AccentMuted;

            return highlighted ? VeneerColors.BorderLight : VeneerColors.Border;
        }

        private Color GetTextColor()
        {
            return _style switch
            {
                ButtonStyle.Primary => VeneerColors.BackgroundSolid,
                ButtonStyle.Danger => VeneerColors.Text,
                ButtonStyle.Ghost => VeneerColors.Text,
                ButtonStyle.Tab => VeneerColors.TextMuted,
                ButtonStyle.TabActive => VeneerColors.Accent, // Gold text for active tab
                _ => VeneerColors.Text
            };
        }

        /// <summary>
        /// Sets the button size.
        /// </summary>
        public void SetButtonSize(ButtonSize size)
        {
            float height = size switch
            {
                ButtonSize.Small => VeneerDimensions.ButtonHeightSmall,
                ButtonSize.Medium => VeneerDimensions.ButtonHeightMedium,
                ButtonSize.Large => VeneerDimensions.ButtonHeightLarge,
                _ => VeneerDimensions.ButtonHeight
            };

            int fontSize = size switch
            {
                ButtonSize.Small => VeneerDimensions.FontSizeSmall,
                ButtonSize.Medium => VeneerDimensions.FontSizeNormal,
                ButtonSize.Large => VeneerDimensions.FontSizeMedium,
                _ => VeneerDimensions.FontSizeNormal
            };

            RectTransform.sizeDelta = new Vector2(RectTransform.sizeDelta.x, height);
            _label.fontSize = VeneerConfig.GetScaledFontSize(fontSize);
        }
    }

    /// <summary>
    /// Button visual styles.
    /// </summary>
    public enum ButtonStyle
    {
        Default,
        Primary,
        Danger,
        Ghost,
        Tab,
        TabActive
    }

    /// <summary>
    /// Button size presets.
    /// </summary>
    public enum ButtonSize
    {
        Small,
        Normal,
        Medium,
        Large
    }
}
