#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using ExcelDataReader; // NuGet: ExcelDataReader, ExcelDataReader.DataSet

namespace SD.DataTable.Editor
{
    internal static class XlsxToCsvConverter
    {
        // xlsx 파일 1개 → **첫 번째 시트만** CSV 1개로 출력
        // 출력 파일명: <xlsx파일명>.csv (예: CardData.xlsx → CardData.csv)
        public static void ConvertXlsxToCsvFiles(string xlsxFullPath, string outDir)
        {
            Directory.CreateDirectory(outDir);

            var fileName = Path.GetFileName(xlsxFullPath);
            if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("~$"))
                return; // 임시/잠금파일 무시

            var csvOutPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(xlsxFullPath)}.csv");

            using var stream = File.Open(xlsxFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // 첫 번째 시트만 처리 (NextResult 호출 없음)
            int rowCount = 0;
            using var sw = new StreamWriter(csvOutPath, false, new UTF8Encoding(false)); // UTF-8 (no BOM)

            while (reader.Read())
            {
                rowCount++;
                int cellCount = reader.FieldCount;
                var line = new string[cellCount];

                for (int i = 0; i < cellCount; i++)
                {
                    var val = reader.GetValue(i)?.ToString() ?? string.Empty;
                    line[i] = CsvEscape(val);
                }
                sw.WriteLine(string.Join(",", line));
            }

            UnityEngine.Debug.Log($"[XLSX→CSV] {fileName} (Sheet#1 only) → {csvOutPath} ({rowCount} rows)");
        }

        private static string CsvEscape(string s)
        {
            bool needQuote = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
            if (s.Contains("\"")) s = s.Replace("\"", "\"\"");
            return needQuote ? $"\"{s}\"" : s;
        }
    }
}
#endif