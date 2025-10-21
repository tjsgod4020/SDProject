using System.Collections.Generic;

namespace SD.Gameplay.Cards.Domain
{
    /// <summary>
    /// 런타임에서 카드 한 장을 표현하기 위한 최소 정의.
    /// CSV(CardData/Name/Desc)로부터 CardFactory가 생성한다.
    /// </summary>
    public sealed class CardDefinition
    {
        // 키/활성
        public string Id { get; set; }
        public bool Enabled { get; set; }

        // 로컬라이즈 키 & 결과
        public string NameId { get; set; }
        public string DescId { get; set; }
        public string DisplayName { get; set; }
        public string DisplayDesc { get; set; }

        // 기본 속성
        public CardType Type { get; set; }
        public CardClass Class { get; set; }      // Myth 단일화 반영
        public CardRarity Rarity { get; set; }
        public string CharId { get; set; }
        public int Cost { get; set; }

        public TargetType TargetType { get; set; }
        public PositionUseFlags PosUse { get; set; }
        public PositionHitFlags PosHit { get; set; }

        // 태그(자유 문자열; v1은 플래그 미사용)
        public List<string> Tags { get; set; } = new();

        // 효과(선택: 별도 모듈에서 해석/실행)
        public List<Effects.EffectSpec> Effects { get; set; } = new();

        // 업그레이드 메타
        public bool Upgradable { get; set; }
        public int UpgradeStep { get; set; }
        public string UpgradeRefId { get; set; }
    }
}
