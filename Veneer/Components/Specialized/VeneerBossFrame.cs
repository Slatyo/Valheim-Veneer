using System.Collections.Generic;
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
    /// Boss health frame displayed at the top of the screen.
    /// Shows the closest boss with 1-second update interval to prevent jumping.
    /// </summary>
    public class VeneerBossFrame : VeneerElement
    {
        private const string ElementIdBoss = "Veneer_BossFrame";
        private const float ClosestBossUpdateInterval = 1.0f;

        private Image _backgroundImage;
        private Image _borderImage;
        private VeneerText _nameText;
        private VeneerBar _healthBar;
        private Character _trackedBoss;
        private bool _isActive;

        // Multi-boss tracking
        private readonly List<Character> _nearbyBosses = new List<Character>();
        private float _lastClosestBossCheck;

        /// <summary>
        /// Creates a boss frame.
        /// </summary>
        public static VeneerBossFrame Create(Transform parent)
        {
            var go = CreateUIObject("VeneerBossFrame", parent);
            var frame = go.AddComponent<VeneerBossFrame>();
            frame.Initialize();
            return frame;
        }

        private void Initialize()
        {
            ElementId = ElementIdBoss;
            IsMoveable = true;
            SavePosition = true;
            LayerType = VeneerLayerType.HUDOverlay;

            VeneerAnchor.Register(ElementId, ScreenAnchor.TopCenter, new Vector2(0, -50));

            float barWidth = 400f;
            float barHeight = VeneerDimensions.BarHeightXLarge;
            float padding = VeneerDimensions.Padding;
            float nameHeight = 24f;
            float spacing = VeneerDimensions.Spacing;

            float totalWidth = barWidth + padding * 2;
            float totalHeight = barHeight + nameHeight + padding * 2 + spacing;

            SetSize(totalWidth, totalHeight);
            AnchorTo(AnchorPreset.TopCenter, new Vector2(0, -50));

            // Background
            _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = VeneerTextures.CreatePanelSprite();
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = VeneerColors.Background;

            // Border (gold for bosses)
            var borderGo = CreateUIObject("Border", transform);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            _borderImage = borderGo.AddComponent<Image>();
            var borderTex = VeneerTextures.CreateSlicedBorderTexture(16, VeneerColors.Legendary, Color.clear, 2);
            _borderImage.sprite = VeneerTextures.CreateSlicedSprite(borderTex, 2);
            _borderImage.type = Image.Type.Sliced;
            _borderImage.raycastTarget = false;

            // Boss name - fixed position at top
            _nameText = VeneerText.CreateHeader(transform, "Boss");
            _nameText.Alignment = TextAnchor.MiddleCenter;
            _nameText.TextColor = VeneerColors.Legendary;
            var nameRect = _nameText.RectTransform;
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -padding);
            nameRect.sizeDelta = new Vector2(-padding * 2, nameHeight);

            // Health bar - fixed position below name
            _healthBar = VeneerBar.Create(transform, "BossHealth", barWidth, barHeight);
            _healthBar.FillColor = VeneerColors.Error;
            _healthBar.BackgroundColor = VeneerColors.Darken(VeneerColors.Error, 0.7f);
            _healthBar.TextFormat = "{0:F0} / {1:F0}";
            var barRect = _healthBar.RectTransform;
            barRect.anchorMin = new Vector2(0.5f, 0);
            barRect.anchorMax = new Vector2(0.5f, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = new Vector2(0, padding);
            barRect.sizeDelta = new Vector2(barWidth, barHeight);

            // Add mover
            var mover = gameObject.AddComponent<VeneerMover>();
            mover.ElementId = ElementId;

            // Add resizer
            var resizer = gameObject.AddComponent<VeneerResizer>();
            resizer.MinSize = new Vector2(200, 50);
            resizer.MaxSize = new Vector2(800, 200);

            // Start hidden
            gameObject.SetActive(false);
            _isActive = false;

            // Apply saved position
            var savedData = VeneerAnchor.GetAnchorData(ElementId);
            if (savedData != null)
            {
                VeneerAnchor.ApplyAnchor(RectTransform, savedData.Anchor, savedData.Offset);
            }
        }

        private void Update()
        {
            // Update closest boss check periodically
            if (Time.time - _lastClosestBossCheck >= ClosestBossUpdateInterval)
            {
                _lastClosestBossCheck = Time.time;
                UpdateClosestBoss();
            }

            // Update health bar for tracked boss
            if (_trackedBoss != null)
            {
                if (_trackedBoss.IsDead() || !_trackedBoss.gameObject.activeInHierarchy)
                {
                    RemoveBoss(_trackedBoss);
                    return;
                }

                UpdateHealthBar();
            }
        }

        private void UpdateClosestBoss()
        {
            // Clean up dead bosses
            _nearbyBosses.RemoveAll(b => b == null || b.IsDead() || !b.gameObject.activeInHierarchy);

            if (_nearbyBosses.Count == 0)
            {
                if (_isActive)
                {
                    ClearAllBosses();
                }
                return;
            }

            // Find closest boss to player
            var player = Player.m_localPlayer;
            if (player == null) return;

            Character closestBoss = null;
            float closestDistance = float.MaxValue;

            foreach (var boss in _nearbyBosses)
            {
                if (boss == null) continue;

                float distance = Vector3.Distance(player.transform.position, boss.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBoss = boss;
                }
            }

            // Only switch if we found a closer boss
            if (closestBoss != null && closestBoss != _trackedBoss)
            {
                SetTrackedBoss(closestBoss);
            }
        }

        /// <summary>
        /// Adds a boss to track. The closest boss will be shown in the main frame.
        /// </summary>
        public void AddBoss(Character boss)
        {
            if (boss == null) return;

            if (!_nearbyBosses.Contains(boss))
            {
                _nearbyBosses.Add(boss);
                Plugin.Log.LogDebug($"VeneerBossFrame: Added boss {boss.m_name}, total bosses: {_nearbyBosses.Count}");
            }

            // If this is the first boss or we have no tracked boss, set it immediately
            if (_trackedBoss == null)
            {
                SetTrackedBoss(boss);
            }

            // Notify boss group about the new boss
            VeneerBossGroup.Instance?.UpdateBossList(_nearbyBosses, _trackedBoss);
        }

        /// <summary>
        /// Removes a boss from tracking.
        /// </summary>
        public void RemoveBoss(Character boss)
        {
            if (boss == null) return;

            _nearbyBosses.Remove(boss);
            Plugin.Log.LogDebug($"VeneerBossFrame: Removed boss {boss.m_name}, remaining bosses: {_nearbyBosses.Count}");

            if (_trackedBoss == boss)
            {
                _trackedBoss = null;

                // Switch to next closest boss if available
                if (_nearbyBosses.Count > 0)
                {
                    _lastClosestBossCheck = 0; // Force immediate update
                    UpdateClosestBoss();
                }
                else
                {
                    ClearAllBosses();
                }
            }

            // Notify boss group
            VeneerBossGroup.Instance?.UpdateBossList(_nearbyBosses, _trackedBoss);
        }

        private void SetTrackedBoss(Character boss)
        {
            _trackedBoss = boss;

            if (boss != null)
            {
                _nameText.Content = Localization.instance.Localize(boss.m_name);
                gameObject.SetActive(true);
                _isActive = true;
                UpdateHealthBar();
            }
        }

        /// <summary>
        /// Sets the boss to track (legacy method, now adds to tracking list).
        /// </summary>
        public void SetBoss(Character boss)
        {
            AddBoss(boss);
        }

        /// <summary>
        /// Clears the tracked boss (legacy method).
        /// </summary>
        public void ClearBoss()
        {
            if (_trackedBoss != null)
            {
                RemoveBoss(_trackedBoss);
            }
        }

        /// <summary>
        /// Clears all tracked bosses.
        /// </summary>
        public void ClearAllBosses()
        {
            _nearbyBosses.Clear();
            _trackedBoss = null;
            gameObject.SetActive(false);
            _isActive = false;

            VeneerBossGroup.Instance?.UpdateBossList(_nearbyBosses, null);
        }

        private void UpdateHealthBar()
        {
            if (_trackedBoss == null || _healthBar == null) return;

            float health = _trackedBoss.GetHealth();
            float maxHealth = _trackedBoss.GetMaxHealth();

            _healthBar.SetValues(health, maxHealth);

            // Change color based on health percentage
            float percent = maxHealth > 0 ? health / maxHealth : 0;
            if (percent < 0.25f)
            {
                _healthBar.FillColor = VeneerColors.Error;
            }
            else if (percent < 0.5f)
            {
                _healthBar.FillColor = VeneerColors.Warning;
            }
            else
            {
                _healthBar.FillColor = VeneerColors.Error;
            }
        }

        /// <summary>
        /// Whether the boss frame is currently showing.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// The currently tracked boss (closest one).
        /// </summary>
        public Character TrackedBoss => _trackedBoss;

        /// <summary>
        /// All nearby bosses being tracked.
        /// </summary>
        public IReadOnlyList<Character> NearbyBosses => _nearbyBosses;

        /// <summary>
        /// Number of bosses currently being tracked.
        /// </summary>
        public int BossCount => _nearbyBosses.Count;
    }
}
