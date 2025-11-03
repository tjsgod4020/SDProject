using UnityEngine;

namespace SD.Gameplay.Battle.Domain
{
    /// 전투 유닛의 최소 정의(스폰용). 확장 필드는 이후 기획 확정 시 추가.
    public sealed class UnitDefinition
    {
        public string Id { get; }
        public string PrefabPath { get; }  // Resources 경로 (확정 전 Addressables 미도입)

        public UnitDefinition(string id, string prefabPath)
        {
            Id = id;
            PrefabPath = prefabPath;
        }
    }
}
