using SD.Gameplay.Cards.Domain;

namespace SD.Gameplay.Battle.Domain
{
    /// 전투 중 한 장의 카드 인스턴스(추후 고유 상태가 생길 수 있어 분리)
    public sealed class CardInstance
    {
        public string Id { get; }
        public CardDefinition Def { get; }

        public CardInstance(CardDefinition def)
        {
            Def = def;
            Id = def.Id;
        }

        public override string ToString() => Id;
    }

    public enum TurnSide { Player, Enemy }
}
