// Assets/SDProject/Scripts/Data/DeckSourceSOAdapter.cs
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataBridge
{
    public sealed class DeckSourceSOAdapter : MonoBehaviour, IDeckSource
    {
        [Header("ScriptableObject Source")]
        [SerializeField] private SDProject.Data.DeckList _deckList;

        // 디버그용 공개 프로퍼티
        public string DebugDeckName => _deckList ? _deckList.name : "(null)";
        public int DebugDeckCount => (_deckList != null && _deckList.cards != null) ? _deckList.cards.Count : -1;

        public IReadOnlyList<SDProject.Data.CardData> GetInitialDeck()
        {
            if (_deckList == null)
            {
                Debug.LogWarning("[DeckSourceSOAdapter] DeckList not assigned.");
                return System.Array.Empty<SDProject.Data.CardData>();
            }

            if (_deckList.cards == null || _deckList.cards.Count == 0)
            {
                Debug.LogWarning($"[DeckSourceSOAdapter] '{_deckList.name}'.cards is empty.");
                return System.Array.Empty<SDProject.Data.CardData>();
            }

            var list = new List<SDProject.Data.CardData>(_deckList.cards.Count);
            foreach (var c in _deckList.cards)
            {
                if (c == null)
                {
                    Debug.LogWarning("[DeckSourceSOAdapter] Null card skipped.");
                    continue;
                }
                list.Add(c);
            }

            Debug.Log($"[DeckSourceSOAdapter] Loaded {list.Count} card(s) from '{_deckList.name}'.");
            return list;
        }
    }
}
