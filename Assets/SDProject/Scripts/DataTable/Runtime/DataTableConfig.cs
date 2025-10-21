using System.Collections.Generic;
using UnityEngine;

namespace SD.DataTable
{
    [CreateAssetMenu(menuName = "SD/DataTables/Config", fileName = "DataTableConfig")]
    public sealed class DataTableConfig : ScriptableObject
    {
        [System.Serializable]
        public struct TableEntry
        {
            public string Id;
            public TextAsset Csv;
            public string RowTypeName; // AssemblyQualifiedName
        }

        [SerializeField] private bool _enabled = true;
        public bool Enabled => _enabled;

        [SerializeField] private List<TableEntry> _tables = new();
        public IReadOnlyList<TableEntry> Tables => _tables;
    }
}
