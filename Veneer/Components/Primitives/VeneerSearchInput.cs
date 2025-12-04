using System;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Primitives
{
    /// <summary>
    /// Styled search input component with placeholder text.
    /// </summary>
    public class VeneerSearchInput : VeneerElement
    {
        private Image _backgroundImage;
        private Image _borderImage;
        private InputField _inputField;
        private Text _placeholderText;
        private Text _inputText;

        /// <summary>
        /// Current input value.
        /// </summary>
        public string Value
        {
            get => _inputField != null ? _inputField.text : string.Empty;
            set
            {
                if (_inputField != null)
                    _inputField.text = value;
            }
        }

        /// <summary>
        /// Placeholder text shown when input is empty.
        /// </summary>
        public string Placeholder
        {
            get => _placeholderText != null ? _placeholderText.text : string.Empty;
            set
            {
                if (_placeholderText != null)
                    _placeholderText.text = value;
            }
        }

        /// <summary>
        /// Fired when the input value changes.
        /// </summary>
        public event Action<string> OnValueChanged;

        /// <summary>
        /// Fired when the user submits (presses Enter).
        /// </summary>
        public event Action<string> OnSubmit;

        /// <summary>
        /// Creates a new VeneerSearchInput.
        /// </summary>
        public static VeneerSearchInput Create(Transform parent, string placeholder = "Search...", string name = "VeneerSearchInput")
        {
            var go = CreateUIObject(name, parent);
            var input = go.AddComponent<VeneerSearchInput>();
            input.Initialize(placeholder);
            return input;
        }

        private void Initialize(string placeholder)
        {
            SetSize(200f, 32f);

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

            // Text area container
            var textAreaGo = CreateUIObject("TextArea", transform);
            var textAreaRect = textAreaGo.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 2);
            textAreaRect.offsetMax = new Vector2(-10, -2);

            // Placeholder text
            var placeholderGo = CreateUIObject("Placeholder", textAreaGo.transform);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            _placeholderText = placeholderGo.AddComponent<Text>();
            _placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _placeholderText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
            _placeholderText.fontStyle = FontStyle.Italic;
            _placeholderText.color = VeneerColors.TextMuted;
            _placeholderText.alignment = TextAnchor.MiddleLeft;
            _placeholderText.text = placeholder;
            _placeholderText.raycastTarget = false;

            // Input text
            var inputTextGo = CreateUIObject("Text", textAreaGo.transform);
            var inputTextRect = inputTextGo.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            _inputText = inputTextGo.AddComponent<Text>();
            _inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _inputText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
            _inputText.color = VeneerColors.Text;
            _inputText.alignment = TextAnchor.MiddleLeft;
            _inputText.supportRichText = false;
            _inputText.raycastTarget = false;

            // Input field component
            _inputField = gameObject.AddComponent<InputField>();
            _inputField.textComponent = _inputText;
            _inputField.placeholder = _placeholderText;
            _inputField.targetGraphic = _backgroundImage;
            _inputField.contentType = InputField.ContentType.Standard;
            _inputField.lineType = InputField.LineType.SingleLine;
            _inputField.caretColor = VeneerColors.Accent;
            _inputField.selectionColor = VeneerColors.WithAlpha(VeneerColors.Accent, 0.3f);

            // Events
            _inputField.onValueChanged.AddListener(OnInputValueChanged);
            _inputField.onEndEdit.AddListener(OnInputSubmit);
        }

        private void OnInputValueChanged(string value)
        {
            OnValueChanged?.Invoke(value);
        }

        private void OnInputSubmit(string value)
        {
            OnSubmit?.Invoke(value);
        }

        /// <summary>
        /// Clears the input.
        /// </summary>
        public void Clear()
        {
            if (_inputField != null)
                _inputField.text = string.Empty;
        }

        /// <summary>
        /// Focuses the input field.
        /// </summary>
        public void Focus()
        {
            if (_inputField != null)
                _inputField.ActivateInputField();
        }
    }
}
