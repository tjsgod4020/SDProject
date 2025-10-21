#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace SD.DataTable.Editor
{
    public sealed class XlsxAssetPostprocessor : AssetPostprocessor
    {
        private const string XlsxDir = "Assets/SDProject/DataTables/Xlsx";
        private const string CsvDir = "Assets/SDProject/DataTables/Csv";

        // 자산 임포트/변경/이동 감지 시 자동 변환
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
        {
            bool any = false;

            void TryConvert(string assetPath)
            {
                if (!assetPath.EndsWith(".xlsx", System.StringComparison.OrdinalIgnoreCase)) return;
                if (!assetPath.StartsWith(XlsxDir)) return;

                var fullIn = Path.GetFullPath(assetPath);
                var fullOut = Path.GetFullPath(CsvDir);

                try
                {
                    XlsxToCsvConverter.ConvertXlsxToCsvFiles(fullIn, fullOut);
                    any = true;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[XLSX→CSV] Convert failed: {assetPath}\n{ex.Message}");
                }
            }

            foreach (var p in imported) TryConvert(p);
            foreach (var p in movedTo) TryConvert(p);

            if (any)
            {
                AssetDatabase.Refresh();
            }
        }

        // 메뉴에서 수동 일괄 변환
        [MenuItem("Tools/DataTables/Convert All XLSX")]
        private static void ConvertAll()
        {
            if (!AssetDatabase.IsValidFolder(XlsxDir))
            {
                UnityEngine.Debug.LogWarning($"[XLSX→CSV] Missing folder: {XlsxDir}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:DefaultAsset", new[] { XlsxDir });
            int count = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path.EndsWith(".xlsx"))
                {
                    try
                    {
                        XlsxToCsvConverter.ConvertXlsxToCsvFiles(Path.GetFullPath(path), Path.GetFullPath(CsvDir));
                        count++;
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[XLSX→CSV] Convert failed: {path}\n{ex.Message}");
                    }
                }
            }
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[XLSX→CSV] Batch done. Converted files: {count}");
        }
    }
}
#endif
