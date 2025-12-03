using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Skills panel replacement.
    /// Features large icons, circular XP progress, and level badges.
    /// </summary>
    public class VeneerSkillsPanel : VeneerElement
    {
        private const string ElementIdSkills = "Veneer_Skills";

        private Image _backgroundImage;
        private Image _borderImage;
        private VeneerText _titleText;
        private VeneerButton _closeButton;
        private RectTransform _skillsContent;
        private ScrollRect _scrollRect;

        private List<SkillCard> _skillCards = new List<SkillCard>();
        private Player _player;

        // Dragging
        private bool _isDragging;
        private Vector2 _dragOffset;

        /// <summary>
        /// Creates the skills panel.
        /// </summary>
        public static VeneerSkillsPanel Create(Transform parent)
        {
            var go = CreateUIObject("VeneerSkillsPanel", parent);
            var panel = go.AddComponent<VeneerSkillsPanel>();
            panel.Initialize();
            return panel;
        }

        private void Initialize()
        {
            ElementId = ElementIdSkills;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.Window;
            AutoRegisterWithManager = true;

            VeneerAnchor.Register(ElementId, ScreenAnchor.Center, Vector2.zero);

            float width = 500f;
            float height = 520f;
            float padding = VeneerDimensions.PaddingLarge;
            float headerHeight = 35f;

            SetSize(width, height);
            AnchorTo(AnchorPreset.MiddleCenter);

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

            // Header with drag handle
            var headerGo = CreateUIObject("Header", transform);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, headerHeight);

            var headerBg = headerGo.AddComponent<Image>();
            headerBg.color = VeneerColors.BackgroundDark;

            // Add drag handler to header
            var dragHandler = headerGo.AddComponent<SkillsPanelDragHandler>();
            dragHandler.Target = this;

            // Title
            var titleGo = CreateUIObject("Title", headerGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(padding, 0);
            titleRect.offsetMax = new Vector2(-50, 0);

            _titleText = titleGo.AddComponent<VeneerText>();
            _titleText.Content = "Skills";
            _titleText.ApplyStyle(TextStyle.Header);
            _titleText.Alignment = TextAnchor.MiddleCenter;

            // Close button
            _closeButton = VeneerButton.Create(headerGo.transform, "X", () => Hide());
            _closeButton.SetButtonSize(ButtonSize.Small);
            _closeButton.SetStyle(ButtonStyle.Ghost);
            var closeRect = _closeButton.RectTransform;
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-8, 0);
            closeRect.sizeDelta = new Vector2(28, 24);

            // Scroll view
            var scrollGo = CreateUIObject("ScrollView", transform);
            var scrollViewRect = scrollGo.GetComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.offsetMin = new Vector2(padding, padding);
            scrollViewRect.offsetMax = new Vector2(-padding, -padding - headerHeight - 5);

            _scrollRect = scrollGo.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 30f;

            // Viewport
            var viewportGo = CreateUIObject("Viewport", scrollGo.transform);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportMask = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.white;

            _scrollRect.viewport = viewportRect;

            // Content - Grid layout for skill cards
            var contentGo = CreateUIObject("Content", viewportGo.transform);
            _skillsContent = contentGo.GetComponent<RectTransform>();
            _skillsContent.anchorMin = new Vector2(0, 1);
            _skillsContent.anchorMax = new Vector2(1, 1);
            _skillsContent.pivot = new Vector2(0.5f, 1);
            _skillsContent.anchoredPosition = Vector2.zero;

            // Use GridLayoutGroup for card-based layout
            var contentLayout = contentGo.AddComponent<GridLayoutGroup>();
            contentLayout.cellSize = new Vector2(140, 100); // Card size
            contentLayout.spacing = new Vector2(10, 10);
            contentLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            contentLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            contentLayout.constraintCount = 3; // 3 columns

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _skillsContent;

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(350, 350);
            resizer.MaxSize = new Vector2(800, 900);

            // Start hidden - must register BEFORE SetActive(false) since Start() won't be called
            RegisterWithManager();
            gameObject.SetActive(false);

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }
        }

        /// <summary>
        /// Shows the skills panel.
        /// </summary>
        public override void Show()
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            UpdateSkills();
            base.Show(); // Fire OnShow event and set visibility
        }

        /// <summary>
        /// Hides the skills panel.
        /// </summary>
        public override void Hide()
        {
            base.Hide(); // Fire OnHide event and set visibility
        }

        private void UpdateSkills()
        {
            if (_player == null) return;

            // Clear existing cards
            foreach (var card in _skillCards)
            {
                Destroy(card.Root);
            }
            _skillCards.Clear();

            // Get all skills and sort by level (highest first)
            var skills = _player.GetSkills().GetSkillList()
                .OrderByDescending(s => s.m_level)
                .ToList();

            foreach (var skill in skills)
            {
                var card = CreateSkillCard(skill);
                _skillCards.Add(card);
            }
        }

        private SkillCard CreateSkillCard(Skills.Skill skill)
        {
            var cardGo = CreateUIObject("SkillCard", _skillsContent);

            // Card background
            var bgImage = cardGo.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundLight;

            // Card border
            var borderGo = CreateUIObject("CardBorder", cardGo.transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(8, VeneerColors.Border, Color.clear, 1);
            borderImg.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;

            // Icon container (centered at top)
            var iconContainer = CreateUIObject("IconContainer", cardGo.transform);
            var iconContainerRect = iconContainer.GetComponent<RectTransform>();
            iconContainerRect.anchorMin = new Vector2(0.5f, 1);
            iconContainerRect.anchorMax = new Vector2(0.5f, 1);
            iconContainerRect.pivot = new Vector2(0.5f, 1);
            iconContainerRect.anchoredPosition = new Vector2(0, -8);
            iconContainerRect.sizeDelta = new Vector2(48, 48);

            // Icon background (dark circle effect)
            var iconBgGo = CreateUIObject("IconBg", iconContainer.transform);
            var iconBgRect = iconBgGo.GetComponent<RectTransform>();
            iconBgRect.anchorMin = Vector2.zero;
            iconBgRect.anchorMax = Vector2.one;
            iconBgRect.offsetMin = Vector2.zero;
            iconBgRect.offsetMax = Vector2.zero;
            var iconBgImg = iconBgGo.AddComponent<Image>();
            iconBgImg.color = VeneerColors.BackgroundDark;

            // Skill icon
            var iconGo = CreateUIObject("Icon", iconContainer.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = skill.m_info.m_icon;
            iconImage.preserveAspect = true;

            // Level badge (gold circle in corner)
            var levelBadge = CreateUIObject("LevelBadge", iconContainer.transform);
            var levelBadgeRect = levelBadge.GetComponent<RectTransform>();
            levelBadgeRect.anchorMin = new Vector2(1, 0);
            levelBadgeRect.anchorMax = new Vector2(1, 0);
            levelBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            levelBadgeRect.anchoredPosition = new Vector2(2, 2);
            levelBadgeRect.sizeDelta = new Vector2(22, 22);

            var levelBadgeBg = levelBadge.AddComponent<Image>();
            levelBadgeBg.color = VeneerColors.Accent;

            var levelTextGo = CreateUIObject("LevelText", levelBadge.transform);
            var levelTextRect = levelTextGo.GetComponent<RectTransform>();
            levelTextRect.anchorMin = Vector2.zero;
            levelTextRect.anchorMax = Vector2.one;
            levelTextRect.offsetMin = Vector2.zero;
            levelTextRect.offsetMax = Vector2.zero;

            var levelText = levelTextGo.AddComponent<Text>();
            levelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            levelText.fontSize = VeneerConfig.GetScaledFontSize(10);
            levelText.fontStyle = FontStyle.Bold;
            levelText.color = VeneerColors.BackgroundDark;
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.text = Mathf.FloorToInt(skill.m_level).ToString();

            // Skill name
            var nameGo = CreateUIObject("Name", cardGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -2);
            nameRect.sizeDelta = new Vector2(-10, 18);

            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = VeneerConfig.GetScaledFontSize(11);
            nameText.color = VeneerColors.Text;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.text = Localization.instance.Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower());

            // XP Progress bar (horizontal at bottom)
            var progressContainer = CreateUIObject("ProgressContainer", cardGo.transform);
            var progressContainerRect = progressContainer.GetComponent<RectTransform>();
            progressContainerRect.anchorMin = new Vector2(0, 0);
            progressContainerRect.anchorMax = new Vector2(1, 0);
            progressContainerRect.pivot = new Vector2(0.5f, 0);
            progressContainerRect.anchoredPosition = new Vector2(0, 8);
            progressContainerRect.sizeDelta = new Vector2(-16, 10);

            // Progress background
            var progressBg = progressContainer.AddComponent<Image>();
            progressBg.color = VeneerColors.BackgroundDark;

            // Progress fill
            var progressFillGo = CreateUIObject("Fill", progressContainer.transform);
            var progressFillRect = progressFillGo.GetComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = new Vector2(0, 1);
            progressFillRect.pivot = new Vector2(0, 0.5f);
            progressFillRect.offsetMin = new Vector2(1, 1);
            progressFillRect.offsetMax = new Vector2(0, -1);

            var progressFill = progressFillGo.AddComponent<Image>();
            progressFill.color = VeneerColors.Accent;

            // Calculate progress to next level
            float levelProgress = skill.m_level - Mathf.Floor(skill.m_level);
            progressFillRect.anchorMax = new Vector2(levelProgress, 1);

            // Progress percentage text
            var progressTextGo = CreateUIObject("ProgressText", progressContainer.transform);
            var progressTextRect = progressTextGo.GetComponent<RectTransform>();
            progressTextRect.anchorMin = Vector2.zero;
            progressTextRect.anchorMax = Vector2.one;
            progressTextRect.offsetMin = Vector2.zero;
            progressTextRect.offsetMax = Vector2.zero;

            var progressText = progressTextGo.AddComponent<Text>();
            progressText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            progressText.fontSize = VeneerConfig.GetScaledFontSize(8);
            progressText.color = VeneerColors.Text;
            progressText.alignment = TextAnchor.MiddleCenter;
            progressText.text = $"{(levelProgress * 100):F0}%";

            return new SkillCard
            {
                Root = cardGo,
                Icon = iconImage,
                NameText = nameText,
                LevelText = levelText,
                ProgressFill = progressFillRect,
                ProgressText = progressText
            };
        }

        private class SkillCard
        {
            public GameObject Root;
            public Image Icon;
            public Text NameText;
            public Text LevelText;
            public RectTransform ProgressFill;
            public Text ProgressText;
        }

        // Drag handling for the panel
        internal void OnBeginPanelDrag(PointerEventData eventData)
        {
            _isDragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);
            _dragOffset = RectTransform.anchoredPosition - localPoint;
        }

        internal void OnPanelDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            RectTransform.anchoredPosition = localPoint + _dragOffset;
        }

        internal void OnEndPanelDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// Helper component for dragging the skills panel by its header.
    /// </summary>
    public class SkillsPanelDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public VeneerSkillsPanel Target { get; set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Target?.OnBeginPanelDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Target?.OnPanelDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Target?.OnEndPanelDrag(eventData);
        }
    }
}
