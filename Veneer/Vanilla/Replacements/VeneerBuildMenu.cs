using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Primitives;
using Veneer.Components.Composite;
using Veneer.Core;
using Veneer.Grid;
using Veneer.Theme;

namespace Veneer.Vanilla.Replacements
{
    /// <summary>
    /// Build menu replacement.
    /// Shows buildable pieces in a grid with category tabs.
    /// </summary>
    public class VeneerBuildMenu : VeneerElement
    {
        private const string ElementIdBuild = "Veneer_BuildMenu";

        private Image _backgroundImage;
        private Image _borderImage;
        private RectTransform _tabBar;
        private RectTransform _piecesContent;
        private ScrollRect _scrollRect;
        private VeneerText _selectedPieceText;

        private List<TabButton> _categoryTabs = new List<TabButton>();
        private List<PieceButton> _pieceButtons = new List<PieceButton>();

        private Player _player;
        private PieceTable _pieceTable;
        private int _selectedCategory;
        private Piece.PieceCategory[] _categories;

        /// <summary>
        /// Creates the build menu.
        /// </summary>
        public static VeneerBuildMenu Create(Transform parent)
        {
            var go = CreateUIObject("VeneerBuildMenu", parent);
            var menu = go.AddComponent<VeneerBuildMenu>();
            menu.Initialize();
            return menu;
        }

        private void Initialize()
        {
            ElementId = ElementIdBuild;
            IsMoveable = true;
            SavePosition = true;

            VeneerAnchor.Register(ElementId, ScreenAnchor.BottomCenter, new Vector2(0, 100));

            float width = 500f;
            float height = 200f;
            float padding = VeneerDimensions.Padding;
            float tabHeight = 28f;

            SetSize(width, height);
            AnchorTo(AnchorPreset.BottomCenter, new Vector2(0, 100));

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

            // Tab bar at top
            var tabBarGo = CreateUIObject("TabBar", transform);
            _tabBar = tabBarGo.GetComponent<RectTransform>();
            _tabBar.anchorMin = new Vector2(0, 1);
            _tabBar.anchorMax = new Vector2(1, 1);
            _tabBar.pivot = new Vector2(0.5f, 1);
            _tabBar.offsetMin = new Vector2(padding, 0);
            _tabBar.offsetMax = new Vector2(-padding, -padding);
            _tabBar.sizeDelta = new Vector2(0, tabHeight);

            var tabLayout = tabBarGo.AddComponent<HorizontalLayoutGroup>();
            tabLayout.childAlignment = TextAnchor.MiddleLeft;
            tabLayout.childControlWidth = false;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = true;
            tabLayout.spacing = VeneerDimensions.Spacing;

            // Pieces scroll view
            var scrollGo = CreateUIObject("PiecesScroll", transform);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(padding, padding + 24);
            scrollRect.offsetMax = new Vector2(-padding, -padding - tabHeight - VeneerDimensions.Spacing);

            _scrollRect = scrollGo.AddComponent<ScrollRect>();
            _scrollRect.horizontal = true;
            _scrollRect.vertical = false;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 20f;

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

            // Content
            var contentGo = CreateUIObject("Content", viewportGo.transform);
            _piecesContent = contentGo.GetComponent<RectTransform>();
            _piecesContent.anchorMin = new Vector2(0, 0);
            _piecesContent.anchorMax = new Vector2(0, 1);
            _piecesContent.pivot = new Vector2(0, 0.5f);
            _piecesContent.anchoredPosition = Vector2.zero;

            var contentLayout = contentGo.AddComponent<HorizontalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.childControlWidth = false;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = VeneerDimensions.Spacing;
            contentLayout.padding = new RectOffset(0, 0, 0, 0);

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            _scrollRect.content = _piecesContent;

            // Selected piece text at bottom
            var selectedGo = CreateUIObject("SelectedPiece", transform);
            var selectedRect = selectedGo.GetComponent<RectTransform>();
            selectedRect.anchorMin = new Vector2(0, 0);
            selectedRect.anchorMax = new Vector2(1, 0);
            selectedRect.pivot = new Vector2(0.5f, 0);
            selectedRect.offsetMin = new Vector2(padding, padding);
            selectedRect.offsetMax = new Vector2(-padding, padding + 20);

            _selectedPieceText = selectedGo.AddComponent<VeneerText>();
            _selectedPieceText.Content = "";
            _selectedPieceText.ApplyStyle(TextStyle.Caption);
            _selectedPieceText.Alignment = TextAnchor.MiddleCenter;

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Start hidden
            gameObject.SetActive(false);

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }
        }

