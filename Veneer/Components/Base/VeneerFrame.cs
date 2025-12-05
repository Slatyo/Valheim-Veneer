using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Components.Base
{
    /// <summary>
    /// Styled frame container with border and background.
    /// Base container for most Veneer UI elements.
    /// Supports optional header with title, close button, and dragging.
    /// </summary>
    public class VeneerFrame : VeneerElement, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Image _backgroundImage;
        private Image _borderImage;
        private RectTransform _contentArea;
        private RectTransform _headerRect;
        private Text _titleText;

        // Dragging
        private bool _isDragging;
        private Vector2 _dragOffset;
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // Configuration
        private bool _hasHeader;
        private bool _hasCloseButton;
        private bool _isDraggable;
        private float _headerHeight = VeneerDimensions.WindowTitleHeight;

        /// <summary>
        /// The content area RectTransform (inside padding, below header if present).
        /// </summary>
        public RectTransform Content => _contentArea;

        /// <summary>
        /// Title text (only valid if HasHeader is true).
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
        /// Whether the frame has a header.
        /// </summary>
        public bool HasHeader => _hasHeader;

        /// <summary>
        /// Whether the frame has a close button.
        /// </summary>
        public bool HasCloseButton => _hasCloseButton;

        /// <summary>
        /// Whether the frame is draggable.
        /// </summary>
        public bool IsDraggable
        {
            get => _isDraggable;
            set => _isDraggable = value;
        }

        /// <summary>
        /// Background color of the frame.
        /// </summary>
        public Color BackgroundColor
        {
            get => _backgroundImage != null ? _backgroundImage.color : VeneerColors.Background;
            set
            {
                if (_backgroundImage != null)
                    _backgroundImage.color = value;
            }
        }

        /// <summary>
        /// Border color of the frame.
        /// </summary>
        public Color BorderColor
        {
            get => _borderImage != null ? _borderImage.color : VeneerColors.Border;
            set
            {
                if (_borderImage != null)
                    _borderImage.color = value;
            }
        }

        /// <summary>
        /// Whether the border is visible.
        /// </summary>
        public bool ShowBorder
        {
            get => _borderImage != null && _borderImage.enabled;
            set
            {
                if (_borderImage != null)
                    _borderImage.enabled = value;
            }
        }

        /// <summary>
        /// Event fired when close button is clicked.
        /// </summary>
        public event Action OnCloseClicked;

        /// <summary>
        /// Creates a new VeneerFrame (simple version without header).
        /// </summary>
        public static VeneerFrame Create(Transform parent, string name = "VeneerFrame", float width = 200, float height = 100)
        {
            var go = CreateUIObject(name, parent);
            var frame = go.AddComponent<VeneerFrame>();
            frame.Initialize(new FrameConfig
            {
                Name = name,
                Width = width,
                Height = height
            });
            return frame;
        }

        /// <summary>
        /// Creates a VeneerFrame with configuration.
        /// </summary>
        public static VeneerFrame Create(Transform parent, FrameConfig config)
        {
            var go = CreateUIObject(config.Name ?? "VeneerFrame", parent);
            var frame = go.AddComponent<VeneerFrame>();
            frame.Initialize(config);
            return frame;
        }

        private void Initialize(FrameConfig config)
        {
            ElementId = config.Id;
            IsMoveable = true; // Always moveable in edit mode now
            SavePosition = config.SavePosition;

            _hasHeader = config.HasHeader;
            _hasCloseButton = config.HasCloseButton;
            _isDraggable = config.IsDraggable;
            _headerHeight = config.HeaderHeight > 0 ? config.HeaderHeight : VeneerDimensions.WindowTitleHeight;

            // Cache canvas reference
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvasRect = _canvas.GetComponent<RectTransform>();
            }

            SetSize(config.Width, config.Height);

            CreateBackground();
            CreateBorder();

            if (_hasHeader)
            {
                CreateHeader(config.Title, _hasCloseButton);
            }

            CreateContentArea();

            if (config.BackgroundColor.HasValue)
                BackgroundColor = config.BackgroundColor.Value;
            if (config.BorderColor.HasValue)
                BorderColor = config.BorderColor.Value;
            ShowBorder = config.ShowBorder;

            if (config.Anchor.HasValue)
                AnchorTo(config.Anchor.Value, config.Offset);

            // Register with anchor system if we have an Id
            // This is needed for Edit Mode positioning to work (VeneerMover uses VeneerAnchor)
            // SavePosition controls whether the position persists to disk
            if (!string.IsNullOrEmpty(config.Id))
            {
                var anchor = config.Anchor.HasValue
                    ? ConvertToScreenAnchor(config.Anchor.Value)
                    : ScreenAnchor.Center;
                VeneerAnchor.Register(config.Id, anchor, config.Offset, RectTransform.sizeDelta);

                // Apply saved position if exists
                var savedData = VeneerAnchor.GetAnchorData(config.Id);
                if (savedData != null)
                {
                    VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
                    if (savedData.Size != Vector2.zero)
                    {
                        SetSize(savedData.Size.x, savedData.Size.y);
                    }
                }
            }

            // Always add VeneerMover for edit mode positioning (if we have an Id)
            // VeneerMover handles Edit Mode dragging - always available
            // IsDraggable controls normal gameplay dragging (header drag)
            if (!string.IsNullOrEmpty(config.Id))
            {
                var mover = gameObject.AddComponent<VeneerMover>();
                mover.ElementId = config.Id;

                // Always add VeneerResizer for edit mode resizing
                // Uses config min/max if provided, otherwise reasonable defaults based on frame size
                var resizer = gameObject.AddComponent<VeneerResizer>();
                resizer.MinSize = config.MinSize ?? new Vector2(
                    Mathf.Max(100, config.Width * 0.5f),
                    Mathf.Max(50, config.Height * 0.5f)
                );
                resizer.MaxSize = config.MaxSize ?? new Vector2(
                    Mathf.Max(config.Width * 2f, 800),
                    Mathf.Max(config.Height * 2f, 600)
                );
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

            var borderTexture = VeneerTextures.CreateSlicedBorderTexture(
                16,
                VeneerColors.Border,
                Color.clear,
                1
            );
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTexture, 1);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.color = VeneerColors.Border;
            _borderImage.raycastTarget = false;
        }

        private void CreateHeader(string title, bool closeable)
        {
            var headerGo = CreateUIObject("Header", transform);
            _headerRect = headerGo.GetComponent<RectTransform>();
            _headerRect.anchorMin = new Vector2(0, 1);
            _headerRect.anchorMax = Vector2.one;
            _headerRect.pivot = new Vector2(0.5f, 1);
            _headerRect.sizeDelta = new Vector2(0, _headerHeight);
            _headerRect.anchoredPosition = Vector2.zero;

            // Header background - use lighter color like VeneerWindow
            var headerBg = headerGo.AddComponent<Image>();
            headerBg.color = VeneerColors.BackgroundLight;

            // Title text
            var titleGo = CreateUIObject("TitleText", _headerRect);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(VeneerDimensions.Padding, 0);
            titleRect.offsetMax = new Vector2(closeable ? -_headerHeight : -VeneerDimensions.Padding, 0);

            _titleText = titleGo.AddComponent<Text>();
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
            var closeGo = CreateUIObject("CloseButton", _headerRect);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.sizeDelta = new Vector2(_headerHeight, 0);
            closeRect.anchoredPosition = Vector2.zero;

            // Background for hover effects
            var bgImage = closeGo.AddComponent<Image>();
            bgImage.color = Color.clear;

            // X label - styled like VeneerWindow
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

            // Hover effect - turns red on hover like VeneerWindow
            var trigger = closeGo.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => xText.color = VeneerColors.Error);
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => xText.color = VeneerColors.TextMuted);
            trigger.triggers.Add(exitEntry);
        }

        /// <summary>
        /// Closes the frame (fires OnCloseClicked).
        /// Note: Does NOT call Hide() - the owner should handle hiding via OnCloseClicked.
        /// This prevents double-hide issues when the frame is nested inside a panel.
        /// </summary>
        public void Close()
        {
            OnCloseClicked?.Invoke();
        }

        private void CreateContentArea()
        {
            var contentGo = CreateUIObject("Content", transform);
            _contentArea = contentGo.GetComponent<RectTransform>();
            _contentArea.anchorMin = Vector2.zero;
            _contentArea.anchorMax = Vector2.one;

            float topOffset = _hasHeader ? _headerHeight + VeneerDimensions.Padding : VeneerDimensions.Padding;
            _contentArea.offsetMin = new Vector2(VeneerDimensions.Padding, VeneerDimensions.Padding);
            _contentArea.offsetMax = new Vector2(-VeneerDimensions.Padding, -topOffset);
        }

        #region Drag Handling

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isDraggable) return;

            // Only drag from header if we have one, otherwise allow dragging from anywhere
            if (_hasHeader && _headerRect != null)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_headerRect, eventData.position, eventData.pressEventCamera))
                    return;
            }

            _isDragging = true;

            // Cache canvas and its RectTransform if not already cached
            if (_canvas == null || _canvasRect == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                if (_canvas != null)
                {
                    _canvasRect = _canvas.GetComponent<RectTransform>();
                }
            }

            if (_canvasRect != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint);

                _dragOffset = RectTransform.anchoredPosition - localPoint;
            }

            // Bring to front
            BringToFront();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            if (_canvasRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            Vector2 newPosition = localPoint + _dragOffset;

            // Clamp to screen bounds
            newPosition = ClampToScreen(newPosition);

            RectTransform.anchoredPosition = newPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Save position if configured
            if (SavePosition && !string.IsNullOrEmpty(ElementId))
            {
                VeneerAnchor.UpdatePosition(ElementId, ScreenAnchor.Center, RectTransform.anchoredPosition, RectTransform.sizeDelta);
            }
        }

        /// <summary>
        /// Clamps position to keep frame on screen.
        /// Uses screen coordinates for accurate bounds regardless of parent hierarchy.
        /// </summary>
        private Vector2 ClampToScreen(Vector2 position)
        {
            if (_canvasRect == null) return position;

            // Get screen dimensions in canvas space
            float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
            float screenWidth = Screen.width / scaleFactor;
            float screenHeight = Screen.height / scaleFactor;

            Vector2 frameSize = RectTransform.sizeDelta;

            // Minimum visible portion - at least the header or 30px should stay on screen
            float minVisible = _hasHeader ? _headerHeight + 10f : 30f;

            // Get the parent's position in canvas space to calculate proper bounds
            // The position is relative to parent, so we need to account for parent's offset from canvas center
            Vector2 parentOffset = Vector2.zero;
            RectTransform parentRect = transform.parent as RectTransform;
            if (parentRect != null && parentRect != _canvasRect)
            {
                // Get world corners of parent and convert to canvas-relative position
                Vector3[] parentCorners = new Vector3[4];
                parentRect.GetWorldCorners(parentCorners);
                Vector3 parentCenter = (parentCorners[0] + parentCorners[2]) / 2f;

                // Convert to canvas local space
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    RectTransformUtility.WorldToScreenPoint(null, parentCenter),
                    null,
                    out parentOffset);
            }

            // Calculate bounds - frame position is relative to its parent's center
            // We need to keep at least minVisible pixels of the frame on screen
            float halfScreenW = screenWidth / 2f;
            float halfScreenH = screenHeight / 2f;

            // Adjust bounds based on parent offset
            float minX = -halfScreenW - parentOffset.x + minVisible;
            float maxX = halfScreenW - parentOffset.x - minVisible;
            float minY = -halfScreenH - parentOffset.y + minVisible;
            float maxY = halfScreenH - parentOffset.y - minVisible;

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);

            return position;
        }

        #endregion

        #region Helper Methods (from VeneerWindow)

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

        #endregion

        #region Styling Methods

        /// <summary>
        /// Sets the internal padding of the content area.
        /// </summary>
        public void SetPadding(float padding)
        {
            if (_contentArea != null)
            {
                float topOffset = _hasHeader ? _headerHeight + padding : padding;
                _contentArea.offsetMin = new Vector2(padding, padding);
                _contentArea.offsetMax = new Vector2(-padding, -topOffset);
            }
        }

        /// <summary>
        /// Sets different padding for each side.
        /// </summary>
        public void SetPadding(float left, float right, float top, float bottom)
        {
            if (_contentArea != null)
            {
                float topOffset = _hasHeader ? _headerHeight + top : top;
                _contentArea.offsetMin = new Vector2(left, bottom);
                _contentArea.offsetMax = new Vector2(-right, -topOffset);
            }
        }

        /// <summary>
        /// Highlights the border with the accent color.
        /// </summary>
        public void Highlight(bool highlighted)
        {
            BorderColor = highlighted ? VeneerColors.BorderHighlight : VeneerColors.Border;
        }

        /// <summary>
        /// Sets the border to a rarity color.
        /// </summary>
        public void SetRarityBorder(int rarityTier)
        {
            BorderColor = VeneerColors.GetRarityColor(rarityTier);
        }

        #endregion

        /// <summary>
        /// Shows the frame and brings it to front.
        /// </summary>
        public override void Show()
        {
            base.Show();
            BringToFront();
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
    /// Configuration for creating a VeneerFrame.
    /// </summary>
    public class FrameConfig
    {
        /// <summary>
        /// Unique identifier for this frame.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// GameObject name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Window title (only shown if HasHeader is true).
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Width in pixels.
        /// </summary>
        public float Width { get; set; } = 200;

        /// <summary>
        /// Height in pixels.
        /// </summary>
        public float Height { get; set; } = 100;

        /// <summary>
        /// Header height in pixels (defaults to VeneerDimensions.WindowTitleHeight).
        /// </summary>
        public float HeaderHeight { get; set; } = 0;

        /// <summary>
        /// Whether to show a header bar with title.
        /// </summary>
        public bool HasHeader { get; set; } = false;

        /// <summary>
        /// Whether to show a close button (requires HasHeader = true).
        /// </summary>
        public bool HasCloseButton { get; set; } = false;

        /// <summary>
        /// Whether the frame can be dragged during normal gameplay (drags from header if present, else anywhere).
        /// Note: Edit Mode dragging (F8) is always available via VeneerMover regardless of this setting.
        /// </summary>
        public bool IsDraggable { get; set; } = false;

        /// <summary>
        /// Optional background color override.
        /// </summary>
        public Color? BackgroundColor { get; set; }

        /// <summary>
        /// Optional border color override.
        /// </summary>
        public Color? BorderColor { get; set; }

        /// <summary>
        /// Whether to show the border.
        /// </summary>
        public bool ShowBorder { get; set; } = true;

        /// <summary>
        /// Anchor preset position.
        /// </summary>
        public AnchorPreset? Anchor { get; set; }

        /// <summary>
        /// Offset from anchor position.
        /// </summary>
        public Vector2 Offset { get; set; }

        /// <summary>
        /// Whether to save the position.
        /// </summary>
        public bool SavePosition { get; set; } = false;

        /// <summary>
        /// Optional minimum size for resizing. If null, defaults to 50% of initial size (min 100x50).
        /// </summary>
        public Vector2? MinSize { get; set; }

        /// <summary>
        /// Optional maximum size for resizing. If null, defaults to 200% of initial size (max 800x600).
        /// </summary>
        public Vector2? MaxSize { get; set; }
    }
}
