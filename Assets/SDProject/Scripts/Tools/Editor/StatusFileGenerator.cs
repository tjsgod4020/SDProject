#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SD.DataTable;

namespace SD.Tools.Editor
{
    public static class StatusFileGenerator
    {
        private const string DefaultOutputRelPath = "Assets/SDProject/Docs/SDProject_Status.md";
        private const string PrefKey_OutputPath = "SD.Status.OutputPath";

        [MenuItem("Tools/Project/Generate Status File", priority = 1)]
        public static void Generate()
        {
            var outputRel = EditorPrefs.GetString(PrefKey_OutputPath, DefaultOutputRelPath);
            var outputFull = Path.GetFullPath(outputRel);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFull)!);

            var sb = new StringBuilder();
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            sb.AppendLine("# SDProject — Project Status");
            sb.AppendLine($"_Last updated: {now} (local)_\n");
            sb.AppendLine("## 1) Overview");
            sb.AppendLine("- Engine: **Unity 6000.2.7f2 (PC/2D)**");
            sb.AppendLine("- Version control: **Git**");
            sb.AppendLine("- Architecture: **Domain / Presentation / Infrastructure / DataTable** with asmdef boundaries.");
            sb.AppendLine("- Current focus: **Data pipeline (XLSX→CSV→Runtime)**, **DataTableConfig Auto-Sync**, folder/asmdef hygiene.\n");

            sb.AppendLine("## 2) Data Pipeline — Rules");
            sb.AppendLine("- Editor: **ExcelDataReader** 기반 **XLSX→CSV** 변환 (첫 번째 시트만, 출력 파일명 동일).");
            sb.AppendLine("- CSV 규약: **Row1=Columns / Row2=Types / Row3+=Data** (Row2 권장).");
            sb.AppendLine("- Runtime: **DataTableLoader(Awake) → TableRegistry** 등록 → 코드에서 `TableRegistry.Get<T>()` 접근.");
            sb.AppendLine("- Auto-Sync: `Assets/SDProject/DataTables/Csv` 변화 시 **DataTableConfig** 자동/수동 동기화 지원.\n");

            string csvDir = "Assets/SDProject/DataTables/Csv";
            var csvList = FindCsv(csvDir);
            sb.AppendLine("## 3) Current Data Tables (CSV)");
            if (csvList.Count == 0) sb.AppendLine("- (none found)");
            else foreach (var c in csvList) sb.AppendLine($"- {c}");
            sb.AppendLine();

            var cfgs = AssetDatabase.FindAssets("t:DataTableConfig")
                        .Select(g => AssetDatabase.LoadAssetAtPath<DataTableConfig>(AssetDatabase.GUIDToAssetPath(g)))
                        .Where(c => c != null).OrderBy(c => c.name, StringComparer.OrdinalIgnoreCase).ToList();

            sb.AppendLine("## 4) DataTableConfig Assets");
            if (cfgs.Count == 0) sb.AppendLine("- (none found)");
            else
            {
                foreach (var cfg in cfgs)
                {
                    sb.AppendLine($"- **{cfg.name}**  ({AssetDatabase.GetAssetPath(cfg)})  Enabled={(cfg.Enabled ? "true" : "false")}");
                    if (cfg.Tables != null && cfg.Tables.Count > 0)
                    {
                        foreach (var t in cfg.Tables)
                        {
                            var csvName = t.Csv ? AssetDatabase.GetAssetPath(t.Csv) : "(null)";
                            sb.AppendLine($"  - Id=`{t.Id}`  Csv=`{csvName}`  RowType=`{t.RowTypeName}`");
                        }
                    }
                    else sb.AppendLine("  - (no tables)");
                }
            }
            sb.AppendLine();

            sb.AppendLine("## 5) Assembly Definitions (under SDProject)");
            foreach (var a in FindAsmdefs("Assets/SDProject")) sb.AppendLine($"- {a}");
            sb.AppendLine();

