// File: Assets/SDProject/Scripts/DataTable/Editor/XlsxAssetPostprocessor.cs
// Fix: remove bogus XamlRootSafe reference; use XlsxRoot directly.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ExcelDataReader;
using UnityEditor;
using UnityEngine;

// System.Data alias (name collision safety)
using DataTableType = global::System.Data.DataTable;
using DataColumnType = global::System.Data.DataColumn;
using DataRowType = global::System.Data.DataRow;

namespace SDProject.DataTable.Editor
{
    public class XlsxAssetPostprocessor : AssetPostprocessor
    {
        // XlsxAssetPostprocessor.cs - OnPostprocessAllAssets 안

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var targets = new List<string>();

            bool IsXlsxUnderRoot(string path)
            {
                var norm = path.Replace('\\', '/');
                return norm.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                    && norm.StartsWith(SDProject.DataTable.DataTablePaths.XlsxRoot, StringComparison.Ordinal);
            }

            void Consider(string path)
            {
                if (IsXlsxUnderRoot(path))
                    targets.Add(path.Replace('\\', '/'));
            }

            foreach (var p in imported) Consider(p);
            foreach (var p in moved) Consider(p);

            foreach (var xlsxPath in targets.Distinct())
            {
                try
                {
                    ConvertOne(xlsxPath, out var tableId, out var headers, out var rowCount);
                    Debug.Log($"[DataTable] Converted '{tableId}.xlsx' → CSV. Rows={rowCount}, Headers=[{string.Join(", ", headers)}], HeaderHash={HeaderHash(headers)}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataTable] Conversion FAILED for '{xlsxPath}': {ex.Message}");
                }
            }
        }


        private static void ConvertOne(string xlsxPath, out string tableId, out List<string> headers, out int rowCount)
        {
            string tid = Path.GetFileNameWithoutExtension(xlsxPath);
            tableId = tid;

            headers = new List<string>();
            rowCount = 0;

            var reg = LoadRegistry();
            TableSchema schema = null;
            string sheetName = null;

            if (reg != null && reg.entries != null)
            {
                var entry = reg.entries.FirstOrDefault(e =>
                    e != null &&
                    !string.IsNullOrWhiteSpace(e.tableId) &&
                    string.Equals(e.tableId, tid, StringComparison.Ordinal));

                if (entry != null)
                {
                    sheetName = string.IsNullOrWhiteSpace(entry.sheetName) ? null : entry.sheetName;
                    schema = entry.schema;
                }
            }

            using var stream = File.Open(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            DataTableType sheet = null;
            if (!string.IsNullOrEmpty(sheetName))
            {
                sheet = dataSet.Tables.Cast<DataTableType>()
                    .FirstOrDefault(t => string.Equals(t.TableName, sheetName, StringComparison.Ordinal));
                if (sheet == null)
                    throw new Exception($"Sheet '{sheetName}' not found in '{tid}.xlsx'.");
            }
            else
            {
                if (dataSet.Tables.Count == 0) throw new Exception("No sheets found.");
                if (dataSet.Tables.Count > 1)
                    Debug.LogWarning($"[DataTable] Multiple sheets in {tid}.xlsx; using first: '{dataSet.Tables[0].TableName}'.");
                sheet = (DataTableType)dataSet.Tables[0];
            }

            headers = sheet.Columns.Cast<DataColumnType>()
                .Select(c => (c.ColumnName ?? "").Trim()).ToList();

            if (headers.Count == 0) throw new Exception("Header row is empty.");
            if (headers.Count != headers.Distinct(StringComparer.Ordinal).Count())
                throw new Exception("Duplicate headers detected.");

            var genPath = DataTablePaths.GetGeneratedCsvPath(tid);
            Directory.CreateDirectory(Path.GetDirectoryName(genPath));
            using var sw = new StreamWriter(genPath, false, new UTF8Encoding(false));

            sw.WriteLine(ToCsvLine(headers));

            foreach (DataRowType row in sheet.Rows)
            {
                var cells = new string[headers.Count];
                for (int i = 0; i < headers.Count; i++)
                {
                    var v = row[i];
                    var s = (v == null || v == DBNull.Value) ? "" : v.ToString();
                    cells[i] = s ?? "";
                }

                if (schema != null) ValidateRowAgainstSchema(schema, headers, cells);

                sw.WriteLine(ToCsvLine(cells));
                rowCount++;
            }

            AssetDatabase.ImportAsset(ToRelativeAssetPath(genPath));
            EditorApplication.delayCall += TryHotReload;
        }

        private static void ValidateRowAgainstSchema(TableSchema schema, List<string> headers, string[] cells)
        {
            foreach (var col in schema.columns)
                if (col.required && !headers.Contains(col.name, StringComparer.Ordinal))
                    throw new Exception($"Required column '{col.name}' missing (schema={schema.name}).");

            for (int i = 0; i < headers.Count; i++)
            {
                if (!schema.TryGetColumn(headers[i], out var col)) continue;

                var value = (i < cells.Length) ? (cells[i] ?? "") : "";
                if (string.IsNullOrEmpty(value))
                {
                    if (col.required && schema.validationLevel == ValidationLevel.Strict)
                        throw new Exception($"Column '{col.name}' required but empty (Strict).");
                    continue;
                }

                switch (col.type)
                {
                    case ColumnType.Int:
                        if (!int.TryParse(value, out _)) FailByLevel(schema, $"Column '{col.name}' expects Int, got '{value}'.");
                        break;
                    case ColumnType.Float:
                        if (!float.TryParse(value, out _)) FailByLevel(schema, $"Column '{col.name}' expects Float, got '{value}'.");
                        break;
                    case ColumnType.Bool:
                        if (!bool.TryParse(value, out _)) FailByLevel(schema, $"Column '{col.name}' expects Bool, got '{value}'.");
                        break;
                    case ColumnType.Flags:
                        if (!int.TryParse(value, out _)) FailByLevel(schema, $"Column '{col.name}' expects Flags(Int), got '{value}'.");
                        break;
                    case ColumnType.String:
                    case ColumnType.Enum:
                    case ColumnType.Ref:
                        break;
                }
            }
        }

        private static void FailByLevel(TableSchema schema, string message)
        {
            if (schema.validationLevel == ValidationLevel.Lenient) Debug.LogWarning($"[DataTable][Lenient] {message}");
            else throw new Exception(message);
        }

        private static string ToCsvLine(IReadOnlyList<string> cells)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var s = cells[i] ?? "";
                bool quote = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
                if (quote) sb.Append('"').Append(s.Replace("\"", "\"\"")).Append('"');
                else sb.Append(s);
            }
            return sb.ToString();
        }

        private static string HeaderHash(List<string> headers)
        {
            var sorted = headers.OrderBy(h => h, StringComparer.Ordinal);
            var text = string.Join("|", sorted);
            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private static string ToRelativeAssetPath(string absolute)
        {
            var p = absolute.Replace('\\', '/');
            var idx = p.IndexOf("Assets/", StringComparison.Ordinal);
            return (idx >= 0) ? p.Substring(idx) : p;
        }

        private static TableRegistry LoadRegistry()
        {
            var guids = AssetDatabase.FindAssets("t:SDProject.DataTable.TableRegistry");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TableRegistry>(path);
        }

        private static void TryHotReload()
        {
            if (!Application.isPlaying) return;
            var loader = UnityEngine.Object.FindFirstObjectByType<DataTableLoader>();
            if (loader != null) loader.Reload();
        }
    }
}
#endif
