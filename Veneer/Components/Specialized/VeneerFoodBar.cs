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
    /// Food slots display showing active food items and their timers.
    /// </summary>
    public class VeneerFoodBar : VeneerElement
    {
        private const string ElementIdFood = "Veneer_FoodBar";
        private const int MaxFoodSlots = 3;

        private List<FoodSlot> _foodSlots = new List<FoodSlot>();
        private Image _backgroundImage;
        private Image _borderImage;
        private Player _trackedPlayer;

        /// <summary>
        /// Creates a food bar.
        /// </summary>
        public static VeneerFoodBar Create(Transform parent)
        {
            var go = CreateUIObject("VeneerFoodBar", parent);
            var bar = go.AddComponent<VeneerFoodBar>();
            bar.Initialize();
            return bar;
        }

        private void Initialize()
        {
            ElementId = ElementIdFood;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.HUD;

            // Position at top-right, left of minimap (minimap is ~200 wide + 10 margin)
            float minimapOffset = VeneerConfig.MinimapSize.Value + 20f;
            VeneerAnchor.Register(ElementId, ScreenAnchor.TopRight, new Vector2(-minimapOffset - 10, -10));

            float slotSize = 32f; // Compact slot size
            float spacing = 2f;
            float padding = 4f;

            float width = slotSize * MaxFoodSlots + spacing * (MaxFoodSlots - 1) + padding * 2;
            float height = slotSize + padding * 2;

            SetSize(width, height);
            AnchorTo(AnchorPreset.TopRight, new Vector2(-minimapOffset - 10, -10));

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

            // Content with horizontal layout
            var content = CreateUIObject("Content", transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);

            var layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = spacing;

            // Create food slots
            for (int i = 0; i < MaxFoodSlots; i++)
            {
                var slot = CreateFoodSlot(content.transform, i, slotSize);
                _foodSlots.Add(slot);
            }

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(80, 30);
            resizer.MaxSize = new Vector2(300, 100);

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }
        }

        private FoodSlot CreateFoodSlot(Transform parent, int index, float size)
        {
            var slotGo = CreateUIObject($"FoodSlot_{index}", parent);
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(size, size);

            // Slot background
            var bgImage = slotGo.AddComponent<Image>();
            bgImage.sprite = VeneerTextures.CreateSlotSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = VeneerColors.SlotEmpty;

            // Icon
            var iconGo = CreateUIObject("Icon", slotGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2, 2);
            iconRect.offsetMax = new Vector2(-2, -2);

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.enabled = false;

            // Timer overlay (darkens as food expires)
            var timerGo = CreateUIObject("Timer", slotGo.transform);
            var timerRect = timerGo.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0, 0);
            timerRect.anchorMax = new Vector2(1, 0);
            timerRect.pivot = new Vector2(0.5f, 0);
            timerRect.offsetMin = new Vector2(2, 2);
            timerRect.offsetMax = new Vector2(-2, 2);
            timerRect.sizeDelta = new Vector2(0, 0);

            var timerImage = timerGo.AddComponent<Image>();
            timerImage.color = new Color(0, 0, 0, 0.6f);

            // Timer text
            var textGo = CreateUIObject("Text", slotGo.transform);
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

            return new FoodSlot
            {
                Root = slotGo,
                Background = bgImage,
                Icon = iconImage,
                TimerOverlay = timerImage,
                TimerRect = timerRect,
                TimerText = text
            };
        }

        private void Update()
        {
            if (_trackedPlayer == null)
            {
                _trackedPlayer = Player.m_localPlayer;
            }

            if (_trackedPlayer == null) return;

            UpdateFoodSlots();
        }

        private void UpdateFoodSlots()
        {
            var foods = _trackedPlayer.GetFoods();

            for (int i = 0; i < MaxFoodSlots; i++)
            {
                var slot = _foodSlots[i];

                if (i < foods.Count)
                {
                    var food = foods[i];
                    UpdateSlot(slot, food);
                }
                else
                {
                    ClearSlot(slot);
                }
            }
        }

        private void UpdateSlot(FoodSlot slot, Player.Food food)
        {
            // Show icon
            if (food.m_item?.m_shared?.m_icons != null && food.m_item.m_shared.m_icons.Length > 0)
            {
                slot.Icon.sprite = food.m_item.m_shared.m_icons[0];
                slot.Icon.enabled = true;
            }

            // Calculate time remaining
            float timeRemaining = food.m_time;
            float totalTime = food.m_item.m_shared.m_foodBurnTime;
            float percent = totalTime > 0 ? timeRemaining / totalTime : 0;

            // Update timer overlay (fills from bottom as food expires)
            float overlayHeight = (1f - percent) * (slot.Root.GetComponent<RectTransform>().sizeDelta.y - 4);
            slot.TimerRect.sizeDelta = new Vector2(0, overlayHeight);

            // Update timer text
            if (timeRemaining < 120) // Show time when less than 2 minutes
            {
                int seconds = Mathf.FloorToInt(timeRemaining);
                slot.TimerText.text = $"{seconds}s";
                slot.TimerText.enabled = true;
            }
            else
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                slot.TimerText.text = $"{minutes}m";
                slot.TimerText.enabled = true;
            }

            // Color based on time remaining
            if (percent < 0.2f)
            {
                slot.Background.color = VeneerColors.Darken(VeneerColors.SlotEmpty, 0.2f);
                slot.TimerText.color = VeneerColors.Error;
            }
            else
            {
                slot.Background.color = VeneerColors.SlotEmpty;
                slot.TimerText.color = VeneerColors.Text;
            }
        }

        private void ClearSlot(FoodSlot slot)
        {
            slot.Icon.enabled = false;
            slot.TimerText.enabled = false;
            slot.TimerRect.sizeDelta = Vector2.zero;
            slot.Background.color = VeneerColors.SlotEmpty;
        }

        private class FoodSlot
        {
            public GameObject Root;
            public Image Background;
            public Image Icon;
            public Image TimerOverlay;
            public RectTransform TimerRect;
            public Text TimerText;
        }
    }
}
