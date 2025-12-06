using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Theme;

namespace Veneer.Components.Specialized
{
    /// <summary>
    /// World-space floating text for damage numbers, XP gains, etc.
    /// Pools instances for performance.
    /// </summary>
    public class VeneerFloatingText : MonoBehaviour
    {
        private static VeneerFloatingText _instance;
        private static Canvas _worldCanvas;
        private static readonly Queue<FloatingTextInstance> _pool = new Queue<FloatingTextInstance>();
        private static readonly List<FloatingTextInstance> _active = new List<FloatingTextInstance>();

        private const int POOL_SIZE = 50;
        private const float DEFAULT_DURATION = 1.5f;
        private const float DEFAULT_FLOAT_SPEED = 80f;
        private const float DEFAULT_FADE_START = 0.7f;

        /// <summary>
        /// Text style presets.
        /// </summary>
        public enum TextStyle
        {
            Normal,
            Critical,
            Heal,
            Experience,
            Miss,
            Block,
            Poison,
            Fire,
            Frost,
            Lightning,
            Spirit,
            DamageTaken  // Red - when player takes damage
        }

        /// <summary>
        /// Initialize the floating text system.
        /// </summary>
        public static void Initialize()
        {
            if (_instance != null) return;

            // Create manager object
            var go = new GameObject("VeneerFloatingTextManager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<VeneerFloatingText>();

            // Create world-space canvas
            var canvasGo = new GameObject("FloatingTextCanvas");
            canvasGo.transform.SetParent(go.transform);
            _worldCanvas = canvasGo.AddComponent<Canvas>();
            _worldCanvas.renderMode = RenderMode.WorldSpace;
            _worldCanvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Pre-populate pool
            for (int i = 0; i < POOL_SIZE; i++)
            {
                _pool.Enqueue(CreateInstance());
            }

            Plugin.Log?.LogInfo("VeneerFloatingText initialized");
        }

        /// <summary>
        /// Show floating text at a world position.
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="worldPosition">World position to show at</param>
        /// <param name="style">Visual style preset</param>
        /// <param name="duration">How long to display (default 1.5s)</param>
        public static void Show(string text, Vector3 worldPosition, TextStyle style = TextStyle.Normal, float duration = DEFAULT_DURATION)
        {
            if (_instance == null) Initialize();

            var instance = GetInstance();
            instance.Setup(text, worldPosition, style, duration);
            _active.Add(instance);
        }

        /// <summary>
        /// Show damage number with automatic styling based on damage type and crit.
        /// </summary>
        /// <param name="damage">Damage amount</param>
        /// <param name="worldPosition">World position</param>
        /// <param name="isCritical">Is this a critical hit?</param>
        /// <param name="damageType">Type of damage for coloring</param>
        /// <param name="isDamageTaken">True if this is damage the player received</param>
        public static void ShowDamage(float damage, Vector3 worldPosition, bool isCritical = false, string damageType = null, bool isDamageTaken = false)
        {
            if (damage <= 0) return;

            TextStyle style = TextStyle.Normal;
            string text = Mathf.RoundToInt(damage).ToString();

            if (isDamageTaken)
            {
                // Player took damage - show in red with minus sign
                style = TextStyle.DamageTaken;
                text = $"-{text}";
            }
            else if (isCritical)
            {
                style = TextStyle.Critical;
                text = $"{text}!";
            }
            else if (!string.IsNullOrEmpty(damageType))
            {
                style = damageType.ToLower() switch
                {
                    "fire" => TextStyle.Fire,
                    "frost" => TextStyle.Frost,
                    "lightning" => TextStyle.Lightning,
                    "poison" => TextStyle.Poison,
                    "spirit" => TextStyle.Spirit,
                    _ => TextStyle.Normal
                };
            }

            // Add slight random offset to prevent overlap
            var offset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0.5f, 1.0f),
                Random.Range(-0.3f, 0.3f)
            );

            Show(text, worldPosition + offset, style);
        }

