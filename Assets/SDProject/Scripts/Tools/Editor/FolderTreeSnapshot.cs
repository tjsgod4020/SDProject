#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SD.Tools.Editor
{
    public static class FolderTreeSnapshot
    {
        private const string DefaultRoot = "Assets/SDProject";
        private const string DefaultOut = "Assets/SDProject/Docs/folder-tree.txt";
        private const string PrefKey_Root = "SD.Status.FolderRoot";
        private const string PrefKey_Out = "SD.Status.FolderTreePath";

        [MenuItem("Tools/Project/Generate Folder Tree", priority = 2)]
        public static void GenerateFolderTreeFile()
        {
            var root = EditorPrefs.GetString(PrefKey_Root, DefaultRoot);
            var outRel = EditorPrefs.GetString(PrefKey_Out, DefaultOut);
            WriteTree(root, outRel);
        }

        [MenuItem("Tools/Project/Generate Status + Folder Tree", priority = 0)]
        public static void GenerateStatusAndTree()
        {
            try { StatusFileGenerator.Generate(); }
            catch (Exception ex) { Debug.LogError($"[Status] Generate failed: {ex.Message}"); }
            GenerateFolderTreeFile();
        }

        [PreferenceItem("SDProject")]
        public static void Prefs()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Folder Tree Snapshot", EditorStyles.boldLabel);

            var root = EditorPrefs.GetString(PrefKey_Root, DefaultRoot);
            var outRel = EditorPrefs.GetString(PrefKey_Out, DefaultOut);

            EditorGUILayout.BeginHorizontal();
            var nextRoot = EditorGUILayout.TextField("Root Folder", root);
            if (GUILayout.Button("Reset", GUILayout.Width(70))) nextRoot = DefaultRoot;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            var nextOut = EditorGUILayout.TextField("Output Path", outRel);
            if (GUILayout.Button("Reset", GUILayout.Width(70))) nextOut = DefaultOut;
            EditorGUILayout.EndHorizontal();

            if (nextRoot != root) EditorPrefs.SetString(PrefKey_Root, nextRoot);
            if (nextOut != outRel) EditorPrefs.SetString(PrefKey_Out, nextOut);

            if (GUILayout.Button("Generate Folder Tree Now")) GenerateFolderTreeFile();
        }

        private static void WriteTree(string rootFolder, string outRelPath)
        {
            if (!AssetDatabase.IsValidFolder(rootFolder))
            {
                Debug.LogWarning($"[folder-tree] Root folder not found: {rootFolder}");
                return;
            }

            var outFull = Path.GetFullPath(outRelPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outFull)!);

            var sb = new StringBuilder();
            sb.AppendLine(rootFolder + "/");
            Recurse(rootFolder, 1, sb);

            File.WriteAllText(outFull, sb.ToString(), new UTF8Encoding(false));
            File.WriteAllText(outFull, sb.ToString(), new UTF8Encoding(false));

            // ★ Unity에 강제 반영
            AssetDatabase.ImportAsset(outRelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // ★ 풀 경로 로그
            Debug.Log($"[folder-tree] Wrote (rel): {outRelPath}");
            Debug.Log($"[folder-tree] Wrote (abs): {outFull}");
        }

        private static void Recurse(string folder, int depth, StringBuilder sb, int maxDepth = 12)
        {
            if (depth > maxDepth) return;
            string indent = new string(' ', depth * 2);

            foreach (var sf in AssetDatabase.GetSubFolders(folder).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"{indent}{Path.GetFileName(sf)}/");
                Recurse(sf, depth + 1, sb, maxDepth);
            }

            var files = AssetDatabase.FindAssets("", new[] { folder })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Where(p => !AssetDatabase.IsValidFolder(p))
                        .Where(p => p.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase)
                                 || p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                                 || p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                                 || p.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                                 || p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var f in files) sb.AppendLine($"{indent}- {Path.GetFileName(f)}");
        }
    }
}
#endif
