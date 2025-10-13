// Assets/SDProject/Scripts/Combat/Cards/Data/CardLibrary.cs
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Combat.Cards
{
    [CreateAssetMenu(menuName = "SDProject/Card Library", fileName = "CardLibrary")]
    public class CardLibrary : ScriptableObject
    {
        public CardDefinition[] cards;

        private Dictionary<string, CardDefinition> _map;

        private void OnEnable()
        {
            _map = new Dictionary<string, CardDefinition>();
            if (cards == null) return;
            foreach (var c in cards)
            {
                if (!c || string.IsNullOrEmpty(c.Id)) continue;
                _map[c.Id] = c;
            }
        }

        public bool TryGet(string id, out CardDefinition def)
        {
            if (_map != null && !string.IsNullOrEmpty(id) && _map.TryGetValue(id, out def))
                return true;

            def = null; // ¡Ú out º¸Àå
            return false;
        }
    }
}