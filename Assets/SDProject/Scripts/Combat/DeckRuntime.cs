// Assets/SDProject/Scripts/Combat/DeckRuntime.cs (보강만)
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;

namespace SDProject.Combat
{
    public class DeckRuntime : MonoBehaviour
    {
        [SerializeField] private DeckList deckList;

        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discardPile = new();
        private System.Random _rng;

        public int DrawPerTurn => deckList ? deckList.drawPerTurn : 5;
        public int HandMax => deckList ? deckList.handMax : 10;

        private void Awake()
        {
            _rng = new System.Random();
            ResetFromList();
        }

        public void ResetFromList()
        {
            _drawPile.Clear();
            _discardPile.Clear();

            if (deckList != null)
            {
                _drawPile.AddRange(deckList.initialDeck);
                Debug.Log($"[Deck] ResetFromList: initialDeck={deckList.initialDeck?.Count ?? 0}, handMax={HandMax}, drawPerTurn={DrawPerTurn}");
            }
            else
            {
                Debug.LogError("[Deck] deckList is NULL — drawPile will be empty");
            }

            Shuffle(_drawPile);
            Debug.Log($"[Deck] init: drawPile={_drawPile.Count}, discard={_discardPile.Count}");
        }

        public List<CardData> Draw(int count)
        {
            var result = new List<CardData>(count);
            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0) break; // no more cards
                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                }

                var idx = _drawPile.Count - 1;
                var card = _drawPile[idx];
                _drawPile.RemoveAt(idx);
                result.Add(card);
            }
            Debug.Log($"[Deck] Draw {result.Count}/{count}, remain={_drawPile.Count}");
            return result;
        }

        public void Discard(IEnumerable<CardData> cards)
        {
            _discardPile.AddRange(cards);
        }

        private void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
