#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;

public static class ExcelReader
{
    /// <summary>
    /// Reads a sheet and returns rows as dictionaries {Header -> CellText}.
    /// </summary>
    public static List<Dictionary<string, string>> ReadSheet(
        string fullXlsxPath,
        string sheetName,
        int headerRowIndex = 0,
        int dataStartRowIndex = 1)
    {
        if (!File.Exists(fullXlsxPath))
        {
            Debug.LogError($"[ExcelReader] File not found: {fullXlsxPath}");
            return new List<Dictionary<string, string>>();
        }

        using (var fs = new FileStream(fullXlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            IWorkbook workbook = new XSSFWorkbook(fs);
            var sheet = workbook.GetSheet(sheetName);
            if (sheet == null)
            {
                Debug.LogError($"[ExcelReader] Sheet not found: {sheetName}");
                return new List<Dictionary<string, string>>();
            }

            // Read headers
            var headerRow = sheet.GetRow(headerRowIndex);
            if (headerRow == null)
            {
                Debug.LogError($"[ExcelReader] Header row missing at index {headerRowIndex} (sheet: {sheetName})");
                return new List<Dictionary<string, string>>();
            }

            var headers = new List<string>();
            for (int c = 0; c < headerRow.LastCellNum; c++)
            {
                var cell = headerRow.GetCell(c);
                headers.Add(cell?.ToString()?.Trim() ?? $"Col{c}");
            }

            var rows = new List<Dictionary<string, string>>();

            for (int r = dataStartRowIndex; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool isAllEmpty = true;

                for (int c = 0; c < headers.Count; c++)
                {
                    var cell = row.GetCell(c);
                    string text = cell?.ToString()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(text)) isAllEmpty = false;
                    dict[headers[c]] = text;
                }

                if (!isAllEmpty) rows.Add(dict);
            }

            Debug.Log($"[ExcelReader] Read {rows.Count} rows from '{sheetName}' ({Path.GetFileName(fullXlsxPath)})");
            return rows;
        }
    }
}
#endif

/*
[Unity 적용 가이드]
- NPOI 설치 후(메뉴 Tools > NuGet > Manage NuGet Packages → NPOI, NPOI.OOXML) 자동으로 사용 가능.
- Editor 전용이므로 #if UNITY_EDITOR 가드가 포함됨.
*/
