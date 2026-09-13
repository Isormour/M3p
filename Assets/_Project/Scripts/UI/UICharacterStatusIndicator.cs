using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UICharacterStatusIndicator : MonoBehaviour
    {
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _stackCount;

        StatusInstance _status;
        BattleCharacter _bearer;
        int _stacks = 1;

        void Awake()
        {
            EnsureLayoutElement();
            EnsureStackLabel();
        }

        void OnValidate()
        {
            if (_icon == null)
                _icon = GetComponentInChildren<Image>(true);

            if (_stackCount == null)
            {
                Transform label = transform.Find("StackCount");
                if (label != null)
                    _stackCount = label.GetComponent<TextMeshProUGUI>();
            }
        }

        public void Configure(StatusInstance status, int stacks = 1, BattleCharacter bearer = null)
        {
            _status = status;
            _bearer = bearer;
            _stacks = Mathf.Max(1, stacks);

            Sprite icon = status != null && status.Definition != null
                ? status.Definition.Icon
                : null;
            SetIcon(icon);
            SetStacks(_stacks);
            EnsureHoverTarget();
            UITooltipTrigger.Ensure(gameObject, () => TooltipBuilder.FromStatus(_status, _bearer, _stacks));
        }

        public void SetIcon(Sprite icon)
        {
            if (_icon == null)
                _icon = GetComponentInChildren<Image>(true);

            if (_icon == null)
                return;

            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        public void SetStacks(int stacks)
        {
            EnsureStackLabel();
            if (_stackCount == null)
                return;

            int count = Mathf.Max(1, stacks);
            _stackCount.text = count.ToString();
            _stackCount.enabled = count > 1;
        }

        void EnsureStackLabel()
        {
            if (_stackCount != null)
                return;

            Transform existing = transform.Find("StackCount");
            if (existing != null)
                _stackCount = existing.GetComponent<TextMeshProUGUI>();

            if (_stackCount != null)
                return;

            GameObject labelObject = new GameObject("StackCount", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)labelObject.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-4f, 2f);
            rect.sizeDelta = new Vector2(48f, 36f);

            _stackCount = labelObject.AddComponent<TextMeshProUGUI>();
            _stackCount.alignment = TextAlignmentOptions.BottomRight;
            _stackCount.fontStyle = FontStyles.Bold;
            _stackCount.fontSize = 28f;
            _stackCount.raycastTarget = false;
        }

        void EnsureHoverTarget()
        {
            if (_icon != null)
                _icon.raycastTarget = true;

            Image root = GetComponent<Image>();
            if (root == null)
                root = gameObject.AddComponent<Image>();

            root.color = Color.clear;
            root.raycastTarget = true;
        }

        void EnsureLayoutElement()
        {
            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout == null)
                layout = gameObject.AddComponent<LayoutElement>();

            RectTransform rect = transform as RectTransform;
            float size = rect != null ? Mathf.Max(rect.sizeDelta.x, rect.sizeDelta.y, 70f) : 70f;
            layout.minWidth = size;
            layout.minHeight = size;
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }
    }
}
