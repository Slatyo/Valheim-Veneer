using System;
using System.Collections;
using UnityEngine;

namespace Veneer.Theme
{
    /// <summary>
    /// Central animation controller for Veneer UI.
    /// Provides coroutine-based animations without external dependencies.
    /// </summary>
    public static class VeneerAnimation
    {
        // === Window Animations ===

        /// <summary>
        /// Animates a window showing with combined fade and scale.
        /// </summary>
        /// <param name="group">CanvasGroup for alpha.</param>
        /// <param name="rect">RectTransform for scale.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator ShowWindow(CanvasGroup group, RectTransform rect, float duration, Action onComplete = null)
        {
            if (group == null || rect == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startScale = 0.95f;
            group.alpha = 0f;
            rect.localScale = Vector3.one * startScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);

                group.alpha = eased;
                rect.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, eased);

                yield return null;
            }

            group.alpha = 1f;
            rect.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Animates a window hiding with combined fade and scale.
        /// </summary>
        /// <param name="group">CanvasGroup for alpha.</param>
        /// <param name="rect">RectTransform for scale.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator HideWindow(CanvasGroup group, RectTransform rect, float duration, Action onComplete = null)
        {
            if (group == null || rect == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float endScale = 0.95f;
            float startAlpha = group.alpha;
            Vector3 startScaleVec = rect.localScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInCubic(t);

                group.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                rect.localScale = Vector3.Lerp(startScaleVec, Vector3.one * endScale, eased);

                yield return null;
            }

            group.alpha = 0f;
            rect.localScale = Vector3.one * endScale;
            onComplete?.Invoke();
        }

        // === Fade Animations ===

        /// <summary>
        /// Fades a CanvasGroup in.
        /// </summary>
        /// <param name="group">CanvasGroup to animate.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator FadeIn(CanvasGroup group, float duration, Action onComplete = null)
        {
            if (group == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(startAlpha, 1f, SmoothStep(t));
                yield return null;
            }

            group.alpha = 1f;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Fades a CanvasGroup out.
        /// </summary>
        /// <param name="group">CanvasGroup to animate.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator FadeOut(CanvasGroup group, float duration, Action onComplete = null)
        {
            if (group == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(startAlpha, 0f, SmoothStep(t));
                yield return null;
            }

            group.alpha = 0f;
            onComplete?.Invoke();
        }

        // === Scale Animations ===

        /// <summary>
        /// Scales a RectTransform in from a smaller size.
        /// </summary>
        /// <param name="rect">RectTransform to animate.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="startScale">Starting scale (default 0.9).</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator ScaleIn(RectTransform rect, float duration, float startScale = 0.9f, Action onComplete = null)
        {
            if (rect == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            rect.localScale = Vector3.one * startScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, EaseOutBack(t));
                yield return null;
            }

            rect.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Scales a RectTransform out to a smaller size.
        /// </summary>
        /// <param name="rect">RectTransform to animate.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="endScale">Ending scale (default 0.9).</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator ScaleOut(RectTransform rect, float duration, float endScale = 0.9f, Action onComplete = null)
        {
            if (rect == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 startScaleVec = rect.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.localScale = Vector3.Lerp(startScaleVec, Vector3.one * endScale, EaseInCubic(t));
                yield return null;
            }

            rect.localScale = Vector3.one * endScale;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Pop animation with overshoot - good for button press feedback.
        /// </summary>
        /// <param name="rect">RectTransform to animate.</param>
        /// <param name="scale">Peak scale (default 1.05).</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator Pop(RectTransform rect, float scale = 1.05f, float duration = 0.1f, Action onComplete = null)
        {
            if (rect == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 startScaleVec = rect.localScale;
            Vector3 peakScaleVec = Vector3.one * scale;
            float halfDuration = duration * 0.5f;

            // Scale up
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                rect.localScale = Vector3.Lerp(startScaleVec, peakScaleVec, EaseOutCubic(t));
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                rect.localScale = Vector3.Lerp(peakScaleVec, Vector3.one, EaseInCubic(t));
                yield return null;
            }

            rect.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        // === Color Animations ===

        /// <summary>
        /// Animates color transition on an Image component.
        /// </summary>
        /// <param name="image">Image to animate.</param>
        /// <param name="targetColor">Target color.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator ColorTransition(UnityEngine.UI.Image image, Color targetColor, float duration, Action onComplete = null)
        {
            if (image == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Color startColor = image.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                image.color = Color.Lerp(startColor, targetColor, SmoothStep(t));
                yield return null;
            }

            image.color = targetColor;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Animates multiple color transitions simultaneously.
        /// </summary>
        /// <param name="targets">Array of (Image, targetColor) tuples.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator MultiColorTransition((UnityEngine.UI.Image image, Color target)[] targets, float duration, Action onComplete = null)
        {
            if (targets == null || targets.Length == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Store start colors
            Color[] startColors = new Color[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                startColors[i] = targets[i].image != null ? targets[i].image.color : Color.white;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = SmoothStep(t);

                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i].image != null)
                    {
                        targets[i].image.color = Color.Lerp(startColors[i], targets[i].target, eased);
                    }
                }

                yield return null;
            }

            // Set final colors
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].image != null)
                {
                    targets[i].image.color = targets[i].target;
                }
            }

