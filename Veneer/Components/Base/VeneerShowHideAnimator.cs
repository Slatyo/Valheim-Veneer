using System;
using System.Collections;
using UnityEngine;
using Veneer.Theme;

namespace Veneer.Components.Base
{
    /// <summary>
    /// Handles animated show/hide transitions for UI elements.
    /// Attach to any GameObject with a CanvasGroup and RectTransform.
    /// </summary>
    public class VeneerShowHideAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public AnimationType ShowAnimation = AnimationType.FadeScale;
        public AnimationType HideAnimation = AnimationType.FadeScale;
        public float ShowDuration = VeneerTheme.WindowShowDuration;
        public float HideDuration = VeneerTheme.WindowHideDuration;

        [Header("Scale Settings")]
        public float ScaleStart = 0.95f;
        public float ScaleEnd = 0.95f;

        [Header("Slide Settings")]
        public Vector2 SlideOffset = new Vector2(0, -20f);

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Coroutine _currentAnimation;
        private Vector2 _originalPosition;
        private bool _isAnimating;

        /// <summary>
        /// True if currently animating.
        /// </summary>
        public bool IsAnimating => _isAnimating;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            // Add CanvasGroup if not present
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_rectTransform != null)
            {
                _originalPosition = _rectTransform.anchoredPosition;
            }
        }

        /// <summary>
        /// Plays the show animation.
        /// </summary>
        /// <param name="onComplete">Callback when animation completes.</param>
        public void AnimateShow(Action onComplete = null)
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            // Can't start coroutines on inactive GameObjects
            if (!gameObject.activeInHierarchy)
            {
                ShowImmediate();
                onComplete?.Invoke();
                return;
            }

            _currentAnimation = StartCoroutine(PlayShowAnimation(onComplete));
        }

        /// <summary>
        /// Plays the hide animation.
        /// </summary>
        /// <param name="onComplete">Callback when animation completes.</param>
        public void AnimateHide(Action onComplete = null)
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            // Can't start coroutines on inactive GameObjects
            if (!gameObject.activeInHierarchy)
            {
                HideImmediate();
                onComplete?.Invoke();
                return;
            }

            _currentAnimation = StartCoroutine(PlayHideAnimation(onComplete));
        }

        /// <summary>
        /// Immediately shows without animation.
        /// </summary>
        public void ShowImmediate()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            _isAnimating = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            if (_rectTransform != null)
            {
                _rectTransform.localScale = Vector3.one;
                _rectTransform.anchoredPosition = _originalPosition;
            }
        }

        /// <summary>
        /// Immediately hides without animation.
        /// </summary>
        public void HideImmediate()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            _isAnimating = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private IEnumerator PlayShowAnimation(Action onComplete)
        {
            _isAnimating = true;

            switch (ShowAnimation)
            {
                case AnimationType.None:
                    ShowImmediate();
                    break;

                case AnimationType.Fade:
                    yield return VeneerAnimation.FadeIn(_canvasGroup, ShowDuration);
                    break;

                case AnimationType.Scale:
                    if (_canvasGroup != null) _canvasGroup.alpha = 1f;
                    yield return VeneerAnimation.ScaleIn(_rectTransform, ShowDuration, ScaleStart);
                    break;

                case AnimationType.FadeScale:
                    yield return VeneerAnimation.ShowWindow(_canvasGroup, _rectTransform, ShowDuration);
                    break;

                case AnimationType.Slide:
                    yield return PlaySlideIn();
                    break;

                case AnimationType.Pop:
                    if (_canvasGroup != null) _canvasGroup.alpha = 1f;
                    _rectTransform.localScale = Vector3.one;
                    yield return VeneerAnimation.Pop(_rectTransform, 1.05f, ShowDuration);
                    break;
            }

            _isAnimating = false;
            _currentAnimation = null;
            onComplete?.Invoke();
        }

        private IEnumerator PlayHideAnimation(Action onComplete)
        {
            _isAnimating = true;

            switch (HideAnimation)
            {
                case AnimationType.None:
                    HideImmediate();
                    break;

                case AnimationType.Fade:
                    yield return VeneerAnimation.FadeOut(_canvasGroup, HideDuration);
                    break;

                case AnimationType.Scale:
                    yield return VeneerAnimation.ScaleOut(_rectTransform, HideDuration, ScaleEnd);
                    if (_canvasGroup != null) _canvasGroup.alpha = 0f;
                    break;

                case AnimationType.FadeScale:
                    yield return VeneerAnimation.HideWindow(_canvasGroup, _rectTransform, HideDuration);
                    break;

                case AnimationType.Slide:
                    yield return PlaySlideOut();
                    break;

                case AnimationType.Pop:
                    yield return VeneerAnimation.FadeOut(_canvasGroup, HideDuration);
                    break;
            }

            _isAnimating = false;
            _currentAnimation = null;
            onComplete?.Invoke();
        }

        private IEnumerator PlaySlideIn()
        {
            if (_rectTransform == null || _canvasGroup == null)
            {
                yield break;
            }

            Vector2 startPos = _originalPosition + SlideOffset;
            _rectTransform.anchoredPosition = startPos;
            _canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < ShowDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ShowDuration);
                float eased = VeneerAnimation.EaseOutCubic(t);

                _canvasGroup.alpha = eased;
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _originalPosition, eased);

                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _originalPosition;
        }

        private IEnumerator PlaySlideOut()
        {
            if (_rectTransform == null || _canvasGroup == null)
            {
                yield break;
            }

            Vector2 endPos = _originalPosition + SlideOffset;
            float startAlpha = _canvasGroup.alpha;
            Vector2 startPos = _rectTransform.anchoredPosition;

            float elapsed = 0f;
            while (elapsed < HideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / HideDuration);
                float eased = VeneerAnimation.EaseInCubic(t);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _rectTransform.anchoredPosition = endPos;
        }

        /// <summary>
        /// Updates the original position (call after repositioning the element).
        /// </summary>
        public void UpdateOriginalPosition()
        {
            if (_rectTransform != null)
            {
                _originalPosition = _rectTransform.anchoredPosition;
            }
        }

        private void OnDisable()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
                _isAnimating = false;
            }
        }
    }
}
