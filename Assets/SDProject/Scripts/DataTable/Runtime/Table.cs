using System.Collections.Generic;
using UnityEngine;

namespace SD.DataTable
{
    public class Table<TRow> : ICsvTable<TRow> where TRow : IHasStringId
    {
        private readonly Dictionary<string, TRow> _byId = new();
        private readonly List<TRow> _rows;

        public Table(List<TRow> rows)
        {
            _rows = rows ?? new List<TRow>();
            foreach (var r in _rows)
            {
                if (r == null || string.IsNullOrEmpty(r.Id))
                {
                    Debug.LogError($"[DataTable] Null/Empty Id in {typeof(TRow).Name}");
                    continue;
                }
                if (_byId.ContainsKey(r.Id))
                {
                    Debug.LogError($"[DataTable] Duplicate Id '{r.Id}' in {typeof(TRow).Name}");
                    continue;
                }
                _byId[r.Id] = r;
            }
        }

        public IReadOnlyDictionary<string, TRow> ById => _byId;
        public IReadOnlyList<TRow> Rows => _rows;
        public bool TryGet(string id, out TRow row) => _byId.TryGetValue(id, out row!);
        public TRow GetOrNull(string id) => _byId.TryGetValue(id, out var r) ? r : default!;
    }
}