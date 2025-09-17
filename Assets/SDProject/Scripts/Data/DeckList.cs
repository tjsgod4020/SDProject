using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Deck List", fileName = "DeckList")]
    public class DeckList : ScriptableObject
    {
        [Min(1)] public int drawPerTurn = 5;
        [Min(1)] public int handMax = 10;

        [Tooltip("Initial deck composition (cards can repeat).")]
        public List<CardData> initialDeck = new List<CardData>();
    }
}