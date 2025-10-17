using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SDProject.DataTable
{
    internal static class CsvUtil
    {
        public static List<Dictionary<string, string>> Parse(string csv)
        {
            var rows = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(csv)) return rows;

            var lines = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            if (lines.Length == 0) return rows;

            var header = SplitLine(lines[0]).Select(h => h.Trim()).ToList();

            for (int li = 1; li < lines.Length; li++)
            {
                var line = lines[li];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("#")) continue;

                var cols = SplitLine(line);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < header.Count; i++)
                {
                    var key = header[i];
                    var val = (i < cols.Count) ? cols[i] : string.Empty;
                    row[key] = val;
                }
                rows.Add(row);
            }
            return rows;
        }

        public static List<string> SplitLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            bool inQuote = false;
            var cur = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') inQuote = !inQuote;
                else if (c == ',' && !inQuote) { result.Add(cur.ToString()); cur.Length = 0; }
                else cur.Append(c);
            }
            result.Add(cur.ToString());

            for (int i = 0; i < result.Count; i++)
            {
                var s = result[i].Trim();
                if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
                    s = s.Substring(1, s.Length - 2);
                result[i] = s;
            }
            return result;
        }

        public static bool TryGet(this Dictionary<string, string> row, string key, out string val)
        {
            foreach (var kv in row)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                { val = kv.Value; return true; }
            val = default; return false;
        }

        public static int ParseInt(string s, int def = 0)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
            return def;
        }
    }
}
