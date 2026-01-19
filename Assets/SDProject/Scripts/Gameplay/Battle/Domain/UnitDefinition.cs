using UnityEngine;

namespace SD.Gameplay.Battle.Domain
{
    /// 유닛 정의의 최소 정보(도메인). 향후 필드 추가 시 인프라 계층에서 처리.
    public sealed class UnitDefinition
    {
        public string Id { get; }
        public string AssetId { get; }  // Addressables AssetId (PrefabKey에서 매핑)

        public UnitDefinition(string id, string assetId)
        {
            Id = id;
            AssetId = assetId;
        }
    }
}
