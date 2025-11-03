using System;

namespace SD.Gameplay.Cards.Domain
{
    // !! SD.DataTable 특성 제거 !!  (AutoSync는 "CardDataModel", "CardData" 규칙으로 자동 매칭함)
    public sealed class CardDataModel
    {
        // CSV 헤더에 맞춰 필드 유지 (필요 없는 건 이후 정리)
        public string Id;
        public int Cost;

        public string Class;          // enum 매핑은 나중에 Factory에서 파싱
        public string Type;           // "
        public string Rarity;         // "
        public string Target;         // "

        public string UsePositions;   // 플래그/마스크면 int로 변경 가능
        public string HitPositions;   // "

        public string Tags;           // "A;B" 형태면 Factory에서 split
        public string Effects;        // JSON 문자열 (예: [{"type":"Damage","value":1}])

        public bool Enabled;        // 없으면 기본값 false 주의
    }
}
