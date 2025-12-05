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
    /// Minimap frame.
    /// Wraps the vanilla minimap with a styled border and optional info display.
    /// </summary>
    public class VeneerMinimapFrame : VeneerElement
    {
        private const string ElementIdMinimap = "Veneer_Minimap";

        private Image _borderImage;
        private VeneerText _coordinatesText;
        private VeneerText _biomeText;
        private RectTransform _infoBar;
        private Player _trackedPlayer;
        private RectTransform _vanillaMinimapRect;
        private Transform _originalMinimapParent;
        private Vector2 _originalMinimapSize;
        private Vector3 _originalMinimapPosition;

        /// <summary>
        /// Creates a minimap frame that wraps the vanilla minimap.
        /// </summary>
        public static VeneerMinimapFrame Create(Transform parent)
        {
            var go = CreateUIObject("VeneerMinimapFrame", parent);
            var frame = go.AddComponent<VeneerMinimapFrame>();
            frame.Initialize();
            return frame;
        }

        private void Initialize()
        {
            ElementId = ElementIdMinimap;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.HUD;

            VeneerAnchor.Register(ElementId, ScreenAnchor.TopRight, new Vector2(-10, -10));

            float mapSize = VeneerConfig.MinimapSize.Value;
            float borderWidth = 2f;
            float infoHeight = (VeneerConfig.MinimapShowCoordinates.Value || VeneerConfig.MinimapShowBiome.Value) ? 16f : 0f;

            float totalWidth = mapSize + borderWidth * 2;
            float totalHeight = mapSize + borderWidth * 2 + infoHeight;

            SetSize(totalWidth, totalHeight);
            AnchorTo(AnchorPreset.TopRight, new Vector2(-10, -10));

            // Border frame around minimap
            _borderImage = gameObject.AddComponent<Image>();
            _borderImage.color = VeneerColors.Border;

            // Info bar at bottom (outside the map area)
            if (infoHeight > 0)
            {
                var infoGo = CreateUIObject("InfoBar", transform);
                _infoBar = infoGo.GetComponent<RectTransform>();
                _infoBar.anchorMin = new Vector2(0, 0);
                _infoBar.anchorMax = new Vector2(1, 0);
                _infoBar.pivot = new Vector2(0.5f, 0);
                _infoBar.anchoredPosition = Vector2.zero;
                _infoBar.sizeDelta = new Vector2(0, infoHeight);

                var infoBg = infoGo.AddComponent<Image>();
                infoBg.color = VeneerColors.BackgroundSolid;

                var infoLayout = infoGo.AddComponent<HorizontalLayoutGroup>();
                infoLayout.childAlignment = TextAnchor.MiddleCenter;
                infoLayout.childControlWidth = true;
                infoLayout.childControlHeight = true;
                infoLayout.childForceExpandWidth = true;
                infoLayout.childForceExpandHeight = true;
                infoLayout.padding = new RectOffset(4, 4, 0, 0);

                // Coordinates
                if (VeneerConfig.MinimapShowCoordinates.Value)
                {
                    _coordinatesText = VeneerText.Create(infoGo.transform, "0, 0");
                    _coordinatesText.FontSize = VeneerConfig.GetScaledFontSize(10);
                    _coordinatesText.Alignment = TextAnchor.MiddleLeft;
                }

                // Biome
                if (VeneerConfig.MinimapShowBiome.Value)
                {
                    _biomeText = VeneerText.Create(infoGo.transform, "Meadows");
                    _biomeText.FontSize = VeneerConfig.GetScaledFontSize(10);
                    _biomeText.Alignment = TextAnchor.MiddleRight;
                    _biomeText.TextColor = VeneerColors.TextGold;
                }
            }

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(100, 100);
            resizer.MaxSize = new Vector2(500, 500);

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }

            // Find and reparent the vanilla minimap
            StartCoroutine(FindAndWrapMinimap());
        }

        private System.Collections.IEnumerator FindAndWrapMinimap()
        {
            // Wait for Minimap to be available
            while (Minimap.instance == null)
            {
                yield return null;
            }

            yield return null; // Extra frame for safety

            var minimap = Minimap.instance;
            if (minimap == null) yield break;

            // Find the small minimap panel
            var smallRoot = minimap.m_smallRoot;
            if (smallRoot == null) yield break;

            _vanillaMinimapRect = smallRoot.GetComponent<RectTransform>();
            if (_vanillaMinimapRect == null) yield break;

            // Store original state
            _originalMinimapParent = _vanillaMinimapRect.parent;
            _originalMinimapSize = _vanillaMinimapRect.sizeDelta;
            _originalMinimapPosition = _vanillaMinimapRect.anchoredPosition;

            // Reparent minimap to our frame
            _vanillaMinimapRect.SetParent(transform, false);

            // Position minimap inside our border
            float borderWidth = 2f;
            float infoHeight = (_infoBar != null) ? 16f : 0f;
            float mapSize = VeneerConfig.MinimapSize.Value;

            _vanillaMinimapRect.anchorMin = new Vector2(0, 0);
            _vanillaMinimapRect.anchorMax = new Vector2(1, 1);
            _vanillaMinimapRect.offsetMin = new Vector2(borderWidth, borderWidth + infoHeight);
            _vanillaMinimapRect.offsetMax = new Vector2(-borderWidth, -borderWidth);
            _vanillaMinimapRect.localScale = Vector3.one;

            // Scale the map content to fit
            var mapImage = smallRoot.GetComponentInChildren<RawImage>();
            if (mapImage != null)
            {
                var mapImageRect = mapImage.GetComponent<RectTransform>();
                mapImageRect.anchorMin = Vector2.zero;
                mapImageRect.anchorMax = Vector2.one;
                mapImageRect.offsetMin = Vector2.zero;
                mapImageRect.offsetMax = Vector2.zero;
            }

            Plugin.Log.LogDebug("VeneerMinimapFrame: Minimap reparented successfully");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Restore minimap to original parent (only if not being destroyed with the scene)
            try
            {
                if (_vanillaMinimapRect != null && _originalMinimapParent != null)
                {
                    // Check if the parent's gameObject is still valid and not being destroyed
                    if (_originalMinimapParent.gameObject != null && _originalMinimapParent.gameObject.scene.isLoaded)
                    {
                        _vanillaMinimapRect.SetParent(_originalMinimapParent, false);
                        _vanillaMinimapRect.sizeDelta = _originalMinimapSize;
                        _vanillaMinimapRect.anchoredPosition = _originalMinimapPosition;
                    }
                }
            }
            catch (System.Exception)
            {
                // Ignore errors during scene destruction
            }
        }

        private void Update()
        {
            if (_trackedPlayer == null)
            {
                _trackedPlayer = Player.m_localPlayer;
            }

            if (_trackedPlayer == null) return;

            UpdateInfo();
        }

        private void UpdateInfo()
        {
            Vector3 pos = _trackedPlayer.transform.position;

            // Update coordinates
            if (_coordinatesText != null)
            {
                _coordinatesText.Content = $"{pos.x:F0}, {pos.z:F0}";
            }

            // Update biome
            if (_biomeText != null)
            {
                var biome = WorldGenerator.instance?.GetBiome(pos) ?? Heightmap.Biome.Meadows;
                _biomeText.Content = GetBiomeName(biome);
            }
        }

        private string GetBiomeName(Heightmap.Biome biome)
        {
            return biome switch
            {
                Heightmap.Biome.Meadows => "Meadows",
                Heightmap.Biome.BlackForest => "Black Forest",
                Heightmap.Biome.Swamp => "Swamp",
                Heightmap.Biome.Mountain => "Mountain",
                Heightmap.Biome.Plains => "Plains",
                Heightmap.Biome.Mistlands => "Mistlands",
                Heightmap.Biome.AshLands => "Ashlands",
                Heightmap.Biome.DeepNorth => "Deep North",
                Heightmap.Biome.Ocean => "Ocean",
                _ => biome.ToString()
            };
        }
    }
}
