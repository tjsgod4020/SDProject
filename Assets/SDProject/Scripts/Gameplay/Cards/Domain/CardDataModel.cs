using System.Collections.Generic;
using SD.DataTable;

namespace SD.Gameplay.Cards.Domain
{
    [DataTableId("CardData")]
    public sealed class CardDataModel
    {
        public CardDataModel() { }
        public string Id { get; set; }
        public bool Enabled { get; set; } = true;
        public string NameId { get; set; }
        public string DescId { get; set; }
        public CardType Type { get; set; }
        public CardClass Class { get; set; }
        public CardRarity Rarity { get; set; }
        public string CharId { get; set; }
        public int Cost { get; set; }
        public TargetType TargetType { get; set; }
        public PositionUseFlags PosUse { get; set; }
        public PositionHitFlags PosHit { get; set; }
        public IReadOnlyList<CardEffect> Effects { get; set; }
        public bool Upgradable { get; set; }
        public int UpgradeStep { get; set; }
        public string UpgradeRefId { get; set; }
        public CardTagFlags Tags { get; set; }

        public CardDataModel(
            string id, bool enabled, string nameId, string descId,
            CardType type, CardClass @class, CardRarity rarity, string charId,
            int cost, TargetType targetType, PositionUseFlags posUse, PositionHitFlags posHit,
            List<CardEffect> effects, bool upgradable, int upgradeStep, string upgradeRefId, CardTagFlags tags)
        {
            Id = id; Enabled = enabled; NameId = nameId; DescId = descId;
            Type = type; Class = @class; Rarity = rarity; CharId = charId;
            Cost = cost; TargetType = targetType; PosUse = posUse; PosHit = posHit;
            Effects = effects ?? new List<CardEffect>();
            Upgradable = upgradable; UpgradeStep = upgradeStep; UpgradeRefId = upgradeRefId; Tags = tags;
        }
    }
}
