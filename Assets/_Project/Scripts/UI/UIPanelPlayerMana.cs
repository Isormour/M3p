using System.Collections;
using System.Collections.Generic;
using Match3;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPanelPlayerMana : MonoBehaviour
    {
        [SerializeField] RectTransform _barContainer;
        [SerializeField] UIPanelPlayerManaBar _barPrefab;

        PlayerBattleCharacter _player;
        SoftStats _boundSoftStats;

        readonly Dictionary<int, UIPanelPlayerManaBar> _barsByTypeId = new Dictionary<int, UIPanelPlayerManaBar>();

        Match3Board _board;
        Coroutine _watchBoardRoutine;

        public void SetPlayer(PlayerBattleCharacter player)
        {
            UnbindSoftStats();
            _player = player;
            BindSoftStats();
            RefreshAllBars();
        }

        void OnEnable()
        {
            if (_watchBoardRoutine == null)
                _watchBoardRoutine = StartCoroutine(WatchBoardRoutine());
        }

        void OnDisable()
        {
            if (_watchBoardRoutine != null)
            {
                StopCoroutine(_watchBoardRoutine);
                _watchBoardRoutine = null;
            }

            UnbindBoard();
            UnbindSoftStats();
            ClearBars();
        }

        IEnumerator WatchBoardRoutine()
        {
            Match3Board boundBoard = null;

            while (true)
            {
                Match3Board activeBoard = BattleManager.Instance != null ? BattleManager.Instance.ActiveBoard : null;

                if (activeBoard != boundBoard)
                {
                    if (boundBoard != null)
                    {
                        UnbindBoard();
                        ClearBars();
                    }

                    if (activeBoard != null)
                        BindBoard(activeBoard);

                    boundBoard = activeBoard;
                }

                if (_player != BattleManager.Instance?.Player)
                    SetPlayer(BattleManager.Instance?.Player);

                yield return null;
            }
        }

        void BindBoard(Match3Board board)
        {
            if (board == null)
                return;

            UnbindBoard();
            _board = board;
            _board.TilesDestroyedInWave.AddListener(HandleTilesDestroyedInWave);
            BindSoftStats();
            BuildBars();
            RefreshAllBars();
        }

        void UnbindBoard()
        {
            if (_board != null)
                _board.TilesDestroyedInWave.RemoveListener(HandleTilesDestroyedInWave);

            _board = null;
        }

        void BindSoftStats()
        {
            UnbindSoftStats();

            _boundSoftStats = _player?.Stats?.Soft;
            if (_boundSoftStats != null)
                _boundSoftStats.Changed += HandleSoftStatsChanged;
        }

        void UnbindSoftStats()
        {
            if (_boundSoftStats != null)
                _boundSoftStats.Changed -= HandleSoftStatsChanged;

            _boundSoftStats = null;
        }

        void HandleSoftStatsChanged()
        {
            RefreshAllBars();
        }

        void BuildBars()
        {
            ClearBars();
            EnsureBarContainer();

            if (_board == null)
                return;

            if (_barPrefab == null)
            {
                Debug.LogError($"{nameof(UIPanelPlayerMana)}: assign {nameof(_barPrefab)}.", this);
                return;
            }

            for (int typeId = 0; typeId < _board.TileTypeCount; typeId++)
            {
                UIPanelPlayerManaBar bar = Instantiate(_barPrefab, _barContainer);
                bar.name = $"ManaBar_Type{typeId}";
                bar.Configure(typeId, _board.GetTileTypeSprite(typeId));
                _barsByTypeId[typeId] = bar;
            }
        }

        void HandleTilesDestroyedInWave(int tileTypeId, int destroyedCount)
        {
            if (_player?.Stats?.Soft == null || destroyedCount <= 0)
                return;

            _player.Stats.Soft.AddManaFromBrokenTiles(tileTypeId, destroyedCount);
        }

        void RefreshAllBars()
        {
            if (_player?.Stats?.Soft == null)
                return;

            SoftStats softStats = _player.Stats.Soft;
            foreach (KeyValuePair<int, UIPanelPlayerManaBar> entry in _barsByTypeId)
                entry.Value.SetAmount(softStats.GetManaForTileType(entry.Key));
        }

        void EnsureBarContainer()
        {
            if (_barContainer == null)
                _barContainer = transform as RectTransform;

            if (_barContainer == null)
                return;

            VerticalLayoutGroup layout = _barContainer.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = _barContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 6f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
        }

        void ClearBars()
        {
            foreach (UIPanelPlayerManaBar bar in _barsByTypeId.Values)
            {
                if (bar != null)
                    Destroy(bar.gameObject);
            }

            _barsByTypeId.Clear();
        }
    }
}
