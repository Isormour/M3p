using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UICardVisuals : MonoBehaviour
    {
        const string ColorMultProperty = "_ColorMult";
        const float ColorMultUnselected = 0f;
        const float ColorMultSelected = 1f;

        [SerializeField] TextMeshProUGUI descriptionLabel;
        [SerializeField] Image cardImage;
        [SerializeField] Transform _costContainer;
        [SerializeField] GameObject _costIndicatorPrefab;
        [SerializeField] GameObject _frameMask;
        [SerializeField] Image _frameImage;

        readonly List<GameObject> _costIndicators = new List<GameObject>();
        Material _frameMaterial;

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

        public void SetSelectedHighlight(bool selected)
        {
            EnsureFrameMaterial();
            if (_frameMaterial != null)
                _frameMaterial.SetFloat(ColorMultProperty, selected ? ColorMultSelected : ColorMultUnselected);
        }

        void Awake()
        {
            ResolveFrameMask();
            SetSelectedHighlight(false);
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

        void EnsureFrameMaterial()
        {
            if (_frameImage == null)
            {
                Transform frame = transform.Find("Frame");
                if (frame != null)
                    _frameImage = frame.GetComponent<Image>();
            }

            if (_frameImage == null || _frameMaterial != null)
                return;

            Material source = _frameImage.material;
            if (source == null || !source.HasProperty(ColorMultProperty))
                return;

            _frameMaterial = new Material(source);
            _frameImage.material = _frameMaterial;
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

            if (_frameMaterial != null)
                Destroy(_frameMaterial);
        }
    }
}
