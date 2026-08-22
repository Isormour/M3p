using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: offers a random handful of skills the profile does not already own.
    /// Confirming a card writes that skill into the save and closes the panel.
    /// </summary>
    public sealed class UIPanelGainSkill : UIPanelClosable
    {
        [SerializeField] UIPanelGainSkillCard cardPrefab;
        [SerializeField] Transform CardLayoutParent;
        [SerializeField, Min(1)] int _offerCount = 3;

        readonly List<UIPanelGainSkillCard> _cards = new List<UIPanelGainSkillCard>();
        readonly List<SkillDefinition> _unownedPool = new List<SkillDefinition>();

        protected override void OnInitialize()
        {
            HideTemplate(cardPrefab);
        }

        void OnDisable()
        {
            ClearCards();
        }

        public override void Show()
        {
            base.Show();
            RefreshOffers();
        }

        public override void Hide()
        {
            ClearCards();
            base.Hide();
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
        }

        void RefreshOffers()
        {
            ClearCards();
            BuildUnownedPool();

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
                int pick = Random.Range(i, _unownedPool.Count);
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

        void BuildUnownedPool()
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
                if (entry.Skill == null || entry.Id == SkillConfig.InvalidSkillId)
                    continue;

                if (profile != null && profile.HasSkill(entry.Id))
                    continue;

                _unownedPool.Add(entry.Skill);
            }
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

            Profiles.Save();
            Hide();
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

        static void HideTemplate(Component template)
        {
            if (template != null && template.gameObject.scene.IsValid())
                template.gameObject.SetActive(false);
        }
    }
}
