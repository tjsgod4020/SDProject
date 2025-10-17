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

        // (����) ���߿� Ÿ��/Ÿ��/ȿ�� JSON ���� ���⿡ Ȯ���մϴ�.
        // public CardType type;
        // public TargetType targetType;
        // [TextArea] public string EffectsJSON;
    }
}