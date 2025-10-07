// ... (using 생략)

using SDProject.Core.Messaging;
using SDProject.Data;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Combat
{
    public class DeckRuntime : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private DeckList deckList;
        [SerializeField] private int drawPerTurn = 5;
        [SerializeField] private int handMax = 5;

        public int DrawPerTurn => drawPerTurn;
        public int HandMax => handMax;

        // ✅ 여기가 내부 저장소
        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discardPile = new();

        // ✅ 🔹추가: BattleController 등에서 읽어갈 공개 카운터
        public int DrawCount => _drawPile.Count;
        public int DiscardCount => _discardPile.Count;

        private void Awake()
        {
            ResetFromList();
        }

        public void ResetFromList()
        {
            _drawPile.Clear();
            _discardPile.Clear();

            if (deckList != null && deckList.cards != null)
                _drawPile.AddRange(deckList.cards);

            Shuffle(_drawPile);
            BroadcastCounts();
            Debug.Log($"[Deck] init: drawPile={_drawPile.Count}, discard={_discardPile.Count}");
        }

        public List<CardData> Draw(int count)
        {
            EnsureDrawable(count);
            var result = new List<CardData>(count);
            for (int i = 0; i < count && _drawPile.Count > 0; i++)
            {
                var top = _drawPile[^1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                result.Add(top);
            }
            BroadcastCounts();
            return result;
        }

        public void Discard(CardData card)
        {
            if (card == null) return;
            _discardPile.Add(card);
            BroadcastCounts();
        }

        public void Discard(IEnumerable<CardData> cards)
        {
            if (cards == null) return;
            _discardPile.AddRange(cards);
            BroadcastCounts();
        }

        private void EnsureDrawable(int needed)
        {
            if (_drawPile.Count >= needed) return;
            if (_discardPile.Count == 0) return;

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        private static void Shuffle(List<CardData> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = Random.Range(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void BroadcastCounts()
        {
            // UI 갱신 이벤트
            GameEvents.RaiseDeckChanged(_drawPile.Count, _discardPile.Count);
        }
    }
}
