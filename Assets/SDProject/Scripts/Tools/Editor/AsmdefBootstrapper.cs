// File: Assets/SDProject/Scripts/Tools/Editor/AsmdefBootstrapper.cs
// 목적: 정식 구조용 asmdef 일괄 생성 (idempotent)
// 메뉴: SD/Bootstrap/Create asmdefs
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SD.Tools.Editor
{
    internal static class AsmdefBootstrapper
    {
        private const bool OverwriteExisting = true;

        // 경로 유틸
        private static string P(params string[] parts) => string.Join("/", parts);

        // 현재 스크립트의 경로를 기반으로 루트(Assets/***까지)를 자동 추출
        // 예: Assets/SDProject/Scripts/Tools/Editor/AsmdefBootstrapper.cs -> Assets/SDProject
        private static string DetectProjectRoot()
        {
            try
            {
                // 이 클래스 소스 에셋 검색
                var guids = AssetDatabase.FindAssets("AsmdefBootstrapper t:Script");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith("AsmdefBootstrapper.cs", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var idx = path.IndexOf("/Scripts/", StringComparison.Ordinal);
                    if (idx > 0)
                    {
                        var root = path.Substring(0, idx); // "Assets/SDProject"
                        if (root.StartsWith("Assets/", StringComparison.Ordinal))
                            return root;
                    }
                }
            }
            catch { /* no-op */ }

            // 폴백: 흔한 루트 후보
            var candidates = new[] { "Assets/SDProject", "Assets/_Project" };
            foreach (var c in candidates)
            {
                if (AssetDatabase.IsValidFolder(c))
                    return c;
            }

            throw new InvalidOperationException("프로젝트 루트를 찾을 수 없습니다. 'Assets/SDProject' 또는 'Assets/_Project' 중 하나를 생성하거나, 스크립트를 'Assets/<Root>/Scripts/.../AsmdefBootstrapper.cs'에 두세요.");
        }

        // asmdef JSON 모델
        [Serializable]
        private class AsmdefJson
        {
            public string name;
            public string[] references = Array.Empty<string>();
            public string[] includePlatforms = Array.Empty<string>();
            public string[] excludePlatforms = Array.Empty<string>();
            public bool allowUnsafeCode = false;
            public bool autoReferenced = true;
            public bool overrideReferences = false;
            public string[] precompiledReferences = Array.Empty<string>();
            public string[] defineConstraints = Array.Empty<string>();
            public bool noEngineReferences = false;
            public string[] optionalUnityReferences = Array.Empty<string>(); // tests
        }

        private class AsmdefSpec
        {
            public string Name;
            public string RelFolder;              // 루트(base)/ 하위 상대 경로 ("Scripts/Core/Domain" 등)
            public string[] References;
            public bool EditorOnly = false;
            public bool IsTestAssembly = false;
            public bool NoEngineRefs = false;
            public bool AutoReferenced = true;
        }

        // ====== 정의 목록 ======
        // 주의: 폴더는 "루트/RelFolder"로 생성됨. 루트는 DetectProjectRoot()로 자동 결정.
        private static readonly List<AsmdefSpec> _specs = new()
        {
            // Core
            new AsmdefSpec { Name = "SD.Core.Domain",          RelFolder = "Scripts/Core/Domain",          References = Array.Empty<string>(), NoEngineRefs = false },
            new AsmdefSpec { Name = "SD.Core.Infrastructure",  RelFolder = "Scripts/Core/Infrastructure",  References = new [] { "SD.Core.Domain" } },
            new AsmdefSpec { Name = "SD.Core.Presentation",    RelFolder = "Scripts/Core/Presentation",    References = new [] { "SD.Core.Domain", "SD.Core.Infrastructure", "Unity.TextMeshPro" } },

            // Gameplay.Cards
            new AsmdefSpec { Name = "SD.Gameplay.Cards.Domain",         RelFolder = "Scripts/Gameplay/Cards/Domain",         References = new [] { "SD.Core.Domain" } },
            new AsmdefSpec { Name = "SD.Gameplay.Cards.Infrastructure", RelFolder = "Scripts/Gameplay/Cards/Infrastructure", References = new [] { "SD.Gameplay.Cards.Domain", "SD.Core.Infrastructure" } },
            new AsmdefSpec { Name = "SD.Gameplay.Cards.Presentation",   RelFolder = "Scripts/Gameplay/Cards/Presentation",   References = new [] { "SD.Gameplay.Cards.Domain", "SD.Core.Presentation" } },

            // Gameplay.Battle
            new AsmdefSpec { Name = "SD.Gameplay.Battle.Domain",         RelFolder = "Scripts/Gameplay/Battle/Domain",         References = new [] { "SD.Core.Domain", "SD.Gameplay.Cards.Domain" } },
            new AsmdefSpec { Name = "SD.Gameplay.Battle.Infrastructure", RelFolder = "Scripts/Gameplay/Battle/Infrastructure", References = new [] { "SD.Gameplay.Battle.Domain", "SD.Core.Infrastructure" } },
            new AsmdefSpec { Name = "SD.Gameplay.Battle.Presentation",   RelFolder = "Scripts/Gameplay/Battle/Presentation",   References = new [] { "SD.Gameplay.Battle.Domain", "SD.Core.Presentation" } },

            // DataTables
            new AsmdefSpec { Name = "SD.DataTables.Runtime", RelFolder = "Scripts/DataTable/Runtime", References = new [] { "SD.Core.Domain", "Unity.Addressables" } },
            new AsmdefSpec { Name = "SD.DataTables.Editor",  RelFolder = "Scripts/DataTable/Editor",  References = new [] { "SD.DataTables.Runtime" }, EditorOnly = true },

            // UI / Tools
            new AsmdefSpec { Name = "SD.UI.Common",  RelFolder = "Scripts/UI/Common",  References = new [] { "SD.Core.Presentation", "Unity.TextMeshPro" } },
            new AsmdefSpec { Name = "SD.UI.Editor",  RelFolder = "Scripts/UI/Editor",  References = new [] { "SD.UI.Common" }, EditorOnly = true },
            new AsmdefSpec { Name = "SD.Tools.Editor", RelFolder = "Scripts/Tools/Editor", References = Array.Empty<string>(), EditorOnly = true },

            // Tests
            new AsmdefSpec {
                Name = "SD.Tests.EditMode",
                RelFolder = "Scripts/Core/Tests",
                References = new [] { "SD.Core.Domain", "SD.Core.Infrastructure" },
                EditorOnly = true, IsTestAssembly = true
            },
            new AsmdefSpec {
                Name = "SD.Gameplay.Tests",
                RelFolder = "Scripts/Gameplay/Battle/Tests",
                References = new [] { "SD.Gameplay.Battle.Domain", "SD.Gameplay.Battle.Infrastructure", "SD.Core.Infrastructure", "SD.Gameplay.Cards.Domain" },
                EditorOnly = true, IsTestAssembly = true
            },
        };

        [MenuItem("SD/Bootstrap/Create asmdefs", priority = 10)]
        public static void CreateAsmdefs()
        {
            var baseRoot = DetectProjectRoot(); // ex) "Assets/SDProject"
            try
            {
                int created = 0, skipped = 0, overwritten = 0;

                foreach (var spec in _specs)
                {
                    var fullFolder = P(baseRoot, spec.RelFolder);
                    EnsureFolder(fullFolder);

                    var asmdefPath = P(fullFolder, $"{spec.Name}.asmdef");
                    var json = BuildJson(spec);

                    if (File.Exists(asmdefPath))
                    {
                        if (!OverwriteExisting)
                        {
                            skipped++;
                            continue;
                        }
                        var old = File.ReadAllText(asmdefPath, Encoding.UTF8);
                        if (old == json)
                        {
                            skipped++;
                            continue;
                        }
                        File.WriteAllText(asmdefPath, json, Encoding.UTF8);
                        overwritten++;
                    }
                    else
                    {
                        File.WriteAllText(asmdefPath, json, Encoding.UTF8);
                        created++;
                    }
                }

                AssetDatabase.Refresh();
                Debug.Log($"[AsmdefBootstrapper] Root='{baseRoot}' Done. created={created}, overwritten={overwritten}, skipped={skipped}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsmdefBootstrapper] ERROR: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            var parts = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new InvalidOperationException($"경로는 'Assets/..'로 시작해야 합니다: {folderPath}");

            var current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                var next = P(current, parts[i]);
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string BuildJson(AsmdefSpec spec)
        {
            var data = new AsmdefJson
            {
                name = spec.Name,
                references = spec.References?.Distinct().ToArray() ?? Array.Empty<string>(),
                includePlatforms = spec.EditorOnly ? new[] { "Editor" } : Array.Empty<string>(),
                excludePlatforms = Array.Empty<string>(),
                allowUnsafeCode = false,
                autoReferenced = spec.AutoReferenced,
                overrideReferences = false,
                precompiledReferences = Array.Empty<string>(),
                defineConstraints = Array.Empty<string>(),
                noEngineReferences = spec.NoEngineRefs,
                optionalUnityReferences = spec.IsTestAssembly ? new[] { "TestAssemblies" } : Array.Empty<string>()
            };

            var sb = new StringBuilder();
            void Arr(string key, IEnumerable<string> arr)
            {
                var list = arr?.ToArray() ?? Array.Empty<string>();
                sb.Append($"  \"{key}\": [");
                if (list.Length > 0)
                {
                    sb.Append("\n");
                    for (int i = 0; i < list.Length; i++)
                    {
                        sb.Append($"    \"{list[i]}\"");
                        if (i < list.Length - 1) sb.Append(",");
                        sb.Append("\n");
                    }
                    sb.Append("  ]");
                }
                else sb.Append("]");
            }

            sb.Append("{\n");
            sb.AppendFormat("  \"name\": \"{0}\",\n", Escape(data.name));
            Arr("references", data.references); sb.Append(",\n");
            Arr("includePlatforms", data.includePlatforms); sb.Append(",\n");
            Arr("excludePlatforms", data.excludePlatforms); sb.Append(",\n");
            sb.AppendFormat("  \"allowUnsafeCode\": {0},\n", data.allowUnsafeCode.ToString().ToLower());
            sb.AppendFormat("  \"autoReferenced\": {0},\n", data.autoReferenced.ToString().ToLower());
            sb.AppendFormat("  \"overrideReferences\": {0},\n", data.overrideReferences.ToString().ToLower());
            Arr("precompiledReferences", data.precompiledReferences); sb.Append(",\n");
            Arr("defineConstraints", data.defineConstraints); sb.Append(",\n");
            sb.AppendFormat("  \"noEngineReferences\": {0}", data.noEngineReferences.ToString().ToLower());
            if (data.optionalUnityReferences != null && data.optionalUnityReferences.Length > 0)
            {
                sb.Append(",\n");
                Arr("optionalUnityReferences", data.optionalUnityReferences);
            }
            sb.Append("\n}\n");
            return sb.ToString();
        }

        private static string Escape(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }
}
#endif
