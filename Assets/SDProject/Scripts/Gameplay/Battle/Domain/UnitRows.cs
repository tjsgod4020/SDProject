using System;

namespace SD.Gameplay.Battle.Domain
{
    // Character.csv ↔ CharacterRow  (관습 매칭)
    public sealed class CharacterRow
    {
        public string Id;      // CSV: "Id"
        public string Prefab;  // CSV: "Prefab" (Resources 경로)
        public bool Enabled; // CSV: "Enabled" (없으면 false로 들어올 수 있음)
    }

    // Enemy.csv ↔ EnemyRow  (관습 매칭)
    public sealed class EnemyRow
    {
        public string Id;
        public string Prefab;
        public bool Enabled;
    }
}