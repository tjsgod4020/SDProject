#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SD.DataTable;

namespace SD.DataTable.Editor
{
    [CustomEditor(typeof(DataTableConfig))]
    public class DataTableConfigInspector : UnityEditor.Editor
    {
        private const string DefaultCsvFolder = "Assets/SDProject/DataTables/Csv";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Auto-Sync", EditorStyles.boldLabel);

            // 폴더 선택
            string folder = EditorPrefs.GetString("SD.DataTable.CsvFolder", DefaultCsvFolder);
            EditorGUI.BeginChangeCheck();
            folder = EditorGUILayout.TextField("CSV Folder", folder);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString("SD.DataTable.CsvFolder", folder);

            using (new EditorGUI.DisabledScope(!AssetDatabase.IsValidFolder(folder)))
            {
                if (GUILayout.Button("Sync From Folder"))
                {
                    SyncFromFolder((DataTableConfig)target, folder);
                }
            }

            EditorGUILayout.HelpBox("CSV 파일명을 테이블 Id로 사용합니다. Row 타입은 [DataTableId] 또는 타입명 일치로 자동 매칭합니다.", MessageType.Info);
        }

        [MenuItem("Tools/DataTables/Sync Active DataTableConfig From Folder")]
        private static void MenuSyncActive()
        {
            var cfg = Selection.activeObject as DataTableConfig;
            if (cfg == null)
            {
                Debug.LogWarning("[DataTable] Select a DataTableConfig asset first.");
                return;
            }
            string folder = EditorPrefs.GetString("SD.DataTable.CsvFolder", DefaultCsvFolder);
            SyncFromFolder(cfg, folder);
        }

        private static void SyncFromFolder(DataTableConfig config, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError($"[DataTable] Invalid folder: {folder}");
                return;
            }

            // 1) CSV 파일 수집
            var csvGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { folder });
            var csvPaths = csvGuids.Select(AssetDatabase.GUIDToAssetPath)
                                   .Where(p => p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                                   .Distinct()
                                   .ToList();

            // 2) 타입 인덱스 빌드
            var rowTypes = FindRowTypes(); // IHasStringId 구현 타입들
            var byAttribute = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            var byName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in rowTypes)
            {
                var attr = t.GetCustomAttribute<DataTableIdAttribute>();
                if (attr != null && !string.IsNullOrWhiteSpace(attr.Id))
                    byAttribute[attr.Id] = t;

                byName[t.Name] = t;
            }

            // 3) 새 목록 만들기
            var list = new List<DataTableConfig.TableEntry>();
            foreach (var p in csvPaths)
            {
                var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
                var id = Path.GetFileNameWithoutExtension(p); // 파일명 = Id

                Type rowType = null;
                if (byAttribute.TryGetValue(id, out var at)) rowType = at;
                else if (byName.TryGetValue(id, out var nt)) rowType = nt;

                var entry = new DataTableConfig.TableEntry
                {
                    Id = id,
                    Csv = csv,
                    RowTypeName = rowType != null
                        ? $"{rowType.FullName}, {rowType.Assembly.GetName().Name}"
                        : string.Empty
                };

                if (rowType == null)
                    Debug.LogWarning($"[DataTable] No row type matched for Id '{id}'. (Add [DataTableId(\"{id}\")] or rename the class to '{id}')");

                list.Add(entry);
            }

            // 4) 정렬/중복 정리
            list = list.OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase).ToList();

            // 5) 적용
            Undo.RecordObject(config, "Sync DataTableConfig");
            var so = new SerializedObject(config);
            var prop = so.FindProperty("_tables");
            prop.ClearArray();
            for (int i = 0; i < list.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Id").stringValue = list[i].Id;
                elem.FindPropertyRelative("Csv").objectReferenceValue = list[i].Csv;
                elem.FindPropertyRelative("RowTypeName").stringValue = list[i].RowTypeName ?? string.Empty;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);

            Debug.Log($"[DataTable] Sync complete. Tables: {list.Count}");
        }

        private static IEnumerable<Type> FindRowTypes()
        {
            var result = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Unity/Editor 등 시스템 어셈블리 대량 건너뛰기(속도 최적화)
                var name = asm.GetName().Name;
                if (name.StartsWith("Unity")) continue;
                if (name.StartsWith("System")) continue;
                if (name.StartsWith("mscorlib")) continue;
                if (name.StartsWith("netstandard")) continue;

                Type iface = typeof(IHasStringId);
                Type attr = typeof(DataTableIdAttribute);

                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (!t.IsClass || t.IsAbstract) continue;
                        if (iface.IsAssignableFrom(t))
                            result.Add(t);
                    }
                }
                catch { /* 일부 동적 어셈블리 예외 무시 */ }
            }
            return result;
        }
    }
}
#endif