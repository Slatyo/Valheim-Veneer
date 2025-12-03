using System;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Composite
{
    /// <summary>
    /// Dialog for splitting item stacks.
    /// </summary>
    public class VeneerSplitDialog : VeneerElement
    {
        private static VeneerSplitDialog _instance;

        private Image _backgroundImage;
        private Image _borderImage;
        private VeneerText _titleText;
        private VeneerText _amountText;
        private Slider _slider;
        private VeneerButton _okButton;
        private VeneerButton _cancelButton;
        private VeneerButton _halfButton;

        private ItemDrop.ItemData _item;
        private int _maxAmount;
        private int _currentAmount;
        private Action<int> _onConfirm;
        private Action _onCancel;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static VeneerSplitDialog Instance => _instance;

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

        /// <summary>
        /// Initializes the split dialog system.
        /// </summary>
        public static void Initialize(Transform parent)
        {
            if (_instance != null) return;

            var go = CreateUIObject("VeneerSplitDialog", parent);
            var dialog = go.AddComponent<VeneerSplitDialog>();
            dialog.Setup();
            dialog.gameObject.SetActive(false);
        }

        private void Setup()
        {
            float width = 280f;
            float height = 180f;
            float padding = VeneerDimensions.PaddingLarge;

            SetSize(width, height);
            AnchorTo(AnchorPreset.MiddleCenter);

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
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Accent, Color.clear, 2);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 2);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Content
            var content = CreateUIObject("Content", transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;

            // Title
            var titleGo = CreateUIObject("Title", content.transform);
            _titleText = titleGo.AddComponent<VeneerText>();
            _titleText.Content = "Split Stack";
            _titleText.ApplyStyle(TextStyle.Header);
            _titleText.Alignment = TextAnchor.MiddleCenter;
            var titleLayout = titleGo.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 24;

            // Amount display
            var amountGo = CreateUIObject("Amount", content.transform);
            _amountText = amountGo.AddComponent<VeneerText>();
            _amountText.Content = "1 / 1";
            _amountText.ApplyStyle(TextStyle.Value);
            _amountText.TextColor = VeneerColors.Accent;
            _amountText.Alignment = TextAnchor.MiddleCenter;
            var amountLayout = amountGo.AddComponent<LayoutElement>();
            amountLayout.preferredHeight = 24;

            // Slider
            var sliderGo = CreateUIObject("Slider", content.transform);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            var sliderLayout = sliderGo.AddComponent<LayoutElement>();
            sliderLayout.preferredHeight = 20;
            sliderLayout.flexibleWidth = 1;

            _slider = sliderGo.AddComponent<Slider>();
            _slider.minValue = 1;
            _slider.maxValue = 100;
            _slider.wholeNumbers = true;
            _slider.onValueChanged.AddListener(OnSliderChanged);

            // Slider background
            var sliderBg = CreateUIObject("Background", sliderGo.transform);
            var sliderBgRect = sliderBg.GetComponent<RectTransform>();
            sliderBgRect.anchorMin = new Vector2(0, 0.25f);
            sliderBgRect.anchorMax = new Vector2(1, 0.75f);
            sliderBgRect.offsetMin = Vector2.zero;
            sliderBgRect.offsetMax = Vector2.zero;
            var sliderBgImage = sliderBg.AddComponent<Image>();
            sliderBgImage.color = VeneerColors.BackgroundDark;

            // Slider fill area
            var fillArea = CreateUIObject("Fill Area", sliderGo.transform);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = CreateUIObject("Fill", fillArea.transform);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = VeneerColors.Accent;

            _slider.fillRect = fillRect;

            // Slider handle
            var handleArea = CreateUIObject("Handle Slide Area", sliderGo.transform);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            var handle = CreateUIObject("Handle", handleArea.transform);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16, 24);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = VeneerColors.AccentHover;

            _slider.handleRect = handleRect;
            _slider.targetGraphic = handleImage;

            // Button row
            var buttonRow = CreateUIObject("Buttons", content.transform);
            var buttonRowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonRowLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonRowLayout.childControlWidth = false;
            buttonRowLayout.childControlHeight = true;
            buttonRowLayout.childForceExpandWidth = false;
            buttonRowLayout.childForceExpandHeight = true;
            buttonRowLayout.spacing = 8f;
            var buttonRowLE = buttonRow.AddComponent<LayoutElement>();
            buttonRowLE.preferredHeight = 30;

            // Half button
            _halfButton = VeneerButton.Create(buttonRow.transform, "Half", OnHalfClick);
            _halfButton.SetButtonSize(ButtonSize.Small);
            var halfLE = _halfButton.gameObject.AddComponent<LayoutElement>();
            halfLE.preferredWidth = 50;

            // OK button
            _okButton = VeneerButton.CreatePrimary(buttonRow.transform, "OK", OnOkClick);
            _okButton.SetButtonSize(ButtonSize.Small);
            var okLE = _okButton.gameObject.AddComponent<LayoutElement>();
            okLE.preferredWidth = 60;

            // Cancel button
            _cancelButton = VeneerButton.Create(buttonRow.transform, "Cancel", OnCancelClick);
            _cancelButton.SetButtonSize(ButtonSize.Small);
            var cancelLE = _cancelButton.gameObject.AddComponent<LayoutElement>();
            cancelLE.preferredWidth = 60;

            // Block input behind dialog
            var canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;

            // Canvas for rendering on top - Modal layer
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = VeneerLayers.Modal;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void OnSliderChanged(float value)
        {
            _currentAmount = Mathf.RoundToInt(value);
            UpdateAmountText();
        }

        private void OnHalfClick()
        {
            _slider.value = _maxAmount / 2f;
        }

        private void OnOkClick()
        {
            _onConfirm?.Invoke(_currentAmount);
            HideDialog();
        }

        private void OnCancelClick()
        {
            _onCancel?.Invoke();
            HideDialog();
        }

        private void UpdateAmountText()
        {
            _amountText.Content = $"{_currentAmount} / {_maxAmount}";
        }

        private void Update()
        {
            // Handle keyboard input
            if (gameObject.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnOkClick();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    OnCancelClick();
                }
            }
        }

        /// <summary>
        /// Shows the split dialog.
        /// </summary>
        public static void Show(ItemDrop.ItemData item, int maxAmount, Action<int> onConfirm, Action onCancel = null)
        {
            if (_instance == null)
            {
                Plugin.Log.LogWarning("VeneerSplitDialog not initialized");
                return;
            }

            _instance._item = item;
            _instance._maxAmount = maxAmount;
            _instance._currentAmount = Mathf.Max(1, maxAmount / 2);
            _instance._onConfirm = onConfirm;
            _instance._onCancel = onCancel;

            _instance._slider.minValue = 1;
            _instance._slider.maxValue = maxAmount;
            _instance._slider.value = _instance._currentAmount;

            string itemName = item != null ? Localization.instance.Localize(item.m_shared.m_name) : "Item";
            _instance._titleText.Content = $"Split {itemName}";
            _instance.UpdateAmountText();

            _instance.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the dialog.
        /// </summary>
        public static void HideDialog()
        {
            if (_instance != null)
            {
                _instance.gameObject.SetActive(false);
                _instance._onConfirm = null;
                _instance._onCancel = null;
            }
        }

        /// <summary>
        /// Whether the dialog is currently showing.
        /// </summary>
        public static bool IsShowing => _instance != null && _instance.gameObject.activeSelf;

        /// <summary>
        /// Cleans up the dialog.
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
}
