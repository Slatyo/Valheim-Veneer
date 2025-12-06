using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Grid
{
    /// <summary>
    /// Component that allows UI elements to be dragged and repositioned.
    /// Shows a mover overlay when in edit mode.
    /// </summary>
    public class VeneerMover : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// Whether edit mode is globally enabled.
        /// </summary>
        public static bool EditModeEnabled { get; private set; }

        /// <summary>
        /// Event fired when edit mode changes.
        /// </summary>
        public static event Action<bool> OnEditModeChanged;

        /// <summary>
        /// Event fired when a mover is added or removed. Used by Edit Mode panel to refresh.
        /// </summary>
        public static event Action OnMoversChanged;

        /// <summary>
        /// The element ID this mover is associated with.
        /// </summary>
        private string _elementId;
        public string ElementId
        {
            get => _elementId;
            set
            {
                _elementId = value;
                if (!string.IsNullOrEmpty(_elementId) && _targetRect != null)
                {
                    // Ensure element is registered with anchor system (required for position saving)
                    // If already registered, this just updates the RectTransform reference
                    if (VeneerAnchor.GetAnchorData(_elementId) == null)
                    {
                        // Not yet registered - register with current position as default
                        VeneerAnchor.Register(_elementId, ScreenAnchor.Center, _targetRect.anchoredPosition, _targetRect.sizeDelta);
                    }

                    // Apply saved position to RectTransform
                    VeneerAnchor.ApplySavedLayout(_targetRect, _elementId);
                }
            }
        }

        /// <summary>
        /// Whether to snap to grid while dragging.
        /// </summary>
        public bool SnapToGrid { get; set; } = true;

        /// <summary>
        /// Grid snap size in pixels. Use config value if available.
        /// </summary>
        public float GridSize { get; set; } = 5f;

        /// <summary>
        /// Event fired when the element is moved.
        /// </summary>
        public event Action<Vector2> OnMoved;

        private RectTransform _targetRect;
        private RectTransform _canvasRect;
        private Canvas _canvas;
        private GameObject _overlay;
        private Image _overlayImage;
        private Text _overlayLabel;
        private Outline _labelOutline;
        private bool _isDragging;
        private Vector2 _dragStartPosition;
        private Vector2 _dragStartMousePosition;
        private bool _wasHiddenBeforeEditMode;
        private bool _overlayCreated;

        // Track ALL movers including disabled ones
        private static List<VeneerMover> _allMovers = new List<VeneerMover>();
        private bool _isRegistered;

        private void Awake()
        {
            _targetRect = GetComponent<RectTransform>();

            // Find canvas and canvas rect immediately
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvas = _canvas.rootCanvas;
                _canvasRect = _canvas.GetComponent<RectTransform>();
            }

            // Register in Awake so we track ALL movers including disabled ones
            if (!_isRegistered)
            {
                _allMovers.Add(this);
                _isRegistered = true;
                OnMoversChanged?.Invoke();
            }

            // Ensure element is registered with anchor system if ElementId is already set
            // This handles cases where ElementId was set before Awake ran
            if (!string.IsNullOrEmpty(_elementId) && _targetRect != null)
            {
                if (VeneerAnchor.GetAnchorData(_elementId) == null)
                {
                    VeneerAnchor.Register(_elementId, ScreenAnchor.Center, _targetRect.anchoredPosition, _targetRect.sizeDelta);
                }
                // Apply saved position to RectTransform
                VeneerAnchor.ApplySavedLayout(_targetRect, _elementId);
            }

            // Subscribe to edit mode changes immediately in Awake
            // This ensures we get notified even if the object is disabled
            OnEditModeChanged += HandleEditModeChanged;
        }

        private void Start()
        {
            // Try again to find canvas if not found in Awake
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                if (_canvas != null)
                {
                    _canvas = _canvas.rootCanvas;
                    _canvasRect = _canvas.GetComponent<RectTransform>();
                }
            }

            // Register our RectTransform with the anchor system for resolution change handling
            if (!string.IsNullOrEmpty(ElementId) && _targetRect != null)
            {
                VeneerAnchor.RegisterRectTransform(ElementId, _targetRect);
            }

            // Only create overlay if we're active and not already created
            if (!_overlayCreated)
            {
                CreateOverlay();
            }
            SyncOverlayState();
        }

        private void OnEnable()
        {
            // Register our RectTransform with the anchor system (in case Start hasn't run yet or ElementId was set later)
            if (!string.IsNullOrEmpty(ElementId) && _targetRect != null)
            {
                VeneerAnchor.RegisterRectTransform(ElementId, _targetRect);
            }

            // Sync overlay state when enabled
            SyncOverlayState();
        }

        private void OnDisable()
        {
            // When disabled, always ensure the overlay is hidden
            if (_overlay != null)
            {
                _overlay.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from event
            OnEditModeChanged -= HandleEditModeChanged;

            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }

            // Only remove from list when destroyed
            if (_isRegistered)
            {
                _allMovers.Remove(this);
                _isRegistered = false;
                OnMoversChanged?.Invoke();
            }
        }

        /// <summary>
        /// Synchronizes the overlay state with edit mode.
        /// </summary>
        private void SyncOverlayState()
        {
            if (!_overlayCreated || _overlay == null) return;

            if (EditModeEnabled && gameObject.activeInHierarchy)
            {
                _overlay.SetActive(true);

                // Ensure overlay fills parent correctly - reset anchors and offsets
                // This fixes misalignment that can occur when parent's RectTransform changes
                var overlayRect = _overlay.GetComponent<RectTransform>();
                if (overlayRect != null)
                {
                    overlayRect.anchorMin = Vector2.zero;
                    overlayRect.anchorMax = Vector2.one;
                    overlayRect.offsetMin = Vector2.zero;
                    overlayRect.offsetMax = Vector2.zero;
                }

                _overlay.transform.SetAsLastSibling();
                EnsureOverlayOnTop();
            }
            else
            {
                _overlay.SetActive(false);
                DisableOverlayCanvas();
            }
        }

        /// <summary>
        /// Enters edit mode globally.
        /// </summary>
        public static void EnterEditMode()
        {
            if (!EditModeEnabled)
            {
                EditModeEnabled = true;

                // Activate ALL movers (including disabled ones) so they can be positioned
                foreach (var mover in _allMovers.ToArray())
                {
                    if (mover != null)
                    {
                        mover.ForceShowForEditMode();
                    }
                }

                OnEditModeChanged?.Invoke(true);
                Plugin.Log.LogInfo($"Veneer: Edit mode enabled - {_allMovers.Count} elements available for positioning");
            }
        }

        /// <summary>
        /// Exits edit mode globally.
        /// </summary>
        public static void ExitEditMode()
        {
            if (EditModeEnabled)
            {
                // First, hide ALL overlays before changing state
                foreach (var mover in _allMovers.ToArray())
                {
                    if (mover != null && mover._overlay != null)
                    {
                        mover._overlay.SetActive(false);
                        mover.DisableOverlayCanvas();
                    }
                }

                EditModeEnabled = false;

                // Restore hidden state for all movers
                foreach (var mover in _allMovers.ToArray())
                {
                    if (mover != null)
                    {
                        mover.RestoreAfterEditMode();
                    }
                }

                OnEditModeChanged?.Invoke(false);
                VeneerLayout.Save();
                Plugin.Log.LogInfo("Veneer: Edit mode disabled - layout saved");
            }
        }

        /// <summary>
        /// Forces this mover to show for edit mode positioning.
        /// </summary>
        private void ForceShowForEditMode()
        {
            _wasHiddenBeforeEditMode = !gameObject.activeInHierarchy;
            if (_wasHiddenBeforeEditMode)
            {
                gameObject.SetActive(true);
            }

            // Make sure overlay is created
            if (!_overlayCreated)
            {
                CreateOverlay();
            }

            // Show and configure the overlay
            if (_overlay != null)
            {
                _overlay.SetActive(true);

                // Ensure overlay fills parent correctly - reset anchors and offsets
                // This fixes misalignment that can occur when parent's RectTransform changes
                var overlayRect = _overlay.GetComponent<RectTransform>();
                if (overlayRect != null)
                {
                    overlayRect.anchorMin = Vector2.zero;
                    overlayRect.anchorMax = Vector2.one;
                    overlayRect.offsetMin = Vector2.zero;
                    overlayRect.offsetMax = Vector2.zero;
                }

                _overlay.transform.SetAsLastSibling();
                EnsureOverlayOnTop();
            }
        }

        /// <summary>
        /// Disables the overlay's canvas and raycaster.
        /// </summary>
        private void DisableOverlayCanvas()
        {
            if (_overlay == null) return;

            var overlayCanvas = _overlay.GetComponent<Canvas>();
            if (overlayCanvas != null)
            {
                overlayCanvas.enabled = false;
            }
            var raycaster = _overlay.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
        }

        private void EnsureOverlayOnTop()
        {
            if (_overlay == null) return;

            var overlayCanvas = _overlay.GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = _overlay.AddComponent<Canvas>();
            }
            overlayCanvas.enabled = true;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = VeneerLayers.EditModeOverlay;

            var raycaster = _overlay.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = _overlay.AddComponent<GraphicRaycaster>();
            }
            raycaster.enabled = true;
        }

        /// <summary>
        /// Restores this mover's state after edit mode.
        /// </summary>
        private void RestoreAfterEditMode()
        {
            // Overlay is already hidden in ExitEditMode, but ensure it's disabled
            if (_overlay != null)
            {
                _overlay.SetActive(false);
                DisableOverlayCanvas();
            }

            if (_wasHiddenBeforeEditMode)
            {
                gameObject.SetActive(false);
                _wasHiddenBeforeEditMode = false;
            }
        }

        /// <summary>
        /// Toggles edit mode.
        /// </summary>
        public static void ToggleEditMode()
        {
            if (EditModeEnabled)
                ExitEditMode();
            else
                EnterEditMode();
        }

        private void CreateOverlay()
        {
            if (_overlayCreated) return;

            _overlay = new GameObject("MoverOverlay", typeof(RectTransform));
            _overlay.transform.SetParent(transform, false);

            var overlayRect = _overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            _overlayImage = _overlay.AddComponent<Image>();
            _overlayImage.color = new Color(0.78f, 0.61f, 0.43f, 0.3f);
            _overlayImage.raycastTarget = true;

            var forwarder = _overlay.AddComponent<MoverDragForwarder>();
            forwarder.Target = this;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(_overlay.transform, false);

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            _overlayLabel = labelGo.AddComponent<Text>();
            _overlayLabel.text = string.IsNullOrEmpty(ElementId) ? "Moveable" : ElementId;
            _overlayLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _overlayLabel.fontSize = VeneerDimensions.FontSizeSmall;
            _overlayLabel.color = new Color(1f, 0f, 1f, 1f);
            _overlayLabel.alignment = TextAnchor.MiddleCenter;
            _overlayLabel.raycastTarget = false;

            _labelOutline = labelGo.AddComponent<Outline>();
            _labelOutline.effectColor = Color.black;
            _labelOutline.effectDistance = new Vector2(1, -1);

            _overlay.SetActive(false);
            _overlayCreated = true;
        }

        private void HandleEditModeChanged(bool enabled)
        {
            // This is called for ALL movers including inactive ones (subscribed in Awake)
            if (!enabled)
            {
                // Edit mode turned off - ensure overlay is hidden
                if (_overlay != null)
                {
                    _overlay.SetActive(false);
                    DisableOverlayCanvas();
                }
            }
            else if (gameObject.activeInHierarchy)
            {
                // Edit mode turned on and we're active - show overlay
                if (!_overlayCreated)
                {
                    CreateOverlay();
                }
                if (_overlay != null)
                {
                    _overlay.SetActive(true);

                    // Ensure overlay fills parent correctly - reset anchors and offsets
                    // This fixes misalignment that can occur when parent's RectTransform changes
                    var overlayRect = _overlay.GetComponent<RectTransform>();
                    if (overlayRect != null)
                    {
                        overlayRect.anchorMin = Vector2.zero;
                        overlayRect.anchorMax = Vector2.one;
                        overlayRect.offsetMin = Vector2.zero;
                        overlayRect.offsetMax = Vector2.zero;
                    }

                    _overlay.transform.SetAsLastSibling();
                    EnsureOverlayOnTop();
                }
            }
        }

        /// <summary>
        /// Gets all registered movers.
        /// </summary>
        public static IReadOnlyList<VeneerMover> GetAllMovers()
        {
            return _allMovers;
        }

        /// <summary>
        /// Gets all registered movers (property accessor).
        /// </summary>
        public static IReadOnlyList<VeneerMover> AllMovers => _allMovers;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!EditModeEnabled) return;

            _isDragging = true;

            // Store starting positions for reliable delta-based movement
            _dragStartPosition = _targetRect.anchoredPosition;
            _dragStartMousePosition = eventData.position;

            // Highlight during drag
            if (_overlayImage != null)
            {
                _overlayImage.color = new Color(0.78f, 0.61f, 0.43f, 0.5f);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || !EditModeEnabled) return;

            // Use simple delta-based movement - much more reliable
            // Calculate total mouse movement since drag start
            Vector2 mouseDelta = eventData.position - _dragStartMousePosition;

            // Scale delta if canvas has a different scale
            if (_canvas != null)
            {
                float scaleFactor = _canvas.scaleFactor;
                if (scaleFactor > 0)
                {
                    mouseDelta /= scaleFactor;
                }
            }

            Vector2 newPosition = _dragStartPosition + mouseDelta;

            // Snap to grid using config value
            float snapSize = VeneerConfig.GridSnapSize?.Value ?? GridSize;
            if (SnapToGrid && snapSize > 0)
            {
                newPosition.x = Mathf.Round(newPosition.x / snapSize) * snapSize;
                newPosition.y = Mathf.Round(newPosition.y / snapSize) * snapSize;
            }

            _targetRect.anchoredPosition = newPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;

            // Restore overlay color
            if (_overlayImage != null)
            {
                _overlayImage.color = new Color(0.78f, 0.61f, 0.43f, 0.3f);
            }

            // Save position without changing anchors - just save current anchored position
            // This prevents the "snapping to center" issue
            if (!string.IsNullOrEmpty(ElementId))
            {
                // Get current anchor from the existing data, or use Center as default
                var existingData = VeneerAnchor.GetAnchorData(ElementId);
                var currentAnchor = existingData?.Anchor ?? ScreenAnchor.Center;

                // Save position with current anchor, offset, and size
                VeneerAnchor.UpdatePosition(ElementId, currentAnchor, _targetRect.anchoredPosition, _targetRect.sizeDelta);
                Plugin.Log.LogDebug($"VeneerMover: Saved position for {ElementId}: offset={_targetRect.anchoredPosition}, size={_targetRect.sizeDelta}");
            }

            OnMoved?.Invoke(_targetRect.anchoredPosition);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!EditModeEnabled) return;

            if (_overlayImage != null)
            {
                _overlayImage.color = new Color(0.78f, 0.61f, 0.43f, 0.4f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!EditModeEnabled || _isDragging) return;

            if (_overlayImage != null)
            {
                _overlayImage.color = new Color(0.78f, 0.61f, 0.43f, 0.3f);
            }
        }

        /// <summary>
        /// Sets the display label for this mover.
        /// </summary>
        public void SetLabel(string label)
        {
            if (_overlayLabel != null)
            {
                _overlayLabel.text = label;
            }
        }
    }

    /// <summary>
    /// Helper component that forwards drag events from the overlay to the VeneerMover.
    /// </summary>
    public class MoverDragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public VeneerMover Target { get; set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Target?.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Target?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Target?.OnEndDrag(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Target?.OnPointerEnter(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Target?.OnPointerExit(eventData);
        }
    }
}
