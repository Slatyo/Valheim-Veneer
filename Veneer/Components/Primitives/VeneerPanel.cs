using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Theme;

namespace Veneer.Components.Primitives
{
    /// <summary>
    /// Basic panel component with background and border.
    /// </summary>
    public class VeneerPanel : VeneerElement
    {
        private Image _backgroundImage;
        private Image _borderImage;

        /// <summary>
        /// Background color.
        /// </summary>
        public Color BackgroundColor
        {
            get => _backgroundImage != null ? _backgroundImage.color : VeneerColors.Background;
            set
            {
                if (_backgroundImage != null)
                    _backgroundImage.color = value;
            }
        }

        /// <summary>
        /// Border color.
        /// </summary>
        public Color BorderColor
        {
            get => _borderImage != null ? _borderImage.color : VeneerColors.Border;
            set
            {
                if (_borderImage != null)
                    _borderImage.color = value;
            }
        }

        /// <summary>
        /// Whether the border is visible.
        /// </summary>
        public bool ShowBorder
        {
            get => _borderImage != null && _borderImage.enabled;
            set
            {
                if (_borderImage != null)
                    _borderImage.enabled = value;
            }
        }

        /// <summary>
        /// Creates a new VeneerPanel.
        /// </summary>
        public static VeneerPanel Create(Transform parent, string name = "VeneerPanel", float width = 100, float height = 100)
        {
            var go = CreateUIObject(name, parent);
            var panel = go.AddComponent<VeneerPanel>();
            panel.Initialize(width, height);
            return panel;
        }

        /// <summary>
        /// Creates a panel with just a background (no border).
        /// </summary>
        public static VeneerPanel CreateBackground(Transform parent, string name = "Background", Color? color = null)
        {
            var panel = Create(parent, name, 100, 100);
            panel.ShowBorder = false;
            panel.BackgroundColor = color ?? VeneerColors.Background;
            panel.StretchToFill();
            return panel;
        }

        /// <summary>
        /// Creates a panel that stretches to fill its parent.
        /// </summary>
        public static VeneerPanel CreateStretched(Transform parent, string name = "VeneerPanel")
        {
            var panel = Create(parent, name, 100, 100);
            panel.StretchToFill();
            return panel;
        }

        /// <summary>
        /// Creates a panel with a custom border color (e.g., for boss frames, rarity).
        /// </summary>
        public static VeneerPanel CreateWithBorder(Transform parent, Color borderColor, string name = "VeneerPanel", float width = 100, float height = 100)
        {
            var panel = Create(parent, name, width, height);
            panel.BorderColor = borderColor;
            return panel;
        }

        /// <summary>
        /// Creates a panel with a thicker border.
        /// </summary>
        public static VeneerPanel CreateWithThickBorder(Transform parent, Color borderColor, int borderWidth = 2, string name = "VeneerPanel", float width = 100, float height = 100)
        {
            var go = CreateUIObject(name, parent);
            var panel = go.AddComponent<VeneerPanel>();
            panel.InitializeWithThickBorder(width, height, borderColor, borderWidth);
            return panel;
        }

        private void InitializeWithThickBorder(float width, float height, Color borderColor, int borderWidth)
        {
            SetSize(width, height);

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.Background;

            // Border overlay with custom thickness
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTexture = VeneerTextures.CreateSlicedBorderTexture(16, borderColor, Color.clear, borderWidth);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTexture, borderWidth);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;
        }

        private void Initialize(float width, float height)
        {
            SetSize(width, height);

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.Background;

            // Border overlay
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTexture = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Border, Color.clear, 1);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTexture, 1);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;
        }

        /// <summary>
        /// Sets the panel to use a lighter background.
        /// </summary>
        public void UseLightBackground()
        {
            BackgroundColor = VeneerColors.BackgroundLight;
        }

        /// <summary>
        /// Sets the panel to use a solid (opaque) background.
        /// </summary>
        public void UseSolidBackground()
        {
            BackgroundColor = VeneerColors.BackgroundSolid;
        }

        /// <summary>
        /// Highlights the border.
        /// </summary>
        public void Highlight(bool highlighted)
        {
            BorderColor = highlighted ? VeneerColors.BorderHighlight : VeneerColors.Border;
        }

        /// <summary>
        /// Sets the border to a rarity color.
        /// </summary>
        public void SetRarityBorder(int rarityTier)
        {
            BorderColor = VeneerColors.GetRarityColor(rarityTier);
        }

        /// <summary>
        /// Sets the border to the legendary/boss gold color.
        /// </summary>
        public void SetLegendaryBorder()
        {
            BorderColor = VeneerColors.Legendary;
        }

        /// <summary>
        /// Updates the border with a new thickness.
        /// </summary>
        public void SetBorderThickness(int borderWidth, Color? borderColor = null)
        {
            if (_borderImage == null) return;

            var color = borderColor ?? BorderColor;
            var borderTexture = VeneerTextures.CreateSlicedBorderTexture(16, color, Color.clear, borderWidth);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTexture, borderWidth);
        }

        /// <summary>
        /// Gets the background Image component for advanced customization.
        /// </summary>
        public Image BackgroundImage => _backgroundImage;

        /// <summary>
        /// Gets the border Image component for advanced customization.
        /// </summary>
        public Image BorderImage => _borderImage;
    }
}
