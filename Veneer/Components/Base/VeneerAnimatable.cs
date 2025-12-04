using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Veneer.Components.Base
{
    /// <summary>
    /// Provides reusable animation capabilities for Veneer components.
    /// Attach to any VeneerElement to gain hover lift, press scale, and disabled opacity effects.
    /// Note: Hover lift is automatically disabled when inside a LayoutGroup.
    /// </summary>
    public class VeneerAnimatable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private bool _isInLayoutGroup;

        // Animation state
        private bool _isHovered;
        private bool _isPressed;
        private bool _isDisabled;

        // Current animated values
        private Vector2 _currentOffset;
        private float _currentScale = 1f;
        private float _currentAlpha = 1f;

        // Target values
        private Vector2 _targetOffset;
        private float _targetScale = 1f;
        private float _targetAlpha = 1f;

        // Original position for offset animations
        private Vector2 _originalPosition;
        private bool _hasOriginalPosition;

        /// <summary>
        /// Configuration for hover lift effect (Y offset in pixels).
        /// </summary>
        public float HoverLift { get; set; } = 4f;

        /// <summary>
        /// Configuration for press scale effect (1.0 = no scale, 0.95 = 5% smaller).
        /// </summary>
        public float PressScale { get; set; } = 0.97f;

        /// <summary>
        /// Configuration for disabled opacity (0-1).
        /// </summary>
        public float DisabledAlpha { get; set; } = 0.45f;

        /// <summary>
        /// Animation speed multiplier.
        /// </summary>
        public float AnimationSpeed { get; set; } = 12f;

        /// <summary>
        /// Whether hover lift animation is enabled.
        /// </summary>
        public bool EnableHoverLift { get; set; } = true;

        /// <summary>
        /// Whether press scale animation is enabled.
        /// </summary>
        public bool EnablePressScale { get; set; } = true;

        /// <summary>
        /// Whether disabled opacity is enabled.
        /// </summary>
        public bool EnableDisabledAlpha { get; set; } = true;

        /// <summary>
        /// Current hover state.
        /// </summary>
        public bool IsHovered => _isHovered;

        /// <summary>
        /// Current pressed state.
        /// </summary>
        public bool IsPressed => _isPressed;

        /// <summary>
        /// Current disabled state.
        /// </summary>
        public bool IsDisabled
        {
            get => _isDisabled;
            set
            {
                _isDisabled = value;
                UpdateTargets();
            }
        }

        protected virtual void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            // Ensure CanvasGroup exists for alpha animations
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Check if we're inside a layout group - if so, disable position animations
            // because the layout group controls our position
            _isInLayoutGroup = transform.parent != null &&
                               transform.parent.GetComponent<LayoutGroup>() != null;
        }

        protected virtual void Start()
        {
            // Store original position on first frame
            if (_rectTransform != null && !_hasOriginalPosition)
            {
                _originalPosition = _rectTransform.anchoredPosition;
                _hasOriginalPosition = true;
            }
        }

        protected virtual void Update()
        {
            float deltaSpeed = AnimationSpeed * Time.unscaledDeltaTime;

            // Animate offset (skip if in layout group - layout controls position)
            if (EnableHoverLift && _rectTransform != null && !_isInLayoutGroup)
            {
                _currentOffset = Vector2.Lerp(_currentOffset, _targetOffset, deltaSpeed);
                _rectTransform.anchoredPosition = _originalPosition + _currentOffset;
            }

            // Animate scale
            if (EnablePressScale && _rectTransform != null)
            {
                _currentScale = Mathf.Lerp(_currentScale, _targetScale, deltaSpeed);
                _rectTransform.localScale = Vector3.one * _currentScale;
            }

            // Animate alpha
            if (EnableDisabledAlpha && _canvasGroup != null)
            {
                _currentAlpha = Mathf.Lerp(_currentAlpha, _targetAlpha, deltaSpeed);
                _canvasGroup.alpha = _currentAlpha;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateTargets();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false; // Also release press when leaving
            UpdateTargets();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            UpdateTargets();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            UpdateTargets();
        }

        private void UpdateTargets()
        {
            // Disabled state overrides everything
            if (_isDisabled)
            {
                _targetOffset = Vector2.zero;
                _targetScale = 1f;
                _targetAlpha = DisabledAlpha;
                return;
            }

            // Normal state
            _targetAlpha = 1f;

            // Hover lift (only when not pressed)
            if (_isHovered && !_isPressed)
            {
                _targetOffset = new Vector2(0, HoverLift);
            }
            else
            {
                _targetOffset = Vector2.zero;
            }

            // Press scale
            if (_isPressed)
            {
                _targetScale = PressScale;
            }
            else
            {
                _targetScale = 1f;
            }
        }

        /// <summary>
        /// Resets the original position (call after moving the element programmatically).
        /// </summary>
        public void ResetOriginalPosition()
        {
            if (_rectTransform != null)
            {
                _originalPosition = _rectTransform.anchoredPosition - _currentOffset;
            }
        }

        /// <summary>
        /// Immediately snaps to target values without animation.
        /// </summary>
        public void SnapToTarget()
        {
            _currentOffset = _targetOffset;
            _currentScale = _targetScale;
            _currentAlpha = _targetAlpha;

            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _originalPosition + _currentOffset;
                _rectTransform.localScale = Vector3.one * _currentScale;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _currentAlpha;
            }
        }

        /// <summary>
        /// Creates and configures a VeneerAnimatable on the given GameObject.
        /// </summary>
        public static VeneerAnimatable Setup(GameObject target, bool hoverLift = true, bool pressScale = true, float liftAmount = 4f)
        {
            var animatable = target.GetComponent<VeneerAnimatable>();
            if (animatable == null)
            {
                animatable = target.AddComponent<VeneerAnimatable>();
            }

            animatable.EnableHoverLift = hoverLift;
            animatable.EnablePressScale = pressScale;
            animatable.HoverLift = liftAmount;

            return animatable;
        }
    }
}
