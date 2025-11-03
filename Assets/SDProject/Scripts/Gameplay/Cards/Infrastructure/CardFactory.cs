using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SD.Gameplay.Cards.Domain;
using SD.Gameplay.Cards.Domain.Effects;

namespace SD.Gameplay.Cards.Infrastructure
{
    public static class CardFactory
    {
        public static List<CardDefinition> BuildAll(
            IList cardDataRows,
            IList nameRows,
            IList descRows,
            string locale = null
        )
        {
            var result = new List<CardDefinition>();
            if (cardDataRows == null) return result;

            var nameMap = ToTextMap(nameRows, "Name");
            var descMap = ToTextMap(descRows, "Desc");
            var lang = NormalizeLocale(locale);

            foreach (var row in cardDataRows.Cast<object>())
            {
                try
                {
                    var enabled = GetBool(row, true, "Enabled", "IsEnabled", "Enable");
                    if (!enabled) continue;

                    var id = GetString(row, "Id");
                    var nameId = GetString(row, "NameId", "NameID", "NameKey");
                    var descId = GetString(row, "DescId", "DescID", "DescKey", "DescriptionId");

                    var def = new CardDefinition
                    {
                        Id = id,
                        Enabled = enabled,

                        NameId = nameId,
                        DescId = descId,
                        // ★ 여기서 nameId/descId가 실패하면 Id로도 찾는다.
                        DisplayName = ResolveTextMulti(nameMap, lang, nameId, id),
                        DisplayDesc = ResolveTextMulti(descMap, lang, descId, id),

                        Type = GetEnum<CardType>(row, "Type", CardType.Unknown),
                        Class = GetEnum<CardClass>(row, "Class", CardClass.Unknown, normalize: NormalizeClass),
                        Rarity = GetEnum<CardRarity>(row, "Rarity", CardRarity.Unknown),

                        CharId = GetString(row, "CharId", "CharacterId"),
                        Cost = GetInt(row, "Cost", 0),

                        TargetType = GetEnum<TargetType>(row, "TargetType", TargetType.Unknown),

                        PosUse = GetFlags<PositionUseFlags>(row, "PosUse", ParseUseFlags),
                        PosHit = GetFlags<PositionHitFlags>(row, "PosHit", ParseHitFlags),

                        Effects = EffectJsonParser.Parse(
                            FirstNonEmpty(
                                GetString(row, "EffectsJSON"),
                                GetString(row, "EffectsJson"),
                                GetString(row, "EffectsText"),
                                GetString(row, "Effects")
                            ) ?? string.Empty
                        ),

                        Upgradable = GetBool(row, true, "Upgradable", "Upgradeable", "IsUpgradable"),
                        UpgradeStep = GetInt(row, "UpgradeStep", 0),
                        UpgradeRefId = GetString(row, "UpgradeRefId", "UpgradeTo"),

                        Tags = GetTags(row, "Tags"),
                    };

                    if (string.IsNullOrWhiteSpace(def.DisplayName))
                        def.DisplayName = def.NameId?.Length > 0 ? def.NameId : def.Id; // 최종 안전장치

                    result.Add(def);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CardFactory] Skip row due to error: {ex.Message}");
                }
            }

            return result;
        }

        // ---------- Reflection helpers ----------
        static readonly BindingFlags _bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;

