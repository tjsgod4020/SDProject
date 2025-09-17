// Assets/SDProject/Scripts/Combat/HandRuntime.cs
using System.Collections.Generic;
using UnityEngine;
using SDProject.Core.Messaging;
using SDProject.Data;

namespace SDProject.Combat
{
    public class HandRuntime : MonoBehaviour
    {
        private readonly List<CardData> _cards = new();
        public IReadOnlyList<CardData> Cards => _cards;
        public int Count => _cards.Count;

        public void Add(CardData card)
        {
            _cards.Add(card);
            GameEvents.RaiseHandChanged(_cards.Count);
        }

        public void Clear()
        {
            _cards.Clear();
            GameEvents.RaiseHandChanged(_cards.Count);
        }

        // 새로 추가: 특정 카드 제거
        public void Remove(CardData card)
        {
            if (_cards.Remove(card))
            {
                GameEvents.RaiseHandChanged(_cards.Count);
            }
        }
        public int AddCards(IEnumerable<CardData> cards, int maxHand)
        {
            int added = 0;
            foreach (var c in cards)
            {
                if (_cards.Count >= maxHand) break;
                _cards.Add(c);
                added++;
            }
            GameEvents.RaiseHandChanged(_cards.Count);
            return added;
        }
    }
}
