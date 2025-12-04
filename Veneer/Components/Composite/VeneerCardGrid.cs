using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Theme;

namespace Veneer.Components.Composite
{
    /// <summary>
    /// A scrollable grid container for VeneerCard components.
    /// Automatically arranges cards in a responsive grid layout.
    /// </summary>
    public class VeneerCardGrid : VeneerElement
    {
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private GridLayoutGroup _gridLayout;
        private List<VeneerCard> _cards = new List<VeneerCard>();

        /// <summary>
        /// Cell size for each card in the grid.
        /// </summary>
        public Vector2 CellSize
        {
            get => _gridLayout != null ? _gridLayout.cellSize : Vector2.zero;
            set
            {
                if (_gridLayout != null)
                    _gridLayout.cellSize = value;
            }
        }

        /// <summary>
        /// Spacing between cards.
        /// </summary>
        public Vector2 Spacing
        {
            get => _gridLayout != null ? _gridLayout.spacing : Vector2.zero;
            set
            {
                if (_gridLayout != null)
                    _gridLayout.spacing = value;
            }
        }

        /// <summary>
        /// Padding around the grid content.
        /// </summary>
        public RectOffset Padding
        {
            get => _gridLayout != null ? _gridLayout.padding : new RectOffset();
            set
            {
                if (_gridLayout != null)
                    _gridLayout.padding = value;
            }
        }

        /// <summary>
        /// Number of visible rows before scrolling is needed.
        /// Set to 0 for unlimited (based on available height).
        /// </summary>
        public int MaxVisibleRows { get; set; } = 0;

        /// <summary>
        /// All cards currently in the grid.
        /// </summary>
        public IReadOnlyList<VeneerCard> Cards => _cards;

        /// <summary>
        /// Content transform for adding custom children.
        /// </summary>
        public RectTransform Content => _content;

        /// <summary>
        /// Creates a new VeneerCardGrid.
        /// </summary>
        public static VeneerCardGrid Create(Transform parent, float width = 400f, float height = 300f, string name = "VeneerCardGrid")
        {
            var go = CreateUIObject(name, parent);
            var grid = go.AddComponent<VeneerCardGrid>();
            grid.Initialize(width, height);
            return grid;
        }

        private void Initialize(float width, float height)
        {
            SetSize(width, height);

            // Background
            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundDark;

            // Scroll view setup
            _scrollRect = gameObject.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 30f;

            // Viewport with mask
            var viewportGo = CreateUIObject("Viewport", transform);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var maskImage = viewportGo.AddComponent<Image>();
            maskImage.color = Color.white;

            _scrollRect.viewport = viewportRect;

            // Content with grid layout
            var contentGo = CreateUIObject("Content", viewportGo.transform);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.anchoredPosition = Vector2.zero;

            _gridLayout = contentGo.AddComponent<GridLayoutGroup>();
            _gridLayout.cellSize = new Vector2(140, 120);
            _gridLayout.spacing = new Vector2(12, 12);
            _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            _gridLayout.childAlignment = TextAnchor.UpperCenter;
            _gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            _gridLayout.padding = new RectOffset(8, 8, 8, 8);

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _content;
        }

        /// <summary>
        /// Adds a card to the grid.
        /// </summary>
        public VeneerCard AddCard()
        {
            var card = VeneerCard.Create(_content, CellSize.x, CellSize.y);
            _cards.Add(card);
            return card;
        }

        /// <summary>
        /// Removes a card from the grid.
        /// </summary>
        public void RemoveCard(VeneerCard card)
        {
            if (_cards.Remove(card) && card != null)
            {
                Destroy(card.gameObject);
            }
        }

        /// <summary>
        /// Clears all cards from the grid.
        /// </summary>
        public void ClearCards()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            _cards.Clear();
        }

        /// <summary>
        /// Scrolls to show the specified card.
        /// </summary>
        public void ScrollToCard(VeneerCard card)
        {
            if (card == null || _scrollRect == null) return;

            // Calculate normalized position
            var cardRect = card.RectTransform;
            var contentHeight = _content.rect.height;
            var viewportHeight = _scrollRect.viewport.rect.height;

            if (contentHeight <= viewportHeight) return;

            float cardY = Mathf.Abs(cardRect.anchoredPosition.y);
            float normalizedPos = 1f - (cardY / (contentHeight - viewportHeight));
            normalizedPos = Mathf.Clamp01(normalizedPos);

            _scrollRect.verticalNormalizedPosition = normalizedPos;
        }

        /// <summary>
        /// Scrolls to the top of the grid.
        /// </summary>
        public void ScrollToTop()
        {
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// Sets the constraint to a fixed column count.
        /// </summary>
        public void SetFixedColumnCount(int columns)
        {
            if (_gridLayout != null)
            {
                _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                _gridLayout.constraintCount = columns;
            }
        }

        /// <summary>
        /// Sets the constraint to flexible (auto-fill based on width).
        /// </summary>
        public void SetFlexibleColumns()
        {
            if (_gridLayout != null)
            {
                _gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            }
        }
    }
}
