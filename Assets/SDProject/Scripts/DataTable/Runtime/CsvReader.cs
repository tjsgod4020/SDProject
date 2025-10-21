using System.Collections.Generic;
using System.Text;

namespace SD.DataTable
{
    internal static class CsvReader
    {
        public static List<string[]> Parse(string text)
        {
            var result = new List<string[]>();
            if (string.IsNullOrEmpty(text)) return result;

            var row = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { row.Add(sb.ToString()); sb.Clear(); }
                    else if (c == '\n')
                    {
                        row.Add(sb.ToString()); sb.Clear();
                        result.Add(row.ToArray()); row.Clear();
                    }
                    else if (c != '\r') sb.Append(c);
                }
            }
            row.Add(sb.ToString());
            result.Add(row.ToArray());
            return result;
        }
    }
}