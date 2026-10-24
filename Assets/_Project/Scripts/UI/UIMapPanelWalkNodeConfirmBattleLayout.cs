using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Enemy preview inside the walk-to-node panel. Shown for battle, elite and boss nodes.
    /// </summary>
    public sealed class UIMapPanelWalkNodeConfirmBattleLayout : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("lableName")] TextMeshProUGUI _labelCharacterName;
        [SerializeField] TextMeshProUGUI _labelCharacterTierName;
        [SerializeField] UIHealthIndicator healthIndicator;
        [SerializeField] UIPlayerPanelSkillsBar[] skills;

        public void Show(EnemyRuntimeSpec spec)
        {
            gameObject.SetActive(true);
            EnsureLabels();
            ApplyNames(spec);

            int maxHp = ResolveMaxHp(spec);
            if (healthIndicator != null)
            {
                if (maxHp > 0)
                    healthIndicator.Bind(() => maxHp, () => maxHp, formatText: FormatHealth);
                else
                    healthIndicator.Unbind();
            }

            ApplySkills(spec != null ? spec.ActiveSkills : null);
        }

        void EnsureLabels()
        {
            if (_labelCharacterName == null)
                _labelCharacterName = FindLabel("LabelCharacterName");

            if (_labelCharacterTierName == null)
                _labelCharacterTierName = FindLabel("LabelCharacterTierName");
        }

        void ApplyNames(EnemyRuntimeSpec spec)
        {
            string characterName = spec != null && spec.Definition != null
                ? spec.Definition.Name
                : string.Empty;
            if (string.IsNullOrEmpty(characterName) && spec != null)
                characterName = spec.DisplayName;

            if (_labelCharacterName != null)
                _labelCharacterName.text = characterName ?? string.Empty;

            string tier = ResolveTierName(spec);
            if (_labelCharacterTierName == null)
                return;

            bool hasTier = !string.IsNullOrEmpty(tier);
            _labelCharacterTierName.gameObject.SetActive(hasTier);
            _labelCharacterTierName.text = hasTier ? tier : string.Empty;
        }

        static string ResolveTierName(EnemyRuntimeSpec spec)
        {
            if (spec == null || spec.Definition == null)
                return string.Empty;

            int floor = spec.EncounterType == MapNodeType.Boss
                ? int.MaxValue
                : spec.EffectiveFloor;
            return EnemyProgressionResolver.ResolveRankName(spec.Definition.RankThresholds, floor);
        }

        TextMeshProUGUI FindLabel(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name != childName)
                    continue;

                return children[i].GetComponent<TextMeshProUGUI>();
            }

            return null;
        }

        public void Hide()
        {
            if (healthIndicator != null)
                healthIndicator.Unbind();

            gameObject.SetActive(false);
        }

        void ApplySkills(IReadOnlyList<SkillDefinition> activeSkills)
        {
            if (skills == null)
                return;

            for (int i = 0; i < skills.Length; i++)
            {
                UIPlayerPanelSkillsBar bar = skills[i];
                if (bar == null)
                    continue;

                SkillDefinition skill = activeSkills != null && i < activeSkills.Count
                    ? activeSkills[i]
                    : null;
                bool show = skill != null;
                bar.gameObject.SetActive(show);
                if (!show)
                    continue;

                bar.Configure(skill, null);
                Transform fade = bar.transform.Find("Fade");
                if (fade != null)
                    fade.gameObject.SetActive(false);

                Button button = bar.GetComponent<Button>();
                if (button != null)
                    button.interactable = false;
            }
        }

        static int ResolveMaxHp(EnemyRuntimeSpec spec)
        {
            if (spec == null || spec.Definition == null)
                return 0;

            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            CharacterStats stats = new CharacterStats(
                spec.HardStats,
                config != null ? config.StatProgression : null);
            stats.RecalculateSoftStatsForBattle();
            if (stats.Soft == null)
                return 0;

            stats.Soft.ScaleMaxHealth(spec.HealthMultiplier);
            stats.Soft.AddBonusMaxHealth(spec.Definition.BonusMaxHp);
            return stats.MaxHealth;
        }

        static string FormatHealth(int current, int max)
        {
            return $"HP {current} / {max}";
        }
    }
}
