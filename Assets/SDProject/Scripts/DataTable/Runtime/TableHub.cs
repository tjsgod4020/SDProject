using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataTable
{
    public static class TableHub
    {
        private static readonly Dictionary<string, TableAsset> _tables =
            new Dictionary<string, TableAsset>(StringComparer.Ordinal);

        public static void Register(string tableId, TableAsset asset)
        {
            _tables[tableId] = asset;
        }

        public static TableAsset Get(string tableId)
        {
            _tables.TryGetValue(tableId, out var a);
            return a;
        }

        public static T Get<T>(string tableId) where T : TableAsset
        {
            var a = Get(tableId);
            return a as T;
        }

        public static IReadOnlyDictionary<string, TableAsset> All => _tables;
    }
}