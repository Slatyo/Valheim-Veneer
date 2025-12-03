using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Components.Specialized
{
    /// <summary>
    /// Status effects bar showing active buffs and debuffs.
    /// </summary>
    public class VeneerStatusBar : VeneerElement
    {
        private const string ElementIdStatus = "Veneer_StatusBar";
        private const int MaxVisibleEffects = 8;

        private List<StatusSlot> _slots = new List<StatusSlot>();
        private Dictionary<string, StatusSlot> _activeEffects = new Dictionary<string, StatusSlot>();
        private Image _backgroundImage;
        private Player _trackedPlayer;
        private RectTransform _contentTransform;
        private float _iconSize;
        private float _spacing;
        private float _padding;
        private int _lastActiveCount = -1;

        /// <summary>
        /// Creates a status bar.
        /// </summary>
        public static VeneerStatusBar Create(Transform parent)
        {
            var go = CreateUIObject("VeneerStatusBar", parent);
            var bar = go.AddComponent<VeneerStatusBar>();
            bar.Initialize();
            return bar;
        }

        private void Initialize()
        {
            ElementId = ElementIdStatus;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.HUDOverlay;

            VeneerAnchor.Register(ElementId, ScreenAnchor.TopRight, new Vector2(-10, -80));

            _iconSize = VeneerDimensions.IconSizeMedium;
            _spacing = VeneerDimensions.Spacing;
            _padding = VeneerDimensions.PaddingSmall;

            // Start hidden - will show and resize when effects are active
            float height = _iconSize + _padding * 2;
            SetSize(0, height);
            AnchorTo(AnchorPreset.TopRight, new Vector2(-10, -80));

            // Always solid black background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.BackgroundSolid;

            // Content with horizontal layout
            _contentTransform = CreateUIObject("Content", transform).GetComponent<RectTransform>();
            _contentTransform.anchorMin = Vector2.zero;
            _contentTransform.anchorMax = Vector2.one;
            _contentTransform.offsetMin = new Vector2(_padding, _padding);
            _contentTransform.offsetMax = new Vector2(-_padding, -_padding);

            var layout = _contentTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = _spacing;
            layout.reverseArrangement = true;

            // Create status slots
            for (int i = 0; i < MaxVisibleEffects; i++)
            {
                var slot = CreateStatusSlot(_contentTransform, i, _iconSize);
                slot.Root.SetActive(false);
                _slots.Add(slot);
            }

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }

            // Listen for edit mode changes to show/hide placeholder
            VeneerMover.OnEditModeChanged += OnEditModeChanged;
        }

        protected override void OnDestroy()
        {
            VeneerMover.OnEditModeChanged -= OnEditModeChanged;
        }

        private void OnEditModeChanged(bool isEditMode)
        {
            // Force resize to show/hide placeholder based on edit mode
            _lastActiveCount = -1; // Force update
        }

        private StatusSlot CreateStatusSlot(Transform parent, int index, float size)
        {
            var slotGo = CreateUIObject($"StatusSlot_{index}", parent);
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(size, size);

            // Border for buff/debuff indication
            var borderGo = CreateUIObject("Border", slotGo.transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            var borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Border, Color.clear, 1);
            borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;

            // Icon
            var iconGo = CreateUIObject("Icon", slotGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(1, 1);
            iconRect.offsetMax = new Vector2(-1, -1);

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;

            // Duration text
            var textGo = CreateUIObject("Duration", slotGo.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeTiny);
            text.color = VeneerColors.Text;
            text.alignment = TextAnchor.LowerCenter;
            text.raycastTarget = false;

            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            // Cooldown overlay
            var cooldownGo = CreateUIObject("Cooldown", slotGo.transform);
            var cooldownRect = cooldownGo.GetComponent<RectTransform>();
            cooldownRect.anchorMin = Vector2.zero;
            cooldownRect.anchorMax = Vector2.one;
            cooldownRect.offsetMin = new Vector2(1, 1);
            cooldownRect.offsetMax = new Vector2(-1, -1);

            var cooldownImage = cooldownGo.AddComponent<Image>();
            cooldownImage.type = Image.Type.Filled;
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            cooldownImage.fillOrigin = (int)Image.Origin360.Top;
            cooldownImage.fillClockwise = false;
            cooldownImage.color = new Color(0, 0, 0, 0.6f);
            cooldownImage.fillAmount = 0;

            return new StatusSlot
            {
                Root = slotGo,
                Border = borderImage,
                Icon = iconImage,
                DurationText = text,
                CooldownOverlay = cooldownImage
            };
        }

        private void Update()
        {
            if (_trackedPlayer == null)
            {
                _trackedPlayer = Player.m_localPlayer;
            }

            if (_trackedPlayer == null) return;

            UpdateStatusEffects();
        }

        private void UpdateStatusEffects()
        {
            var statusEffects = _trackedPlayer.GetSEMan()?.GetStatusEffects();
            if (statusEffects == null) return;

            // Hide all slots first
            foreach (var slot in _slots)
            {
                slot.Root.SetActive(false);
            }

            int slotIndex = 0;
            foreach (var effect in statusEffects)
            {
                if (slotIndex >= MaxVisibleEffects) break;
                if (effect.m_icon == null) continue;

                var slot = _slots[slotIndex];
                UpdateSlot(slot, effect);
                slot.Root.SetActive(true);
                slotIndex++;
            }

            // Resize bar to fit only active effects
            if (slotIndex != _lastActiveCount)
            {
                _lastActiveCount = slotIndex;
                ResizeToFitEffects(slotIndex);
            }
        }

        private void ResizeToFitEffects(int effectCount)
        {
            float height = _iconSize + _padding * 2;

            if (effectCount == 0)
            {
                // In edit mode, show a minimum size placeholder so it can be dragged
                if (VeneerMover.EditModeEnabled)
                {
                    float minWidth = _iconSize * 2 + _spacing + _padding * 2;
                    SetSize(minWidth, height);
                    _backgroundImage.enabled = true;
                    _backgroundImage.color = new Color(VeneerColors.BackgroundSolid.r,
                                                        VeneerColors.BackgroundSolid.g,
                                                        VeneerColors.BackgroundSolid.b,
                                                        0.5f); // Semi-transparent to indicate placeholder
                }
                else
                {
                    // Hide completely when no effects
                    SetSize(0, height);
                    _backgroundImage.enabled = false;
                }
            }
            else
            {
                // Calculate exact width needed for active effects
                float width = _iconSize * effectCount + _spacing * (effectCount - 1) + _padding * 2;
                SetSize(width, height);
                _backgroundImage.enabled = true;
                _backgroundImage.color = VeneerColors.BackgroundSolid;
            }
        }

        private void UpdateSlot(StatusSlot slot, StatusEffect effect)
        {
            slot.Icon.sprite = effect.m_icon;

            // Determine if buff or debuff for border color
            bool isDebuff = effect.m_tooltip.ToLower().Contains("damage") ||
                           effect.m_tooltip.ToLower().Contains("cold") ||
                           effect.m_tooltip.ToLower().Contains("wet") ||
                           effect.m_tooltip.ToLower().Contains("poison");

            slot.Border.color = isDebuff ? VeneerColors.Error : VeneerColors.Success;

            // Duration
            float timeRemaining = effect.GetRemaningTime();
            float totalTime = effect.m_ttl;

            if (totalTime > 0 && timeRemaining > 0)
            {
                // Show cooldown spiral
                float percent = timeRemaining / totalTime;
                slot.CooldownOverlay.fillAmount = 1f - percent;

                // Duration text
                if (timeRemaining < 60)
                {
                    slot.DurationText.text = $"{Mathf.CeilToInt(timeRemaining)}";
                }
                else
                {
                    slot.DurationText.text = $"{Mathf.FloorToInt(timeRemaining / 60)}m";
                }
                slot.DurationText.enabled = true;
            }
            else
            {
                slot.CooldownOverlay.fillAmount = 0;
                slot.DurationText.enabled = false;
            }
        }

        private class StatusSlot
        {
            public GameObject Root;
            public Image Border;
            public Image Icon;
            public Text DurationText;
            public Image CooldownOverlay;
        }
    }
}
