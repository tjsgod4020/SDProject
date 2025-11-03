#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SD.Tools.Editor
{
    /// <summary>
    /// 프로젝트 상태 요약 Markdown 파일 생성기.
    /// FolderTreeSnapshot에서 호출하는 Generate()를 제공해
    /// "Generate Status + Folder Tree" 메뉴가 정상 동작하도록 한다.
    /// </summary>
    public static class StatusFileGenerator
    {
        private const string DefaultOut = "Assets/SDProject/Docs/SDProject_Status.md";

        [MenuItem("Tools/Project/Generate Status File", priority = 1)]
        public static void GenerateMenu()
        {
            Generate(); // 메뉴에서 호출 시 동일 로직 사용
        }

        /// <summary>
        /// FolderTreeSnapshot.GenerateStatusAndTree()가 호출하는 API.
        /// 프로젝트 요약을 간단히 기록한다(의존성 최소화).
        /// </summary>
        public static void Generate()
        {
            try
            {
                WriteStatus(DefaultOut);
                Debug.Log($"[Status] Wrote: {DefaultOut}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Status] Generate failed: {ex.Message}");
            }
        }

        private static void WriteStatus(string outRelPath)
        {
            var outFull = Path.GetFullPath(outRelPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outFull)!);

            var sb = new StringBuilder();
            sb.AppendLine("# SDProject - Status");
            sb.AppendLine();
            sb.AppendLine($"- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Unity: {Application.unityVersion}");
#if UNITY_EDITOR
            sb.AppendLine($"- Platform: {EditorUserBuildSettings.activeBuildTarget}");
#endif
            sb.AppendLine();

            // 최소 정보만 남김 (강한 의존성 없이 항상 성공하게)
            sb.AppendLine("## Notes");
            sb.AppendLine("- 이 파일은 Tools ▸ Project ▸ Generate Status File 혹은");
            sb.AppendLine("- Tools ▸ Project ▸ Generate Status + Folder Tree 실행 시 생성/갱신됩니다.");
            sb.AppendLine();

            File.WriteAllText(outFull, sb.ToString(), new UTF8Encoding(false));

            // Unity에 반영
            AssetDatabase.ImportAsset(outRelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif