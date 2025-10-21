using SD.DataTable;

namespace SD.Gameplay.Cards.Domain.Localization
{
    /// <summary>
    /// CardName.csv → (Id, Ko, En, …)
    /// 기획서: Id = 로컬라이즈 키, Ko/En = 언어별 텍스트
    /// </summary>
    [DataTableId("CardName")]
    public sealed class CardNameRow : IHasStringId
    {
        // CSV 헤더와 동일한 public set 가능 프로퍼티여야 ReflectionMapper가 채워 넣습니다.
        public string Id { get; set; }
        public string Ko { get; set; }
        public string En { get; set; }
        // 필요 시 추가 언어 컬럼도 그대로 프로퍼티로 확장: public string Jp { get; set; } 등
    }

    /// <summary>
    /// CardDesc.csv → (Id, Ko, En, …)
    /// </summary>
    [DataTableId("CardDesc")]
    public sealed class CardDescRow : IHasStringId
    {
        public string Id { get; set; }
        public string Ko { get; set; }
        public string En { get; set; }
    }
}
