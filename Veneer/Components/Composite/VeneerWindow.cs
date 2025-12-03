using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Components.Composite
{
    /// <summary>
    /// Draggable window component with title bar and close button.
    /// </summary>
    public class VeneerWindow : VeneerElement, IBeginDragHandler, IDragHandler
    {
        private Image _backgroundImage;
        private Image _borderImage;
        private RectTransform _titleBar;
        private RectTransform _contentArea;
        private Text _titleText;
        private VeneerButton _closeButton;
        private Canvas _canvas;

        private bool _isDraggable = true;
        private bool _isDragging;
        private Vector2 _dragOffset;

        /// <summary>
        /// Window title text.
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
        /// Whether the window can be dragged.
        /// </summary>
        public bool IsDraggable
        {
            get => _isDraggable;
            set => _isDraggable = value;
        }

        /// <summary>
        /// The content area for adding child elements.
        /// </summary>
        public RectTransform Content => _contentArea;

        /// <summary>
        /// Called when the window is closed.
        /// </summary>
        public event Action OnClosed;

        /// <summary>
        /// Creates a new VeneerWindow.
        /// </summary>
        public static VeneerWindow Create(Transform parent, WindowConfig config)
        {
            var go = CreateUIObject(config.Name ?? "VeneerWindow", parent);
            var window = go.AddComponent<VeneerWindow>();
            window.Initialize(config);
            return window;
        }

        /// <summary>
        /// Creates a simple window.
        /// </summary>
        public static VeneerWindow Create(Transform parent, string title, float width = 400, float height = 300)
        {
            return Create(parent, new WindowConfig
            {
                Title = title,
                Width = width,
                Height = height
            });
        }

        private void Initialize(WindowConfig config)
        {
            ElementId = config.Id;
            IsMoveable = config.Moveable;
            SavePosition = config.SavePosition;

            _canvas = GetComponentInParent<Canvas>();

            SetSize(config.Width, config.Height);

            if (config.Anchor.HasValue)
            {
                AnchorTo(config.Anchor.Value, config.Offset);
            }
            else
            {
                // Default to center
                AnchorTo(AnchorPreset.MiddleCenter);
            }

            CreateBackground();
            CreateBorder();
            CreateTitleBar(config.Title, config.Closeable);
            CreateContentArea();

            // Register with anchor system if saveable
            if (!string.IsNullOrEmpty(config.Id) && config.SavePosition)
            {
                var anchor = config.Anchor.HasValue
                    ? ConvertToScreenAnchor(config.Anchor.Value)
                    : ScreenAnchor.Center;
                VeneerAnchor.Register(config.Id, anchor, config.Offset);

                // Apply saved position if exists
                var savedData = VeneerAnchor.GetAnchorData(config.Id);
                if (savedData != null)
                {
                    VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
                }
            }
        }

        private void CreateBackground()
        {
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.Background;
        }

        private void CreateBorder()
        {
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
        }

        private void CreateTitleBar(string title, bool closeable)
        {
            var titleBarGo = CreateUIObject("TitleBar", transform);
            _titleBar = titleBarGo.GetComponent<RectTransform>();
            _titleBar.anchorMin = new Vector2(0, 1);
            _titleBar.anchorMax = Vector2.one;
            _titleBar.pivot = new Vector2(0.5f, 1);
            _titleBar.sizeDelta = new Vector2(0, VeneerDimensions.WindowTitleHeight);
            _titleBar.anchoredPosition = Vector2.zero;

            // Title bar background
            var titleBg = titleBarGo.AddComponent<Image>();
            titleBg.color = VeneerColors.BackgroundLight;

            // Title text
            var titleTextGo = CreateUIObject("TitleText", _titleBar);
            var titleTextRect = titleTextGo.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(VeneerDimensions.Padding, 0);
            titleTextRect.offsetMax = new Vector2(closeable ? -VeneerDimensions.WindowTitleHeight : -VeneerDimensions.Padding, 0);

            _titleText = titleTextGo.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.text = title ?? "Window";
            _titleText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = VeneerColors.TextGold;
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.raycastTarget = false;

            // Close button
            if (closeable)
            {
                CreateCloseButton();
            }
        }

        private void CreateCloseButton()
        {
            var closeGo = CreateUIObject("CloseButton", _titleBar);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.sizeDelta = new Vector2(VeneerDimensions.WindowTitleHeight, 0);
            closeRect.anchoredPosition = Vector2.zero;

            _closeButton = closeGo.AddComponent<VeneerButton>();

            // Manual setup since we can't use static Create here
            var bgImage = closeGo.AddComponent<Image>();
            bgImage.color = Color.clear;

            // X label
            var xGo = CreateUIObject("X", closeRect);
            var xRect = xGo.GetComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;

            var xText = xGo.AddComponent<Text>();
            xText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            xText.text = "×";
            xText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeLarge);
            xText.color = VeneerColors.TextMuted;
            xText.alignment = TextAnchor.MiddleCenter;
            xText.raycastTarget = false;

            var button = closeGo.AddComponent<Button>();
            button.targetGraphic = bgImage;
            button.onClick.AddListener(Close);

            // Hover effect
            var trigger = closeGo.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => xText.color = VeneerColors.Error);
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => xText.color = VeneerColors.TextMuted);
            trigger.triggers.Add(exitEntry);
        }

        private void CreateContentArea()
        {
            var contentGo = CreateUIObject("Content", transform);
            _contentArea = contentGo.GetComponent<RectTransform>();
            _contentArea.anchorMin = Vector2.zero;
            _contentArea.anchorMax = Vector2.one;
            _contentArea.offsetMin = new Vector2(VeneerDimensions.Padding, VeneerDimensions.Padding);
            _contentArea.offsetMax = new Vector2(-VeneerDimensions.Padding, -VeneerDimensions.WindowTitleHeight - VeneerDimensions.Padding);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isDraggable) return;

            // Only drag from title bar
            if (!RectTransformUtility.RectangleContainsScreenPoint(_titleBar, eventData.position, eventData.pressEventCamera))
                return;

            _isDragging = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            _dragOffset = RectTransform.anchoredPosition - localPoint;

            // Bring to front
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || !_isDraggable) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            RectTransform.anchoredPosition = localPoint + _dragOffset;
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void Close()
        {
            OnClosed?.Invoke();
            Hide();
        }

        /// <summary>
        /// Shows the window and brings it to front.
        /// </summary>
        public override void Show()
        {
            base.Show();
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Adds a text element to the content area.
        /// </summary>
        public VeneerText AddText(string content, TextStyle style = TextStyle.Body)
        {
            var text = VeneerText.Create(_contentArea, content);
            text.ApplyStyle(style);
            return text;
        }

        /// <summary>
        /// Adds a button to the content area.
        /// </summary>
        public VeneerButton AddButton(string label, Action onClick)
        {
            return VeneerButton.Create(_contentArea, label, onClick);
        }

        /// <summary>
        /// Adds a panel to the content area.
        /// </summary>
        public VeneerPanel AddPanel(float width, float height)
        {
            return VeneerPanel.Create(_contentArea, "Panel", width, height);
        }

        /// <summary>
        /// Adds a bar to the content area.
        /// </summary>
        public VeneerBar AddBar(float width = 200, float height = 20)
        {
            return VeneerBar.Create(_contentArea, "Bar", width, height);
        }

        private ScreenAnchor ConvertToScreenAnchor(AnchorPreset preset)
        {
            return preset switch
            {
                AnchorPreset.TopLeft => ScreenAnchor.TopLeft,
                AnchorPreset.TopCenter => ScreenAnchor.TopCenter,
                AnchorPreset.TopRight => ScreenAnchor.TopRight,
                AnchorPreset.MiddleLeft => ScreenAnchor.Left,
                AnchorPreset.MiddleCenter => ScreenAnchor.Center,
                AnchorPreset.MiddleRight => ScreenAnchor.Right,
                AnchorPreset.BottomLeft => ScreenAnchor.BottomLeft,
                AnchorPreset.BottomCenter => ScreenAnchor.BottomCenter,
                AnchorPreset.BottomRight => ScreenAnchor.BottomRight,
                _ => ScreenAnchor.Center
            };
        }
    }

    /// <summary>
    /// Configuration for creating a VeneerWindow.
    /// </summary>
    public class WindowConfig
    {
        /// <summary>
        /// Unique identifier for the window.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// GameObject name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Window title.
        /// </summary>
        public string Title { get; set; } = "Window";

        /// <summary>
        /// Width in pixels.
        /// </summary>
        public float Width { get; set; } = VeneerDimensions.WindowDefaultWidth;

        /// <summary>
        /// Height in pixels.
        /// </summary>
        public float Height { get; set; } = VeneerDimensions.WindowDefaultHeight;

        /// <summary>
        /// Whether the window has a close button.
        /// </summary>
        public bool Closeable { get; set; } = true;

        /// <summary>
        /// Whether the window can be moved.
        /// </summary>
        public bool Moveable { get; set; } = true;

        /// <summary>
        /// Whether to save the position.
        /// </summary>
        public bool SavePosition { get; set; } = true;

        /// <summary>
        /// Anchor preset position.
        /// </summary>
        public AnchorPreset? Anchor { get; set; }

        /// <summary>
        /// Offset from anchor.
        /// </summary>
        public Vector2 Offset { get; set; }
    }
}
