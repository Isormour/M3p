using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Map HUD panel: owned skills in the grid, equipped skills in the five chosen slots.
    /// Clicking a grid skill equips or unequips it. Hover previews the skill.
    /// </summary>
    public sealed class UIPanelChooseSkill : UIPanelClosable
    {
        [SerializeField] GridLayoutGroup skillsGrid;
        [SerializeField] Button skillButtonPrefab;
        [SerializeField] UISkillVisuals chosenSkillVisuals;
        [SerializeField] Button[] chosenSkills;
        [SerializeField] GameObject manaPrefab;
        [SerializeField] HorizontalLayoutGroup manaLayout;

        readonly List<Button> _ownedViews = new List<Button>();
        readonly List<UIPanelPlayerManaBar> _manaViews = new List<UIPanelPlayerManaBar>();

        void OnEnable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged += Refresh;

            Refresh();
        }

        void OnDisable()
        {
            ProfileManager profiles = Profiles;
            if (profiles != null)
                profiles.ProfileChanged -= Refresh;

            ClearOwnedViews();
            ClearManaViews();
        }

        public override void Show()
        {
            base.Show();
            Refresh();
        }

        static ProfileManager Profiles => GameManager.Instance != null ? GameManager.Instance.ProfileManager : null;

        static GameConfig Config => GameManager.Instance != null ? GameManager.Instance.Config : null;

        static SkillConfig Skills => Config != null ? Config.Skills : null;

        protected override void ResolveRefs()
        {
            base.ResolveRefs();

            if (skillsGrid == null)
            {
                Transform grid = FindDescendant("SkillGrid") ?? FindDescendant("skillsGrid");
                if (grid != null)
                    skillsGrid = grid.GetComponent<GridLayoutGroup>();
            }

            if (chosenSkillVisuals == null)
                chosenSkillVisuals = FindDescendantComponent<UISkillVisuals>("SkillVisuals")
                    ?? FindDescendantComponent<UISkillVisuals>("chosenSkillVisuals")
                    ?? GetComponentInChildren<UISkillVisuals>(true);

            if (chosenSkills == null || chosenSkills.Length == 0)
            {
                Transform slots = FindDescendant("ChosenSkills") ?? FindDescendant("chosenSkills");
                if (slots != null)
                    chosenSkills = slots.GetComponentsInChildren<Button>(true);
            }

            if (manaLayout == null)
            {
                Transform container = FindDescendant("Container") ?? FindDescendant("ManaCost");
                if (container != null)
                    manaLayout = container.GetComponent<HorizontalLayoutGroup>()
                        ?? container.GetComponentInChildren<HorizontalLayoutGroup>(true);
            }

            if (skillButtonPrefab == null && chosenSkills != null && chosenSkills.Length > 0)
                skillButtonPrefab = chosenSkills[0];
        }

        void Refresh()
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            SkillConfig skillConfig = Skills;
            if (profile == null || skillConfig == null)
            {
                ClearPreview();
                return;
            }

            BuildOwnedSkills(profile, skillConfig);
            RefreshChosenSlots(profile, skillConfig);
        }

        void BuildOwnedSkills(PlayerProfile profile, SkillConfig skillConfig)
        {
            ClearOwnedViews();

            if (skillsGrid == null)
            {
                Debug.LogError($"{nameof(UIPanelChooseSkill)}: assign {nameof(skillsGrid)} on the prefab.", this);
                return;
            }

            if (skillButtonPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelChooseSkill)}: assign {nameof(skillButtonPrefab)} on the prefab.", this);
                return;
            }

            bool loadoutFull = IsLoadoutFull(profile);

            if (profile.Skills == null)
                return;

            for (int i = 0; i < profile.Skills.Count; i++)
            {
                int skillId = profile.Skills[i].SkillId;
                if (!skillConfig.TryGetSkill(skillId, out SkillDefinition skill) || skill == null)
                    continue;

                bool equipped = IsInLoadout(profile, skillId);
                Button view = Instantiate(skillButtonPrefab, skillsGrid.transform);
                view.gameObject.SetActive(true);
                view.name = $"OwnedSkill_{skill.name}";
                ApplySkillIcon(view, skill);
                ApplyEquippedVisual(view, equipped);
                BindSkillTarget(view, skill, HandleOwnedSkillClicked, HandleSkillHovered, ClearPreview);
                view.interactable = equipped || !loadoutFull;
                _ownedViews.Add(view);
            }
        }

        void RefreshChosenSlots(PlayerProfile profile, SkillConfig skillConfig)
        {
            if (chosenSkills == null || chosenSkills.Length == 0)
            {
                Debug.LogError($"{nameof(UIPanelChooseSkill)}: assign {nameof(chosenSkills)} on the prefab.", this);
                return;
            }

            IReadOnlyList<int> loadout = profile.SkillLoadout;
            for (int i = 0; i < chosenSkills.Length; i++)
            {
                Button slot = chosenSkills[i];
                if (slot == null)
                    continue;

                int slotIndex = i;
                SkillDefinition skill = null;
                if (loadout != null && i < loadout.Count)
                    skillConfig.TryGetSkill(loadout[i], out skill);

                ApplySkillIcon(slot, skill);
                ApplyEquippedVisual(slot, skill != null);
                BindSkillTarget(slot, skill, _ => HandleChosenSkillClicked(slotIndex), HandleSkillHovered, ClearPreview);
                slot.interactable = skill != null;
            }
        }

        void HandleOwnedSkillClicked(SkillDefinition skill)
        {
            if (skill == null)
                return;

            SkillConfig skillConfig = Skills;
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (skillConfig == null || profile == null)
                return;

            int skillId = skillConfig.GetSkillId(skill);
            if (skillId == SkillConfig.InvalidSkillId)
                return;

            bool changed = IsInLoadout(profile, skillId)
                ? profile.TryRemoveSkillFromLoadout(skillId)
                : profile.TryAddSkillToLoadout(skillId);
            if (!changed)
                return;

            Profiles.Save();
        }

        void HandleChosenSkillClicked(int slotIndex)
        {
            PlayerProfile profile = Profiles?.CurrentProfile;
            if (profile == null || !profile.TryRemoveLoadoutAt(slotIndex))
                return;

            Profiles.Save();
        }

        void HandleSkillHovered(SkillDefinition skill)
        {
            if (chosenSkillVisuals != null)
                chosenSkillVisuals.SetSkill(skill);

            BuildManaCosts(skill);
        }

        void ClearPreview()
        {
            if (chosenSkillVisuals != null)
                chosenSkillVisuals.SetSkill(null);

            ClearManaViews();
        }

        void BuildManaCosts(SkillDefinition skill)
        {
            ClearManaViews();
            if (skill == null)
                return;

            if (manaLayout == null)
            {
                Debug.LogError($"{nameof(UIPanelChooseSkill)}: assign {nameof(manaLayout)} on the prefab.", this);
                return;
            }

            if (manaPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelChooseSkill)}: assign {nameof(manaPrefab)} on the prefab.", this);
                return;
            }

            GameConfig config = Config;
            TileTypeManaCost[] costs = skill.ManaCosts;
            for (int i = 0; i < costs.Length; i++)
            {
                TileTypeManaCost cost = costs[i];
                if (cost.Amount <= 0 || cost.TileType == null)
                    continue;

                int typeId = config != null ? config.GetTileTypeId(cost.TileType) : -1;
                Sprite icon = typeId >= 0 && config != null
                    ? config.GetTileTypeSprite(typeId)
                    : cost.TileType.Sprite;
                Match3.TileTypeGraphics runeGraphics = typeId >= 0 && config != null
                    ? config.GetTileTypeRuneGraphics(typeId)
                    : cost.TileType.RuneGraphics;

                AddManaView($"SkillCost_{cost.TileType.name}", typeId, icon, runeGraphics?.SpriteMaterial, cost.Amount);
            }

            if (skill.DistinctColorManaCost > 0)
                AddManaView("SkillCost_DistinctColors", -1, null, null, skill.DistinctColorManaCost);
        }

        void AddManaView(string viewName, int typeId, Sprite icon, Material material, int amount)
        {
            GameObject instance = Instantiate(manaPrefab, manaLayout.transform);
            instance.SetActive(true);
            instance.name = viewName;

            UIPanelPlayerManaBar view = instance.GetComponent<UIPanelPlayerManaBar>();
            if (view == null)
            {
                Destroy(instance);
                Debug.LogError(
                    $"{nameof(UIPanelChooseSkill)}: {nameof(manaPrefab)} needs a {nameof(UIPanelPlayerManaBar)}.",
                    this);
                return;
            }

            view.Configure(typeId, icon, material);
            view.SetAmount(amount);
            _manaViews.Add(view);
        }

        static void ApplySkillIcon(Button button, SkillDefinition skill)
        {
            Image icon = FindIconImage(button);
            if (icon == null)
                return;

            Sprite artwork = skill != null ? skill.Artwork : null;
            icon.sprite = artwork;
            icon.enabled = artwork != null;
            icon.gameObject.SetActive(artwork != null);
        }

        static void ApplyEquippedVisual(Button button, bool equipped)
        {
            Image icon = FindIconImage(button);
            if (icon == null)
                return;

            icon.color = equipped ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }

        static bool IsLoadoutFull(PlayerProfile profile)
        {
            return profile.SkillLoadout != null
                && profile.SkillLoadout.Count >= SkillConfig.MaxLoadoutSize;
        }

        static Image FindIconImage(Button button)
        {
            if (button == null)
                return null;

            Transform icon = button.transform.Find("icon") ?? button.transform.Find("Icon");
            if (icon != null)
            {
                Image image = icon.GetComponent<Image>();
                if (image != null)
                    return image;
            }

            Image[] images = button.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject != button.gameObject)
                    return images[i];
            }

            return button.GetComponent<Image>();
        }

        static void BindSkillTarget(
            Button button,
            SkillDefinition skill,
            System.Action<SkillDefinition> clicked,
            System.Action<SkillDefinition> hovered,
            System.Action unhovered)
        {
            UISkillPointerTarget target = button.GetComponent<UISkillPointerTarget>();
            if (target == null)
                target = button.gameObject.AddComponent<UISkillPointerTarget>();

            target.Bind(skill, clicked, hovered, unhovered);
        }

        static bool IsInLoadout(PlayerProfile profile, int skillId)
        {
            if (profile.SkillLoadout == null || skillId == SkillConfig.InvalidSkillId)
                return false;

            return profile.SkillLoadout.Contains(skillId);
        }

        void ClearOwnedViews()
        {
            for (int i = 0; i < _ownedViews.Count; i++)
            {
                if (_ownedViews[i] != null)
                    Destroy(_ownedViews[i].gameObject);
            }

            _ownedViews.Clear();
        }

        void ClearManaViews()
        {
            for (int i = 0; i < _manaViews.Count; i++)
            {
                if (_manaViews[i] != null)
                    Destroy(_manaViews[i].gameObject);
            }

            _manaViews.Clear();
        }
    }

    public sealed class UISkillPointerTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        SkillDefinition _skill;
        System.Action<SkillDefinition> _clicked;
        System.Action<SkillDefinition> _hovered;
        System.Action _unhovered;
        Button _button;

        public void Bind(
            SkillDefinition skill,
            System.Action<SkillDefinition> clicked,
            System.Action<SkillDefinition> hovered,
            System.Action unhovered)
        {
            _skill = skill;
            _clicked = clicked;
            _hovered = hovered;
            _unhovered = unhovered;
            WireButton();
        }

        void Awake()
        {
            WireButton();
        }

        void WireButton()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null)
                return;

            _button.onClick.RemoveListener(HandleClick);
            _button.onClick.AddListener(HandleClick);
        }

        void HandleClick()
        {
            if (_skill == null)
                return;

            _clicked?.Invoke(_skill);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_skill == null)
                return;

            _hovered?.Invoke(_skill);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _unhovered?.Invoke();
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }
    }
}
