using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;
using SDProject.Core.Messaging;

namespace SDProject.Combat
{
    public class DeckRuntime : MonoBehaviour
    {
        [SerializeField] private DeckList deckList;

        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discardPile = new();
        private System.Random _rng;

        public int DrawPerTurn => deckList ? deckList.drawPerTurn : 5;
        public int HandMax => deckList ? deckList.handMax : 5;

        public int DrawCount => _drawPile.Count;
        public int DiscardCount => _discardPile.Count;

        private void Awake()
        {
            _rng = new System.Random();
            ResetFromList();
        }

        public void ResetFromList()
        {
            _drawPile.Clear();
            _discardPile.Clear();

            if (deckList != null && deckList.initialDeck != null)
                _drawPile.AddRange(deckList.initialDeck);

            Shuffle(_drawPile);
            Debug.Log($"[Deck] init: drawPile={_drawPile.Count}, discard={_discardPile.Count}, handMax={HandMax}, drawPerTurn={DrawPerTurn}");
            RaiseCounts();
        }

        public List<CardData> Draw(int count)
        {
            var result = new List<CardData>(count);

            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    // Reshuffle from discard if available
                    if (_discardPile.Count == 0) break;
                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                    Debug.Log("[Deck] Reshuffle from discard.");
                }

                int idx = _drawPile.Count - 1;
                var card = _drawPile[idx];
                _drawPile.RemoveAt(idx);
                result.Add(card);
            }

            Debug.Log($"[Deck] Draw {result.Count}/{count}, remain draw={_drawPile.Count}, discard={_discardPile.Count}");
            RaiseCounts();
            return result;
        }

        public void Discard(IEnumerable<CardData> cards)
        {
            if (cards == null) return;
            foreach (var c in cards)
            {
                if (c != null) _discardPile.Add(c);
            }
            Debug.Log($"[Deck] Discard +{_discardPile.Count}, now draw={_drawPile.Count}, discard={_discardPile.Count}");
            RaiseCounts();
        }

        private void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void RaiseCounts()
        {
            GameEvents.RaiseDeckChanged(_drawPile.Count, _discardPile.Count);
        }
    }
}