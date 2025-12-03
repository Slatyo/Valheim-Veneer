using System;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Base
{
    /// <summary>
    /// Base class for all Veneer UI elements.
    /// Provides common functionality for visibility, anchoring, layer management, and lifecycle.
    /// </summary>
    public abstract class VeneerElement : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier for this element (used for layout persistence).
        /// </summary>
        public string ElementId { get; set; }

        /// <summary>
        /// The RectTransform of this element.
        /// </summary>
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                    if (_rectTransform == null)
                    {
                        _rectTransform = gameObject.AddComponent<RectTransform>();
                    }
                }
                return _rectTransform;
            }
        }
        private RectTransform _rectTransform;

        /// <summary>
        /// Whether this element can be repositioned by the user.
        /// </summary>
        public bool IsMoveable { get; set; } = false;

        /// <summary>
        /// Whether this element's position should be saved.
        /// </summary>
        public bool SavePosition { get; set; } = false;

        /// <summary>
        /// The layer type for this element (determines z-ordering).
        /// </summary>
        public VeneerLayerType LayerType
        {
            get => _layerType;
            set
            {
                if (_layerType != value)
                {
                    _layerType = value;
                    ApplyLayer();
                }
            }
        }
        private VeneerLayerType _layerType = VeneerLayerType.None;

        /// <summary>
        /// Additional sorting order offset within the layer (for fine-tuning).
        /// </summary>
        public int SortingOrderOffset { get; set; } = 0;

        /// <summary>
        /// Whether this element should auto-register with VeneerWindowManager.
        /// </summary>
        protected bool AutoRegisterWithManager { get; set; } = false;

        /// <summary>
        /// Called when this element is shown.
        /// </summary>
        public event Action OnShow;

        /// <summary>
        /// Called when this element is hidden.
        /// </summary>
        public event Action OnHide;

        /// <summary>
        /// Called when this element is destroyed.
        /// </summary>
        public event Action OnDestroyed;

        private CanvasGroup _canvasGroup;
        private Canvas _layerCanvas;
        private bool _isVisible = true;
        private bool _isRegistered = false;

        /// <summary>
        /// Whether the element is currently visible.
        /// Returns the actual GameObject active state for accuracy.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                // Return actual GameObject state to avoid desync
                if (gameObject != null)
                {
                    return gameObject.activeSelf;
                }
                return _isVisible;
            }
            set
            {
                // Sync _isVisible with actual gameObject state first
                // This handles cases where SetActive was called directly
                if (gameObject != null)
                {
                    _isVisible = gameObject.activeSelf;
                }

                if (_isVisible != value)
                {
                    _isVisible = value;
                    ApplyVisibility();
                }
            }
        }

        /// <summary>
        /// Alpha transparency (0-1).
        /// </summary>
        public float Alpha
        {
            get => _canvasGroup != null ? _canvasGroup.alpha : 1f;
            set
            {
                EnsureCanvasGroup();
                _canvasGroup.alpha = value;
            }
        }

        /// <summary>
        /// Whether this element blocks raycasts (mouse input).
        /// </summary>
        public bool BlocksRaycasts
        {
            get => _canvasGroup != null && _canvasGroup.blocksRaycasts;
            set
            {
                EnsureCanvasGroup();
                _canvasGroup.blocksRaycasts = value;
            }
        }

        /// <summary>
        /// Whether this element is interactable.
        /// </summary>
        public bool Interactable
        {
            get => _canvasGroup != null && _canvasGroup.interactable;
            set
            {
                EnsureCanvasGroup();
                _canvasGroup.interactable = value;
            }
        }

        protected virtual void Awake()
        {
            // RectTransform is lazily initialized via property getter
            _ = RectTransform;
        }

        protected virtual void Start()
        {
            // Register with window manager if configured and not already registered
            // Note: If the element starts inactive, Start() won't be called
            // In that case, use RegisterWithManager() manually in Initialize()
            if (AutoRegisterWithManager && !_isRegistered)
            {
                VeneerWindowManager.RegisterWindow(this);
                _isRegistered = true;
            }

            // Apply layer if set
            if (_layerType != VeneerLayerType.None)
            {
                ApplyLayer();
            }
        }

        protected virtual void OnDestroy()
        {
            // Unregister from window manager
            if (_isRegistered)
            {
                VeneerWindowManager.UnregisterWindow(this);
                _isRegistered = false;
            }

            OnDestroyed?.Invoke();
        }

        /// <summary>
        /// Shows this element.
        /// </summary>
        public virtual void Show()
        {
            IsVisible = true;
            OnShow?.Invoke();
        }

        /// <summary>
        /// Hides this element.
        /// </summary>
        public virtual void Hide()
        {
            IsVisible = false;
            OnHide?.Invoke();
        }

        /// <summary>
        /// Toggles visibility.
        /// </summary>
        public void Toggle()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Sets the size of this element.
        /// </summary>
        public void SetSize(float width, float height)
        {
            RectTransform.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Sets the size of this element.
        /// </summary>
        public void SetSize(Vector2 size)
        {
            RectTransform.sizeDelta = size;
        }

        /// <summary>
        /// Sets the anchored position.
        /// </summary>
        public void SetPosition(float x, float y)
        {
            RectTransform.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// Sets the anchored position.
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            RectTransform.anchoredPosition = position;
        }

        /// <summary>
        /// Sets anchors for this element.
        /// </summary>
        public void SetAnchors(Vector2 min, Vector2 max)
        {
            RectTransform.anchorMin = min;
            RectTransform.anchorMax = max;
        }

        /// <summary>
        /// Sets pivot point.
        /// </summary>
        public void SetPivot(Vector2 pivot)
        {
            RectTransform.pivot = pivot;
        }

        /// <summary>
        /// Sets pivot point.
        /// </summary>
        public void SetPivot(float x, float y)
        {
            RectTransform.pivot = new Vector2(x, y);
        }

        /// <summary>
        /// Anchors to a specific corner/edge with optional offset.
        /// </summary>
        public void AnchorTo(AnchorPreset preset, Vector2 offset = default)
        {
            ApplyAnchorPreset(preset);
            RectTransform.anchoredPosition = offset;
        }

        /// <summary>
        /// Stretches to fill parent.
        /// </summary>
        public void StretchToFill(float padding = 0)
        {
            RectTransform.anchorMin = Vector2.zero;
            RectTransform.anchorMax = Vector2.one;
            RectTransform.offsetMin = new Vector2(padding, padding);
            RectTransform.offsetMax = new Vector2(-padding, -padding);
        }

        protected void ApplyVisibility()
        {
            gameObject.SetActive(_isVisible);
        }

        /// <summary>
        /// Applies the layer settings to this element.
        /// </summary>
        protected virtual void ApplyLayer()
        {
            if (_layerType == VeneerLayerType.None)
            {
                // Remove layer override if set to None
                if (_layerCanvas != null)
                {
                    _layerCanvas.overrideSorting = false;
                }
                return;
            }

            int baseOrder = VeneerLayers.GetLayerValue(_layerType);
            int finalOrder = baseOrder + SortingOrderOffset;

            _layerCanvas = VeneerLayers.EnsureCanvas(gameObject, finalOrder, true);
        }

        /// <summary>
        /// Sets the layer type and optional offset.
        /// </summary>
        public void SetLayer(VeneerLayerType layerType, int offset = 0)
        {
            SortingOrderOffset = offset;
            LayerType = layerType;
        }

        /// <summary>
        /// Brings this element to the front within its layer.
        /// </summary>
        public virtual void BringToFront()
        {
            if (_layerType == VeneerLayerType.None)
            {
                // No layer set - just use sibling order
                transform.SetAsLastSibling();
                return;
            }

            // For windows, use the focused layer
            if (_layerType == VeneerLayerType.Window)
            {
                int focusedOrder = VeneerLayers.WindowFocused + VeneerLayers.GetNextWindowOffset();
                if (_layerCanvas == null)
                {
                    _layerCanvas = VeneerLayers.EnsureCanvas(gameObject, focusedOrder, true);
                }
                else
                {
                    _layerCanvas.sortingOrder = focusedOrder;
                }
            }
            else if (_layerType == VeneerLayerType.Popup)
            {
                int popupOrder = VeneerLayers.Popup + VeneerLayers.GetNextPopupOffset();
                if (_layerCanvas == null)
                {
                    _layerCanvas = VeneerLayers.EnsureCanvas(gameObject, popupOrder, true);
                }
                else
                {
                    _layerCanvas.sortingOrder = popupOrder;
                }
            }
            else
            {
                // For other layers, just ensure we're at the base layer order
                ApplyLayer();
            }

            // Also set as last sibling for transform order
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Gets the current sorting order of this element.
        /// </summary>
        public int GetSortingOrder()
        {
            return VeneerLayers.GetLayer(gameObject);
        }

        /// <summary>
        /// Registers this element with VeneerWindowManager manually.
        /// </summary>
        public void RegisterWithManager()
        {
            if (!_isRegistered)
            {
                VeneerWindowManager.RegisterWindow(this);
                _isRegistered = true;
            }
        }

        protected void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void ApplyAnchorPreset(AnchorPreset preset)
        {
            switch (preset)
            {
                case AnchorPreset.TopLeft:
                    SetAnchors(new Vector2(0, 1), new Vector2(0, 1));
                    SetPivot(0, 1);
                    break;
                case AnchorPreset.TopCenter:
                    SetAnchors(new Vector2(0.5f, 1), new Vector2(0.5f, 1));
                    SetPivot(0.5f, 1);
                    break;
                case AnchorPreset.TopRight:
                    SetAnchors(new Vector2(1, 1), new Vector2(1, 1));
                    SetPivot(1, 1);
                    break;
                case AnchorPreset.MiddleLeft:
                    SetAnchors(new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                    SetPivot(0, 0.5f);
                    break;
                case AnchorPreset.MiddleCenter:
                    SetAnchors(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                    SetPivot(0.5f, 0.5f);
                    break;
                case AnchorPreset.MiddleRight:
                    SetAnchors(new Vector2(1, 0.5f), new Vector2(1, 0.5f));
                    SetPivot(1, 0.5f);
                    break;
                case AnchorPreset.BottomLeft:
                    SetAnchors(Vector2.zero, Vector2.zero);
                    SetPivot(0, 0);
                    break;
                case AnchorPreset.BottomCenter:
                    SetAnchors(new Vector2(0.5f, 0), new Vector2(0.5f, 0));
                    SetPivot(0.5f, 0);
                    break;
                case AnchorPreset.BottomRight:
                    SetAnchors(new Vector2(1, 0), new Vector2(1, 0));
                    SetPivot(1, 0);
                    break;
                case AnchorPreset.StretchTop:
                    SetAnchors(new Vector2(0, 1), new Vector2(1, 1));
                    SetPivot(0.5f, 1);
                    break;
                case AnchorPreset.StretchMiddle:
                    SetAnchors(new Vector2(0, 0.5f), new Vector2(1, 0.5f));
                    SetPivot(0.5f, 0.5f);
                    break;
                case AnchorPreset.StretchBottom:
                    SetAnchors(new Vector2(0, 0), new Vector2(1, 0));
                    SetPivot(0.5f, 0);
                    break;
                case AnchorPreset.StretchLeft:
                    SetAnchors(new Vector2(0, 0), new Vector2(0, 1));
                    SetPivot(0, 0.5f);
                    break;
                case AnchorPreset.StretchCenter:
                    SetAnchors(new Vector2(0.5f, 0), new Vector2(0.5f, 1));
                    SetPivot(0.5f, 0.5f);
                    break;
                case AnchorPreset.StretchRight:
                    SetAnchors(new Vector2(1, 0), new Vector2(1, 1));
                    SetPivot(1, 0.5f);
                    break;
                case AnchorPreset.StretchAll:
                    SetAnchors(Vector2.zero, Vector2.one);
                    SetPivot(0.5f, 0.5f);
                    break;
            }
        }

        /// <summary>
        /// Creates a new GameObject with RectTransform as a child.
        /// </summary>
        protected static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }

    /// <summary>
    /// Anchor preset positions.
    /// </summary>
    public enum AnchorPreset
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        StretchTop,
        StretchMiddle,
        StretchBottom,
        StretchLeft,
        StretchCenter,
        StretchRight,
        StretchAll
    }
}
