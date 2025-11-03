using System;

namespace SD.Gameplay.Cards.Domain.Localization
{
    // !! SD.DataTable 특성과 IHasStringId 제거 !!
    // AutoSync: "CardNameRow" → Id "CardName" 로 매칭, "CardDescRow" → "CardDesc" 로 매칭
    public sealed class CardNameRow
    {
        public string Id;   // 카드 키
        public string ko;   // 한국어
        public string en;   // 영어 (없으면 빈칸)
    }

    public sealed class CardDescRow
    {
        public string Id;
        public string ko;
        public string en;
    }
}
