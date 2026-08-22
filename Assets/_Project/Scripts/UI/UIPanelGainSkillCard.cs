using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPanelGainSkillCard : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI labelSKillName;
        [SerializeField] Image skillArtwork;
        [SerializeField] TextMeshProUGUI labelDescription;
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
            if (labelSKillName != null)
                labelSKillName.text = _skill != null ? _skill.DisplayName : string.Empty;

            if (labelDescription != null)
                labelDescription.text = _skill != null ? _skill.Description : string.Empty;

            if (skillArtwork != null)
            {
                Sprite artwork = _skill != null ? _skill.Artwork : null;
                skillArtwork.sprite = artwork;
                skillArtwork.enabled = artwork != null;
            }

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
            if (labelSKillName == null)
                labelSKillName = FindDescendantComponent<TextMeshProUGUI>("labelSKillName")
                    ?? FindDescendantComponent<TextMeshProUGUI>("SkillName");

            if (skillArtwork == null)
            {
                Transform artwork = FindDescendant("skillArtwork") ?? FindDescendant("SkillArtwork");
                if (artwork != null)
                    skillArtwork = artwork.GetComponent<Image>();
            }

            if (labelDescription == null)
                labelDescription = FindDescendantComponent<TextMeshProUGUI>("labelDescription")
                    ?? FindDescendantComponent<TextMeshProUGUI>("Description");

            if (confirmButton == null)
                confirmButton = FindDescendantButton("confirmButton")
                    ?? FindDescendantButton("ConfirmButton")
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

        T FindDescendantComponent<T>(string childName) where T : Component
        {
            Transform child = FindDescendant(childName);
            return child != null ? child.GetComponentInChildren<T>(true) : null;
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
