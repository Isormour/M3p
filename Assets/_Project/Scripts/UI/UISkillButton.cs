using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UISkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        static readonly Color EquippedColor = Color.white;
        static readonly Color UnequippedIconColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        static readonly Color UnequippedFrameColor = new Color(0.65f, 0.65f, 0.65f, 1f);

        [SerializeField] Image icon;
        [SerializeField] Image frame;
        [SerializeField] Button _button;

        SkillDefinition _skill;
        Action<SkillDefinition> _clicked;
        Action<SkillDefinition> _hovered;
        Action _unhovered;

        public SkillDefinition Skill => _skill;

        public void Configure(
            SkillDefinition skill,
            bool equipped,
            Action<SkillDefinition> clicked = null,
            Action<SkillDefinition> hovered = null,
            Action unhovered = null)
        {
            _skill = skill;
            _clicked = clicked;
            _hovered = hovered;
            _unhovered = unhovered;

            ApplyIcon();
            ApplyEquippedVisual(equipped);
            WireButton();
            UITooltipTrigger.Ensure(gameObject, BuildTooltip);
        }

        public void SetInteractable(bool interactable)
        {
            EnsureButton();
            if (_button != null)
                _button.interactable = interactable;
        }

        void Awake()
        {
            WireButton();
        }

        void ApplyIcon()
        {
            if (icon == null)
                return;

            Sprite artwork = _skill != null ? _skill.Artwork : null;
            icon.sprite = artwork;
            icon.enabled = artwork != null;
            icon.gameObject.SetActive(artwork != null);
        }

        void ApplyEquippedVisual(bool equipped)
        {
            if (icon != null)
                icon.color = equipped ? EquippedColor : UnequippedIconColor;

            if (frame != null)
                frame.color = equipped ? EquippedColor : UnequippedFrameColor;
        }

        TooltipContent BuildTooltip()
        {
            return TooltipBuilder.FromSkill(_skill, null, null);
        }

        void EnsureButton()
        {
            if (_button == null)
                _button = GetComponent<Button>();
        }

        void WireButton()
        {
            EnsureButton();
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
