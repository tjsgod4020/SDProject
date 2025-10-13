using System.Collections.Generic;
using System.Text;

namespace SDProject.DataTable
{
    public static class CsvLite
    {
        public struct CsvData
        {
            public List<string> headers;
            public List<string[]> rows;
        }

        public static CsvData ParseWithHeader(string csv)
        {
            var data = new CsvData { headers = new List<string>(), rows = new List<string[]>() };
            if (string.IsNullOrEmpty(csv)) return data;

            var lines = SplitLines(csv);
            if (lines.Count == 0) return data;

            data.headers = ParseRow(lines[0]);

            for (int i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                data.rows.Add(ParseRow(lines[i]).ToArray());
            }
            return data;
        }

        private static List<string> SplitLines(string text)
        {
            var list = new List<string>();
            using (var reader = new System.IO.StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    list.Add(line);
            }
            return list;
        }

        private static List<string> ParseRow(string line)
        {
            var cells = new List<string>();
            if (line == null) return cells;

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else inQuotes = !inQuotes;
                }
                else if (ch == ',' && !inQuotes)
                {
                    cells.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(ch);
            }
            cells.Add(sb.ToString());
            return cells;
        }
    }
}