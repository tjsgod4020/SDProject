using System.Collections;
using System.Collections.Generic;

namespace SD.DataTable
{
    public static class TableRegistry
    {
        private static readonly Dictionary<string, IList> _tables = new();

        public static void Set(string id, IList rows)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            _tables[id] = rows ?? new List<object>();
        }

        public static IList Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _tables.TryGetValue(id, out var list) ? list : null;
        }

        public static IEnumerable<string> Keys => _tables.Keys;
        public static void Clear() => _tables.Clear();
    }
}