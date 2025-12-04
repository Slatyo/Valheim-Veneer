using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Core;
using Veneer.Theme;

namespace Veneer.Components.Primitives
{
    /// <summary>
    /// A row component displaying a crafting requirement with icon, name, and have/need count.
    /// Shows visual feedback for met/unmet requirements.
    /// </summary>
    public class VeneerRequirementRow : VeneerElement
    {
        private Image _backgroundImage;
        private Image _indicatorImage;
        private Image _iconImage;
        private Text _nameText;
        private Text _countText;
        private bool _isMet;

        /// <summary>
        /// Icon for the required item.
        /// </summary>
        public Sprite Icon
        {
            get => _iconImage != null ? _iconImage.sprite : null;
            set
            {
                if (_iconImage != null)
                    _iconImage.sprite = value;
            }
        }

        /// <summary>
        /// Name of the required item.
        /// </summary>
        public string ItemName
        {
            get => _nameText != null ? _nameText.text : string.Empty;
            set
            {
                if (_nameText != null)
                    _nameText.text = value;
            }
        }

        /// <summary>
        /// Whether the requirement is met.
        /// </summary>
        public bool IsMet
        {
            get => _isMet;
            set
            {
                _isMet = value;
                UpdateVisuals();
            }
        }

        /// <summary>
        /// Creates a new VeneerRequirementRow.
        /// </summary>
        public static VeneerRequirementRow Create(Transform parent, string name = "VeneerRequirementRow")
        {
            var go = CreateUIObject(name, parent);
            var row = go.AddComponent<VeneerRequirementRow>();
            row.Initialize();
            return row;
        }

        private void Initialize()
        {
            SetSize(200f, 32f);

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreateButtonSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.BackgroundDark;

            // Left indicator bar
            var indicatorGo = CreateUIObject("Indicator", transform);
            var indicatorRect = indicatorGo.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0, 0);
            indicatorRect.anchorMax = new Vector2(0, 1);
            indicatorRect.pivot = new Vector2(0, 0.5f);
            indicatorRect.anchoredPosition = Vector2.zero;
            indicatorRect.sizeDelta = new Vector2(3, 0);

            _indicatorImage = indicatorGo.AddComponent<Image>();
            _indicatorImage.color = VeneerColors.Success;

            // Icon
            var iconGo = CreateUIObject("Icon", transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(10, 0);
            iconRect.sizeDelta = new Vector2(24, 24);

            _iconImage = iconGo.AddComponent<Image>();
            _iconImage.preserveAspect = true;

            // Item name
            var nameGo = CreateUIObject("Name", transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(40, 0);
            nameRect.offsetMax = new Vector2(-60, 0);

            _nameText = nameGo.AddComponent<Text>();
            _nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _nameText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
            _nameText.color = VeneerColors.Text;
            _nameText.alignment = TextAnchor.MiddleLeft;
            _nameText.raycastTarget = false;

            // Count text (have/need)
            var countGo = CreateUIObject("Count", transform);
            var countRect = countGo.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1, 0);
            countRect.anchorMax = new Vector2(1, 1);
            countRect.pivot = new Vector2(1, 0.5f);
            countRect.anchoredPosition = new Vector2(-8, 0);
            countRect.sizeDelta = new Vector2(50, 0);

            _countText = countGo.AddComponent<Text>();
            _countText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _countText.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeNormal);
            _countText.fontStyle = FontStyle.Bold;
            _countText.color = VeneerColors.Success;
            _countText.alignment = TextAnchor.MiddleRight;
            _countText.raycastTarget = false;

            UpdateVisuals();
        }

        /// <summary>
        /// Sets the requirement data.
        /// </summary>
        /// <param name="icon">Item icon</param>
        /// <param name="itemName">Item name</param>
        /// <param name="have">Amount player has</param>
        /// <param name="need">Amount required</param>
        public void SetRequirement(Sprite icon, string itemName, int have, int need)
        {
            Icon = icon;
            ItemName = itemName;
            _isMet = have >= need;

            if (_countText != null)
            {
                _countText.text = $"{have}/{need}";
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Sets the count display directly.
        /// </summary>
        public void SetCount(int have, int need)
        {
            _isMet = have >= need;

            if (_countText != null)
            {
                _countText.text = $"{have}/{need}";
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_indicatorImage != null)
            {
                _indicatorImage.color = _isMet ? VeneerColors.Success : VeneerColors.Error;
            }

            if (_countText != null)
            {
                _countText.color = _isMet ? VeneerColors.Success : VeneerColors.Error;
            }

            if (_backgroundImage != null)
            {
                // Subtle tint for missing requirements
                _backgroundImage.color = _isMet
                    ? VeneerColors.BackgroundDark
                    : VeneerColors.WithAlpha(VeneerColors.Error, 0.08f);
            }
        }
    }
}
