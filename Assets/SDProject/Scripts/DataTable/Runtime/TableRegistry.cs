using System.Collections;
using System.Collections.Generic;

namespace SD.DataTable
{
    /// <summary>
    /// DataTableLoader가 로드한 테이블(IList)을 Id로 보관/조회하는 전역 레지스트리.
    /// 런타임에서 읽기 전용으로만 사용하세요.
    /// </summary>
    public static class TableRegistry
    {
        private static readonly Dictionary<string, IList> _tables = new Dictionary<string, IList>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>로드 시점에만 사용: 테이블 등록/갱신.</summary>
        public static void Set(string id, IList rows)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            _tables[id] = rows ?? (IList)System.Array.Empty<object>();
        }

        /// <summary>조회: 없으면 null 반환.</summary>
        public static IList Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _tables.TryGetValue(id, out var list) ? list : null;
        }

        /// <summary>조회: 없으면 비어있는 리스트 반환(Null-안전).</summary>
        public static IList GetOrEmpty(string id)
        {
            return Get(id) ?? (IList)System.Array.Empty<object>();
        }

        /// <summary>초기화(필요 시).</summary>
        public static void Clear() => _tables.Clear();
    }
}
