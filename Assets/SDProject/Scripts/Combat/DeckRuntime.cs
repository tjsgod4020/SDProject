// Assets/SDProject/Scripts/Combat/DeckRuntime.cs
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;
using SDProject.DataBridge; // ← 어댑터 네임스페이스

namespace SDProject.Combat
{
    public sealed class DeckRuntime : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private DeckSourceSOAdapter _deckSource;  // ← 강타입 필드로 변경

        [Header("Config")]
        [Min(1)][SerializeField] private int _drawPerTurn = 5;
        [Min(1)][SerializeField] private int _handMax = 10;

        // runtime
        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discard = new();
        private bool _initialized;

        public int DrawPerTurn => _drawPerTurn;
        public int HandMax => _handMax;
        public int DrawCount => _drawPile.Count;
        public int DiscardCount => _discard.Count;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (_initialized) return;

            if (_deckSource == null)
            {
                Debug.LogError("[Deck] _deckSource is null. Assign DeckSourceSOAdapter in Inspector.");
                return;
            }

            var init = _deckSource.GetInitialDeck();
            var cnt = init?.Count ?? 0;

            _drawPile.Clear();
            if (cnt > 0) _drawPile.AddRange(init);

            Debug.Log($"[Deck] init: drawPile={_drawPile.Count}, discard={_discard.Count} "
                    + $"(source='{_deckSource.DebugDeckName}', listCount={_deckSource.DebugDeckCount})");

            _initialized = true;
        }

        public List<CardData> Draw(int count)
        {
            EnsureInitialized();
            var result = new List<CardData>(count);
            for (int i = 0; i < count && _drawPile.Count > 0; i++)
            {
                var last = _drawPile[^1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                result.Add(last);
            }
            return result;
        }

        public void Discard(CardData c)
        {
            if (c != null) _discard.Add(c);
        }
        public void Discard(IEnumerable<CardData> cs)
        {
            if (cs == null) return;
            foreach (var c in cs) if (c != null) _discard.Add(c);
        }
    }
}