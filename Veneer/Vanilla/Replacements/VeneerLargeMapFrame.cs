using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Large map frame with Veneer styling.
    /// Wraps the vanilla large map in a moveable/resizable Veneer window.
    /// NOTE: This class does NOT implement drag handlers - they are on the header only,
    /// so drag events on the map area pass through to vanilla for pan functionality.
    /// </summary>
    public class VeneerLargeMapFrame : VeneerElement
    {
        private const string ElementIdLargeMap = "Veneer_LargeMap";

        private Image _backgroundImage;
        private Image _borderImage;
        private RectTransform _headerRect;
        private VeneerText _titleText;
        private VeneerButton _closeButton;
        private MapFrameHeaderDragger _headerDragger;
        private RectTransform _mapContainerRect;

        // Vanilla map reference
        private RectTransform _vanillaLargeMapRect;
        private Transform _originalParent;
        private Vector2 _originalAnchorMin;
        private Vector2 _originalAnchorMax;
        private Vector2 _originalPivot;
        private Vector2 _originalAnchoredPosition;
        private Vector2 _originalSizeDelta;

        private bool _isWrapped;

        /// <summary>
        /// Creates the large map frame.
        /// </summary>
        public static VeneerLargeMapFrame Create(Transform parent)
        {
            var go = CreateUIObject("VeneerLargeMapFrame", parent);
            var frame = go.AddComponent<VeneerLargeMapFrame>();
            frame.Initialize();
            return frame;
        }

        private void Initialize()
        {
            ElementId = ElementIdLargeMap;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.Popup;
            AutoRegisterWithManager = true;

            VeneerAnchor.Register(ElementId, ScreenAnchor.Center, Vector2.zero);

            // Default size - can be resized
            float width = 900f;
            float height = 850f;
            float padding = 4f;
            float headerHeight = 32f;

            SetSize(width, height);
            AnchorTo(AnchorPreset.MiddleCenter);

            // Background - set raycastTarget to false so clicks pass through to vanilla map
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.BackgroundSolid;
            _backgroundImage.raycastTarget = false; // Let clicks through to vanilla map for pan/zoom

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

            // Header (draggable) - has its own drag handler so map area drags pass through
            var headerGo = CreateUIObject("Header", transform);
            _headerRect = headerGo.GetComponent<RectTransform>();
            _headerRect.anchorMin = new Vector2(0, 1);
            _headerRect.anchorMax = new Vector2(1, 1);
            _headerRect.pivot = new Vector2(0.5f, 1);
            _headerRect.anchoredPosition = Vector2.zero;
            _headerRect.sizeDelta = new Vector2(0, headerHeight);

            var headerBg = headerGo.AddComponent<Image>();
            headerBg.color = VeneerColors.BackgroundDark;
            headerBg.raycastTarget = true; // Header catches clicks/drags

            // Add drag handler to header only - this way map area drags go to vanilla
            _headerDragger = headerGo.AddComponent<MapFrameHeaderDragger>();
            _headerDragger.Initialize(this);

            // Title
            var titleGo = CreateUIObject("Title", headerGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(padding + 8, 0);
            titleRect.offsetMax = new Vector2(-40, 0);

            _titleText = titleGo.AddComponent<VeneerText>();
            _titleText.Content = "World Map";
            _titleText.ApplyStyle(TextStyle.Header);
            _titleText.Alignment = TextAnchor.MiddleLeft;

            // Close button
            _closeButton = VeneerButton.Create(headerGo.transform, "X", OnCloseClicked);
            _closeButton.SetButtonSize(ButtonSize.Small);
            _closeButton.SetStyle(ButtonStyle.Ghost);
            var closeRect = _closeButton.RectTransform;
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-6, 0);
            closeRect.sizeDelta = new Vector2(26, 24);

            // Map container (where vanilla map will be placed)
            var containerGo = CreateUIObject("MapContainer", transform);
            _mapContainerRect = containerGo.GetComponent<RectTransform>();
            _mapContainerRect.anchorMin = Vector2.zero;
            _mapContainerRect.anchorMax = Vector2.one;
            _mapContainerRect.offsetMin = new Vector2(padding, padding);
            _mapContainerRect.offsetMax = new Vector2(-padding, -headerHeight);

            // Add mover for edit mode
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(500, 450);
            resizer.MaxSize = new Vector2(1600, 1200);

            // Start hidden - must register BEFORE SetActive(false) since Start() won't be called
            RegisterWithManager();
            gameObject.SetActive(false);

            // Apply saved position/size
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
                if (savedData.Size != Vector2.zero)
                {
                    SetSize(savedData.Size.x, savedData.Size.y);
                }
            }

            Plugin.Log.LogDebug("VeneerLargeMapFrame: Initialized");
        }

        private void OnCloseClicked()
        {
            // Close via vanilla method
            if (Minimap.instance != null)
            {
                Minimap.instance.SetMapMode(Minimap.MapMode.Small);
            }
        }

        /// <summary>
        /// Called by header dragger when drag ends to save position.
        /// </summary>
        public void SaveCurrentPosition()
        {
            VeneerAnchor.UpdatePosition(ElementId, ScreenAnchor.Center, RectTransform.anchoredPosition, RectTransform.sizeDelta);
        }

        /// <summary>
        /// Wraps the vanilla large map into this frame.
        /// </summary>
        public void WrapVanillaMap()
        {
            if (_isWrapped) return;
            if (Minimap.instance == null) return;

            var largeRoot = Minimap.instance.m_largeRoot;
            if (largeRoot == null) return;

            _vanillaLargeMapRect = largeRoot.GetComponent<RectTransform>();
            if (_vanillaLargeMapRect == null) return;

            // Store original state
            _originalParent = _vanillaLargeMapRect.parent;
            _originalAnchorMin = _vanillaLargeMapRect.anchorMin;
            _originalAnchorMax = _vanillaLargeMapRect.anchorMax;
            _originalPivot = _vanillaLargeMapRect.pivot;
            _originalAnchoredPosition = _vanillaLargeMapRect.anchoredPosition;
            _originalSizeDelta = _vanillaLargeMapRect.sizeDelta;

            // Find our container
            var container = transform.Find("MapContainer");
            if (container == null)
            {
                Plugin.Log.LogError("VeneerLargeMapFrame: MapContainer not found");
                return;
            }

            // Reparent vanilla map to our container
            _vanillaLargeMapRect.SetParent(container, false);

            // Fill the container
            _vanillaLargeMapRect.anchorMin = Vector2.zero;
            _vanillaLargeMapRect.anchorMax = Vector2.one;
            _vanillaLargeMapRect.pivot = new Vector2(0.5f, 0.5f);
            _vanillaLargeMapRect.anchoredPosition = Vector2.zero;
            _vanillaLargeMapRect.offsetMin = Vector2.zero;
            _vanillaLargeMapRect.offsetMax = Vector2.zero;
            _vanillaLargeMapRect.localScale = Vector3.one;

            _isWrapped = true;
            Plugin.Log.LogInfo("VeneerLargeMapFrame: Vanilla large map wrapped");
        }

        /// <summary>
        /// Unwraps the vanilla large map back to its original parent.
        /// </summary>
        public void UnwrapVanillaMap()
        {
            if (!_isWrapped) return;
            if (_vanillaLargeMapRect == null) return;

            try
            {
                if (_originalParent != null && _originalParent.gameObject != null)
                {
                    _vanillaLargeMapRect.SetParent(_originalParent, false);
                    _vanillaLargeMapRect.anchorMin = _originalAnchorMin;
                    _vanillaLargeMapRect.anchorMax = _originalAnchorMax;
                    _vanillaLargeMapRect.pivot = _originalPivot;
                    _vanillaLargeMapRect.anchoredPosition = _originalAnchoredPosition;
                    _vanillaLargeMapRect.sizeDelta = _originalSizeDelta;
                }
            }
            catch (System.Exception)
            {
                // Ignore errors during scene destruction
            }

            _isWrapped = false;
            Plugin.Log.LogDebug("VeneerLargeMapFrame: Vanilla large map unwrapped");
        }

        public override void Show()
        {
            WrapVanillaMap();

            // Ensure the vanilla map content is also active
            if (_vanillaLargeMapRect != null)
            {
                _vanillaLargeMapRect.gameObject.SetActive(true);
            }

            base.Show(); // Fire OnShow event and set visibility
        }

        public override void Hide()
        {
            base.Hide(); // Fire OnHide event and set visibility
            UnwrapVanillaMap();
        }

        public bool IsShowing => gameObject.activeSelf;

        protected override void OnDestroy()
        {
            UnwrapVanillaMap();
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Separate drag handler for the map frame header.
    /// By having this on the header GameObject only, drags on the map area
    /// pass through to the vanilla map for panning.
    /// </summary>
    public class MapFrameHeaderDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private VeneerLargeMapFrame _frame;
        private RectTransform _frameRect;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Vector2 _dragOffset;
        private bool _isDragging;

        public void Initialize(VeneerLargeMapFrame frame)
        {
            _frame = frame;
            _frameRect = frame.RectTransform;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;

            // Cache canvas
            if (_canvas == null || _canvasRect == null)
            {
                _canvas = _frame.GetComponentInParent<Canvas>();
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

                _dragOffset = _frameRect.anchoredPosition - localPoint;
            }

            // Bring frame to front
            _frame.BringToFront();
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

            _frameRect.anchoredPosition = localPoint + _dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Save position
            _frame.SaveCurrentPosition();
        }
    }

}
