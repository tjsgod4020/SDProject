#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SD.DataTable.Editor
{
    /// <summary>
    /// CSV 파일의 헤더를 읽어서 Row 클래스를 자동 생성합니다.
    /// </summary>
    public static class RowClassGenerator
    {
        [MenuItem("Tools/DataTables/Generate Row Class from CSV...", priority = 20)]
        public static void GenerateRowClass()
        {
            string csvPath = EditorUtility.OpenFilePanel("Select CSV File", "Assets/SDProject/DataTables/Csv", "csv");
            if (string.IsNullOrEmpty(csvPath)) return;

            string projPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            if (!csvPath.StartsWith(projPath))
            {
                EditorUtility.DisplayDialog("Error", "CSV 파일은 프로젝트 Assets 폴더 내에 있어야 합니다.", "OK");
                return;
            }

            string relPath = csvPath.Substring(projPath.Length).Replace('\\', '/');
            if (!relPath.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog("Error", "CSV 파일은 Assets 폴더 내에 있어야 합니다.", "OK");
                return;
            }

            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(relPath);
            if (csv == null)
            {
                EditorUtility.DisplayDialog("Error", "CSV 파일을 로드할 수 없습니다: " + relPath, "OK");
                return;
            }

            GenerateFromCsv(csv, relPath);
        }

        private static void GenerateFromCsv(TextAsset csv, string csvPath)
        {
            try
            {
                // CSV 헤더 읽기
                var lines = ReadCsv(csv.text);
                if (lines.Count < 2)
                {
                    EditorUtility.DisplayDialog("Error", "CSV 파일에 헤더와 타입 행이 없습니다.", "OK");
                    return;
                }

                var headers = lines[0].Select(h => h.Trim()).ToArray();
                var types = lines[1].Select(t => t.Trim().ToLower()).ToArray();

                if (headers.Length != types.Length)
                {
                    EditorUtility.DisplayDialog("Error", "CSV 헤더와 타입 행의 개수가 일치하지 않습니다.", "OK");
                    return;
                }

                // 테이블 ID 추출 (파일명에서)
                string fileName = Path.GetFileNameWithoutExtension(csvPath);
                string tableId = fileName;

                // 클래스명 생성
                string className = ToPascalCase(fileName) + "Row";

                // 네임스페이스 결정
                string namespaceName = DetermineNamespace(csvPath);

                // 타겟 폴더 결정
                string targetFolder = DetermineTargetFolder(csvPath);
                string targetPath = Path.Combine(targetFolder, className + ".cs").Replace('\\', '/');

                // 클래스 코드 생성
                string classCode = GenerateClassCode(namespaceName, className, tableId, headers, types);

                // 파일 저장
                if (File.Exists(targetPath))
                {
                    if (!EditorUtility.DisplayDialog("File Exists", 
                        $"파일이 이미 존재합니다:\n{targetPath}\n\n덮어쓰시겠습니까?", 
                        "Yes", "No"))
                    {
                        return;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.WriteAllText(targetPath, classCode, Encoding.UTF8);

                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(targetPath);

                Debug.Log($"[RowClassGenerator] Generated: {targetPath}");

                // DataTableConfig 자동 동기화
                EditorApplication.delayCall += () =>
                {
                    var configs = AssetDatabase.FindAssets("t:DataTableConfig")
                        .Select(guid => AssetDatabase.LoadAssetAtPath<DataTableConfig>(
                            AssetDatabase.GUIDToAssetPath(guid)))
                        .Where(c => c != null)
                        .ToList();

                    foreach (var config in configs)
                    {
                        DataTableConfigAutoSync.SyncOneConfigNow(config);
                    }

                    EditorUtility.DisplayDialog("Success", 
                        $"Row 클래스가 생성되었습니다:\n{targetPath}\n\nDataTableConfig도 자동으로 동기화되었습니다.", 
                        "OK");
                };
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Row 클래스 생성 실패:\n{ex.Message}", "OK");
                Debug.LogError($"[RowClassGenerator] Error: {ex}");
            }
        }

        private static List<string[]> ReadCsv(string text)
        {
            var result = new List<string[]>();
            using var sr = new StringReader(text);

            var row = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            while (true)
            {
                int c = sr.Read();
                if (c == -1)
                {
                    if (inQuotes) throw new InvalidDataException("CSV: unmatched quotes");
                    if (sb.Length > 0 || row.Count > 0)
                    {
                        row.Add(sb.ToString()); sb.Clear();
                        result.Add(row.ToArray()); row.Clear();
                    }
                    break;
                }

                char ch = (char)c;
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        int next = sr.Peek();
                        if (next == '"') { sr.Read(); sb.Append('"'); }
                        else inQuotes = false;
                    }
                    else sb.Append(ch);
                }
                else
                {
                    if (ch == '"') inQuotes = true;
                    else if (ch == ',') { row.Add(sb.ToString()); sb.Clear(); }
                    else if (ch == '\n') { row.Add(sb.ToString()); sb.Clear(); result.Add(row.ToArray()); row.Clear(); }
                    else if (ch == '\r') { /* ignore */ }
                    else sb.Append(ch);
                }
            }

            return result;
        }

        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            var parts = name.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                        sb.Append(part.Substring(1));
                }
            }
            return sb.ToString();
        }

        private static string DetermineNamespace(string csvPath)
        {
            // 경로에서 네임스페이스 추론
            // 예: Assets/SDProject/Scripts/Gameplay/Battle/Domain/... → SD.Gameplay.Battle.Domain
            if (csvPath.Contains("Battle/Domain"))
            {
                if (csvPath.Contains("Localization"))
                    return "SD.Gameplay.Battle.Domain.Localization";
                return "SD.Gameplay.Battle.Domain";
            }
            if (csvPath.Contains("Cards/Domain"))
            {
                if (csvPath.Contains("Localization"))
                    return "SD.Gameplay.Cards.Domain.Localization";
                return "SD.Gameplay.Cards.Domain";
            }

            // 기본값
            return "SD.Gameplay.Battle.Domain";
        }

        private static string DetermineTargetFolder(string csvPath)
        {
            // CSV 파일 위치를 기반으로 타겟 폴더 결정
            // 예: Assets/SDProject/DataTables/Csv/CharacterData.csv
            // → Assets/SDProject/Scripts/Gameplay/Battle/Domain/

            string dir = Path.GetDirectoryName(csvPath).Replace('\\', '/');

            // DataTables/Csv → Scripts/Gameplay/Battle/Domain
            if (dir.Contains("DataTables/Csv"))
            {
                // Localization 파일인지 확인
                string fileName = Path.GetFileNameWithoutExtension(csvPath);
                if (fileName.Contains("Name") || fileName.Contains("Desc") || fileName.Contains("Text"))
                {
                    return "Assets/SDProject/Scripts/Gameplay/Battle/Domain/Localization";
                }
                return "Assets/SDProject/Scripts/Gameplay/Battle/Domain";
            }

            // 기본값
            return "Assets/SDProject/Scripts/Gameplay/Battle/Domain";
        }

        private static string GenerateClassCode(string namespaceName, string className, string tableId, 
            string[] headers, string[] types)
        {
            var sb = new StringBuilder();

            // using 문
            sb.AppendLine("using System;");
            sb.AppendLine("using SD.DataTable;");
            sb.AppendLine();

            // 네임스페이스
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            // 클래스 주석
            sb.AppendLine($"    // {tableId}.csv → {className}");
            sb.AppendLine($"    [DataTableId(\"{tableId}\")]");
            sb.AppendLine($"    public sealed class {className}");
            sb.AppendLine("    {");

            // 필드 생성
            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i];
                string typeStr = types[i];
                string fieldType = MapCsvTypeToCSharpType(typeStr);
                string fieldName = ToPascalCase(header);

                // 주석
                sb.AppendLine($"        public {fieldType} {fieldName};    // CSV: \"{header}\"");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string MapCsvTypeToCSharpType(string csvType)
        {
            if (string.IsNullOrEmpty(csvType)) return "string";

            csvType = csvType.ToLower().Trim();

            // 기본 타입 매핑
            if (csvType == "string" || csvType == "str" || csvType == "text")
                return "string";
            if (csvType == "bool" || csvType == "boolean")
                return "bool";
            if (csvType == "int" || csvType == "integer")
                return "int";
            if (csvType == "float" || csvType == "double")
                return "float";
            if (csvType == "enum")
                return "string"; // enum은 나중에 처리

            // 기본값은 string
            return "string";
        }
    }
}
#endif
