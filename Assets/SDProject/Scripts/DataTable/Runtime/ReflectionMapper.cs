using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace SD.DataTable
{
    internal static class ReflectionMapper
    {
        public static T MapRow<T>(IReadOnlyDictionary<string, int> headerIndex, string[] row) where T : new()
        {
            var obj = new T();
            var t = typeof(T);
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanWrite) continue;
                if (!headerIndex.TryGetValue(p.Name, out int c)) continue;
                if (c < 0 || c >= row.Length) continue;

                string cell = row[c]?.Trim() ?? "";
                p.SetValue(obj, ConvertCell(cell, p.PropertyType));
            }
            return obj;
        }

        private static object ConvertCell(string s, Type type)
        {
            if (type == typeof(string)) return s;
            if (type == typeof(bool)) return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
            if (type == typeof(int)) return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;
            if (type == typeof(float)) return float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : 0f;
            if (type == typeof(double)) return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0d;
            if (type.IsEnum)
            {
                try { return Enum.Parse(type, s, true); }
                catch { return Activator.CreateInstance(type)!; }
            }
            return Activator.CreateInstance(type)!;
        }
    }
}