            sb.AppendLine("## 6) Folder Tree Snapshot (Assets/SDProject)");
            sb.AppendLine("```");
            sb.AppendLine(BuildFolderTree("Assets/SDProject", 6));
            sb.AppendLine("```\n");

            sb.AppendLine("## 7) Open Tasks / Next Steps");
            sb.AppendLine("- [ ] Add/verify Row models & `[DataTableId]` attributes for new CSVs");
            sb.AppendLine("- [ ] Decide if Row2(types) should be required & enforce");
            sb.AppendLine("- [ ] Flags/bitfield parsing helpers for position/tag columns");
            sb.AppendLine("- [ ] Unit tests: CSV parsing, enum mapping, duplicate Id detection\n");

            // Changelog append
            string prev = File.Exists(outputFull) ? File.ReadAllText(outputFull, Encoding.UTF8) : string.Empty;
            if (!prev.Contains("## 8) Changelog")) sb.AppendLine("## 8) Changelog");
            else
            {
                var idx = prev.IndexOf("## 8) Changelog", StringComparison.Ordinal);
                sb.Append(prev.Substring(idx));
            }
            sb.AppendLine($"\n- {now}: Status file regenerated.");

            File.WriteAllText(outputFull, sb.ToString(), new UTF8Encoding(false));
            // 파일 쓰기
            File.WriteAllText(outputFull, sb.ToString(), new UTF8Encoding(false));

            // ★ Unity에 강제 반영
            AssetDatabase.ImportAsset(outputRel, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // ★ 풀 경로까지 로그로 찍기(경로 혼동 방지)
            Debug.Log($"[Status] Generated (rel): {outputRel}");
            Debug.Log($"[Status] Generated (abs): {outputFull}");

        }

        [PreferenceItem("SDProject")]
        public static void PreferencesGUI()
        {
            EditorGUILayout.LabelField("Status File", EditorStyles.boldLabel);
            var curr = EditorPrefs.GetString(PrefKey_OutputPath, DefaultOutputRelPath);
            EditorGUILayout.BeginHorizontal();
            var next = EditorGUILayout.TextField("Output Path (relative)", curr);
            if (GUILayout.Button("Reset", GUILayout.Width(70))) next = DefaultOutputRelPath;
            EditorGUILayout.EndHorizontal();

            if (next != curr) EditorPrefs.SetString(PrefKey_OutputPath, next);
            EditorGUILayout.HelpBox("Recommended: Assets/SDProject/Docs/SDProject_Status.md", MessageType.Info);

            if (GUILayout.Button("Generate Now")) Generate();
        }

        private static List<string> FindCsv(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return new();
            return AssetDatabase.FindAssets("t:TextAsset", new[] { folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> FindAsmdefs(string rootFolder)
        {
            if (!AssetDatabase.IsValidFolder(rootFolder)) return new();
            return AssetDatabase.FindAssets("t:TextAsset", new[] { rootFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string BuildFolderTree(string root, int maxDepth = 5)
        {
            if (!AssetDatabase.IsValidFolder(root)) return "(folder not found)";
            var sb = new StringBuilder();
            Recurse(root, 0);
            return sb.ToString();

            void Recurse(string folder, int depth)
            {
                if (depth > maxDepth) return;
                string indent = new string(' ', depth * 2);
                sb.AppendLine($"{indent}{Path.GetFileName(folder)}/");

                foreach (var sf in AssetDatabase.GetSubFolders(folder).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                    Recurse(sf, depth + 1);

                var files = AssetDatabase.FindAssets("", new[] { folder })
                              .Select(AssetDatabase.GUIDToAssetPath)
                              .Where(p => !AssetDatabase.IsValidFolder(p))
                              .Where(p => p.EndsWith(".asmdef") || p.EndsWith(".asset") || p.EndsWith(".csv") || p.EndsWith(".xlsx") || p.EndsWith(".cs"))
                              .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
                foreach (var f in files) sb.AppendLine($"{indent}  - {Path.GetFileName(f)}");
            }
        }
    }
}
#endif
