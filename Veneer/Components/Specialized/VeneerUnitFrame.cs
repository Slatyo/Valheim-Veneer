using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Components.Specialized
{
    /// <summary>
    /// Unit frame displaying health, stamina, and eitr bars.
    /// </summary>
    public class VeneerUnitFrame : VeneerElement
    {
        private const string ElementIdPlayer = "Veneer_PlayerFrame";

        private VeneerBar _healthBar;
        private VeneerBar _staminaBar;
        private VeneerBar _eitrBar;
        private VeneerText _nameText;

        // Reserved for future text overlays on bars
#pragma warning disable CS0169
        private VeneerText _healthText;
        private VeneerText _staminaText;
        private VeneerText _eitrText;
#pragma warning restore CS0169
        private Image _backgroundImage;
        private Image _borderImage;

        private Player _trackedPlayer;
        private bool _showEitr;

        /// <summary>
        /// The player being tracked.
        /// </summary>
        public Player TrackedPlayer => _trackedPlayer;

        /// <summary>
        /// Creates a player unit frame.
        /// </summary>
        public static VeneerUnitFrame CreatePlayerFrame(Transform parent)
        {
            var go = CreateUIObject("VeneerPlayerFrame", parent);
            var frame = go.AddComponent<VeneerUnitFrame>();
            frame.Initialize(true);
            return frame;
        }

        /// <summary>
        /// Creates a target unit frame.
        /// </summary>
        public static VeneerUnitFrame CreateTargetFrame(Transform parent)
        {
            var go = CreateUIObject("VeneerTargetFrame", parent);
            var frame = go.AddComponent<VeneerUnitFrame>();
            frame.Initialize(false);
            return frame;
        }

        private void Initialize(bool isPlayerFrame)
        {
            ElementId = isPlayerFrame ? ElementIdPlayer : "Veneer_TargetFrame";
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.HUD;

            // Register with anchor system - bottom left with margin
            VeneerAnchor.Register(ElementId, ScreenAnchor.BottomLeft, new Vector2(20, 20));

            // Size based on config - bars with text need more height
            float barWidth = 200f;
            float barHeight = 16f; // Taller to fit text
            float spacing = 2f;
            float padding = 6f;
            float nameHeight = 16f;

            // Calculate frame size - much more compact
            float frameWidth = barWidth + padding * 2;
            float frameHeight = nameHeight + barHeight * 2 + spacing * 2 + padding * 2; // 2 bars by default

            SetSize(frameWidth, frameHeight);
            AnchorTo(AnchorPreset.BottomLeft, new Vector2(20, 20));

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.Background;

            // Border
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Border, Color.clear, 1);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Content area
            var content = CreateUIObject("Content", transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);

            // Add vertical layout
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = spacing;

            // Name text - compact
            _nameText = VeneerText.Create(content.transform, "Player");
            _nameText.ApplyStyle(TextStyle.Gold);
            _nameText.FontSize = VeneerConfig.GetScaledFontSize(11);
            var nameLayout = _nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = nameHeight;

            // Health bar - compact
            _healthBar = CreateCompactBar(content.transform, "Health", VeneerColors.Health, VeneerColors.HealthBackground, barHeight);

            // Stamina bar - compact
            _staminaBar = CreateCompactBar(content.transform, "Stamina", VeneerColors.Stamina, VeneerColors.StaminaBackground, barHeight);

            // Eitr bar (hidden by default until player has eitr) - compact
            _eitrBar = CreateCompactBar(content.transform, "Eitr", VeneerColors.Eitr, VeneerColors.EitrBackground, barHeight);
            _eitrBar.gameObject.SetActive(false);

            // Add mover component
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer component
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(150, 60);
            resizer.MaxSize = new Vector2(400, 200);

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }
        }

        private VeneerBar CreateCompactBar(Transform parent, string label, Color fillColor, Color bgColor, float height)
        {
            var bar = VeneerBar.Create(parent, $"{label}Bar", 200, height);
            bar.FillColor = fillColor;
            bar.BackgroundColor = bgColor;
            bar.ShowText = true; // Show current/max text
            bar.TextFormat = "{0:F0} / {1:F0}"; // Format: "100 / 150"

            var layoutElement = bar.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.flexibleWidth = 1;

            return bar;
        }

        private void Update()
        {
            if (_trackedPlayer == null)
            {
                _trackedPlayer = Player.m_localPlayer;
                if (_trackedPlayer != null && _nameText != null)
                {
                    _nameText.Content = _trackedPlayer.GetPlayerName();
                }
            }

            if (_trackedPlayer == null) return;

            UpdateBars();
        }

        private void UpdateBars()
        {
            // Health
            float health = _trackedPlayer.GetHealth();
            float maxHealth = _trackedPlayer.GetMaxHealth();
            _healthBar?.SetValues(health, maxHealth);

            // Stamina
            float stamina = _trackedPlayer.GetStamina();
            float maxStamina = _trackedPlayer.GetMaxStamina();
            _staminaBar?.SetValues(stamina, maxStamina);

            // Eitr
            float maxEitr = _trackedPlayer.GetMaxEitr();
            bool hasEitr = maxEitr > 0;

            if (hasEitr != _showEitr)
            {
                _showEitr = hasEitr;
                _eitrBar?.gameObject.SetActive(hasEitr);

                // Recalculate frame height with bar sizing
                float barHeight = 16f;
                float spacing = 2f;
                float padding = 6f;
                float nameHeight = 16f;
                int barCount = hasEitr ? 3 : 2;
                float frameHeight = nameHeight + barHeight * barCount + spacing * barCount + padding * 2;
                SetSize(RectTransform.sizeDelta.x, frameHeight);
            }

            if (hasEitr)
            {
                float eitr = _trackedPlayer.GetEitr();
                _eitrBar?.SetValues(eitr, maxEitr);
            }
        }

        /// <summary>
        /// Sets the player to track.
        /// </summary>
        public void SetPlayer(Player player)
        {
            _trackedPlayer = player;
            if (_nameText != null && player != null)
            {
                _nameText.Content = player.GetPlayerName();
            }
        }
    }
}
