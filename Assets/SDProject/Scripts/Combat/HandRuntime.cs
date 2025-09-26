using System.Collections.Generic;
using UnityEngine;
using SDProject.Core.Messaging;
using SDProject.Data;

namespace SDProject.Combat
{
    public class HandRuntime : MonoBehaviour
    {
        [SerializeField] private List<CardData> _cards = new();
        public int Count => _cards.Count;
        public System.Collections.Generic.IReadOnlyList<CardData> Cards => _cards;

        public void Clear()
        {
            if (_cards.Count == 0) return;
            _cards.Clear();
            GameEvents.RaiseHandChanged(_cards.Count);
        }

        public void Add(CardData card)
        {
            if (!card) return;
            _cards.Add(card);
            GameEvents.RaiseHandChanged(_cards.Count);
        }

        public bool Remove(CardData card)
        {
            var ok = _cards.Remove(card);
            if (ok) GameEvents.RaiseHandChanged(_cards.Count);
            return ok;
        }

        public int AddCards(IEnumerable<CardData> cards, int maxHand)
        {
            int added = 0;
            foreach (var c in cards)
            {
                if (_cards.Count >= maxHand) break;
                if (!c) continue;
                _cards.Add(c);
                added++;
            }
            GameEvents.RaiseHandChanged(_cards.Count);
            return added;
        }

        public List<CardData> TakeAll()
        {
            var all = new List<CardData>(_cards);
            _cards.Clear();
            GameEvents.RaiseHandChanged(_cards.Count);
            return all;
        }
    }
}