        /// <summary>
        /// Show healing number.
        /// </summary>
        public static void ShowHeal(float amount, Vector3 worldPosition)
        {
            if (amount <= 0) return;
            string text = $"+{Mathf.RoundToInt(amount)}";
            Show(text, worldPosition + Vector3.up * 0.5f, TextStyle.Heal);
        }

        /// <summary>
        /// Show XP gain.
        /// </summary>
        public static void ShowXP(long amount, Vector3 worldPosition)
        {
            if (amount <= 0) return;
            string text = $"+{amount} XP";
            Show(text, worldPosition + Vector3.up * 1.5f, TextStyle.Experience, 2f);
        }

        /// <summary>
        /// Show "Miss" text.
        /// </summary>
        public static void ShowMiss(Vector3 worldPosition)
        {
            Show("Miss", worldPosition + Vector3.up * 0.5f, TextStyle.Miss);
        }

        /// <summary>
        /// Show "Blocked" text.
        /// </summary>
        public static void ShowBlocked(float amount, Vector3 worldPosition)
        {
            string text = amount > 0 ? $"Blocked {Mathf.RoundToInt(amount)}" : "Blocked";
            Show(text, worldPosition + Vector3.up * 0.5f, TextStyle.Block);
        }

        private void Update()
        {
            // Face camera
            if (Camera.main != null && _worldCanvas != null)
            {
                _worldCanvas.transform.rotation = Camera.main.transform.rotation;
            }

            // Update active instances
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var instance = _active[i];
                if (instance.Update())
                {
                    // Still active
                    continue;
                }

                // Finished - return to pool
                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
                _active.RemoveAt(i);
            }
        }

        private static FloatingTextInstance GetInstance()
        {
            if (_pool.Count > 0)
            {
                var instance = _pool.Dequeue();
                instance.gameObject.SetActive(true);
                return instance;
            }

            // Pool exhausted - create new
            return CreateInstance();
        }

        private static FloatingTextInstance CreateInstance()
        {
            var go = new GameObject("FloatingText");
            go.transform.SetParent(_worldCanvas.transform);
            go.SetActive(false);

            var instance = go.AddComponent<FloatingTextInstance>();
            return instance;
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public static void Cleanup()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
            _pool.Clear();
            _active.Clear();
        }
    }

    /// <summary>
    /// Individual floating text instance.
    /// </summary>
    public class FloatingTextInstance : MonoBehaviour
    {
        private Text _text;
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Outline _outline;

        private float _duration;
        private float _elapsed;
        private float _floatSpeed;
        private float _fadeStart;
        private Vector3 _worldPosition;
        private Color _baseColor;

        private void Awake()
        {
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(200, 50);

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _text = gameObject.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;

            _outline = gameObject.AddComponent<Outline>();
            _outline.effectColor = Color.black;
            _outline.effectDistance = new Vector2(1, -1);
        }

        public void Setup(string text, Vector3 worldPosition, VeneerFloatingText.TextStyle style, float duration)
        {
            _text.text = text;
            _worldPosition = worldPosition;
            _duration = duration;
            _elapsed = 0f;
            _floatSpeed = 80f;
            _fadeStart = 0.7f;
            _canvasGroup.alpha = 1f;

            // Apply style
            ApplyStyle(style);

            // Position in world space
            UpdatePosition();
        }

        private void ApplyStyle(VeneerFloatingText.TextStyle style)
        {
            switch (style)
            {
                case VeneerFloatingText.TextStyle.Critical:
                    _baseColor = new Color(1f, 0.85f, 0.2f); // Gold/Yellow for crits
                    _text.fontSize = 28;
                    _text.fontStyle = FontStyle.Bold;
                    _floatSpeed = 100f;
                    break;

                case VeneerFloatingText.TextStyle.Heal:
                    _baseColor = new Color(0.3f, 1f, 0.3f); // Green
                    _text.fontSize = 22;
                    _text.fontStyle = FontStyle.Normal;
                    break;

                case VeneerFloatingText.TextStyle.Experience:
                    _baseColor = new Color(1f, 0.9f, 0.4f); // Gold
                    _text.fontSize = 20;
                    _text.fontStyle = FontStyle.Bold;
                    _floatSpeed = 60f;
                    break;

                case VeneerFloatingText.TextStyle.Miss:
                    _baseColor = new Color(0.7f, 0.7f, 0.7f); // Gray
                    _text.fontSize = 18;
                    _text.fontStyle = FontStyle.Italic;
                    break;

                case VeneerFloatingText.TextStyle.Block:
                    _baseColor = new Color(0.5f, 0.7f, 1f); // Light blue
                    _text.fontSize = 20;
                    _text.fontStyle = FontStyle.Normal;
                    break;

                case VeneerFloatingText.TextStyle.Fire:
                    _baseColor = new Color(1f, 0.5f, 0.1f); // Orange
                    _text.fontSize = 22;
                    _text.fontStyle = FontStyle.Normal;
                    break;

                case VeneerFloatingText.TextStyle.Frost:
                    _baseColor = new Color(0.5f, 0.8f, 1f); // Ice blue
                    _text.fontSize = 22;
                    _text.fontStyle = FontStyle.Normal;
                    break;

                case VeneerFloatingText.TextStyle.Lightning:
                    _baseColor = new Color(1f, 1f, 0.3f); // Yellow
                    _text.fontSize = 22;
                    _text.fontStyle = FontStyle.Normal;
                    break;

                case VeneerFloatingText.TextStyle.Poison:
                    _baseColor = new Color(0.6f, 1f, 0.3f); // Lime green
                    _text.fontSize = 22;
                    _text.fontStyle = FontStyle.Normal;
                    break;

                case VeneerFloatingText.TextStyle.Spirit:
                    _baseColor = new Color(0.8f, 0.6f, 1f); // Purple/violet
                    _text.fontSize = 22;
                    _text.fontStyle = FontStyle.Normal;
                    break;

                case VeneerFloatingText.TextStyle.DamageTaken:
                    _baseColor = new Color(1f, 0.2f, 0.2f); // Red for damage taken
                    _text.fontSize = 24;
                    _text.fontStyle = FontStyle.Bold;
                    break;

                default:
                    _baseColor = Color.white;
                    _text.fontSize = 22;
                    _text.fontStyle = FontStyle.Normal;
                    break;
            }

            _text.color = _baseColor;
        }

        public bool Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed >= _duration)
            {
                return false; // Finished
            }

            // Float upward
            _worldPosition.y += _floatSpeed * Time.deltaTime * 0.01f;
            UpdatePosition();

            // Fade out near end
            float t = _elapsed / _duration;
            if (t > _fadeStart)
            {
                float fadeT = (t - _fadeStart) / (1f - _fadeStart);
                _canvasGroup.alpha = 1f - fadeT;
            }

            // Scale animation for crits
            if (_text.fontStyle == FontStyle.Bold && _elapsed < 0.2f)
            {
                float scaleT = _elapsed / 0.2f;
                float scale = 1f + (1f - scaleT) * 0.3f; // Start big, shrink to normal
                _rect.localScale = Vector3.one * scale;
            }

            return true; // Still active
        }

        private void UpdatePosition()
        {
            if (Camera.main == null) return;

            // Convert world position to screen, then back to world canvas position
            Vector3 screenPos = Camera.main.WorldToScreenPoint(_worldPosition);

            // If behind camera, hide
            if (screenPos.z < 0)
            {
                _canvasGroup.alpha = 0;
                return;
            }

            // Scale based on distance
            float distance = Vector3.Distance(Camera.main.transform.position, _worldPosition);
            float scale = Mathf.Clamp(10f / distance, 0.5f, 2f);
            _rect.localScale = Vector3.one * scale * 0.01f;

            // Position in world space (face camera)
            transform.position = _worldPosition;
        }
    }
}