            onComplete?.Invoke();
        }

        // === Value Animations ===

        /// <summary>
        /// Animates a float value over time.
        /// </summary>
        /// <param name="startValue">Starting value.</param>
        /// <param name="endValue">Ending value.</param>
        /// <param name="duration">Animation duration in seconds.</param>
        /// <param name="onUpdate">Called each frame with current value.</param>
        /// <param name="onComplete">Callback when animation completes.</param>
        public static IEnumerator AnimateValue(float startValue, float endValue, float duration, Action<float> onUpdate, Action onComplete = null)
        {
            if (onUpdate == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onUpdate(Mathf.Lerp(startValue, endValue, SmoothStep(t)));
                yield return null;
            }

            onUpdate(endValue);
            onComplete?.Invoke();
        }

        // === Easing Functions ===

        /// <summary>
        /// SmoothStep easing (ease-in-out).
        /// </summary>
        /// <param name="t">Input value (0-1).</param>
        /// <returns>Eased value.</returns>
        public static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// SmootherStep easing (smoother ease-in-out).
        /// </summary>
        /// <param name="t">Input value (0-1).</param>
        /// <returns>Eased value.</returns>
        public static float SmootherStep(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /// <summary>
        /// Ease out cubic (decelerating).
        /// </summary>
        /// <param name="t">Input value (0-1).</param>
        /// <returns>Eased value.</returns>
        public static float EaseOutCubic(float t)
        {
            float t1 = t - 1f;
            return t1 * t1 * t1 + 1f;
        }

        /// <summary>
        /// Ease in cubic (accelerating).
        /// </summary>
        /// <param name="t">Input value (0-1).</param>
        /// <returns>Eased value.</returns>
        public static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        /// <summary>
        /// Ease out back (slight overshoot at end).
        /// </summary>
        /// <param name="t">Input value (0-1).</param>
        /// <returns>Eased value.</returns>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float t1 = t - 1f;
            return 1f + c3 * t1 * t1 * t1 + c1 * t1 * t1;
        }

        /// <summary>
        /// Ease in back (slight pullback at start).
        /// </summary>
        /// <param name="t">Input value (0-1).</param>
        /// <returns>Eased value.</returns>
        public static float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        /// <summary>
        /// Elastic ease out (bouncy effect).
        /// </summary>
        /// <param name="t">Input value (0-1).</param>
        /// <returns>Eased value.</returns>
        public static float EaseOutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;

            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>
        /// Bounce ease out.
        /// </summary>
        /// <param name="t">Input value (0-1).</param>
        /// <returns>Eased value.</returns>
        public static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / d1;
                return n1 * t * t + 0.984375f;
            }
        }
    }
}