        /// <summary>
        /// Shows the build menu for a piece table.
        /// </summary>
        public void Show(PieceTable pieceTable)
        {
            _player = Player.m_localPlayer;
            if (_player == null || pieceTable == null) return;

            _pieceTable = pieceTable;
            _selectedCategory = 0;

            CreateCategoryTabs();
            UpdatePieces();

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the build menu.
        /// </summary>
        public override void Hide()
        {
            gameObject.SetActive(false);
            _pieceTable = null;
        }

        private void CreateCategoryTabs()
        {
            // Clear existing tabs
            foreach (var tab in _categoryTabs)
            {
                Destroy(tab.Root);
            }
            _categoryTabs.Clear();

            if (_pieceTable == null) return;

            // Get unique categories from pieces
            var categories = new HashSet<Piece.PieceCategory>();
            foreach (var piece in _pieceTable.m_pieces)
            {
                if (piece != null)
                {
                    var pieceComp = piece.GetComponent<Piece>();
                    if (pieceComp != null)
                    {
                        categories.Add(pieceComp.m_category);
                    }
                }
            }

            _categories = new Piece.PieceCategory[categories.Count];
            categories.CopyTo(_categories);
            System.Array.Sort(_categories);

            // Create tabs
            for (int i = 0; i < _categories.Length; i++)
            {
                var category = _categories[i];
                var tab = CreateCategoryTab(category, i);
                _categoryTabs.Add(tab);
            }

            UpdateTabVisuals();
        }

        private TabButton CreateCategoryTab(Piece.PieceCategory category, int index)
        {
            var tabGo = CreateUIObject($"Tab_{category}", _tabBar);
            var tabRect = tabGo.GetComponent<RectTransform>();

            var layoutElement = tabGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 70;

            var bgImage = tabGo.AddComponent<Image>();
            bgImage.color = VeneerColors.BackgroundLight;

            var button = tabGo.AddComponent<Button>();
            button.targetGraphic = bgImage;

            int capturedIndex = index;
            button.onClick.AddListener(() => SelectCategory(capturedIndex));

            // Tab text
            var textGo = CreateUIObject("Text", tabGo.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = VeneerConfig.GetScaledFontSize(VeneerDimensions.FontSizeSmall);
            text.color = VeneerColors.Text;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = GetCategoryName(category);
            text.raycastTarget = false;

            return new TabButton
            {
                Root = tabGo,
                Background = bgImage,
                Text = text,
                Category = category
            };
        }

        private void SelectCategory(int index)
        {
            _selectedCategory = index;
            UpdateTabVisuals();
            UpdatePieces();
        }

        private void UpdateTabVisuals()
        {
            for (int i = 0; i < _categoryTabs.Count; i++)
            {
                var tab = _categoryTabs[i];
                bool isSelected = i == _selectedCategory;
                tab.Background.color = isSelected ? VeneerColors.Accent : VeneerColors.BackgroundLight;
                tab.Text.color = isSelected ? VeneerColors.BackgroundSolid : VeneerColors.Text;
            }
        }

        private void UpdatePieces()
        {
            // Clear existing pieces
            foreach (var btn in _pieceButtons)
            {
                Destroy(btn.Root);
            }
            _pieceButtons.Clear();

            if (_pieceTable == null || _categories == null || _selectedCategory >= _categories.Length) return;

            var selectedCategory = _categories[_selectedCategory];

            foreach (var pieceGo in _pieceTable.m_pieces)
            {
                if (pieceGo == null) continue;

                var piece = pieceGo.GetComponent<Piece>();
                if (piece == null || piece.m_category != selectedCategory) continue;

                var btn = CreatePieceButton(piece);
                _pieceButtons.Add(btn);
            }
        }

        private PieceButton CreatePieceButton(Piece piece)
        {
            float size = 48f;

            var btnGo = CreateUIObject($"Piece_{piece.name}", _piecesContent);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(size, size);

            var layoutElement = btnGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size;
            layoutElement.preferredHeight = size;

            var bgImage = btnGo.AddComponent<Image>();
            bgImage.sprite = VeneerTextures.CreateSlotSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = VeneerColors.SlotEmpty;

            var button = btnGo.AddComponent<Button>();
            button.targetGraphic = bgImage;

            var capturedPiece = piece;
            button.onClick.AddListener(() => SelectPiece(capturedPiece));

            // Icon
            var iconGo = CreateUIObject("Icon", btnGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2, 2);
            iconRect.offsetMax = new Vector2(-2, -2);

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = piece.m_icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            // Hover tooltip
            var tooltip = btnGo.AddComponent<PieceTooltipTrigger>();
            tooltip.Piece = piece;

            return new PieceButton
            {
                Root = btnGo,
                Background = bgImage,
                Icon = iconImage,
                Piece = piece
            };
        }

        private void SelectPiece(Piece piece)
        {
            if (_player == null || _pieceTable == null) return;

            int index = _pieceTable.m_pieces.IndexOf(piece.gameObject);
            if (index >= 0)
            {
                _player.SetSelectedPiece(new Vector2Int(_selectedCategory, index));
                _selectedPieceText.Content = Localization.instance.Localize(piece.m_name);
            }
        }

        private string GetCategoryName(Piece.PieceCategory category)
        {
            return category switch
            {
                Piece.PieceCategory.Misc => "Misc",
                Piece.PieceCategory.Crafting => "Craft",
                Piece.PieceCategory.BuildingWorkbench => "Build",
                Piece.PieceCategory.BuildingStonecutter => "Stone",
                Piece.PieceCategory.Furniture => "Furn",
                Piece.PieceCategory.All => "All",
                _ => category.ToString()
            };
        }

        private class TabButton
        {
            public GameObject Root;
            public Image Background;
            public Text Text;
            public Piece.PieceCategory Category;
        }

        private class PieceButton
        {
            public GameObject Root;
            public Image Background;
            public Image Icon;
            public Piece Piece;
        }
    }

    /// <summary>
    /// Simple tooltip trigger for pieces.
    /// </summary>
    public class PieceTooltipTrigger : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        public Piece Piece;

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (Piece != null)
            {
                var name = Localization.instance.Localize(Piece.m_name);
                var desc = Localization.instance.Localize(Piece.m_description);
                VeneerTooltip.Show(name, desc);
            }
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            VeneerTooltip.Hide();
        }
    }
}
