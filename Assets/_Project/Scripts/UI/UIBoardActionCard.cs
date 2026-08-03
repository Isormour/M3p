using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIBoardActionCard : MonoBehaviour
    {
        const float SelectedScale = 1.2f;

        [SerializeField] Button _button;
        [SerializeField] TextMeshProUGUI descriptionLabel;
        [SerializeField] Image cardImage;
        [SerializeField] Transform _costContainer;
        [SerializeField] GameObject _costIndicatorPrefab;

        readonly List<GameObject> _costIndicators = new List<GameObject>();

        BoardActionCardDefinition _card;
        CardPlayController _controller;
        int _handIndex = -1;

        public BoardActionCardDefinition Card => _card;
        public int HandIndex => _handIndex;

        void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null)
            {
                Debug.LogError($"{nameof(UIBoardActionCard)}: assign {nameof(_button)} or add a {nameof(Button)} component.", this);
                return;
            }

            _button.onClick.AddListener(HandleClick);
        }

        public void Configure(BoardActionCardDefinition card, CardPlayController controller, int handIndex)
        {
            _card = card;
            _controller = controller;
            _handIndex = handIndex;
            SetSelected(false);
            SetCardData(card);
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;
        }

        public void SetSelected(bool selected)
        {
            transform.localScale = Vector3.one * (selected ? SelectedScale : 1f);
        }

        void SetCardData(BoardActionCardDefinition card)
        {
            if (descriptionLabel != null)
                descriptionLabel.text = card != null ? card.Description : string.Empty;

            if (cardImage != null)
            {
                Sprite artwork = card?.Artwork;
                cardImage.sprite = artwork;
                cardImage.enabled = artwork != null;
            }

            BuildCostIndicators(card != null ? card.ActionPointCost : 0);
        }

        void BuildCostIndicators(int cost)
        {
            ClearCostIndicators();

            if (cost <= 0)
                return;

            if (_costContainer == null)
            {
                Debug.LogError($"{nameof(UIBoardActionCard)}: assign {nameof(_costContainer)} on the prefab.", this);
                return;
            }

            if (_costIndicatorPrefab == null)
            {
                Debug.LogError($"{nameof(UIBoardActionCard)}: assign {nameof(_costIndicatorPrefab)} on the prefab.", this);
                return;
            }

            for (int i = 0; i < cost; i++)
            {
                GameObject indicator = Instantiate(_costIndicatorPrefab, _costContainer);
                indicator.name = $"{_costIndicatorPrefab.name}_{i + 1}";
                _costIndicators.Add(indicator);
            }
        }

        void ClearCostIndicators()
        {
            for (int i = 0; i < _costIndicators.Count; i++)
            {
                if (_costIndicators[i] != null)
                    Destroy(_costIndicators[i]);
            }

            _costIndicators.Clear();
        }

        void HandleClick()
        {
            if (_card == null || _controller == null)
                return;

            _controller.SelectCardAt(_handIndex);
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);

            ClearCostIndicators();
        }
    }
}
