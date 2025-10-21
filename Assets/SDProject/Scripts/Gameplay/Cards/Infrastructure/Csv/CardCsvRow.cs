namespace SD.Gameplay.Cards.Infrastructure.Csv
{
    internal struct CardCsvRow
    {
        public string Id, Enabled, NameId, DescId, Type, Class, Rarity, CharId, Cost,
                      TargetType, PosUse, PosHit, EffectsJSON, Upgradable, UpgradeStep, UpgradeRefId, Tags;
    }
}
