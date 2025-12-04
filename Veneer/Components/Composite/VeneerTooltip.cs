using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Composite
{
    /// <summary>
    /// Context passed to tooltip providers when building item tooltips.
    /// </summary>
    public class ItemTooltipContext
    {
        /// <summary>
        /// The item being displayed.
        /// </summary>
        public ItemDrop.ItemData Item { get; set; }

        /// <summary>
        /// The tooltip data being built. Providers can modify this.
        /// </summary>
        public TooltipData Tooltip { get; set; }
    }

    /// <summary>
    /// Interface for tooltip providers that can modify item tooltips.
    /// </summary>
    public interface IItemTooltipProvider
    {
        /// <summary>
        /// Priority for ordering providers. Higher values run later.
        /// Default providers (vanilla) are 0, affixes should be negative to prepend.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Modifies the tooltip data for an item.
        /// </summary>
        void ModifyTooltip(ItemTooltipContext context);
    }

    /// <summary>
    /// Tooltip component with rich formatting and positioning.
    /// Follows mouse cursor and auto-positions to stay on screen.
    /// </summary>
    public class VeneerTooltip : VeneerElement
    {
        private static VeneerTooltip _instance;
        private static readonly List<IItemTooltipProvider> _tooltipProviders = new List<IItemTooltipProvider>();

        /// <summary>
        /// Registers a tooltip provider that will be called when item tooltips are shown.
        /// Providers are called in order of priority (lowest first).
        /// </summary>
        public static void RegisterProvider(IItemTooltipProvider provider)
        {
            if (provider == null) return;

            // Insert in priority order
            int index = 0;
            for (; index < _tooltipProviders.Count; index++)
            {
                if (_tooltipProviders[index].Priority > provider.Priority)
                    break;
            }
            _tooltipProviders.Insert(index, provider);
            Plugin.Log.LogInfo($"[VeneerTooltip] Registered tooltip provider: {provider.GetType().Name} (priority {provider.Priority})");
        }

        /// <summary>
        /// Unregisters a tooltip provider.
        /// </summary>
        public static void UnregisterProvider(IItemTooltipProvider provider)
        {
            if (provider == null) return;
            _tooltipProviders.Remove(provider);
            Plugin.Log.LogInfo($"[VeneerTooltip] Unregistered tooltip provider: {provider.GetType().Name}");
        }

        /// <summary>
        /// Builds tooltip data for an item, allowing all registered providers to modify it.
        /// </summary>
        public static TooltipData BuildItemTooltip(ItemDrop.ItemData item, TooltipData baseTooltip)
        {
            if (item == null) return baseTooltip;

            var context = new ItemTooltipContext
            {
                Item = item,
                Tooltip = baseTooltip
            };

            foreach (var provider in _tooltipProviders)
            {
                try
                {
                    provider.ModifyTooltip(context);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[VeneerTooltip] Provider {provider.GetType().Name} threw exception: {ex.Message}");
                }
            }

            return context.Tooltip;
        }

        /// <summary>
        /// Singleton instance. Returns null if not initialized.
        /// </summary>
        public static VeneerTooltip Instance => _instance;

        /// <summary>
        /// Whether the tooltip is initialized.
        /// </summary>
        public static bool IsInitialized => _instance != null;

        private Image _backgroundImage;
        private Image _borderImage;
        private RectTransform _contentRect;
        private Canvas _canvas;

        private Text _titleText;
        private Text _subtitleText;
        private Text _bodyText;
        private GameObject _divider;

        private bool _isShowing;
        private TooltipData _currentData;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            if (_isShowing)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// Initializes the tooltip system.
        /// </summary>
        public static void Initialize(Transform parent)
        {
            // Check if instance exists and is still valid (not destroyed)
            if (_instance != null && _instance.gameObject != null)
            {
                Plugin.Log.LogDebug("[VeneerTooltip] Already initialized with valid instance, skipping");
                return;
            }

            // Clear any destroyed instance reference
            if (_instance != null)
            {
                Plugin.Log.LogDebug("[VeneerTooltip] Previous instance was destroyed, reinitializing...");
                _instance = null;
            }

            if (parent == null)
            {
                Plugin.Log.LogError("[VeneerTooltip] Initialize called with null parent!");
                return;
            }

            try
            {
                // Create GameObject directly since CreateUIObject is a protected instance method
                var go = new GameObject("VeneerTooltip", typeof(RectTransform));
                go.transform.SetParent(parent, false);

                var tooltip = go.AddComponent<VeneerTooltip>();

                // Set instance explicitly here in case Awake hasn't run yet
                _instance = tooltip;

                tooltip.Setup();
                tooltip.HideInternal();

                Plugin.Log.LogInfo($"[VeneerTooltip] Initialized successfully. Instance set: {_instance != null}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[VeneerTooltip] Failed to initialize: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Setup()
        {
            // Get the root canvas for scale factor reference BEFORE adding our own
            _canvas = GetComponentInParent<Canvas>();

            // Ensure tooltip renders on top of everything - add Canvas FIRST
            var tooltipCanvas = gameObject.AddComponent<Canvas>();
            tooltipCanvas.overrideSorting = true;
            tooltipCanvas.sortingOrder = VeneerLayers.Tooltip;
            gameObject.AddComponent<GraphicRaycaster>();

            var canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Main container uses VerticalLayoutGroup with padding for the background effect
            var mainLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            mainLayout.childAlignment = TextAnchor.UpperLeft;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.spacing = VeneerDimensions.Spacing;
            int padding = (int)VeneerDimensions.TooltipPadding;
            mainLayout.padding = new RectOffset(padding, padding, padding, padding);

            // Main object size fitter
            var mainFitter = gameObject.AddComponent<ContentSizeFitter>();
            mainFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            mainFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Background with semi-transparent dark color
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreateSlicedSprite(
                VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.BorderLight, VeneerColors.Background, 2), 2);
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = Color.white; // The sprite already has the colors baked in

            // Border overlay for rarity coloring (sits on top of background)
            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(transform, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(
                VeneerTextures.CreateSlicedBorderTexture(16, Color.white, Color.clear, 2), 2);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.color = VeneerColors.BorderLight; // Will be tinted for rarity
            _borderImage.raycastTarget = false;

            // Make border ignore layout
            var borderLayout = borderGo.AddComponent<LayoutElement>();
            borderLayout.ignoreLayout = true;

            // Store content rect reference (it's now the main object)
            _contentRect = RectTransform;

            // Create text elements directly as children
            CreateTextElements();

            Plugin.Log.LogDebug($"[VeneerTooltip] Setup complete. Parent canvas scale: {(_canvas != null ? _canvas.scaleFactor.ToString() : "null")}");
        }

        private void CreateTextElements()
        {
            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(transform, false);
            _titleText = titleGo.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeMedium);
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = VeneerColors.Text;
            _titleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var titleLayout = titleGo.AddComponent<LayoutElement>();
            titleLayout.preferredWidth = VeneerDimensions.TooltipMaxWidth;

            // Subtitle
            var subtitleGo = new GameObject("Subtitle", typeof(RectTransform));
            subtitleGo.transform.SetParent(transform, false);
            _subtitleText = subtitleGo.AddComponent<Text>();
            _subtitleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _subtitleText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
            _subtitleText.color = VeneerColors.TextMuted;
            _subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var subtitleLayout = subtitleGo.AddComponent<LayoutElement>();
            subtitleLayout.preferredWidth = VeneerDimensions.TooltipMaxWidth;

            // Divider
            _divider = new GameObject("Divider", typeof(RectTransform));
            _divider.transform.SetParent(transform, false);
            var dividerImage = _divider.AddComponent<Image>();
            dividerImage.color = VeneerColors.BorderLight;
            var dividerLayout = _divider.AddComponent<LayoutElement>();
            dividerLayout.preferredHeight = 1;
            dividerLayout.flexibleWidth = 1;

            // Body
            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(transform, false);
            _bodyText = bodyGo.AddComponent<Text>();
            _bodyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _bodyText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
            _bodyText.color = VeneerColors.Text;
            _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var bodyLayout = bodyGo.AddComponent<LayoutElement>();
            bodyLayout.preferredWidth = VeneerDimensions.TooltipMaxWidth;
        }

        /// <summary>
        /// Shows the tooltip with simple text.
        /// </summary>
        public static void Show(string text)
        {
            Show(new TooltipData { Body = text });
        }

        /// <summary>
        /// Shows the tooltip with title and body.
        /// </summary>
        public static void Show(string title, string body)
        {
            Show(new TooltipData { Title = title, Body = body });
        }

        /// <summary>
        /// Shows the tooltip with full data.
        /// </summary>
        public static void Show(TooltipData data)
        {
            if (Instance == null)
            {
                Plugin.Log.LogWarning("[VeneerTooltip] Show called but Instance is null!");
                return;
            }
            Instance.ShowInternal(data);
        }

        /// <summary>
        /// Shows a tooltip for an item, allowing registered providers to modify it.
        /// This is the preferred method for showing item tooltips.
        /// </summary>
        public static void ShowForItem(ItemDrop.ItemData item, TooltipData baseTooltip)
        {
            if (item == null || Instance == null) return;

            var finalTooltip = BuildItemTooltip(item, baseTooltip);
            Instance.ShowInternal(finalTooltip);
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public new static void Hide()
        {
            if (_instance != null)
                _instance.HideInternal();
        }

        private void ShowInternal(TooltipData data)
        {
            _currentData = data;
            _isShowing = true;

            // Update title
            if (!string.IsNullOrEmpty(data.Title))
            {
                _titleText.text = data.Title;
                _titleText.color = data.TitleColor ?? VeneerColors.Text;
                _titleText.gameObject.SetActive(true);
            }
            else
            {
                _titleText.gameObject.SetActive(false);
            }

            // Update subtitle
            if (!string.IsNullOrEmpty(data.Subtitle))
            {
                _subtitleText.text = data.Subtitle;
                _subtitleText.color = data.SubtitleColor ?? VeneerColors.TextMuted;
                _subtitleText.gameObject.SetActive(true);
            }
            else
            {
                _subtitleText.gameObject.SetActive(false);
            }

            // Update divider
            _divider.SetActive(!string.IsNullOrEmpty(data.Title) && !string.IsNullOrEmpty(data.Body));

            // Update body
            if (!string.IsNullOrEmpty(data.Body))
            {
                _bodyText.text = data.Body;
                _bodyText.gameObject.SetActive(true);
            }
            else
            {
                _bodyText.gameObject.SetActive(false);
            }

            // Update border color (for rarity)
            if (data.RarityTier.HasValue)
            {
                _borderImage.color = VeneerColors.GetRarityColor(data.RarityTier.Value);
                _titleText.color = VeneerColors.GetRarityColor(data.RarityTier.Value);
            }
            else
            {
                _borderImage.color = VeneerColors.Border;
            }

            gameObject.SetActive(true);

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);

            // Ensure tooltip is on top
            transform.SetAsLastSibling();

            UpdatePosition();
        }

        private void HideInternal()
        {
            _isShowing = false;
            _currentData = null;
            gameObject.SetActive(false);
        }

        private void UpdatePosition()
        {
            Vector2 mousePos = Input.mousePosition;
            float offset = 12f;

            // Force layout update to get accurate size
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);

            Vector2 tooltipSize = RectTransform.sizeDelta;

            // Handle case where size isn't calculated yet
            if (tooltipSize.x <= 0 || tooltipSize.y <= 0)
            {
                tooltipSize = new Vector2(200, 100); // Fallback size
            }

            // Get canvas scale factor for proper positioning
            float scaleFactor = 1f;
            if (_canvas != null)
            {
                scaleFactor = _canvas.scaleFactor;
            }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Scale the tooltip size for screen coordinate calculations
            Vector2 scaledSize = tooltipSize * scaleFactor;

            // Determine which quadrant to place tooltip based on available space
            // Default: top-right of cursor (tooltip's bottom-left corner at cursor)
            bool placeRight = true;
            bool placeAbove = true;

            // Check if tooltip would go off right edge
            if (mousePos.x + offset + scaledSize.x > screenWidth)
            {
                placeRight = false;
            }

            // Check if tooltip would go off top edge
            if (mousePos.y + offset + scaledSize.y > screenHeight)
            {
                placeAbove = false;
            }

            // Calculate position based on quadrant
            Vector2 position;
            Vector2 pivot;

            if (placeRight && placeAbove)
            {
                // Top-right: tooltip bottom-left at cursor top-right
                position = new Vector2(mousePos.x + offset, mousePos.y + offset);
                pivot = new Vector2(0, 0); // Bottom-left pivot
            }
            else if (!placeRight && placeAbove)
            {
                // Top-left: tooltip bottom-right at cursor top-left
                position = new Vector2(mousePos.x - offset, mousePos.y + offset);
                pivot = new Vector2(1, 0); // Bottom-right pivot
            }
            else if (placeRight && !placeAbove)
            {
                // Bottom-right: tooltip top-left at cursor bottom-right
                position = new Vector2(mousePos.x + offset, mousePos.y - offset);
                pivot = new Vector2(0, 1); // Top-left pivot
            }
            else
            {
                // Bottom-left: tooltip top-right at cursor bottom-left
                position = new Vector2(mousePos.x - offset, mousePos.y - offset);
                pivot = new Vector2(1, 1); // Top-right pivot
            }

            // Update pivot for proper anchoring
            RectTransform.pivot = pivot;

            // Final safety clamp to keep tooltip fully on screen
            if (pivot.x == 0) // Left-anchored
            {
                position.x = Mathf.Min(position.x, screenWidth - scaledSize.x);
            }
            else // Right-anchored
            {
                position.x = Mathf.Max(position.x, scaledSize.x);
            }

            if (pivot.y == 0) // Bottom-anchored
            {
                position.y = Mathf.Min(position.y, screenHeight - scaledSize.y);
            }
            else // Top-anchored
            {
                position.y = Mathf.Max(position.y, scaledSize.y);
            }

            RectTransform.position = position;
        }

        /// <summary>
        /// Cleans up the tooltip system.
        /// </summary>
        public static void Cleanup()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }

    /// <summary>
    /// Data for tooltip display.
    /// </summary>
    public class TooltipData
    {
        /// <summary>
        /// Main title text.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Title color override.
        /// </summary>
        public Color? TitleColor { get; set; }

        /// <summary>
        /// Subtitle text (below title).
        /// </summary>
        public string Subtitle { get; set; }

        /// <summary>
        /// Subtitle color override.
        /// </summary>
        public Color? SubtitleColor { get; set; }

        /// <summary>
        /// Body text.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Rarity tier for border/title color.
        /// </summary>
        public int? RarityTier { get; set; }
    }
}
