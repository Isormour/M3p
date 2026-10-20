using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// One tree on the unlock offer. Text, icon and the first thresholds come from the tree and its perks.
    /// </summary>
    public sealed class UIPerkTreeOfferCard : MonoBehaviour
    {
        const float RowStep = 54f;

        [SerializeField] Image _frame;
        [SerializeField] Image _fill;
        [SerializeField] GameObject _check;
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _name;
        [SerializeField] TextMeshProUGUI _subtitle;
        [SerializeField] TextMeshProUGUI _description;
        [SerializeField] TextMeshProUGUI _status;
        [SerializeField] UIPerkTreeOfferRow _rowTemplate;
        [SerializeField] RectTransform _rowParent;
        [SerializeField] Button _button;

        [SerializeField] Color _frameIdle = new Color(0.42f, 0.34f, 0.18f, 0.45f);
        [SerializeField] Color _frameSelected = new Color(0.86f, 0.68f, 0.32f, 1f);
        [SerializeField] Color _fillIdle = new Color(0.09f, 0.092f, 0.105f, 1f);
        [SerializeField] Color _fillSelected = new Color(0.13f, 0.11f, 0.07f, 1f);
        [SerializeField] Color _statusIdle = new Color(0.62f, 0.58f, 0.42f, 1f);
        [SerializeField] Color _statusSelected = new Color(0.86f, 0.70f, 0.38f, 1f);

        readonly List<UIPerkTreeOfferRow> _rows = new List<UIPerkTreeOfferRow>();

        string _chooseLabel = "Wybierz drzewko";
        string _chosenLabel = "Drzewko odblokowane";
        bool _selected;

        public event Action<UIPerkTreeOfferCard> Clicked;

        public PerkTree Tree { get; private set; }

        void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
                _button.onClick.AddListener(HandleClicked);
            }
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        public void Bind(
            PerkTree tree,
            PerkConfig catalog,
            int previewCount,
            string thresholdFormat,
            string chooseLabel,
            string chosenLabel)
        {
            Tree = tree;
            _chooseLabel = string.IsNullOrEmpty(chooseLabel) ? _chooseLabel : chooseLabel;
            _chosenLabel = string.IsNullOrEmpty(chosenLabel) ? _chosenLabel : chosenLabel;

            if (_icon != null)
            {
                _icon.sprite = tree != null ? tree.Icon : null;
                _icon.enabled = _icon.sprite != null;
                _icon.preserveAspect = true;
            }

            if (_name != null)
                _name.text = tree != null ? tree.DisplayName : string.Empty;

            if (_subtitle != null)
                _subtitle.text = tree != null ? tree.Subtitle : string.Empty;

            if (_description != null)
                _description.text = tree != null ? tree.Description : string.Empty;

            RebuildRows(tree, catalog, previewCount, thresholdFormat);
            SetSelected(_selected);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;

            if (_frame != null)
                _frame.color = selected ? _frameSelected : _frameIdle;

            if (_fill != null)
                _fill.color = selected ? _fillSelected : _fillIdle;

            if (_check != null)
                _check.SetActive(selected);

            if (_status != null)
            {
                _status.text = selected ? _chosenLabel : _chooseLabel;
                _status.color = selected ? _statusSelected : _statusIdle;
            }
        }

        void RebuildRows(PerkTree tree, PerkConfig catalog, int previewCount, string thresholdFormat)
        {
            ClearRows();

            if (tree == null || _rowTemplate == null || _rowParent == null || previewCount <= 0)
                return;

            StatPerk[] ladder = tree.Perks;
            int shown = 0;
            for (int i = 0; i < ladder.Length && shown < previewCount; i++)
            {
                PerkDefinition perk = ladder[i].Perk;
                if (perk == null)
                    continue;

                if (catalog != null && !catalog.Contains(perk))
                    continue;

                UIPerkTreeOfferRow row = Instantiate(_rowTemplate, _rowParent);
                row.gameObject.SetActive(true);
                row.name = $"Perk{ladder[i].StatLevel}";
                row.Bind(perk, ladder[i].StatLevel, thresholdFormat);

                RectTransform rect = (RectTransform)row.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(-12f, 50f);
                rect.anchoredPosition = new Vector2(0f, -shown * RowStep);

                _rows.Add(row);
                shown++;
            }

            for (int i = 0; i < _rows.Count; i++)
                _rows[i].SetContinue(i < _rows.Count - 1);
        }

        void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    DestroyObject(_rows[i].gameObject);
            }

            _rows.Clear();

            if (_rowParent == null)
                return;

            for (int i = _rowParent.childCount - 1; i >= 0; i--)
            {
                Transform child = _rowParent.GetChild(i);
                if (_rowTemplate != null && child == _rowTemplate.transform)
                    continue;

                if (child.GetComponent<UIPerkTreeOfferRow>() != null)
                    DestroyOwned(child.gameObject);
            }
        }

        void HandleClicked()
        {
            Clicked?.Invoke(this);
        }

        static void DestroyOwned(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
