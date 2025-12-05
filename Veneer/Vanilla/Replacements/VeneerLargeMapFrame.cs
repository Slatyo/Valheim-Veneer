using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Large map frame with Veneer styling.
    /// Wraps the vanilla large map in a moveable/resizable Veneer window.
    /// Uses VeneerFrame for consistent header/dragging/close button.
    /// NOTE: The map container area allows pointer events through to vanilla for pan/zoom.
    /// </summary>
    public class VeneerLargeMapFrame : VeneerElement
    {
        private const string ElementIdLargeMap = "Veneer_LargeMap";

        private VeneerFrame _frame;
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

            // Default size - can be resized
            float width = 900f;
            float height = 850f;
            float padding = 4f;

            SetSize(width, height);
            AnchorTo(AnchorPreset.MiddleCenter);

            // Create VeneerFrame with header, close button, and dragging
            // Note: No Id on child frame - the wrapper (this panel) handles positioning via VeneerMover
            _frame = VeneerFrame.Create(transform, new FrameConfig
            {
                // Id intentionally not set - wrapper handles edit mode positioning
                Name = "LargeMapFrameInner",
                Title = "World Map",
                Width = width,
                Height = height,
                HasHeader = true,
                HasCloseButton = true,
                IsDraggable = true,
                SavePosition = false,
                Anchor = AnchorPreset.MiddleCenter
            });

            // Add VeneerMover to THIS wrapper panel (not the child frame)
            // VeneerMover automatically registers with VeneerAnchor for position persistence
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Fill parent
            _frame.RectTransform.anchorMin = Vector2.zero;
            _frame.RectTransform.anchorMax = Vector2.one;
            _frame.RectTransform.offsetMin = Vector2.zero;
            _frame.RectTransform.offsetMax = Vector2.zero;

            // Connect close event to vanilla map close
            _frame.OnCloseClicked += OnCloseClicked;

            // Override background to not block clicks for map pan/zoom
            // VeneerFrame's background is on the frame itself, set it to not receive raycasts
            var frameImage = _frame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.raycastTarget = false;
            }

            // Map container (where vanilla map will be placed) - in frame's content
            var containerGo = CreateUIObject("MapContainer", _frame.Content);
            _mapContainerRect = containerGo.GetComponent<RectTransform>();
            _mapContainerRect.anchorMin = Vector2.zero;
            _mapContainerRect.anchorMax = Vector2.one;
            _mapContainerRect.offsetMin = new Vector2(padding, padding);
            _mapContainerRect.offsetMax = new Vector2(-padding, -padding);

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(500, 450);
            resizer.MaxSize = new Vector2(1600, 1200);

            // Start hidden - must register BEFORE SetActive(false) since Start() won't be called
            RegisterWithManager();
            gameObject.SetActive(false);

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

            // Reparent vanilla map to our container
            _vanillaLargeMapRect.SetParent(_mapContainerRect, false);

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
}
