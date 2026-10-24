using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    /// <summary>
    /// Confirms walking to a map node before the token moves. Close cancels; Confirm runs the move.
    /// </summary>
    public sealed class UIMapPanelWalkNodeConfirm : MonoBehaviour
    {
        [SerializeField] GameObject _panelRoot;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _confirmButton;
        [SerializeField] TextMeshProUGUI _labelNodeName;
        [SerializeField] UIMapPanelWalkNodeConfirmBattleLayout _battleLayout;
        [SerializeField] UIMapPanelWalkNodeConfirmShopLayout _shopLayout;

        Action _onConfirm;
        Action _onCancel;
        bool _initialized;
        bool _opening;

        public bool IsOpen => Root.activeInHierarchy;

        GameObject Root => _panelRoot != null ? _panelRoot : gameObject;

        void Awake()
        {
            Initialize();
            // Show activates an inactive panel, and that activation runs Awake in the same call.
            // Hiding here would close the panel before the player ever sees it.
            if (!_opening)
                Hide();
        }

        /// <summary>
        /// Also runs from <see cref="Show"/>, because a panel that starts inactive never reaches
        /// <see cref="Awake"/> until something opens it.
        /// </summary>
        void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            ResolveRefs();

            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);

            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        void ResolveRefs()
        {
            if (_closeButton == null)
                _closeButton = FindDescendantButton("CloseButton");

            if (_confirmButton == null)
            {
                _confirmButton = FindDescendantButton("ConfirmButton");
                if (_confirmButton == null)
                    _confirmButton = FindDescendantButton("ConfirmButton (1)");
            }

            if (_labelNodeName == null)
                _labelNodeName = FindDescendantComponent<TextMeshProUGUI>("LabelNodeType");

            Transform layout = FindDescendant("Layout");
            if (_battleLayout == null && layout != null)
                _battleLayout = layout.GetComponentInChildren<UIMapPanelWalkNodeConfirmBattleLayout>(true);

            if (_shopLayout == null && layout != null)
                _shopLayout = layout.GetComponentInChildren<UIMapPanelWalkNodeConfirmShopLayout>(true);
        }

        /// <summary>
        /// Shows the panel for <paramref name="node"/>. <paramref name="onConfirm"/> runs only if
        /// Confirm is pressed; <paramref name="onCancel"/> runs only if Close is pressed.
        /// </summary>
        public void Show(MapNode node, Action onConfirm, Action onCancel = null)
        {
            Initialize();
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _opening = true;
            UIPanelClosable.BlockWorldInput();
            Root.SetActive(true);
            _opening = false;
            Present(node);
        }

        public void Hide()
        {
            if (Root.activeInHierarchy)
                UIPanelClosable.BlockWorldInput();

            _onConfirm = null;
            _onCancel = null;
            Root.SetActive(false);
        }

        void Present(MapNode node)
        {
            MapNodeType type = node != null ? node.NodeType : MapNodeType.Battle;
            if (_labelNodeName != null)
                _labelNodeName.text = type.DisplayName();

            if (type.IsCombat())
            {
                _shopLayout?.Hide();
                _battleLayout?.Show(ResolveEnemy(node));
            }
            else
            {
                _battleLayout?.Hide();
                EncounterConfig encounter = node != null ? node.Encounter : null;
                string description = encounter != null
                    ? encounter.ResolveDescription(type)
                    : type.DefaultDescription();
                _shopLayout?.Show(description);
            }

            if (Root.transform is RectTransform rootRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        static EnemyRuntimeSpec ResolveEnemy(MapNode node)
        {
            if (node == null || !node.NodeType.IsCombat())
                return null;

            EncounterConfig encounter = node.Encounter;
            if (encounter == null)
                return null;

            MapRunState run = MapRunState.Active;
            EnemyDefinition enemy = run != null
                ? run.PickEncounterEnemy(encounter, node.NodeId)
                : null;
            if (enemy == null && encounter.HasEnemy)
                enemy = encounter.PickEnemy(0, node.NodeId);
            if (enemy == null)
                return null;

            int floor = run != null && run.FloorIndex > 0 ? run.FloorIndex : 1;
            return EnemyProgressionResolver.Resolve(enemy, floor, node.NodeType);
        }

        void HandleCloseClicked()
        {
            Sfx.Play(SfxId.RouteCancel);
            Action cancel = _onCancel;
            Hide();
            cancel?.Invoke();
        }

        void HandleConfirmClicked()
        {
            Sfx.Play(SfxId.RouteConfirm);
            Action callback = _onConfirm;
            Hide();
            callback?.Invoke();
        }

        Button FindDescendantButton(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name != childName)
                    continue;

                Button button = children[i].GetComponent<Button>();
                if (button != null)
                    return button;

                return children[i].GetComponentInChildren<Button>(true);
            }

            return null;
        }

        T FindDescendantComponent<T>(string childName) where T : Component
        {
            Transform child = FindDescendant(childName);
            return child != null ? child.GetComponent<T>() : null;
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
    }
}
