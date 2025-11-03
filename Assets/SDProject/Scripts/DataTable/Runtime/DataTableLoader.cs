using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SD.DataTable
{
    /// <summary>
    /// CSV → RowType 파싱 → TableRegistry 등록.
    /// - RowTypeName 비어있는 항목은 "미사용 CSV"로 간주하여 조용히 스킵(옵션).
    /// - 에디터에선 Config SO 자동 바인딩(최다 테이블 보유 자산 1개).
    /// - 빈 줄/주석(#)/Enabled=false는 파싱 단계에서 제외.
    /// </summary>
    [DefaultExecutionOrder(-100)] // 부트스트랩보다 먼저 실행
    public sealed partial class DataTableLoader : MonoBehaviour
    {
        [Serializable]
        private struct TableEntry
        {
            public string Id;
            public TextAsset Csv;
            public string RowTypeName; // AssemblyQualifiedName (비면 '미사용 CSV'로 간주 가능)
        }

        [SerializeField] private bool _enabled = true;

        // 인스펙터에서 연결할 Config (Auto Sync로 채워진 SO)
        [SerializeField] private DataTableConfig _config;

        // (옵션) 수동 등록용 리스트: Config가 비어있을 때만 사용
        [SerializeField] private List<TableEntry> _tables = new();

        // ▼ 유연 스킵 모드: RowTypeName 비었으면 미사용 CSV로 간주해 조용히 스킵
        [Header("Unused Table Handling")]
        [SerializeField] private bool _treatEmptyRowTypeAsUnused = true; // 권장: true
        [SerializeField] private bool _warnOnUnusedTable = true;         // 스킵 시 경고 로그 남길지

        private void Awake()
        {
            if (!_enabled)
            {
                Debug.Log("[DataTable] Loader disabled (_enabled=false).");
                return;
            }

#if UNITY_EDITOR
            // 에디터에서 Config가 비어있으면 프로젝트 내에서 자동 탐색/바인딩(테이블 가장 많은 것)
            if (_config == null)
            {
                var cfg = FindBestConfigAsset();
                if (cfg != null)
                {
                    _config = cfg;
                    Debug.Log($"[DataTable] Auto-bound Config: {UnityEditor.AssetDatabase.GetAssetPath(cfg)} (Tables={cfg.Tables?.Count ?? 0})");
                }
                else
                {
                    Debug.LogWarning("[DataTable] No DataTableConfig asset found in project. Using local _tables.");
                }
            }
#endif

            LoadAll();
        }

        public void LoadAll()
        {
            // 우선순위: Config.Tables → _tables
            IEnumerable<(string Id, TextAsset Csv, string RowTypeName)> src;

            if (_config != null && _config.Tables != null && _config.Tables.Count > 0)
                src = _config.Tables.Select(t => (t.Id, t.Csv, t.RowTypeName));
            else
                src = _tables.Select(t => (t.Id, t.Csv, t.RowTypeName));

            var list = src.ToList();
            Debug.Log($"[DataTable] LoadAll source: Config={(_config != null ? "YES" : "NO")}, entries={list.Count}");

            int total = 0;
            foreach (var t in list)
            {
                try
                {
                    // 0) CSV 없음 → 조용히 스킵
                    if (t.Csv == null)
                    {
                        if (_warnOnUnusedTable)
                            Debug.LogWarning($"[DataTable] Load skipped: {t.Id} :: CSV is null");
                        continue;
                    }

                    // 1) RowTypeName 비었으면 '미사용 CSV'로 취급
                    if (string.IsNullOrWhiteSpace(t.RowTypeName))
                    {
                        if (_treatEmptyRowTypeAsUnused)
                        {
                            if (_warnOnUnusedTable)
                                Debug.LogWarning($"[DataTable] Skip(unused): {t.Id} :: RowTypeName empty");
                            continue; // 조용히 스킵
                        }
                        else
                        {
                            Debug.LogError(
                                $"[DataTable] Load skipped: {t.Id} :: RowTypeName is empty. " +
                                $"Tools > DataTables > Sync All Configs Now 또는 [DataTableId(\"{t.Id}\")]");
                            continue;
                        }
                    }

                    // 2) 타입 확인
                    var rowType = Type.GetType(t.RowTypeName, throwOnError: false);
                    if (rowType == null)
                    {
                        if (_treatEmptyRowTypeAsUnused && _warnOnUnusedTable)
                            Debug.LogWarning($"[DataTable] Skip(unresolved): {t.Id} :: Cannot resolve '{t.RowTypeName}'");
                        else
                            Debug.LogError($"[DataTable] Load failed: {t.Id} :: Cannot resolve type '{t.RowTypeName}'");
                        continue;
                    }

                    // 3) 파싱
                    var rows = BuildRows(t.Csv.text, rowType);

                    // 4) 전역 레지스트리에 등록
                    TableRegistry.Set(t.Id, rows);

                    var count = (rows as System.Collections.ICollection)?.Count ?? 0;
                    Debug.Log($"[DataTable] Loaded {t.Id} → {rowType.Name} ({count} rows)");
                    total += count;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataTable] Load failed: {t.Id} :: {ex.Message}");
                }
            }

            if (total == 0)
                Debug.LogWarning("[DataTable] No rows loaded. Check Config reference and RowTypeName.");
        }

        /// <summary>
        /// CSV 텍스트를 rowType으로 매핑한 행 리스트를 반환한다.
        /// 반환 타입은 비제네릭 IList (List&lt;rowType&gt;를 박싱).
        /// - 빈 줄/주석(#...) 스킵
        /// - (존재 시) Id 공란 스킵
        /// - (존재 시) Enabled=false 스킵
        /// </summary>
        private static IList BuildRows(string csvText, Type rowType)
        {
            if (string.IsNullOrWhiteSpace(csvText))
                return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(rowType));

            var lines = ReadCsv(csvText);
            if (lines.Count == 0)
                return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(rowType));

            var header = lines[0].Select(h => h.Trim()).ToArray();
            var listType = typeof(List<>).MakeGenericType(rowType);
            var list = (IList)Activator.CreateInstance(listType);

            // 멤버 인덱스(헤더→프로퍼티/필드) 준비
            var members = CacheMembers(rowType, header);

            // 유효컬럼 인덱스 탐색(Id/Enabled)
            int idCol = Array.FindIndex(header, h => string.Equals(h, "Id", StringComparison.OrdinalIgnoreCase));
            int enCol = Array.FindIndex(header, h => string.Equals(h, "Enabled", StringComparison.OrdinalIgnoreCase));

            // --- 행 파싱 ---
            for (int r = 1; r < lines.Count; r++)
            {
                var cells = lines[r];
                int n = Math.Min(header.Length, cells.Length);

                // 0) 빈 줄 스킵
                bool allEmpty = true;
                for (int c = 0; c < n; c++)
                    if (!string.IsNullOrWhiteSpace(cells[c])) { allEmpty = false; break; }
                if (allEmpty) continue;

                // 1) 주석 줄 스킵: 첫 셀 "#..." 이면 무시
                if (n > 0 && cells[0].TrimStart().StartsWith("#")) continue;

                // 2) Id 빈 값 스킵 (Id 컬럼이 존재할 때만)
                if (idCol >= 0)
                {
                    var idText = idCol < cells.Length ? cells[idCol] : null;
                    if (string.IsNullOrWhiteSpace(idText)) continue;
                }

                // 3) Enabled=false 스킵 (Enabled 컬럼이 존재할 때만)
                if (enCol >= 0 && enCol < cells.Length)
                {
                    var s = cells[enCol]?.Trim();

                    // 공란을 true로 볼지 여부:
                    //   - 공란 = 활성(true)로 보려면 다음 줄의 주석을 해제
                    // if (string.IsNullOrWhiteSpace(s)) goto BUILD;

                    bool enabled = !string.IsNullOrEmpty(s) &&
                                   (s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase));
                    if (!enabled) continue;
                }

                // ---- 인스턴스 생성 & 멤버 매핑 ----
                var row = Activator.CreateInstance(rowType); // 기본 생성자 필요
                for (int c = 0; c < n; c++)
                {
                    var m = members[c];
                    if (m == null) continue;
                    var text = cells[c];

                    try
                    {
                        if (m is PropertyInfo pi)
                        {
                            if (!pi.CanWrite) continue;
                            var val = ConvertFromString(text, pi.PropertyType);
                            pi.SetValue(row, val);
                        }
                        else if (m is FieldInfo fi)
                        {
                            var val = ConvertFromString(text, fi.FieldType);
                            fi.SetValue(row, val);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[DataTable] Convert failed @ row {r + 1}, col {c + 1} ({header[c]}='{text}') → {e.Message}");
                    }
                }

                list.Add(row);
            }

            return list;
        }

        // --- Helpers ---------------------------------------------------------

        private static List<string[]> ReadCsv(string text)
        {
            var result = new List<string[]>();
            using var sr = new StringReader(text);

            var row = new List<string>();
            var sb = new System.Text.StringBuilder();
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

        /// <summary>헤더 순서대로 매칭되는 멤버 캐시(없으면 null).</summary>
        private static MemberInfo[] CacheMembers(Type rowType, string[] header)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
            var props = rowType.GetProperties(flags).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            var fields = rowType.GetFields(flags).ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            var result = new MemberInfo[header.Length];
            for (int i = 0; i < header.Length; i++)
            {
                var name = header[i];
                if (string.IsNullOrWhiteSpace(name)) { result[i] = null; continue; }

                if (props.TryGetValue(name, out var p)) result[i] = p;
                else if (fields.TryGetValue(name, out var f)) result[i] = f;
                else result[i] = null; // 매칭되는 멤버 없음
            }
            return result;
        }

        private static object ConvertFromString(string s, Type target)
        {
            if (target == typeof(string)) return s ?? string.Empty;
            if (target == typeof(int)) return int.TryParse(s, out var i) ? i : 0;
            if (target == typeof(float)) return float.TryParse(s, out var f) ? f : 0f;
            if (target == typeof(double)) return double.TryParse(s, out var d) ? d : 0d;
            if (target == typeof(bool)) return !string.IsNullOrEmpty(s) &&
                (s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase));

            if (target.IsEnum)
            {
                try { return Enum.Parse(target, s, ignoreCase: true); }
                catch { return Activator.CreateInstance(target); }
            }

            // Nullable<T>
            var u = Nullable.GetUnderlyingType(target);
            if (u != null)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                return ConvertFromString(s, u);
            }

            // 기타 구조체/복합타입은 JSON으로 시도 (["..."], {"...":...} 형태)
            if (!string.IsNullOrWhiteSpace(s) && (s.StartsWith("{") || s.StartsWith("[")))
            {
                try
                {
                    // UnityEngine.JsonUtility는 루트 배열 미지원 → 래핑
                    if (s.StartsWith("["))
                    {
                        var wrapperType = typeof(Wrapper<>).MakeGenericType(target);
                        var json = "{\"Items\":" + s + "}";
                        var wrapper = JsonUtility.FromJson(json, wrapperType);
                        var itemsProp = wrapperType.GetField("Items");
                        return itemsProp?.GetValue(wrapper);
                    }
                    return JsonUtility.FromJson(s, target);
                }
                catch { /* ignore */ }
            }

            return target.IsValueType ? Activator.CreateInstance(target) : null;
        }

        [Serializable] private class Wrapper<T> { public T Items; }

#if UNITY_EDITOR
        // 에디터에서 Config 자동 탐색용
        private static SD.DataTable.DataTableConfig FindBestConfigAsset()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:DataTableConfig");
            var assets = guids
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<SD.DataTable.DataTableConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();

            if (assets.Count == 0) return null;
            return assets.OrderByDescending(a => a.Tables?.Count ?? 0).First();
        }
#endif
    }
}