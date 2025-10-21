using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SD.Gameplay.Cards.Infrastructure.Csv
{
    internal static class CsvReader
    {
        // RFC4180 호환 간단판: 따옴표로 감싼 필드의 콤마 허용, "" → " 이스케이프 처리
        public static List<string[]> ReadAll(TextReader reader, char delimiter = ',')
        {
            var result = new List<string[]>();
            var row = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            while (true)
            {
                int c = reader.Read();
                if (c == -1)
                {
                    if (inQuotes) throw new InvalidDataException("CSV: 문자열 닫힘 누락");
                    if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); result.Add(row.ToArray()); }
                    break;
                }

                char ch = (char)c;
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        int next = reader.Peek();
                        if (next == '"') { reader.Read(); sb.Append('"'); } // 이스케이프
                        else inQuotes = false;
                    }
                    else sb.Append(ch);
                }
                else
                {
                    if (ch == '"') inQuotes = true;
                    else if (ch == delimiter) { row.Add(sb.ToString()); sb.Clear(); }
                    else if (ch == '\n')
                    {
                        // CRLF/ LF 처리
                        if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); sb.Clear(); result.Add(row.ToArray()); row.Clear(); }
                    }
                    else if (ch == '\r') { /* ignore */ }
                    else sb.Append(ch);
                }
            }

            return result;
        }
    }
}
