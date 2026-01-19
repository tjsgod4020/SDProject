using System;
using SD.DataTable;

namespace SD.Gameplay.Battle.Domain.Localization
{
    // CharacterName.csv → CharacterNameRow
    [DataTableId("CharacterName")]
    public sealed class CharacterNameRow
    {
        public string Id;      // 캐릭터 키
        public bool Enabled;    // CSV: "Enabled"
        public string Ko;       // 한국어
        public string En;       // 영어 (필요시 확장)
    }

    // EnemyName.csv → EnemyNameRow
    [DataTableId("EnemyName")]
    public sealed class EnemyNameRow
    {
        public string Id;
        public bool Enabled;    // CSV: "Enabled"
        public string Ko;
        public string En;
    }
}
