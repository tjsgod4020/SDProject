using System;
using SD.DataTable;

namespace SD.Gameplay.Battle.Domain
{
    // Character.csv → CharacterRow  (자동 매칭)
    [DataTableId("CharacterData")]
    public sealed class CharacterRow
    {
        public string Id;      // CSV: "Id"
        public string PrefabKey;  // CSV: "PrefabKey" (AssetId로 사용, Addressables 로드)
        public bool Enabled; // CSV: "Enabled" (비활성화면 false로 설정 시 스킵)
    }

    // Enemy.csv → EnemyRow  (자동 매칭)
    [DataTableId("EnemyData")]
    public sealed class EnemyRow
    {
        public string Id;
        public string PrefabKey;  // CSV: "PrefabKey" (AssetId로 사용, Addressables 로드)
        public bool Enabled;
    }
}
