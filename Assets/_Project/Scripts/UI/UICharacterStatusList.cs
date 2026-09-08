using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    public sealed class UICharacterStatusList : MonoBehaviour
    {
        [SerializeField] UICharacterStatusIndicator _indicatorPrefab;
        [SerializeField] RectTransform _container;

        BattleCharacter _character;
        readonly List<UICharacterStatusIndicator> _spawned = new List<UICharacterStatusIndicator>();

        void OnValidate()
        {
            if (_container == null)
                _container = transform as RectTransform;
        }

        void OnDisable()
        {
            UnbindCharacter();
            ClearIndicators();
        }

        void OnDestroy()
        {
            UnbindCharacter();
            ClearIndicators();
        }

        public void SetCharacter(BattleCharacter character)
        {
            if (_character == character)
            {
                Rebuild();
                return;
            }

            UnbindCharacter();
            _character = character;
            BindCharacter();
            Rebuild();
        }

        void BindCharacter()
        {
            if (_character != null)
                _character.StatusesChanged += Rebuild;
        }

        void UnbindCharacter()
        {
            if (_character == null)
                return;

            _character.StatusesChanged -= Rebuild;
            _character = null;
        }

        void Rebuild()
        {
            ClearIndicators();

            if (_character == null)
                return;

            if (_indicatorPrefab == null)
            {
                Debug.LogError($"{nameof(UICharacterStatusList)}: assign {nameof(_indicatorPrefab)}.", this);
                return;
            }

            RectTransform parent = _container != null ? _container : transform as RectTransform;
            IReadOnlyList<StatusInstance> statuses = _character.Statuses;

            for (int i = 0; i < statuses.Count; i++)
            {
                StatusInstance status = statuses[i];
                if (status?.Definition == null)
                    continue;

                UICharacterStatusIndicator indicator = Instantiate(_indicatorPrefab, parent);
                indicator.name = $"Status_{status.Definition.name}";
                indicator.Configure(status);
                _spawned.Add(indicator);
            }
        }

        void ClearIndicators()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i].gameObject);
            }

            _spawned.Clear();
        }
    }
}
