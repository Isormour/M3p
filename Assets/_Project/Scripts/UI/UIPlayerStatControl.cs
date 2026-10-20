using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// One row of the stat panel: which perk tree it shows, its current value and the two spend buttons.
    /// The row reports clicks and displays what it is told; the panel owns all the rules.
    /// A locked row shows the padlock; <see cref="Unlock"/> is the only way to play the open sequence.
    /// </summary>
    public sealed class UIPlayerStatControl : MonoBehaviour
    {
        static readonly int DissolveId = Shader.PropertyToID("_DissolveValue");

        [SerializeField] TextMeshProUGUI _nameLabel;
        [SerializeField] TextMeshProUGUI _valueLabel;
        [SerializeField] Button _increaseButton;
        [SerializeField] Button _decreaseButton;
        [SerializeField] Image statIcon;

        [Tooltip("Perk ladder drawn above the row; optional for rows that only show a number.")]
        [SerializeField] UIPanelStatsPerkSection _perkSection;

        [SerializeField] bool _locked;
        [SerializeField] GameObject _lockedStats;
        [SerializeField] GameObject _unlockedStats;

        [Tooltip("Pieces that rise on Y while the lock opens. The shackle is the piece that lifts.")]
        [SerializeField] RectTransform[] _lockOpenParts = Array.Empty<RectTransform>();

        [Tooltip("How far those pieces travel upward, in anchored units.")]
        [SerializeField] float _lockOpenLift = 80f;

        [Min(0f)]
        [SerializeField] float _lockOpenDuration = 1f;

        [Tooltip("Dissolve and label fade after the lock has opened.")]
        [Min(0f)]
        [SerializeField] float _revealDuration = 1f;

        readonly List<Graphic> _dissolveGraphics = new List<Graphic>();
        readonly List<Material> _dissolveMaterials = new List<Material>();

        PerkTree _tree;
        Vector2[] _lockRestPositions;
        Coroutine _unlockRoutine;

        public event Action<int> IncreaseClicked;
        public event Action<int> DecreaseClicked;

        public PerkTree Tree => _tree;
        public int TreeId => _tree != null ? _tree.Id : PerkTree.InvalidId;
        public bool IsLocked => _locked;

        void Awake()
        {
            if (_increaseButton != null)
                _increaseButton.onClick.AddListener(HandleIncreaseClicked);

            if (_decreaseButton != null)
                _decreaseButton.onClick.AddListener(HandleDecreaseClicked);

            CacheLockRests();
            ApplyState();
        }

        void OnDisable()
        {
            // Disabling the row kills the coroutine. The open was already requested, so finish on the unlocked side.
            if (_unlockRoutine == null)
                return;

            StopCoroutine(_unlockRoutine);
            _unlockRoutine = null;
            _locked = false;
            ApplyState();
        }

        void OnDestroy()
        {
            if (_increaseButton != null)
                _increaseButton.onClick.RemoveListener(HandleIncreaseClicked);

            if (_decreaseButton != null)
                _decreaseButton.onClick.RemoveListener(HandleDecreaseClicked);

            DestroyDissolveMaterials();
        }

        /// <summary>
        /// Shows the locked or unlocked parent immediately. A locked row stays closed until <see cref="Unlock"/>.
        /// </summary>
        public void SetLocked(bool locked)
        {
            StopUnlock();
            _locked = locked;
            ApplyState();
        }

        /// <summary>Opens the lock, then reveals the unlocked section.</summary>
        public void Unlock()
        {
            if (!_locked || _unlockRoutine != null)
                return;

            _unlockRoutine = StartCoroutine(UnlockRoutine());
        }

        void OnValidate()
        {
            if (_increaseButton == null)
                _increaseButton = FindChild<Button>("ButtonIncrease");

            if (_decreaseButton == null)
                _decreaseButton = FindChild<Button>("ButtonDecrease");

            if (_nameLabel == null)
                _nameLabel = FindChild<TextMeshProUGUI>("LabelStatName");

            if (_valueLabel == null)
                _valueLabel = FindChild<TextMeshProUGUI>("LabelStatValue");

            if (_perkSection == null)
                _perkSection = GetComponentInChildren<UIPanelStatsPerkSection>(true);

            if (statIcon == null)
                statIcon = FindChild<Image>("StatImage");

            if (_lockedStats == null)
            {
                Transform locked = transform.Find("LockedStats");
                if (locked != null)
                    _lockedStats = locked.gameObject;
            }

            if (_unlockedStats == null)
            {
                Transform unlocked = transform.Find("UnlockedStats");
                if (unlocked != null)
                    _unlockedStats = unlocked.gameObject;
            }

            if (_lockOpenParts == null || _lockOpenParts.Length == 0)
            {
                Transform shackle = transform.Find("LockedStats/Bar");
                if (shackle is RectTransform shackleRect)
                    _lockOpenParts = new[] { shackleRect };
            }
        }

        IEnumerator UnlockRoutine()
        {
            ResetLockParts();
            SetRootActive(_lockedStats, true);
            SetRootActive(_unlockedStats, false);

            yield return Animate(_lockOpenDuration, SetLockOpenAmount);

            // Hide the reveal before the unlocked parent turns on, so it does not flash in fully drawn.
            BindDissolveGraphics();
            SetDissolve(0f);
            SetTextsAlpha(0f);
            SetRootActive(_lockedStats, false);
            SetRootActive(_unlockedStats, true);
            SetDissolve(0f);
            SetTextsAlpha(0f);

            yield return Animate(_revealDuration, SetRevealAmount);

            _locked = false;
            _unlockRoutine = null;
        }

        void ApplyState()
        {
            ResetLockParts();
            SetRootActive(_lockedStats, _locked);
            SetRootActive(_unlockedStats, !_locked);

            if (_locked)
                return;

            BindDissolveGraphics();
            SetDissolve(1f);
            SetTextsAlpha(1f);
        }

        void StopUnlock()
        {
            if (_unlockRoutine == null)
                return;

            StopCoroutine(_unlockRoutine);
            _unlockRoutine = null;
        }

        IEnumerator Animate(float duration, Action<float> apply)
        {
            if (duration <= 0f)
            {
                apply(1f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                apply(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            apply(1f);
        }

        void CacheLockRests()
        {
            int count = _lockOpenParts != null ? _lockOpenParts.Length : 0;
            _lockRestPositions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                RectTransform part = _lockOpenParts[i];
                _lockRestPositions[i] = part != null ? part.anchoredPosition : Vector2.zero;
            }
        }

        void ResetLockParts()
        {
            if (_lockRestPositions == null)
                CacheLockRests();

            SetLockOpenAmount(0f);
        }

        void SetLockOpenAmount(float amount)
        {
            if (_lockOpenParts == null || _lockRestPositions == null)
                return;

            int count = Mathf.Min(_lockOpenParts.Length, _lockRestPositions.Length);
            for (int i = 0; i < count; i++)
            {
                RectTransform part = _lockOpenParts[i];
                if (part == null)
                    continue;

                Vector2 rest = _lockRestPositions[i];
                part.anchoredPosition = new Vector2(rest.x, rest.y + _lockOpenLift * amount);
            }
        }

        void SetRevealAmount(float amount)
        {
            // Perks can be spawned while the section is still fading in.
            BindDissolveGraphics();
            SetDissolve(amount);
            SetTextsAlpha(amount);
        }

        void BindDissolveGraphics()
        {
            if (_unlockedStats == null)
                return;

            Graphic[] graphics = _unlockedStats.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic is TMP_Text)
                    continue;

                Material source = graphic.material;
                if (source == null || !source.HasProperty(DissolveId) || OwnsDissolveMaterial(source))
                    continue;

                // The authored material is shared by every row; writing it would dissolve them all.
                Material copy = new Material(source);
                graphic.material = copy;
                _dissolveGraphics.Add(graphic);
                _dissolveMaterials.Add(copy);
            }
        }

        bool OwnsDissolveMaterial(Material material)
        {
            for (int i = 0; i < _dissolveMaterials.Count; i++)
            {
                if (_dissolveMaterials[i] == material)
                    return true;
            }

            return false;
        }

        void SetDissolve(float value)
        {
            for (int i = 0; i < _dissolveMaterials.Count; i++)
            {
                Material material = _dissolveMaterials[i];
                if (material != null)
                    material.SetFloat(DissolveId, value);
            }

            for (int i = 0; i < _dissolveGraphics.Count; i++)
            {
                Graphic graphic = _dissolveGraphics[i];
                if (graphic == null)
                    continue;

                // Masks copy the material; the copy only picks up the value if it is set too.
                Material rendered = graphic.materialForRendering;
                if (rendered != null && rendered.HasProperty(DissolveId))
                    rendered.SetFloat(DissolveId, value);
            }
        }

        void SetTextsAlpha(float alpha)
        {
            if (_unlockedStats == null)
                return;

            TextMeshProUGUI[] labels = _unlockedStats.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshProUGUI label = labels[i];
                if (label != null)
                    label.alpha = alpha;
            }
        }

        void DestroyDissolveMaterials()
        {
            for (int i = 0; i < _dissolveMaterials.Count; i++)
            {
                Material material = _dissolveMaterials[i];
                if (material != null)
                    Destroy(material);
            }

            _dissolveMaterials.Clear();
            _dissolveGraphics.Clear();
        }

        static void SetRootActive(GameObject root, bool active)
        {
            if (root != null && root.activeSelf != active)
                root.SetActive(active);
        }

        /// <summary>Tells the row which perk tree it represents. Called by the panel, not authored per row.</summary>
        public void Bind(PerkTree tree)
        {
            _tree = tree;

            if (_nameLabel != null)
                _nameLabel.text = tree != null ? tree.DisplayName : string.Empty;
        }

        /// <param name="progression">Source of the cap the ladder is scaled against.</param>
        /// <param name="value">Committed value plus anything pending, so the row reads as the result.</param>
        /// <param name="pendingPoints">Points spent here but not yet written to the profile.</param>
        /// <param name="canIncrease">False once there are no points left to spend or the cap is reached.</param>
        public void Refresh(StatProgressionConfig progression, int value, int pendingPoints, bool canIncrease)
        {
            if (_valueLabel != null)
                _valueLabel.text = value.ToString();

            ApplyStatIcon();

            if (_perkSection != null)
            {
                _perkSection.Bind(_tree, progression);
                _perkSection.Refresh(value);
            }

            if (_increaseButton != null)
                _increaseButton.interactable = canIncrease;

            if (_decreaseButton != null)
                _decreaseButton.interactable = pendingPoints > 0;
        }

        void ApplyStatIcon()
        {
            if (statIcon == null)
                return;

            Sprite icon = _tree != null ? _tree.Icon : null;
            statIcon.sprite = icon;
            statIcon.enabled = icon != null;
        }

        void HandleIncreaseClicked()
        {
            if (TreeId != PerkTree.InvalidId)
                IncreaseClicked?.Invoke(TreeId);
        }

        void HandleDecreaseClicked()
        {
            if (TreeId != PerkTree.InvalidId)
                DecreaseClicked?.Invoke(TreeId);
        }

        T FindChild<T>(string childName) where T : Component
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }
    }
}
