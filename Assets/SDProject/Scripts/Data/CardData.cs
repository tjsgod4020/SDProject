using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Card", fileName = "Card_")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string cardId;
        public string displayName;

        [Header("Cost")]
        [Min(0)] public int apCost = 1;
    }
}
