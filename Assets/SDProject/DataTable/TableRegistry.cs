using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataTable
{
    [CreateAssetMenu(menuName = "SDProject/DataTable/Table Registry", fileName = "TableRegistry")]
    public class TableRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Unique table id; must match the .xlsx file name (without extension)")]
            public string tableId;

            [Tooltip("Worksheet name inside the .xlsx. Leave empty to use the first/only sheet.")]
            public string sheetName = "Sheet1";

            [Tooltip("Load order (ascending). Use when dependencies exist.")]
            public int order = 0;

            [Tooltip("Uncheck to skip this table at runtime.")]
            public bool enabled = true;

            [Tooltip("Optional schema for validation at conversion time (editor).")]
            public TableSchema schema;
        }

        public List<Entry> entries = new List<Entry>();
    }
}