using System;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPanelGainSkillCard : MonoBehaviour
    {
        [SerializeField] UISkillVisuals skillVisuals;
        [SerializeField] Button confirmButton;

        SkillDefinition _skill;
        Action<SkillDefinition> _onConfirm;

        public SkillDefinition Skill => _skill;

        void Awake()
        {
            ResolveRefs();
            WireButton();
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        public void Configure(SkillDefinition skill, Action<SkillDefinition> onConfirm)
        {
            ResolveRefs();
            _skill = skill;
            _onConfirm = onConfirm;
            ApplyVisuals();
            WireButton();
        }

        void ApplyVisuals()
        {
            if (skillVisuals != null)
                skillVisuals.SetSkill(_skill);

            if (confirmButton != null)
                confirmButton.interactable = _skill != null;
        }

        void WireButton()
        {
            if (confirmButton == null)
                return;

            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        void HandleConfirmClicked()
        {
            if (_skill == null)
                return;

            _onConfirm?.Invoke(_skill);
        }

        void ResolveRefs()
        {
            if (skillVisuals == null)
                skillVisuals = GetComponentInChildren<UISkillVisuals>(true);

            if (confirmButton == null)
                confirmButton = FindDescendantButton("confirmButton")
                    ?? FindDescendantButton("ConfirmButton")
                    ?? FindDescendantButton("BasicButton")
                    ?? GetComponent<Button>();
        }

        Button FindDescendantButton(string childName)
        {
            Transform child = FindDescendant(childName);
            if (child == null)
                return null;

            Button button = child.GetComponent<Button>();
            return button != null ? button : child.GetComponentInChildren<Button>(true);
        }

        Transform FindDescendant(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                    return children[i];
            }

            return null;
        }

        void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }
    }
}
