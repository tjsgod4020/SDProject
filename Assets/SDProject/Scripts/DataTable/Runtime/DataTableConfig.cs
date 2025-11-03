using System;
using System.Collections.Generic;
using UnityEngine;

namespace SD.DataTable
{
    [CreateAssetMenu(menuName = "SD/DataTableConfig")]
    public sealed class DataTableConfig : ScriptableObject
    {
        [Serializable]
        public struct TableEntry
        {
            public string Id;
            public TextAsset Csv;
            public string RowTypeName; // AssemblyQualifiedName
        }

        [SerializeField] private List<TableEntry> _tables = new();
        public IReadOnlyList<TableEntry> Tables => _tables;
    }
}