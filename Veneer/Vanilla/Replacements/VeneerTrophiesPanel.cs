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
    /// Trophies panel replacement.
    /// Shows all trophies with monster names and collection status.
    /// </summary>
    public class VeneerTrophiesPanel : VeneerElement
    {
        private const string ElementIdTrophies = "Veneer_Trophies";

        private Image _backgroundImage;
        private Image _borderImage;
        private VeneerText _titleText;
        private VeneerText _countText;
        private VeneerButton _closeButton;
        private RectTransform _trophiesContent;
        private ScrollRect _scrollRect;

        private List<TrophyCard> _trophyCards = new List<TrophyCard>();
        private Player _player;

        // Dragging
        private bool _isDragging;
        private Vector2 _dragOffset;

        /// <summary>
        /// Creates the trophies panel.
        /// </summary>
        public static VeneerTrophiesPanel Create(Transform parent)
        {
            var go = CreateUIObject("VeneerTrophiesPanel", parent);
            var panel = go.AddComponent<VeneerTrophiesPanel>();
            panel.Initialize();
            return panel;
        }

        private void Initialize()
        {
            ElementId = ElementIdTrophies;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.Window;
            AutoRegisterWithManager = true;

            VeneerAnchor.Register(ElementId, ScreenAnchor.Center, Vector2.zero);

            float width = 550f;
            float height = 550f;
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
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0, headerHeight);

            var headerBg = headerGo.AddComponent<Image>();
            headerBg.color = VeneerColors.BackgroundDark;

            // Add drag handler to header
            var dragHandler = headerGo.AddComponent<TrophiesPanelDragHandler>();
            dragHandler.Target = this;

            // Title
            var titleGo = CreateUIObject("Title", headerGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.offsetMin = new Vector2(padding, 0);
            titleRect.offsetMax = Vector2.zero;

            _titleText = titleGo.AddComponent<VeneerText>();
            _titleText.Content = "Trophies";
            _titleText.ApplyStyle(TextStyle.Header);
            _titleText.Alignment = TextAnchor.MiddleLeft;

            // Count text
            var countGo = CreateUIObject("Count", headerGo.transform);
            var countRect = countGo.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0);
            countRect.anchorMax = new Vector2(1, 1);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = new Vector2(-50, 0);

            _countText = countGo.AddComponent<VeneerText>();
            _countText.Content = "0 / 0";
            _countText.ApplyStyle(TextStyle.Gold);
            _countText.Alignment = TextAnchor.MiddleRight;

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

            // Content - Grid layout for trophy cards
            var contentGo = CreateUIObject("Content", viewportGo.transform);
            _trophiesContent = contentGo.GetComponent<RectTransform>();
            _trophiesContent.anchorMin = new Vector2(0, 1);
            _trophiesContent.anchorMax = new Vector2(1, 1);
            _trophiesContent.pivot = new Vector2(0.5f, 1);
            _trophiesContent.anchoredPosition = Vector2.zero;

            var contentLayout = contentGo.AddComponent<GridLayoutGroup>();
            contentLayout.cellSize = new Vector2(160, 90); // Card size for trophy + name
            contentLayout.spacing = new Vector2(8, 8);
            contentLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            contentLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            contentLayout.constraintCount = 3;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _trophiesContent;

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(400, 400);
            resizer.MaxSize = new Vector2(900, 900);

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
        /// Shows the trophies panel.
        /// </summary>
        public override void Show()
        {
            _player = Player.m_localPlayer;
            if (_player == null) return;

            UpdateTrophies();
            base.Show(); // Fire OnShow event and set visibility
        }

        /// <summary>
        /// Hides the trophies panel.
        /// </summary>
        public override void Hide()
        {
            base.Hide(); // Fire OnHide event and set visibility
        }

        private void UpdateTrophies()
        {
            if (_player == null) return;

            // Clear existing cards
            foreach (var card in _trophyCards)
            {
                Destroy(card.Root);
            }
            _trophyCards.Clear();

            // Get all trophies from ObjectDB
            var objectDB = ObjectDB.instance;
            if (objectDB == null) return;

            // Get player's collected trophies
            var collectedTrophies = new HashSet<string>();
            var uniqueNames = _player.GetTrophies();
            foreach (var name in uniqueNames)
            {
                collectedTrophies.Add(name);
            }

            // Find all trophy items and sort (collected first, then alphabetically)
            var trophyItems = new List<(ItemDrop.ItemData data, string prefabName, bool collected)>();

            foreach (var prefab in objectDB.m_items)
            {
                if (prefab == null) continue;

                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null) continue;

                var itemData = itemDrop.m_itemData;
                if (itemData == null) continue;

                // Check if this is a trophy item
                if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                {
                    bool isCollected = collectedTrophies.Contains(prefab.name);
                    trophyItems.Add((itemData, prefab.name, isCollected));
                }
            }

            // Sort: collected first, then by name
            trophyItems = trophyItems
                .OrderByDescending(t => t.collected)
                .ThenBy(t => Localization.instance.Localize(t.data.m_shared.m_name))
                .ToList();

            int collectedCount = trophyItems.Count(t => t.collected);
            _countText.Content = $"{collectedCount} / {trophyItems.Count}";

            foreach (var trophy in trophyItems)
            {
                var card = CreateTrophyCard(trophy.data, trophy.prefabName, trophy.collected);
                _trophyCards.Add(card);
            }
        }

        private TrophyCard CreateTrophyCard(ItemDrop.ItemData itemData, string prefabName, bool isCollected)
        {
            var cardGo = CreateUIObject("TrophyCard", _trophiesContent);

            // Card background - different color for collected vs not
            var bgImage = cardGo.AddComponent<Image>();
            bgImage.color = isCollected ? VeneerColors.BackgroundLight : new Color(0.05f, 0.05f, 0.05f, 0.9f);

            // Card border - gold for collected
            var borderGo = CreateUIObject("CardBorder", cardGo.transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGo.AddComponent<Image>();
            var borderColor = isCollected ? VeneerColors.Accent : VeneerColors.Border;
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(8, borderColor, Color.clear, isCollected ? 2 : 1);
            borderImg.sprite = VeneerTextures.CreateSlicedSprite(borderTex, isCollected ? 2 : 1);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;

            // Icon area (left side)
            var iconContainer = CreateUIObject("IconContainer", cardGo.transform);
            var iconContainerRect = iconContainer.GetComponent<RectTransform>();
            iconContainerRect.anchorMin = new Vector2(0, 0.25f);
            iconContainerRect.anchorMax = new Vector2(0.45f, 1);
            iconContainerRect.offsetMin = new Vector2(8, 0);
            iconContainerRect.offsetMax = new Vector2(0, -8);

            // Icon background
            var iconBgGo = CreateUIObject("IconBg", iconContainer.transform);
            var iconBgRect = iconBgGo.GetComponent<RectTransform>();
            iconBgRect.anchorMin = Vector2.zero;
            iconBgRect.anchorMax = Vector2.one;
            iconBgRect.offsetMin = Vector2.zero;
            iconBgRect.offsetMax = Vector2.zero;
            var iconBgImg = iconBgGo.AddComponent<Image>();
            iconBgImg.color = VeneerColors.BackgroundDark;

            // Trophy icon
            var iconGo = CreateUIObject("Icon", iconContainer.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = itemData.m_shared.m_icons != null && itemData.m_shared.m_icons.Length > 0
                ? itemData.m_shared.m_icons[0]
                : null;
            iconImage.preserveAspect = true;

            // Dim if not collected
            if (!isCollected)
            {
                iconImage.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            }

            // Text area (right side)
            var textContainer = CreateUIObject("TextContainer", cardGo.transform);
            var textContainerRect = textContainer.GetComponent<RectTransform>();
            textContainerRect.anchorMin = new Vector2(0.45f, 0);
            textContainerRect.anchorMax = new Vector2(1, 1);
            textContainerRect.offsetMin = new Vector2(4, 8);
            textContainerRect.offsetMax = new Vector2(-8, -8);

            // Trophy name
            var nameGo = CreateUIObject("Name", textContainer.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var nameText = nameGo.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = VeneerConfig.GetScaledFontSize(11);
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = isCollected ? VeneerColors.TextGold : VeneerColors.TextMuted;
            nameText.alignment = TextAnchor.MiddleLeft;

            // Get localized trophy name
            string trophyName = Localization.instance.Localize(itemData.m_shared.m_name);
            nameText.text = trophyName;

            // Monster name (derived from trophy name)
            var monsterGo = CreateUIObject("Monster", textContainer.transform);
            var monsterRect = monsterGo.GetComponent<RectTransform>();
            monsterRect.anchorMin = new Vector2(0, 0);
            monsterRect.anchorMax = new Vector2(1, 0.5f);
            monsterRect.offsetMin = Vector2.zero;
            monsterRect.offsetMax = Vector2.zero;

            var monsterText = monsterGo.AddComponent<Text>();
            monsterText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            monsterText.fontSize = VeneerConfig.GetScaledFontSize(9);
            monsterText.color = isCollected ? VeneerColors.Text : VeneerColors.TextMuted;
            monsterText.alignment = TextAnchor.MiddleLeft;

            // Try to extract monster name from trophy name or prefab name
            string monsterName = GetMonsterName(trophyName, prefabName);
            monsterText.text = monsterName;

            // Collected indicator
            if (isCollected)
            {
                var checkGo = CreateUIObject("Check", cardGo.transform);
                var checkRect = checkGo.GetComponent<RectTransform>();
                checkRect.anchorMin = new Vector2(1, 1);
                checkRect.anchorMax = new Vector2(1, 1);
                checkRect.pivot = new Vector2(1, 1);
                checkRect.anchoredPosition = new Vector2(-4, -4);
                checkRect.sizeDelta = new Vector2(16, 16);

                var checkBg = checkGo.AddComponent<Image>();
                checkBg.color = VeneerColors.Success;

                var checkTextGo = CreateUIObject("CheckText", checkGo.transform);
                var checkTextRect = checkTextGo.GetComponent<RectTransform>();
                checkTextRect.anchorMin = Vector2.zero;
                checkTextRect.anchorMax = Vector2.one;
                checkTextRect.offsetMin = Vector2.zero;
                checkTextRect.offsetMax = Vector2.zero;

                var checkText = checkTextGo.AddComponent<Text>();
                checkText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                checkText.fontSize = VeneerConfig.GetScaledFontSize(10);
                checkText.fontStyle = FontStyle.Bold;
                checkText.color = Color.white;
                checkText.alignment = TextAnchor.MiddleCenter;
                checkText.text = "✓";
            }

            return new TrophyCard
            {
                Root = cardGo,
                Icon = iconImage,
                NameText = nameText,
                MonsterText = monsterText,
                IsCollected = isCollected
            };
        }

        private string GetMonsterName(string trophyName, string prefabName)
        {
            // Try to derive monster name from trophy name
            // Common patterns: "Boar trophy" -> "Boar", "Neck trophy" -> "Neck"
            string name = trophyName;

            // Remove common suffixes
            string[] suffixes = { " trophy", " Trophy", " head", " Head", "'s head", "'s trophy" };
            foreach (var suffix in suffixes)
            {
                if (name.EndsWith(suffix))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    break;
                }
            }

            // If that didn't work, try to use prefab name
            if (name == trophyName)
            {
                // Prefab names like "TrophyBoar", "TrophyNeck"
                if (prefabName.StartsWith("Trophy"))
                {
                    name = prefabName.Substring(6); // Remove "Trophy" prefix

                    // Add spaces before capitals
                    var result = new System.Text.StringBuilder();
                    foreach (char c in name)
                    {
                        if (char.IsUpper(c) && result.Length > 0)
                        {
                            result.Append(' ');
                        }
                        result.Append(c);
                    }
                    name = result.ToString();
                }
            }

            return $"Dropped by: {name}";
        }

        private class TrophyCard
        {
            public GameObject Root;
            public Image Icon;
            public Text NameText;
            public Text MonsterText;
            public bool IsCollected;
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
    /// Helper component for dragging the trophies panel by its header.
    /// </summary>
    public class TrophiesPanelDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public VeneerTrophiesPanel Target { get; set; }

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
