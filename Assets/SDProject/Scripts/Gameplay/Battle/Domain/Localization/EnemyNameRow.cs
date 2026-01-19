using System;
using SD.DataTable;

namespace SD.Gameplay.Battle.Domain
{
    // EnemyName.csv → EnemyNameRow
    [DataTableId("EnemyName")]
    public sealed class EnemyNameRow
    {
        public string Id;    // CSV: "Id"
        public bool Enabled;    // CSV: "Enabled"
        public string Ko;    // CSV: "Ko"
        public string En;    // CSV: "En"
    }
}
