using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataTable
{
    /// <summary>
    /// CardName.csv / CardDesc.csv 를 읽어 Id->문자열 사전으로 보관.
    /// v1: Ko 우선, 없으면 En -> (name은 Id, desc는 "")
    /// </summary>
    public static class TextTableLoader
    {
        private const string Folder = "DataTableGen";
        private const string NameFile = "CardName";
        private const string DescFile = "CardDesc";

        private static Dictionary<string, string> _nameKo;
        private static Dictionary<string, string> _nameEn;
        private static Dictionary<string, string> _descKo;
        private static Dictionary<string, string> _descEn;
        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;

            _nameKo = LoadLang($"{Folder}/{NameFile}", "Ko");
            _nameEn = LoadLang($"{Folder}/{NameFile}", "En");
            _descKo = LoadLang($"{Folder}/{DescFile}", "Ko");
            _descEn = LoadLang($"{Folder}/{DescFile}", "En");

            Debug.Log($"[TextTableLoader] Names(Ko={_nameKo.Count}, En={_nameEn.Count}) / Desc(Ko={_descKo.Count}, En={_descEn.Count})");
            _loaded = true;
        }

        public static string ResolveName(string nameId)
        {
            if (string.IsNullOrWhiteSpace(nameId)) return string.Empty;
            EnsureLoaded();

            if (_nameKo.TryGetValue(nameId, out var ko) && !string.IsNullOrWhiteSpace(ko)) return ko;
            if (_nameEn.TryGetValue(nameId, out var en) && !string.IsNullOrWhiteSpace(en)) return en;
            return nameId; // fallback
        }

        public static string ResolveDesc(string descId)
        {
            if (string.IsNullOrWhiteSpace(descId)) return string.Empty;
            EnsureLoaded();

            if (_descKo.TryGetValue(descId, out var ko) && !string.IsNullOrWhiteSpace(ko)) return ko;
            if (_descEn.TryGetValue(descId, out var en) && !string.IsNullOrWhiteSpace(en)) return en;
            return string.Empty; // fallback
        }

        // --- internals ---

        private static Dictionary<string, string> LoadLang(string resPathNoExt, string colLang)
        {
            var dict = new Dictionary<string, string>();
            var ta = Resources.Load<TextAsset>(resPathNoExt);
            if (ta == null || string.IsNullOrEmpty(ta.text))
            {
                Debug.LogWarning($"[TextTableLoader] Missing or empty: {resPathNoExt}.csv");
                return dict;
            }

            var rows = CsvUtil.Parse(ta.text); // list of row-dicts
            int ok = 0;
            foreach (var row in rows)
            {
                if (!row.TryGet("Id", out var id) || string.IsNullOrWhiteSpace(id)) continue;
                if (!row.TryGet(colLang, out var text)) text = string.Empty;
                dict[id.Trim()] = text ?? string.Empty;
                ok++;
            }
            Debug.Log($"[TextTableLoader] Loaded {resPathNoExt}.csv ({colLang}) rows={ok}");
            return dict;
        }
    }
}
