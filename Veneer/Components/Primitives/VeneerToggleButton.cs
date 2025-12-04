using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Primitives
{
    /// <summary>
    /// A button that toggles between active and inactive states.
    /// Useful for filter buttons, sort toggles, and option switches.
    /// </summary>
    public class VeneerToggleButton : VeneerElement, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Image _backgroundImage;
        private Image _borderImage;
        private Text _label;
        private Button _button;
        private bool _isToggled;
        private bool _isHovered;
        private bool _isPressed;

        /// <summary>
        /// Current toggle state.
        /// </summary>
        public bool IsToggled
        {
            get => _isToggled;
            set
            {
                if (_isToggled != value)
                {
                    _isToggled = value;
                    UpdateVisuals();
                }
            }
        }

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
        /// Fired when the toggle state changes.
        /// </summary>
        public event Action<bool> OnToggled;

        /// <summary>
        /// Fired when clicked (regardless of toggle behavior).
        /// </summary>
        public event Action OnClick;

        /// <summary>
        /// Creates a new VeneerToggleButton.
        /// </summary>
        public static VeneerToggleButton Create(Transform parent, string label, Action<bool> onToggled = null, string name = "VeneerToggleButton")
        {
            var go = CreateUIObject(name, parent);
            var button = go.AddComponent<VeneerToggleButton>();
            button.Initialize(label, onToggled);
            return button;
        }

        /// <summary>
        /// Creates a toggle button that acts as a simple click button (no toggle state visible).
        /// Useful for sort buttons where clicking cycles through states.
        /// </summary>
        public static VeneerToggleButton CreateAction(Transform parent, string label, Action onClick, string name = "VeneerActionButton")
        {
            var go = CreateUIObject(name, parent);
            var button = go.AddComponent<VeneerToggleButton>();
            button.Initialize(label, null);
            if (onClick != null)
                button.OnClick += onClick;
            return button;
        }

        private void Initialize(string label, Action<bool> onToggled)
        {
            SetSize(80f, 28f);

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreateButtonSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.BackgroundDark;

            // Border
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTexture = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.BorderDark, Color.clear, 1);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTexture, 1);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Label
            var labelGo = CreateUIObject("Label", transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8, 0);
            labelRect.offsetMax = new Vector2(-8, 0);

            _label = labelGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.text = label;
            _label.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
            _label.color = VeneerColors.TextMuted;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.raycastTarget = false;

            // Button component
            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = _backgroundImage;
            _button.transition = Selectable.Transition.None;
            _button.onClick.AddListener(HandleClick);

            if (onToggled != null)
                OnToggled += onToggled;

            UpdateVisuals();
        }

        private void HandleClick()
        {
            _isToggled = !_isToggled;
            UpdateVisuals();
            OnToggled?.Invoke(_isToggled);
            OnClick?.Invoke();
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

            Color bgColor;
            Color borderColor;
            Color textColor;

            if (_isToggled)
            {
                // Active/toggled state - use accent color
                if (_isPressed)
                {
                    bgColor = VeneerColors.AccentPressed;
                    borderColor = VeneerColors.Accent;
                }
                else if (_isHovered)
                {
                    bgColor = VeneerColors.AccentHover;
                    borderColor = VeneerColors.Accent;
                }
                else
                {
                    bgColor = VeneerColors.BackgroundLight;
                    borderColor = VeneerColors.Accent;
                }
                textColor = VeneerColors.Accent;
            }
            else
            {
                // Inactive state
                if (_isPressed)
                {
                    bgColor = VeneerColors.BackgroundSolid;
                    borderColor = VeneerColors.Border;
                }
                else if (_isHovered)
                {
                    bgColor = VeneerColors.BackgroundLight;
                    borderColor = VeneerColors.Border;
                }
                else
                {
                    bgColor = VeneerColors.BackgroundDark;
                    borderColor = VeneerColors.BorderDark;
                }
                textColor = VeneerColors.TextMuted;
            }

            _backgroundImage.color = bgColor;
            _borderImage.color = borderColor;
            _label.color = textColor;
        }

        /// <summary>
        /// Sets the toggle state without firing the OnToggled event.
        /// </summary>
        public void SetToggledSilent(bool toggled)
        {
            _isToggled = toggled;
            UpdateVisuals();
        }
    }
}
