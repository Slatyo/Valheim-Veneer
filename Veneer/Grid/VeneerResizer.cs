using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Grid
{
    /// <summary>
    /// Component that allows UI elements to be resized via corner drag handles.
    /// Features proportional scaling, grid snapping, and size display during resize.
    /// </summary>
    public class VeneerResizer : MonoBehaviour
    {
        /// <summary>
        /// Whether to maintain aspect ratio when resizing.
        /// </summary>
        public bool MaintainAspectRatio { get; set; } = true;

        /// <summary>
        /// Whether to snap to grid while resizing.
        /// </summary>
        public bool SnapToGrid { get; set; } = true;

        /// <summary>
        /// Grid snap size in pixels.
        /// </summary>
        public float GridSize { get; set; } = VeneerDimensions.GridSnapSize;

        /// <summary>
        /// Minimum size for the element.
        /// </summary>
        public Vector2 MinSize { get; set; } = new Vector2(100, 100);

        /// <summary>
        /// Maximum size for the element.
        /// </summary>
        public Vector2 MaxSize { get; set; } = new Vector2(800, 800);

        /// <summary>
        /// Size of the resize handles in pixels.
        /// </summary>
        public float HandleSize { get; set; } = 20f;

        /// <summary>
        /// Event fired when the element is resized.
        /// </summary>
        public event Action<Vector2> OnResized;

        private RectTransform _targetRect;
        private GameObject _handleBottomRight;
        private GameObject _handleBottomLeft;
        private GameObject _handleTopRight;
        private GameObject _handleTopLeft;

        // Size indicator
        private GameObject _sizeIndicator;
        private Text _sizeText;
        private float _initialAspectRatio;
        private Vector2 _initialSize;

        private void Awake()
        {
            _targetRect = GetComponent<RectTransform>();
        }

        private void Start()
        {
            CreateHandles();
            CreateSizeIndicator();
            UpdateHandleVisibility();
        }

        private void OnEnable()
        {
            VeneerMover.OnEditModeChanged += HandleEditModeChanged;
            UpdateHandleVisibility();
        }

        private void OnDisable()
        {
            VeneerMover.OnEditModeChanged -= HandleEditModeChanged;
        }

        private void OnDestroy()
        {
            DestroyHandles();
            if (_sizeIndicator != null) Destroy(_sizeIndicator);
        }

        private void HandleEditModeChanged(bool enabled)
        {
            UpdateHandleVisibility();
        }

        private void UpdateHandleVisibility()
        {
            bool show = VeneerMover.EditModeEnabled;

            // Update handles
            UpdateHandleActive(_handleBottomRight, show);
            UpdateHandleActive(_handleBottomLeft, show);
            UpdateHandleActive(_handleTopRight, show);
            UpdateHandleActive(_handleTopLeft, show);

            // Hide size indicator when not resizing
            if (_sizeIndicator != null)
            {
                _sizeIndicator.SetActive(false);
            }
        }

        private void UpdateHandleActive(GameObject handle, bool show)
        {
            if (handle != null)
            {
                handle.SetActive(show);
                if (show)
                {
                    handle.transform.SetAsLastSibling();
                    // Ensure canvas is on top for handles too
                    EnsureHandleOnTop(handle);
                }
            }
        }

        private void EnsureHandleOnTop(GameObject handle)
        {
            var canvas = handle.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = handle.AddComponent<Canvas>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = VeneerLayers.EditModeHandles;

            var raycaster = handle.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                handle.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreateHandles()
        {
            // Bottom-right corner (most common)
            _handleBottomRight = CreateHandle("HandleBR", new Vector2(1, 0), new Vector2(1, 0), ResizeCorner.BottomRight);

            // Bottom-left corner
            _handleBottomLeft = CreateHandle("HandleBL", new Vector2(0, 0), new Vector2(0, 0), ResizeCorner.BottomLeft);

            // Top-right corner
            _handleTopRight = CreateHandle("HandleTR", new Vector2(1, 1), new Vector2(1, 1), ResizeCorner.TopRight);

            // Top-left corner
            _handleTopLeft = CreateHandle("HandleTL", new Vector2(0, 1), new Vector2(0, 1), ResizeCorner.TopLeft);
        }

        private void CreateSizeIndicator()
        {
            _sizeIndicator = new GameObject("SizeIndicator", typeof(RectTransform));
            _sizeIndicator.transform.SetParent(transform, false);

            var rect = _sizeIndicator.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120, 40);

            // Background
            var bg = _sizeIndicator.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);
            bg.raycastTarget = false;

            // Border
            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(_sizeIndicator.transform, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(4, VeneerColors.Accent, Color.clear, 1);
            borderImg.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;

            // Text
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(_sizeIndicator.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 4);
            textRect.offsetMax = new Vector2(-4, -4);

            _sizeText = textGo.AddComponent<Text>();
            _sizeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _sizeText.fontSize = VeneerConfig.GetScaledFontSize(12);
            _sizeText.color = VeneerColors.Text;
            _sizeText.alignment = TextAnchor.MiddleCenter;
            _sizeText.raycastTarget = false;

            _sizeIndicator.SetActive(false);
        }

        private void DestroyHandles()
        {
            if (_handleBottomRight != null) Destroy(_handleBottomRight);
            if (_handleBottomLeft != null) Destroy(_handleBottomLeft);
            if (_handleTopRight != null) Destroy(_handleTopRight);
            if (_handleTopLeft != null) Destroy(_handleTopLeft);
        }

        private GameObject CreateHandle(string name, Vector2 anchor, Vector2 pivot, ResizeCorner corner)
        {
            var handle = new GameObject(name, typeof(RectTransform));
            handle.transform.SetParent(transform, false);

            var rect = handle.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(HandleSize, HandleSize);
            rect.anchoredPosition = Vector2.zero;

            // Visual indicator - simple gold square, clean look
            var image = handle.AddComponent<Image>();
            image.color = new Color(0.78f, 0.61f, 0.43f, 0.7f);
            image.raycastTarget = true;

            // Add a small border for visibility
            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(handle.transform, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            var borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(4, new Color(0.1f, 0.1f, 0.1f, 0.9f), Color.clear, 1);
            borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;

            // Add resize handler
            var handler = handle.AddComponent<ResizeHandle>();
            handler.Initialize(this, corner);

            handle.SetActive(false);
            return handle;
        }

        internal void OnResizeStart()
        {
            _initialSize = _targetRect.sizeDelta;
            _initialAspectRatio = _initialSize.x / _initialSize.y;

            // Show size indicator
            if (_sizeIndicator != null)
            {
                _sizeIndicator.SetActive(true);
                _sizeIndicator.transform.SetAsLastSibling();
                UpdateSizeIndicator();
            }
        }

        internal void ApplyResize(ResizeCorner corner, Vector2 delta)
        {
            if (_targetRect == null) return;

            Vector2 currentSize = _targetRect.sizeDelta;
            Vector2 newSize = currentSize;
            Vector2 positionDelta = Vector2.zero;

            switch (corner)
            {
                case ResizeCorner.BottomRight:
                    newSize.x += delta.x;
                    newSize.y -= delta.y;
                    break;

                case ResizeCorner.BottomLeft:
                    newSize.x -= delta.x;
                    newSize.y -= delta.y;
                    positionDelta.x = delta.x;
                    break;

                case ResizeCorner.TopRight:
                    newSize.x += delta.x;
                    newSize.y += delta.y;
                    break;

                case ResizeCorner.TopLeft:
                    newSize.x -= delta.x;
                    newSize.y += delta.y;
                    positionDelta.x = delta.x;
                    break;
            }

            // Snap to grid
            if (SnapToGrid && GridSize > 0)
            {
                newSize.x = Mathf.Round(newSize.x / GridSize) * GridSize;
                newSize.y = Mathf.Round(newSize.y / GridSize) * GridSize;
            }

            // Maintain aspect ratio if enabled (use initial aspect ratio)
            if (MaintainAspectRatio && _initialAspectRatio > 0)
            {
                // Determine which dimension changed more from initial
                float widthChange = Mathf.Abs(newSize.x - _initialSize.x);
                float heightChange = Mathf.Abs(newSize.y - _initialSize.y);

                if (widthChange > heightChange)
                {
                    // Width changed more - calculate height from width
                    newSize.y = newSize.x / _initialAspectRatio;
                }
                else
                {
                    // Height changed more - calculate width from height
                    newSize.x = newSize.y * _initialAspectRatio;
                }

                // Re-snap after aspect ratio adjustment
                if (SnapToGrid && GridSize > 0)
                {
                    newSize.x = Mathf.Round(newSize.x / GridSize) * GridSize;
                    newSize.y = Mathf.Round(newSize.y / GridSize) * GridSize;
                }
            }

            // Clamp to min/max
            newSize.x = Mathf.Clamp(newSize.x, MinSize.x, MaxSize.x);
            newSize.y = Mathf.Clamp(newSize.y, MinSize.y, MaxSize.y);

            // Apply size
            _targetRect.sizeDelta = newSize;

            // Apply position adjustment for left-side handles
            if (corner == ResizeCorner.BottomLeft || corner == ResizeCorner.TopLeft)
            {
                float actualDeltaX = newSize.x - currentSize.x;
                _targetRect.anchoredPosition += new Vector2(-actualDeltaX * (1 - _targetRect.pivot.x), 0);
            }

            // Force layout rebuild so children with percentage anchors update
            RebuildLayout();

            // Update size indicator
            UpdateSizeIndicator();

            OnResized?.Invoke(newSize);
        }

        private void UpdateSizeIndicator()
        {
            if (_sizeText == null || _targetRect == null) return;

            Vector2 size = _targetRect.sizeDelta;

            // Calculate percentage change from initial
            float widthPercent = (_initialSize.x > 0) ? (size.x / _initialSize.x * 100f) : 100f;
            float heightPercent = (_initialSize.y > 0) ? (size.y / _initialSize.y * 100f) : 100f;

            _sizeText.text = $"{size.x:F0} x {size.y:F0}\n{widthPercent:F0}%";
        }

        /// <summary>
        /// Forces a layout rebuild on this element and all children.
        /// This ensures child elements with percentage-based anchors update properly.
        /// </summary>
        private void RebuildLayout()
        {
            if (_targetRect == null) return;

            // Mark layout as dirty
            LayoutRebuilder.MarkLayoutForRebuild(_targetRect);

            // Force immediate rebuild of all child layout groups
            LayoutRebuilder.ForceRebuildLayoutImmediate(_targetRect);

            // Also rebuild any nested scroll rects' content
            var scrollRects = _targetRect.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scrollRect in scrollRects)
            {
                if (scrollRect.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                }
            }

            // Force canvas to update
            Canvas.ForceUpdateCanvases();
        }

        internal void OnResizeComplete()
        {
            // Hide size indicator
            if (_sizeIndicator != null)
            {
                _sizeIndicator.SetActive(false);
            }

            // Final layout rebuild to ensure everything is settled
            RebuildLayout();

            // Save the new size if element has an ID
            var mover = GetComponent<VeneerMover>();
            if (mover != null && !string.IsNullOrEmpty(mover.ElementId))
            {
                // Update the size in the anchor system
                VeneerAnchor.UpdateSize(mover.ElementId, _targetRect.sizeDelta);
                Plugin.Log.LogDebug($"VeneerResizer: Saved size for {mover.ElementId}: {_targetRect.sizeDelta}");
            }
        }
    }

    /// <summary>
    /// Which corner the resize handle is on.
    /// </summary>
    public enum ResizeCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// Individual resize handle that captures drag events.
    /// </summary>
    public class ResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private VeneerResizer _resizer;
        private ResizeCorner _corner;
        private Image _image;
        private Color _normalColor;
        private Color _hoverColor;
        private Color _dragColor;

        public void Initialize(VeneerResizer resizer, ResizeCorner corner)
        {
            _resizer = resizer;
            _corner = corner;
            _image = GetComponent<Image>();

            _normalColor = new Color(0.78f, 0.61f, 0.43f, 0.6f);
            _hoverColor = new Color(0.78f, 0.61f, 0.43f, 0.85f);
            _dragColor = new Color(0.78f, 0.61f, 0.43f, 1f);

            if (_image != null)
            {
                _image.color = _normalColor;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!VeneerMover.EditModeEnabled) return;

            if (_image != null)
            {
                _image.color = _dragColor;
            }

            _resizer?.OnResizeStart();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!VeneerMover.EditModeEnabled) return;

            _resizer?.ApplyResize(_corner, eventData.delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_image != null)
            {
                _image.color = _normalColor;
            }

            _resizer?.OnResizeComplete();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!VeneerMover.EditModeEnabled) return;

            if (_image != null)
            {
                _image.color = _hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!VeneerMover.EditModeEnabled) return;

            if (_image != null)
            {
                _image.color = _normalColor;
            }
        }
    }
}
