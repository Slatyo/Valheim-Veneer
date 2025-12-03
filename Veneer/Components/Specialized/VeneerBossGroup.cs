using System.Collections.Generic;
using System.Linq;
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
    /// Compact boss list panel shown when multiple bosses are nearby.
    /// Displays smaller health bars for all tracked bosses except the main one.
    /// </summary>
    public class VeneerBossGroup : VeneerElement
    {
        private const string ElementIdBossGroup = "Veneer_BossGroup";
        private const int MaxDisplayedBosses = 20;
        private const int MaxNameLength = 25;

        private static VeneerBossGroup _instance;
        public static VeneerBossGroup Instance => _instance;

        private Image _backgroundImage;
        private Image _borderImage;
        private VeneerText _headerText;
        private readonly List<BossListEntry> _entries = new List<BossListEntry>();
        private Character _mainBoss;

        // Layout constants
        private const float EntryWidth = 180f;
        private const float EntryHeight = 20f;
        private const float Padding = 6f;
        private const float HeaderHeight = 18f;
        private const float Spacing = 4f;

        /// <summary>
        /// Creates a boss group panel.
        /// </summary>
        public static VeneerBossGroup Create(Transform parent)
        {
            if (_instance != null) return _instance;

            var go = CreateUIObject("VeneerBossGroup", parent);
            var group = go.AddComponent<VeneerBossGroup>();
            group.Initialize();
            _instance = group;
            return group;
        }

        private void Initialize()
        {
            ElementId = ElementIdBossGroup;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.HUDOverlay;

            // Default position beside the main boss frame
            VeneerAnchor.Register(ElementId, ScreenAnchor.TopCenter, new Vector2(220, -50));

            // Initial size (will be updated based on boss count)
            float totalWidth = EntryWidth + Padding * 2;
            float totalHeight = HeaderHeight + Padding * 2;

            SetSize(totalWidth, totalHeight);
            AnchorTo(AnchorPreset.TopCenter, new Vector2(220, -50));

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.Background;

            // Border (gold for bosses, thinner)
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Legendary, Color.clear, 1);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Header - fixed position at top
            _headerText = VeneerText.CreateCaption(transform, "Other Bosses");
            _headerText.Alignment = TextAnchor.MiddleCenter;
            _headerText.TextColor = VeneerColors.Legendary;
            _headerText.FontSize = VeneerConfig.GetScaledFontSize(11);
            var headerRect = _headerText.RectTransform;
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -Padding);
            headerRect.sizeDelta = new Vector2(0, HeaderHeight);

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

        private void Update()
        {
            // Handle Edit Mode visibility
            if (VeneerMover.EditModeEnabled)
            {
                if (!gameObject.activeSelf)
                {
                    ShowEditModePlaceholder();
                }
            }
            else if (_editModePlaceholder)
            {
                // Hide placeholder when leaving edit mode
                // Clear placeholder entries and hide
                foreach (var entry in _entries)
                {
                    if (entry.Root != null)
                    {
                        Destroy(entry.Root);
                    }
                }
                _entries.Clear();
                gameObject.SetActive(false);
                _editModePlaceholder = false;
            }

            // Update health bars for all entries (only real bosses, not placeholders)
            foreach (var entry in _entries)
            {
                if (entry.Boss != null && !entry.Boss.IsDead())
                {
                    entry.UpdateHealth();
                }
            }
        }

        private bool _editModePlaceholder;

        private void ShowEditModePlaceholder()
        {
            // Show panel with placeholder content for Edit Mode positioning
            _editModePlaceholder = true;

            // Clear any existing entries
            foreach (var entry in _entries)
            {
                if (entry.Root != null)
                {
                    Destroy(entry.Root);
                }
            }
            _entries.Clear();

            // Set size for 2 sample entries
            UpdatePanelSize(2);

            // Create placeholder entries (no real boss, just visual)
            CreatePlaceholderEntry(0, "Sample Boss 1", 0.75f);
            CreatePlaceholderEntry(1, "Sample Boss 2", 0.5f);

            // Make background semi-transparent to indicate placeholder
            if (_backgroundImage != null)
            {
                _backgroundImage.color = new Color(VeneerColors.Background.r,
                                                    VeneerColors.Background.g,
                                                    VeneerColors.Background.b,
                                                    0.7f);
            }

            gameObject.SetActive(true);
        }

        private void CreatePlaceholderEntry(int index, string name, float healthPercent)
        {
            // Calculate Y position from top (below header)
            float yOffset = -(Padding + HeaderHeight + Spacing + index * (EntryHeight + Spacing));

            // Entry container
            var entryGo = CreateUIObject($"PlaceholderEntry_{index}", transform);
            var entryRect = entryGo.GetComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0, 1);
            entryRect.anchorMax = new Vector2(1, 1);
            entryRect.pivot = new Vector2(0.5f, 1);
            entryRect.anchoredPosition = new Vector2(0, yOffset);
            entryRect.sizeDelta = new Vector2(-Padding * 2, EntryHeight);

            // Health bar background
            var barBgGo = CreateUIObject("BarBackground", entryGo.transform);
            var barBgRect = barBgGo.GetComponent<RectTransform>();
            barBgRect.anchorMin = Vector2.zero;
            barBgRect.anchorMax = Vector2.one;
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;

            var barBgImage = barBgGo.AddComponent<Image>();
            barBgImage.color = VeneerColors.Darken(VeneerColors.Error, 0.7f);

            // Health bar fill
            var fillGo = CreateUIObject("Fill", entryGo.transform);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(healthPercent, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.offsetMin = new Vector2(1, 1);
            fillRect.offsetMax = new Vector2(-1, -1);

            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = VeneerColors.Error;

            // Border
            var borderGo = CreateUIObject("Border", entryGo.transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            var borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(8, VeneerColors.Border, Color.clear, 1);
            borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;

            // Name text
            var nameGo = CreateUIObject("NameText", entryGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = new Vector2(0.6f, 1);
            nameRect.offsetMin = new Vector2(4, 0);
            nameRect.offsetMax = new Vector2(0, 0);

            var nameText = nameGo.AddComponent<Text>();
            nameText.text = name;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = VeneerConfig.GetScaledFontSize(10);
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.raycastTarget = false;

            var nameOutline = nameGo.AddComponent<Outline>();
            nameOutline.effectColor = Color.black;
            nameOutline.effectDistance = new Vector2(1, -1);

            // HP text
            var hpGo = CreateUIObject("HPText", entryGo.transform);
            var hpRect = hpGo.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.6f, 0);
            hpRect.anchorMax = Vector2.one;
            hpRect.offsetMin = new Vector2(0, 0);
            hpRect.offsetMax = new Vector2(-4, 0);

            var hpText = hpGo.AddComponent<Text>();
            int sampleMax = 10000;
            int sampleCurrent = Mathf.RoundToInt(sampleMax * healthPercent);
            hpText.text = $"{sampleCurrent}/{sampleMax}";
            hpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            hpText.fontSize = VeneerConfig.GetScaledFontSize(10);
            hpText.color = Color.white;
            hpText.alignment = TextAnchor.MiddleRight;
            hpText.raycastTarget = false;

            var hpOutline = hpGo.AddComponent<Outline>();
            hpOutline.effectColor = Color.black;
            hpOutline.effectDistance = new Vector2(1, -1);

            // Add to entries list (with null boss) so it gets cleaned up properly
            _entries.Add(new BossListEntry
            {
                Boss = null,
                Root = entryGo,
                FillRect = fillRect,
                FillImage = fillImage,
                NameText = nameText,
                HPText = hpText
            });
        }

        /// <summary>
        /// Updates the boss list display.
        /// </summary>
        public void UpdateBossList(IReadOnlyList<Character> bosses, Character mainBoss)
        {
            _mainBoss = mainBoss;

            // Clear existing entries
            foreach (var entry in _entries)
            {
                if (entry.Root != null)
                {
                    Destroy(entry.Root);
                }
            }
            _entries.Clear();

            // Reset edit mode placeholder state when real bosses update
            _editModePlaceholder = false;

            // Restore normal background color
            if (_backgroundImage != null)
            {
                _backgroundImage.color = VeneerColors.Background;
            }

            // Count bosses that aren't the main one
            int otherBossCount = 0;
            foreach (var boss in bosses)
            {
                if (boss != null && boss != mainBoss && !boss.IsDead())
                {
                    otherBossCount++;
                }
            }

            // Only show if there's more than 1 boss total (at least 1 other boss)
            // In Edit Mode, the Update() method will show the placeholder instead
            if (otherBossCount == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            // Update panel size first so entries are positioned correctly
            int displayCount = Mathf.Min(otherBossCount, MaxDisplayedBosses);
            UpdatePanelSize(displayCount);

            // Filter and sort bosses alphabetically by name (stable list, no jumping)
            var sortedBosses = bosses
                .Where(b => b != null && b != mainBoss && !b.IsDead())
                .OrderBy(b => Localization.instance.Localize(b.m_name))
                .Take(MaxDisplayedBosses)
                .ToList();

            // Create entries for sorted bosses
            for (int i = 0; i < sortedBosses.Count; i++)
            {
                var entry = CreateBossEntry(sortedBosses[i], i);
                _entries.Add(entry);
            }

            gameObject.SetActive(true);
        }

        private BossListEntry CreateBossEntry(Character boss, int index)
        {
            // Calculate Y position from top (below header)
            float yOffset = -(Padding + HeaderHeight + Spacing + index * (EntryHeight + Spacing));

            // Entry container
            var entryGo = CreateUIObject($"BossEntry_{index}", transform);
            var entryRect = entryGo.GetComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0, 1);
            entryRect.anchorMax = new Vector2(1, 1);
            entryRect.pivot = new Vector2(0.5f, 1);
            entryRect.anchoredPosition = new Vector2(0, yOffset);
            entryRect.sizeDelta = new Vector2(-Padding * 2, EntryHeight);

            // Health bar background
            var barBgGo = CreateUIObject("BarBackground", entryGo.transform);
            var barBgRect = barBgGo.GetComponent<RectTransform>();
            barBgRect.anchorMin = Vector2.zero;
            barBgRect.anchorMax = Vector2.one;
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;

            var barBgImage = barBgGo.AddComponent<Image>();
            barBgImage.color = VeneerColors.Darken(VeneerColors.Error, 0.7f);

            // Health bar fill
            var fillGo = CreateUIObject("Fill", entryGo.transform);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.offsetMin = new Vector2(1, 1);
            fillRect.offsetMax = new Vector2(-1, -1);

            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = VeneerColors.Error;

            // Border
            var borderGo = CreateUIObject("Border", entryGo.transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            var borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(8, VeneerColors.Border, Color.clear, 1);
            borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 1);
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;

            // Name text (inside the bar on the left, white with black outline)
            var nameGo = CreateUIObject("NameText", entryGo.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = new Vector2(0.6f, 1);
            nameRect.offsetMin = new Vector2(4, 0);
            nameRect.offsetMax = new Vector2(0, 0);

            var nameText = nameGo.AddComponent<Text>();
            nameText.text = TruncateName(Localization.instance.Localize(boss.m_name));
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = VeneerConfig.GetScaledFontSize(10);
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.raycastTarget = false;

            // Black outline for name (1px)
            var nameOutline = nameGo.AddComponent<Outline>();
            nameOutline.effectColor = Color.black;
            nameOutline.effectDistance = new Vector2(1, -1);

            // HP text (inside the bar on the right, white with black outline)
            var hpGo = CreateUIObject("HPText", entryGo.transform);
            var hpRect = hpGo.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.6f, 0);
            hpRect.anchorMax = Vector2.one;
            hpRect.offsetMin = new Vector2(0, 0);
            hpRect.offsetMax = new Vector2(-4, 0);

            var hpText = hpGo.AddComponent<Text>();
            float health = boss.GetHealth();
            float maxHealth = boss.GetMaxHealth();
            hpText.text = $"{health:F0}/{maxHealth:F0}";
            hpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            hpText.fontSize = VeneerConfig.GetScaledFontSize(10);
            hpText.color = Color.white;
            hpText.alignment = TextAnchor.MiddleRight;
            hpText.raycastTarget = false;

            // Black outline for HP (1px)
            var hpOutline = hpGo.AddComponent<Outline>();
            hpOutline.effectColor = Color.black;
            hpOutline.effectDistance = new Vector2(1, -1);

            // Initial health update
            float fillAmount = maxHealth > 0 ? health / maxHealth : 0;
            fillRect.anchorMax = new Vector2(fillAmount, 1);

            return new BossListEntry
            {
                Boss = boss,
                Root = entryGo,
                FillRect = fillRect,
                FillImage = fillImage,
                NameText = nameText,
                HPText = hpText
            };
        }

        private void UpdatePanelSize(int entryCount)
        {
            int count = Mathf.Min(entryCount, MaxDisplayedBosses);
            float totalHeight = Padding * 2 + HeaderHeight + Spacing + count * EntryHeight + (count - 1) * Spacing;
            float totalWidth = EntryWidth + Padding * 2;

            SetSize(totalWidth, totalHeight);
        }

        private static string TruncateName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (name.Length <= MaxNameLength) return name;
            return name.Substring(0, MaxNameLength - 3) + "...";
        }

        private new void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Cleans up the boss group.
        /// </summary>
        public static void Cleanup()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private class BossListEntry
        {
            public Character Boss;
            public GameObject Root;
            public RectTransform FillRect;
            public Image FillImage;
            public Text NameText;
            public Text HPText;

            public void UpdateHealth()
            {
                if (Boss == null || FillRect == null) return;

                float health = Boss.GetHealth();
                float maxHealth = Boss.GetMaxHealth();
                float fillAmount = maxHealth > 0 ? Mathf.Clamp01(health / maxHealth) : 0;

                // Update fill width
                FillRect.anchorMax = new Vector2(fillAmount, 1);

                // Update HP text
                if (HPText != null)
                {
                    HPText.text = $"{health:F0}/{maxHealth:F0}";
                }

                // Color based on health
                if (FillImage != null)
                {
                    if (fillAmount < 0.25f)
                        FillImage.color = VeneerColors.Error;
                    else if (fillAmount < 0.5f)
                        FillImage.color = VeneerColors.Warning;
                    else
                        FillImage.color = VeneerColors.Error;
                }
            }
        }
    }
}
