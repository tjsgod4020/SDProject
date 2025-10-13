using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataTable
{
    [CreateAssetMenu(menuName = "SDProject/DataTable/Generic CSV Table", fileName = "GenericCsvTable")]
    public class GenericCsvTable : TableAsset
    {
        [SerializeField] private List<string> headers = new List<string>();
        [SerializeField] private List<string[]> rows = new List<string[]>();
        private Dictionary<string, int> headerIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyList<string> Headers => headers;
        public int RowCount => rows.Count;
        public string Get(int row, string header)
        {
            if (!headerIndex.TryGetValue(header, out var idx)) return string.Empty;
            if (row < 0 || row >= rows.Count) return string.Empty;
            var arr = rows[row];
            return (idx >= 0 && idx < arr.Length) ? arr[idx] : string.Empty;
        }

        public override void Apply(string rawText)
        {
            headers.Clear();
            rows.Clear();
            headerIndex.Clear();

            var parsed = CsvLite.ParseWithHeader(rawText);
            headers.AddRange(parsed.headers);
            for (int i = 0; i < headers.Count; i++)
                headerIndex[headers[i]] = i;

            rows.AddRange(parsed.rows);
            Debug.Log($"[GenericCsvTable] Applied. Rows={rows.Count}, Cols={headers.Count}");
        }
    }
}