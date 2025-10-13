using UnityEngine;

namespace SDProject.Combat.Cards
{
    [CreateAssetMenu(menuName = "SDProject/Card Definition", fileName = "CardDefinition")]
    public class CardDefinition : ScriptableObject
    {
        [Header("Data Table Fields")]
        public string Id;
        public bool Enabled = true;
        public string NameId;
        public string DescId;
        public CardType Type;
        public CardClass Class;
        public CardRarity Rarity;
        public string CharId = "Public";
        [Min(0)] public int Cost = 1;

        [Header("Targeting & Position")]
        public TargetType TargetType = TargetType.EnemyFrontMost;
        public PositionFlags PosUse = PositionFlags.All; // caster allowed lanes
        public PositionFlags PosHit = PositionFlags.All; // target allowed lanes

        [Header("Upgrade (data only in v1)")]
        public bool Upgradable = true;
        [Min(0)] public int UpgradeStep = 0;
        public string UpgradeRefId;

        [Header("Composed Effects")]
        public ScriptableObject[] Effects; // each implements ICardEffect

        public override string ToString() =>
            $"Card[{Id}] Type={Type} Cost={Cost} Target={TargetType} Step={UpgradeStep}";
    }
}