using System;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>One card waiting in the sequence, with the commands it will run at Resolve.</summary>
    public sealed class QueuedCard
    {
        public QueuedCard(
            BoardActionCardDefinition card,
            int handIndex,
            int staminaPaid,
            Vector2Int[] targets,
            int extraChoice,
            int seed,
            BoardOp[] ops)
        {
            Card = card;
            HandIndex = handIndex;
            StaminaPaid = staminaPaid;
            Targets = targets ?? Array.Empty<Vector2Int>();
            ExtraChoice = extraChoice;
            Seed = seed;
            Ops = ops ?? Array.Empty<BoardOp>();
        }

        public BoardActionCardDefinition Card { get; }

        /// <summary>Where the card sat in hand, so Undo can put it back in the same place.</summary>
        public int HandIndex { get; }

        public int StaminaPaid { get; }

        public Vector2Int[] Targets { get; }

        public int ExtraChoice { get; }

        public int Seed { get; }

        public BoardOp[] Ops { get; }
    }

    /// <summary>
    /// The sequence the player is building this turn. Holds the predicted board: the real board plus
    /// every queued command, which is what later cards target and what the preview draws. Cards enter
    /// and leave only at the end, because removing one from the middle would invalidate the targets of
    /// everything queued behind it.
    /// </summary>
    public sealed class CardQueue
    {
        readonly List<QueuedCard> _entries = new List<QueuedCard>();

        SimBoard _baseState;
        SimBoard _predicted;

        /// <summary>Raised whenever the queue or the predicted board changes.</summary>
        public event Action Changed;

        public IReadOnlyList<QueuedCard> Entries => _entries;

        /// <summary>The real board plus every queued command. Null until a sequence starts.</summary>
        public SimBoard Predicted => _predicted;

        public int Count => _entries.Count;

        public bool IsEmpty => _entries.Count == 0;

        public bool HasBaseState => _baseState != null;

        /// <summary>True once a Finale card is queued, which closes the sequence to further cards.</summary>
        public bool IsClosed
        {
            get
            {
                if (_entries.Count == 0)
                    return false;

                BoardActionLogic logic = _entries[_entries.Count - 1].Card?.Logic;
                return logic != null && logic.IsFinale;
            }
        }

        public int TotalStaminaPaid
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _entries.Count; i++)
                    total += _entries[i].StaminaPaid;

                return total;
            }
        }

        /// <summary>Starts a fresh sequence from the current real board.</summary>
        public void Reset(SimBoard baseState)
        {
            _entries.Clear();
            _baseState = baseState;
            _predicted = baseState?.Clone();
            Changed?.Invoke();
        }

        public void Clear()
        {
            Reset(null);
        }

        public bool CanEnqueue(BoardActionLogic logic)
        {
            return _baseState != null && logic != null && logic.IsAvailable && !IsClosed;
        }

        public void Enqueue(QueuedCard entry)
        {
            if (entry == null)
                return;

            _entries.Add(entry);
            _predicted?.Apply(entry.Ops);
            Changed?.Invoke();
        }

        /// <summary>
        /// Takes the last card back out and recomputes the predicted board from the real one. Earlier
        /// commands stay valid because they were planned against states this cannot have changed.
        /// </summary>
        public QueuedCard RemoveLast()
        {
            if (_entries.Count == 0)
                return null;

            int last = _entries.Count - 1;
            QueuedCard entry = _entries[last];
            _entries.RemoveAt(last);
            Rebuild();
            Changed?.Invoke();
            return entry;
        }

        public void CollectOps(List<BoardOp> destination)
        {
            destination.Clear();

            for (int i = 0; i < _entries.Count; i++)
            {
                BoardOp[] ops = _entries[i].Ops;
                for (int o = 0; o < ops.Length; o++)
                    destination.Add(ops[o]);
            }
        }

        void Rebuild()
        {
            _predicted = _baseState?.Clone();
            if (_predicted == null)
                return;

            for (int i = 0; i < _entries.Count; i++)
                _predicted.Apply(_entries[i].Ops);
        }
    }
}