        static MemberInfo FindMember(object obj, params string[] names)
        {
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n, _bf);
                if (p != null) return p;
                var f = t.GetField(n, _bf);
                if (f != null) return f;
            }
            return null;
        }

        static object GetRaw(object obj, params string[] names)
        {
            var m = FindMember(obj, names);
            if (m == null) return null;
            return m is PropertyInfo pi ? pi.GetValue(obj) : ((FieldInfo)m).GetValue(obj);
        }

        static string GetString(object obj, params string[] names)
        {
            foreach (var n in names)
            {
                var v = GetRaw(obj, n);
                if (v == null) continue;
                if (v is string s) return s;
                return v.ToString();
            }
            return string.Empty;
        }

        static int GetInt(object obj, string name, int def = 0)
        {
            var v = GetRaw(obj, name);
            if (v == null) return def;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var p)) return p;
            return def;
        }

        static bool GetBool(object obj, bool def, params string[] names)
        {
            foreach (var name in names)
            {
                var v = GetRaw(obj, name);
                if (v == null) continue;
                if (v is bool b) return b;

                var s = v.ToString();
                if (bool.TryParse(s, out var pb)) return pb;
                if (int.TryParse(s, out var pi)) return pi != 0;
                if (s.Equals("yes", StringComparison.OrdinalIgnoreCase)) return true;
                if (s.Equals("no", StringComparison.OrdinalIgnoreCase)) return false;
            }
            return def;
        }

        static TEnum GetEnum<TEnum>(object obj, string name, TEnum def, Func<string, string> normalize = null) where TEnum : struct
        {
            var v = GetRaw(obj, name);
            if (v == null) return def;

            if (v is TEnum e) return e;

            if (v is IConvertible)
            {
                try
                {
                    var i = Convert.ToInt32(v);
                    if (Enum.IsDefined(typeof(TEnum), i))
                        return (TEnum)Enum.ToObject(typeof(TEnum), i);
                }
                catch { }
            }

            var s = v.ToString();
            if (string.IsNullOrWhiteSpace(s)) return def;
            if (normalize != null) s = normalize(s);
            return Enum.TryParse<TEnum>(s, true, out var parsed) ? parsed : def;
        }

        static TFlags GetFlags<TFlags>(object obj, string name, Func<string, TFlags> parseFromString) where TFlags : struct
        {
            var v = GetRaw(obj, name);
            if (v == null) return parseFromString(string.Empty);

            if (v is TFlags en) return en;

            if (v is IConvertible)
            {
                try { return (TFlags)Enum.ToObject(typeof(TFlags), Convert.ToInt32(v)); }
                catch { }
            }

            return parseFromString(v.ToString());
        }

        static string FirstNonEmpty(params string[] ss)
        {
            foreach (var s in ss) if (!string.IsNullOrWhiteSpace(s)) return s;
            return null;
        }

        // ---------- Domain parsers ----------
        static string NormalizeClass(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            return s.Equals("Mythic", StringComparison.OrdinalIgnoreCase) ? "Myth" : s;
        }

        static PositionUseFlags ParseUseFlags(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return PositionUseFlags.Front | PositionUseFlags.Mid1 | PositionUseFlags.Mid2 | PositionUseFlags.Back;

            PositionUseFlags acc = 0;
            foreach (var t in SplitTokens(csv))
            {
                if (t.Equals("Front", StringComparison.OrdinalIgnoreCase)) acc |= PositionUseFlags.Front;
                else if (t.Equals("Mid1", StringComparison.OrdinalIgnoreCase)) acc |= PositionUseFlags.Mid1;
                else if (t.Equals("Mid2", StringComparison.OrdinalIgnoreCase)) acc |= PositionUseFlags.Mid2;
                else if (t.Equals("Back", StringComparison.OrdinalIgnoreCase)) acc |= PositionUseFlags.Back;
            }
            return acc == 0 ? (PositionUseFlags.Front | PositionUseFlags.Mid1 | PositionUseFlags.Mid2 | PositionUseFlags.Back) : acc;
        }

        static PositionHitFlags ParseHitFlags(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return PositionHitFlags.Front | PositionHitFlags.Mid1 | PositionHitFlags.Mid2 | PositionHitFlags.Mid3 | PositionHitFlags.Back;

            PositionHitFlags acc = 0;
            foreach (var t in SplitTokens(csv))
            {
                if (t.Equals("Front", StringComparison.OrdinalIgnoreCase)) acc |= PositionHitFlags.Front;
                else if (t.Equals("Mid1", StringComparison.OrdinalIgnoreCase)) acc |= PositionHitFlags.Mid1;
                else if (t.Equals("Mid2", StringComparison.OrdinalIgnoreCase)) acc |= PositionHitFlags.Mid2;
                else if (t.Equals("Mid3", StringComparison.OrdinalIgnoreCase)) acc |= PositionHitFlags.Mid3;
                else if (t.Equals("Back", StringComparison.OrdinalIgnoreCase)) acc |= PositionHitFlags.Back;
            }
            return acc == 0 ? (PositionHitFlags.Front | PositionHitFlags.Mid1 | PositionHitFlags.Mid2 | PositionHitFlags.Mid3 | PositionHitFlags.Back) : acc;
        }

        static List<string> GetTags(object obj, string name)
        {
            var v = GetRaw(obj, name);
            if (v == null) return new List<string>();

            if (v is string s)
                return SplitTokens(s).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var t = v.GetType();
            if (t.IsEnum)
            {
                var val = Convert.ToInt64(v);
                if (val == 0) return new List<string>();
                var labels = Enum.GetValues(t).Cast<object>()
                    .Select(x => new { x, i = Convert.ToInt64(x) })
                    .Where(e => e.i != 0 && (val & e.i) == e.i)
                    .Select(e => e.x.ToString())
                    .Distinct()
                    .ToList();
                return labels;
            }

            return new List<string> { v.ToString() };
        }

        // ---------- Localization ----------
        static readonly string[] KoCandidates = { "Ko", "KO", "ko" };
        static readonly string[] EnCandidates = { "En", "EN", "en" };
        static readonly string[] IdCandidates = { "Id", "ID", "id", "Key" };

        static readonly string[] Prefixes = {
            "CardData_", "CardName_", "CardDesc_",
            "Name_", "Desc_", "Card_", "Data_"
        };

        static string Canon(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            foreach (var p in Prefixes)
                if (s.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    s = s.Substring(p.Length);

            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            return sb.ToString();
        }

        static Dictionary<string, (string ko, string en)> ToTextMap(IList rows, string label)
        {
            var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            if (rows == null) return map;

            int added = 0;
            foreach (var any in rows)
            {
                if (any == null) continue;

                var rawId = GetString(any, IdCandidates);
                if (string.IsNullOrWhiteSpace(rawId)) continue;

                var ko = GetString(any, KoCandidates);
                var en = GetString(any, EnCandidates);
                if (string.IsNullOrWhiteSpace(ko) && string.IsNullOrWhiteSpace(en))
                    continue;

                map[rawId] = (ko ?? "", en ?? "");
                added++;

                var c = Canon(rawId);
                if (!string.IsNullOrEmpty(c) && !map.ContainsKey(c))
                    map[c] = (ko ?? "", en ?? "");
            }

            Debug.Log($"[CardFactory] ToTextMap({label}) built: {added} entries");
            return map;
        }

        // 여러 후보 키(예: NameId, Id)를 받아 순차적으로 매칭
        static string ResolveTextMulti(Dictionary<string, (string ko, string en)> map, string lang, params string[] keys)
        {
            foreach (var key in keys)
            {
                var v = ResolveTextSingle(map, key, lang);
                if (!string.IsNullOrEmpty(v) && !v.Equals(key ?? "", StringComparison.Ordinal)) // 실제 텍스트를 찾은 경우
                    return v;
            }
            // 전부 실패 → 마지막 키(대개 NameId 또는 Id)를 그대로
            var last = keys.LastOrDefault() ?? string.Empty;
            return last;
        }

        // 단일 키에 대해 원본/Trim/Canon/접두사제거/Canon을 모두 시도
        static string ResolveTextSingle(Dictionary<string, (string ko, string en)> map, string key, string lang)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            var candidates = new List<string>(8) { key, key.Trim() };
            var c = Canon(key);
            if (!string.IsNullOrEmpty(c)) candidates.Add(c);

            foreach (var p in Prefixes)
            {
                if (key.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                {
                    var trimmed = key.Substring(p.Length).Trim();
                    if (!string.IsNullOrEmpty(trimmed)) candidates.Add(trimmed);

                    var c2 = Canon(trimmed);
                    if (!string.IsNullOrEmpty(c2)) candidates.Add(c2);
                }
            }

            foreach (var cand in candidates.Distinct())
            {
                if (map.TryGetValue(cand, out var t))
                {
                    if (lang == "ko")
                        return string.IsNullOrWhiteSpace(t.ko)
                            ? (string.IsNullOrWhiteSpace(t.en) ? key : t.en)
                            : t.ko;
                    else
                        return string.IsNullOrWhiteSpace(t.en)
                            ? (string.IsNullOrWhiteSpace(t.ko) ? key : t.ko)
                            : t.en;
                }
            }
            return key; // 못 찾으면 키 반환
        }

        static string NormalizeLocale(string loc)
        {
            if (string.IsNullOrEmpty(loc))
                return (Application.systemLanguage == SystemLanguage.Korean) ? "ko" : "en";
            loc = loc.ToLowerInvariant();
            return loc.StartsWith("ko") ? "ko" : "en";
        }

        static IEnumerable<string> SplitTokens(string s)
            => s.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());
    }
}