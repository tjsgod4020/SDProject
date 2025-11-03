#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SD.DataTable.Editor
{
    /// CSV 폴더를 스캔하여 모든 DataTableConfig의 _tables 배열을 동기화하고
    /// RowTypeName을 AssemblyQualifiedName으로 자동 채운다.
    /// - 우선순위: [DataTableId("Id")] > 관습(Id / IdRow / IdModel / IdDataModel)
    /// - 유일성 보장: 후보가 0개/2개 이상이면 채우지 않고 경고만 남긴다.
    /// - 메뉴 제공: Auto Sync 토글 / CSV 폴더 지정 / 전체 동기화 / 단일 Config 동기화
    public sealed class DataTableConfigAutoSync : AssetPostprocessor
    {
        private const string CsvFolderDefault = "Assets/SDProject/DataTables/Csv";
        private const string PrefKeyAutoSync = "SD.DataTable.AutoSync.Enabled";
        private const string PrefKeyCsvFolder = "SD.DataTable.CsvFolder";

        private static bool _isSyncInProgress;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (!EditorPrefs.HasKey(PrefKeyAutoSync))
                EditorPrefs.SetBool(PrefKeyAutoSync, true);
            if (!EditorPrefs.HasKey(PrefKeyCsvFolder))
                EditorPrefs.SetString(PrefKeyCsvFolder, CsvFolderDefault);
        }

        [MenuItem("Tools/DataTables/Auto Sync Config", priority = 10)]
        private static void ToggleAutoSync()
        {
            bool now = EditorPrefs.GetBool(PrefKeyAutoSync, true);
            EditorPrefs.SetBool(PrefKeyAutoSync, !now);
            Debug.Log("[DataTable] Auto Sync: " + (!now ? "ON" : "OFF"));
        }

        [MenuItem("Tools/DataTables/Auto Sync Config", true)]
        private static bool ToggleAutoSyncValidate()
        {
            Menu.SetChecked("Tools/DataTables/Auto Sync Config", EditorPrefs.GetBool(PrefKeyAutoSync, true));
            return true;
        }

        [MenuItem("Tools/DataTables/Set CSV Folder...", priority = 11)]
        private static void SetCsvFolder()
        {
            string current = EditorPrefs.GetString(PrefKeyCsvFolder, CsvFolderDefault);
            string sel = EditorUtility.OpenFolderPanel("Select CSV Folder (under Assets)", current, "");
            if (string.IsNullOrEmpty(sel)) return;

            string proj = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            if (!sel.StartsWith(proj))
            {
                Debug.LogError("선택한 폴더는 프로젝트 Assets 내부여야 합니다.");
                return;
            }

            string rel = sel.Substring(proj.Length).Replace('\\', '/');
            if (!rel.StartsWith("Assets/")) rel = "Assets/" + rel.TrimStart('/');

            if (!AssetDatabase.IsValidFolder(rel))
            {
                Debug.LogError("유효하지 않은 폴더: " + rel);
                return;
            }

            EditorPrefs.SetString(PrefKeyCsvFolder, rel);
            Debug.Log("[DataTable] CSV Folder: " + rel);
        }

        [MenuItem("Tools/DataTables/Sync All Configs Now", priority = 12)]
        private static void SyncAllNow()
        {
            string csvFolder = EditorPrefs.GetString(PrefKeyCsvFolder, CsvFolderDefault);
            SyncAllConfigs(csvFolder);
        }

        /// <summary>인스펙터 버튼에서 호출: 단일 Config만 동기화</summary>
        public static void SyncOneConfigNow(UnityEngine.ScriptableObject cfg)
        {
            if (cfg == null) return;

            string csvFolder = EditorPrefs.GetString(PrefKeyCsvFolder, CsvFolderDefault);
            if (!AssetDatabase.IsValidFolder(csvFolder))
            {
                Debug.LogWarning("[DataTable] CSV folder missing: " + csvFolder);
                return;
            }

            List<string> csvPaths = CollectCsvPaths(csvFolder);
            Dictionary<string, Type> byAttr, byName, byNameRow;
            BuildTypeIndexes(out byAttr, out byName, out byNameRow);

            int typed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                ApplyToConfig(cfg, csvPaths, byAttr, byName, byNameRow, ref typed);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[DataTable] Sync '" + cfg.name + "' done. Typed: " + typed);
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
        {
            if (!EditorPrefs.GetBool(PrefKeyAutoSync, true)) return;
            if (_isSyncInProgress) return;

            string csvFolder = EditorPrefs.GetString(PrefKeyCsvFolder, CsvFolderDefault);
            bool relevant =
                imported.Any(p => IsCsvUnder(p, csvFolder)) ||
                movedTo.Any(p => IsCsvUnder(p, csvFolder)) ||
                deleted.Any(p => IsCsvUnder(p, csvFolder)) ||
                movedFrom.Any(p => IsCsvUnder(p, csvFolder));

            if (relevant) SyncAllConfigs(csvFolder);
        }

        private static bool IsCsvUnder(string path, string root)
        {
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
                   path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
        }

        private static void SyncAllConfigs(string csvFolder)
        {
            if (_isSyncInProgress) return;
            _isSyncInProgress = true;

            try
            {
                if (!AssetDatabase.IsValidFolder(csvFolder))
                {
                    Debug.LogWarning("[DataTable] CSV folder missing: " + csvFolder);
                    return;
                }

                // 1) 모든 DataTableConfig
                string[] cfgGuids = AssetDatabase.FindAssets("t:DataTableConfig");
                List<UnityEngine.ScriptableObject> cfgs = cfgGuids
                    .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(c => c != null)
                    .ToList();
                if (cfgs.Count == 0) return;

                // 2) CSV 수집
                List<string> csvPaths = CollectCsvPaths(csvFolder);

                // 3) 타입 인덱스
                Dictionary<string, Type> byAttr, byName, byNameRow;
                BuildTypeIndexes(out byAttr, out byName, out byNameRow);

                // 4) 적용
                int totalEntries = 0, filledTypes = 0;
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var cfg in cfgs)
                        totalEntries += ApplyToConfig(cfg, csvPaths, byAttr, byName, byNameRow, ref filledTypes);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                Debug.Log("[DataTable] Auto Sync complete → Configs: " + cfgs.Count + ", Tables: " + totalEntries + ", Typed: " + filledTypes);
            }
            finally
            {
                _isSyncInProgress = false;
            }
        }

        // --- Internals -------------------------------------------------------

        private static List<string> CollectCsvPaths(string csvFolder)
        {
            string[] csvGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { csvFolder });
            List<string> list = csvGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return list;
        }

        private static int ApplyToConfig(
            UnityEngine.ScriptableObject cfg,
            List<string> csvPaths,
            Dictionary<string, Type> byAttr,
            Dictionary<string, Type> byName,
            Dictionary<string, Type> byNameRow,
            ref int filledTypes)
        {
            // DataTableConfig 구조를 SerializedObject로 접근:
            // - 리스트 필드명: "_tables"
            // - 각 요소: "Id"(string) / "Csv"(TextAsset) / "RowTypeName"(string)
            SerializedObject so = new SerializedObject(cfg);
            SerializedProperty listProp = so.FindProperty("_tables");
            if (listProp == null || !listProp.isArray)
            {
                Debug.LogWarning("[DataTable] " + _nameof(cfg) + " 에 '_tables' 배열이 없습니다. 스킵.");
                return 0;
            }

            listProp.ClearArray();
            int i = 0, localFilled = 0;

            for (int k = 0; k < csvPaths.Count; k++)
            {
                string p = csvPaths[k];
                TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
                string id = Path.GetFileNameWithoutExtension(p);

                string reason;
                Type rowType = ResolveRowTypeStrict(id, byAttr, byName, byNameRow, out reason);

                listProp.InsertArrayElementAtIndex(i);
                SerializedProperty elem = listProp.GetArrayElementAtIndex(i);
                i++;

                SerializedProperty idProp = elem.FindPropertyRelative("Id");
                if (idProp != null) idProp.stringValue = id;

                SerializedProperty csvProp = elem.FindPropertyRelative("Csv");
                if (csvProp != null) csvProp.objectReferenceValue = csv;

                SerializedProperty rtypeProp = elem.FindPropertyRelative("RowTypeName");
                if (rowType != null)
                {
                    if (rtypeProp != null) rtypeProp.stringValue = rowType.AssemblyQualifiedName;
                    localFilled++;
                    if (reason != "attribute")
                        Debug.Log("[DataTable] RowTypeName set by convention: Id='" + id + "' → " + rowType.FullName);
                }
                else
                {
                    if (rtypeProp != null) rtypeProp.stringValue = string.Empty;
                    string msg = (reason == "no-match")
                        ? "No type found for Id '" + id + "'. Row 타입에 [SD.DataTable.DataTableId(\"" + id + "\")] 특성을 부여(권장)하거나, 클래스명을 '" + id + "', '" + id + "Row', '" + id + "Model', '" + id + "DataModel' 중 하나로 맞추세요."
                        : "Multiple candidate types for Id '" + id + "' → " + reason + ". 특성 [SD.DataTable.DataTableId(\"" + id + "\")]으로 명시해 주세요.";
                    Debug.LogWarning("[DataTable] " + msg + " (Config: " + cfg.name + ")");
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(cfg);
            filledTypes += localFilled;
            return i;

            // local helper
            static string _nameof(UnityEngine.Object o) { return o != null ? o.name : "(null)"; }
        }

        private static Type ResolveRowTypeStrict(
            string id,
            Dictionary<string, Type> byAttr,
            Dictionary<string, Type> byName,
            Dictionary<string, Type> byNameRow,
            out string reason)
        {
            // 1) 특성 우선
            Type viaAttr;
            if (byAttr.TryGetValue(id, out viaAttr)) { reason = "attribute"; return viaAttr; }

            // 2) 관습 후보 수집
            List<Type> candidates = new List<Type>();
            Type t0;
            if (byName.TryGetValue(id, out t0)) candidates.Add(t0);
            Type t1;
            if (byNameRow.TryGetValue(id, out t1)) candidates.Add(t1);
            Type t2;
            if (byName.TryGetValue(id + "Model", out t2)) candidates.Add(t2);
            Type t3;
            if (byName.TryGetValue(id + "DataModel", out t3)) candidates.Add(t3);

            // 유일성 판단
            List<Type> distinct = candidates.Distinct().ToList();
            if (distinct.Count == 1) { reason = "convention"; return distinct[0]; }

            if (distinct.Count == 0) reason = "no-match";
            else reason = "ambiguous: " + string.Join(", ", distinct.Select(x => x.FullName));
            return null;
        }

        private static void BuildTypeIndexes(
            out Dictionary<string, Type> byAttribute,
            out Dictionary<string, Type> byName,
            out Dictionary<string, Type> byNameRow)
        {
            byAttribute = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            byName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            byNameRow = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string n = asm.GetName().Name;
                // 시스템계열만 배제, SD.* / 게임용 asmdef 전부 포함
                if (n.StartsWith("Unity") || n.StartsWith("System") || n.StartsWith("mscorlib") || n.StartsWith("netstandard"))
                    continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                for (int i = 0; i < types.Length; i++)
                {
                    Type t = types[i];
                    if (!t.IsClass || t.IsAbstract) continue;

                    // 1) [DataTableId("CardData")] 같은 특성이 있으면 최우선 키로 등록
                    SD.DataTable.DataTableIdAttribute attr =
                        (SD.DataTable.DataTableIdAttribute)Attribute.GetCustomAttribute(t, typeof(SD.DataTable.DataTableIdAttribute));
                    if (attr != null && !string.IsNullOrWhiteSpace(attr.Id))
                        byAttribute[attr.Id] = t;

                    // 2) 클래스명 그대로 매칭 (CardData, CardName, CardDesc ...)
                    byName[t.Name] = t;

                    // 3) 클래스명이 {Id}Row 형태면 {Id} 키로 등록 (CardDataRow → "CardData")
                    if (t.Name.EndsWith("Row", StringComparison.OrdinalIgnoreCase))
                    {
                        string key = t.Name.Substring(0, t.Name.Length - "Row".Length);
                        if (!string.IsNullOrWhiteSpace(key))
                            byNameRow[key] = t;
                    }
                }
            }
        }
    }

    // SerializedProperty 편의 확장
    internal static class SerializedPropertyExt
    {
        public static void SetString(this SerializedProperty p, string v) { if (p != null) p.stringValue = v; }
        public static void SetObject(this SerializedProperty p, UnityEngine.Object o) { if (p != null) p.objectReferenceValue = o; }
    }
}
#endif