using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataTable
{
    public enum ValidationLevel { Strict, Compat, Lenient }
    public enum ColumnType { String, Int, Float, Bool, Enum, Flags, Ref }

    [CreateAssetMenu(menuName = "SDProject/DataTable/Table Schema", fileName = "TableSchema")]
    public class TableSchema : ScriptableObject
    {
        [Serializable]
        public class Column
        {
            public string name;
            public ColumnType type = ColumnType.String;
            public bool required = false;
            public string defaultValue = "";
            public string refTableId; // used when type == Ref
        }

        public string tableId;
        public ValidationLevel validationLevel = ValidationLevel.Compat;
        public List<Column> columns = new List<Column>();

        public bool TryGetColumn(string name, out Column c)
        {
            foreach (var col in columns)
            {
                if (string.Equals(col.name, name, StringComparison.Ordinal))
                {
                    c = col;
                    return true;
                }
            }
            c = null;
            return false;
        }
    }
}