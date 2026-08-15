using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UICardVisuals : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI descriptionLabel;
        [SerializeField] Image cardImage;
        [SerializeField] Transform _costContainer;
        [SerializeField] GameObject _costIndicatorPrefab;
        [SerializeField] GameObject _frameMask;

        readonly List<GameObject> _costIndicators = new List<GameObject>();

        public void SetCardData(BoardActionCardDefinition card)
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

        public void SetFrameMaskEnabled(bool enabled)
        {
            ResolveFrameMask();

            if (_frameMask != null)
                _frameMask.SetActive(enabled);
        }

        void Awake()
        {
            ResolveFrameMask();
        }

        void OnValidate()
        {
            ResolveFrameMask();
        }

        void ResolveFrameMask()
        {
            if (_frameMask != null)
                return;

            Transform child = transform.Find("FrameMask");
            if (child != null)
                _frameMask = child.gameObject;
        }

        void BuildCostIndicators(int cost)
        {
            ClearCostIndicators();

            if (cost <= 0)
                return;

            if (_costContainer == null)
            {
                Debug.LogError($"{nameof(UICardVisuals)}: assign {nameof(_costContainer)} on the prefab.", this);
                return;
            }

            if (_costIndicatorPrefab == null)
            {
                Debug.LogError($"{nameof(UICardVisuals)}: assign {nameof(_costIndicatorPrefab)} on the prefab.", this);
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

        void OnDestroy()
        {
            ClearCostIndicators();
        }
    }
}
