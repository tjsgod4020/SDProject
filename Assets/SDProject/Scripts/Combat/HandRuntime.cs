using System;
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;
using SDProject.Core.Messaging;

namespace SDProject.Combat
{
    /// <summary>
    /// Holds current hand; raises events when changed.
    /// </summary>
    public sealed class HandRuntime : MonoBehaviour
    {
        private readonly List<CardData> _items = new();

        public int Count => _items.Count;
        public IReadOnlyList<CardData> Items => _items;
        // ▼ 레거시 호환(읽기 전용 별칭)
        public IReadOnlyList<CardData> Cards => _items;


        public event Action<CardData> OnAdded;
        public event Action<CardData> OnUsed;

        public int AddCards(List<CardData> drawResult, int handMax)
        {
            if (drawResult == null) return 0;
            int added = 0;
            foreach (var c in drawResult)
            {
                if (c == null) continue;
                if (_items.Count >= handMax) break;
                _items.Add(c);
                added++;
                OnAdded?.Invoke(c);
            }
            GameEvents.RaiseHandChanged(_items.Count);
            return added;
        }

        public void Clear()
        {
            _items.Clear();
            GameEvents.RaiseHandChanged(_items.Count);
        }

        public List<CardData> TakeAll()
        {
            var all = new List<CardData>(_items);
            _items.Clear();
            GameEvents.RaiseHandChanged(_items.Count);
            return all;
        }

        public void MarkUsed(CardData used)
        {
            if (_items.Remove(used))
            {
                OnUsed?.Invoke(used);
                GameEvents.RaiseHandChanged(_items.Count);
            }
        }
        public void Use(SDProject.Data.CardData card)
        {
            MarkUsed(card);
        }
    }
}
