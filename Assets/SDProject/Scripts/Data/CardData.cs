using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Card", fileName = "Card_")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string cardId;
        public string displayName;
        public string description;

        [Header("Cost")]
        [Min(0)] public int apCost = 1;

        // (선택) 나중에 타입/타깃/효과 JSON 등을 여기에 확장합니다.
        // public CardType type;
        // public TargetType targetType;
        // [TextArea] public string EffectsJSON;
    }
}