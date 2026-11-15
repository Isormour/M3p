using System;
using System.Collections.Generic;
using Match3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: offers a random handful of skills the profile does not already own.
    /// Confirming a card writes that skill into the save and closes the panel.
    /// Reroll spends one shard of every colour, then raises that cost by one.
    /// </summary>
    public sealed class UIPanelGainSkill : UIPanelClosable
    {
        [SerializeField] UIPanelGainSkillCard cardPrefab;
        [SerializeField] Transform CardLayoutParent;
        [SerializeField, Min(1)] int _offerCount = 3;
        [SerializeField] Transform CostParent;
        [SerializeField] Transform OwnedParent;
        [SerializeField] UIPanelPlayerManaBar CostPrefab;
        [SerializeField] Button RerollButton;
        [SerializeField] TextMeshProUGUI _rerollHint;
        [SerializeField, Min(1)] int _startingRerollCost = 1;

        readonly List<UIPanelGainSkillCard> _cards = new List<UIPanelGainSkillCard>();
        readonly List<SkillDefinition> _unownedPool = new List<SkillDefinition>();
        readonly List<UIPanelPlayerManaBar> _costViews = new List<UIPanelPlayerManaBar>();
        readonly List<UIPanelPlayerManaBar> _ownedViews = new List<UIPanelPlayerManaBar>();
        readonly List<TileTypeShardCost> _rerollCosts = new List<TileTypeShardCost>();

        int _rerollCost = 1;
        bool _refreshingReroll;

        /// <summary>Raised when the panel closes after being open, including a skill pick or Close.</summary>
        public event Action Closed;

        protected override void OnInitialize()
        {
            HideTemplate(cardPrefab);
            HideTemplate(CostPrefab);
            WireRerollButton();
            Hide();
        }

        void OnEnable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged += HandleProfileChanged;
        }

        void OnDisable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged -= HandleProfileChanged;

            ClearCards();
            ClearShardViews();
        }

        protected override void OnDestroy()
        {
            if (RerollButton != null)
                RerollButton.onClick.RemoveListener(HandleRerollClicked);

            base.OnDestroy();
        }

        public override void Show()
        {
            bool opening = !IsOpen;
            if (opening)
                _rerollCost = Mathf.Max(1, _startingRerollCost);

            base.Show();
            RefreshOffers(false);
            RefreshRerollUi();
        }

        public override void Hide()
        {
            bool wasOpen = IsOpen;
            ClearCards();
            base.Hide();
            if (wasOpen)
                Closed?.Invoke();
        }

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        static GameConfig Config => GameManager.Instance != null ? GameManager.Instance.Config : null;

        static SkillConfig Skills => Config != null ? Config.Skills : null;

        protected override void ResolveRefs()
        {
            base.ResolveRefs();

            if (CardLayoutParent == null)
                CardLayoutParent = FindDescendant("CardLayoutParent");

            if (cardPrefab == null)
            {
                Transform card = FindDescendant("UIPanelGainSkillCard");
                if (card != null)
                    cardPrefab = card.GetComponent<UIPanelGainSkillCard>();
            }

            if (CostParent == null)
                CostParent = FindDescendant("CostParent");

            if (OwnedParent == null)
                OwnedParent = FindDescendant("OwnedList");

            if (RerollButton == null)
                RerollButton = FindDescendantButton("RerollButton");

            if (_rerollHint == null)
                _rerollHint = FindDescendantComponent<TextMeshProUGUI>("RerollHint");
        }

        void HandleProfileChanged()
        {
            if (_refreshingReroll || !IsOpen)
                return;

            RefreshRerollUi();
        }

        void RefreshOffers(bool avoidCurrent)
        {
            List<SkillDefinition> avoid = null;
            if (avoidCurrent && _cards.Count > 0)
            {
                avoid = new List<SkillDefinition>(_cards.Count);
                for (int i = 0; i < _cards.Count; i++)
                {
                    if (_cards[i] != null && _cards[i].Skill != null)
                        avoid.Add(_cards[i].Skill);
                }
            }

            ClearCards();
            BuildUnownedPool(avoid);

            if (CardLayoutParent == null)
            {
                Debug.LogError($"{nameof(UIPanelGainSkill)}: assign {nameof(CardLayoutParent)} on the prefab.", this);
                return;
            }

            if (cardPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelGainSkill)}: assign {nameof(cardPrefab)} on the prefab.", this);
                return;
            }

            int count = Mathf.Min(_offerCount, _unownedPool.Count);
            if (count == 0)
            {
                Debug.LogWarning(
                    $"{nameof(UIPanelGainSkill)}: no unowned skills left in {nameof(SkillConfig)} to offer.",
                    this);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int pick = UnityEngine.Random.Range(i, _unownedPool.Count);
                SkillDefinition skill = _unownedPool[pick];
                _unownedPool[pick] = _unownedPool[i];
                _unownedPool[i] = skill;

                UIPanelGainSkillCard card = Instantiate(cardPrefab, CardLayoutParent);
                card.gameObject.SetActive(true);
                card.name = $"GainSkill_{skill.name}";
                card.Configure(skill, HandleSkillChosen);
                _cards.Add(card);
            }
        }

        void BuildUnownedPool(IReadOnlyList<SkillDefinition> avoid)
        {
            _unownedPool.Clear();

            SkillConfig skillConfig = Skills;
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (skillConfig == null)
                return;

            SkillConfig.Entry[] entries = skillConfig.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                SkillConfig.Entry entry = entries[i];
                if (!IsOfferable(entry, profile))
                    continue;

                _unownedPool.Add(entry.Skill);
            }

            if (avoid == null || avoid.Count == 0)
                return;

            int alternatives = 0;
            for (int i = 0; i < _unownedPool.Count; i++)
            {
                if (!ContainsSkill(avoid, _unownedPool[i]))
                    alternatives++;
            }

            if (alternatives < Mathf.Min(_offerCount, _unownedPool.Count))
                return;

            int kept = 0;
            for (int i = 0; i < _unownedPool.Count; i++)
            {
                if (ContainsSkill(avoid, _unownedPool[i]))
                    continue;

                _unownedPool[kept] = _unownedPool[i];
                kept++;
            }

            _unownedPool.RemoveRange(kept, _unownedPool.Count - kept);
        }

        void HandleSkillChosen(SkillDefinition skill)
        {
            if (skill == null)
                return;

            SkillConfig skillConfig = Skills;
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (skillConfig == null || profile == null)
                return;

            int skillId = skillConfig.GetSkillId(skill);
            if (!profile.TryAddSkill(skillId, skill.DisplayName))
                return;

            Sfx.Play(SfxId.NewSkillChoose);

            Profiles.Save();
            Hide();
        }

        void WireRerollButton()
        {
            if (RerollButton == null)
                return;

            RerollButton.onClick.RemoveListener(HandleRerollClicked);
            RerollButton.onClick.AddListener(HandleRerollClicked);
        }

        void HandleRerollClicked()
        {
            RebuildRerollCosts();
            if (!CanReroll())
                return;

            PlayerProfile profile = Profiles?.CurrentProfile;
            ProfileManager profiles = Profiles;
            if (profile == null || profiles == null)
                return;

            if (!profile.TrySpendShardCost(_rerollCosts))
                return;

            _rerollCost++;
            Sfx.Play(SfxId.MenuConfirm);
            RefreshOffers(true);
            _refreshingReroll = true;
            try
            {
                profiles.Save();
            }
            finally
            {
                _refreshingReroll = false;
            }

            RefreshRerollUi();
        }

        void RefreshRerollUi()
        {
            RebuildRerollCosts();
            BuildShardRow(CostListRoot, _costViews, true);
            BuildShardRow(OwnedParent, _ownedViews, false);
            RefreshRerollButton();
            RefreshRerollHint();
        }

        void RebuildRerollCosts()
        {
            _rerollCosts.Clear();

            GameConfig config = Config;
            Match3TileTypeDefinition[] tileTypes = config != null ? config.TileTypes : null;
            if (tileTypes == null)
                return;

            int amount = Mathf.Max(1, _rerollCost);
            for (int i = 0; i < tileTypes.Length; i++)
            {
                Match3TileTypeDefinition tileType = tileTypes[i];
                if (tileType == null)
                    continue;

                _rerollCosts.Add(new TileTypeShardCost(tileType, amount));
            }
        }

        void BuildShardRow(Transform parent, List<UIPanelPlayerManaBar> views, bool costRow)
        {
            ClearViews(views);

            if (parent == null)
            {
                Debug.LogError(
                    $"{nameof(UIPanelGainSkill)}: assign {(costRow ? nameof(CostParent) : nameof(OwnedParent))} on the prefab.",
                    this);
                return;
            }

            if (CostPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelGainSkill)}: assign {nameof(CostPrefab)} on the prefab.", this);
                return;
            }

            GameConfig config = Config;
            if (config == null)
                return;

            PlayerProfile profile = Profiles?.CurrentProfile;
            for (int i = 0; i < _rerollCosts.Count; i++)
            {
                TileTypeShardCost cost = _rerollCosts[i];
                Match3TileTypeDefinition tileType = cost.TileType;
                if (tileType == null)
                    continue;

                int typeId = config.GetTileTypeId(tileType);
                Sprite icon = typeId >= 0 ? config.GetTileTypeShardIcon(typeId) : tileType.ResolveShardIcon();
                int amount = costRow
                    ? cost.Amount
                    : (profile != null ? profile.GetShards(tileType.name) : 0);

                UIPanelPlayerManaBar view = Instantiate(CostPrefab, parent);
                view.gameObject.SetActive(true);
                view.name = (costRow ? "RerollCost_" : "OwnedShards_") + tileType.name;
                view.Configure(typeId, icon, tileType.UIMaterial);
                view.SetAmount(amount);
                views.Add(view);
            }
        }

        void RefreshRerollButton()
        {
            if (RerollButton == null)
            {
                Debug.LogError($"{nameof(UIPanelGainSkill)}: assign {nameof(RerollButton)} on the prefab.", this);
                return;
            }

            RerollButton.interactable = CanReroll();
        }

        void RefreshRerollHint()
        {
            if (_rerollHint == null)
                return;

            int count = Mathf.Max(1, _offerCount);
            string noun = count == 1 ? "skill" : "skills";
            _rerollHint.text = $"Rerolls\n{count} {noun}";
        }

        bool CanReroll()
        {
            if (_rerollCosts.Count == 0 || !HasFreshOffer())
                return false;

            PlayerProfile profile = Profiles?.CurrentProfile;
            return profile != null && profile.CanAffordCraftCost(_rerollCosts);
        }

        bool HasFreshOffer()
        {
            SkillConfig skillConfig = Skills;
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (skillConfig == null)
                return false;

            int unowned = 0;
            int shown = 0;
            SkillConfig.Entry[] entries = skillConfig.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                SkillConfig.Entry entry = entries[i];
                if (!IsOfferable(entry, profile))
                    continue;

                unowned++;
                if (IsShown(entry.Skill))
                    shown++;
            }

            return unowned > shown;
        }

        bool IsShown(SkillDefinition skill)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null && _cards[i].Skill == skill)
                    return true;
            }

            return false;
        }

        static bool IsOfferable(SkillConfig.Entry entry, PlayerProfile profile)
        {
            if (entry.Skill == null || entry.Id == SkillConfig.InvalidSkillId || entry.Skill.ExcludedFromUnlocks)
                return false;

            if (profile != null && profile.HasSkill(entry.Id))
                return false;

            return true;
        }

        static bool ContainsSkill(IReadOnlyList<SkillDefinition> skills, SkillDefinition skill)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] == skill)
                    return true;
            }

            return false;
        }

        Transform CostListRoot
        {
            get
            {
                if (CostParent == null)
                    return null;

                Transform list = CostParent.Find("CostList");
                return list != null ? list : CostParent;
            }
        }

        void ClearCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                    Destroy(_cards[i].gameObject);
            }

            _cards.Clear();
        }

        void ClearShardViews()
        {
            ClearViews(_costViews);
            ClearViews(_ownedViews);
        }

        static void ClearViews(List<UIPanelPlayerManaBar> views)
        {
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] != null)
                    Destroy(views[i].gameObject);
            }

            views.Clear();
        }

        static void HideTemplate(Component template)
        {
            if (template != null && template.gameObject.scene.IsValid())
                template.gameObject.SetActive(false);
        }
    }
}
