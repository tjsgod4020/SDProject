using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SD.Gameplay.Cards.Domain;
using SD.Gameplay.Cards.Domain.Effects;           // EffectJsonParser / EffectSpec
using SD.Gameplay.Cards.Domain.Localization;      // CardNameRow / CardDescRow

namespace SD.Gameplay.Cards.Infrastructure
{
    /// <summary>
    /// CSV 로우(IList)들을 묶어 CardDefinition 리스트를 생성.
    /// - 필드 타입(string/enum/flags/int)을 리플렉션으로 관대하게 수용
    /// - Effects: EffectModel의 EffectJsonParser.Parse만 사용 (Damage만 허용)
    /// - 파싱 실패는 Unknown/기본값으로 안전 귀결 + 경고 로그
    /// </summary>
    public static class CardFactory
    {
        public static List<CardDefinition> BuildAll(
            IList cardDataRows,   // List<CardDataModel> as IList
            IList nameRows,       // List<CardNameRow>  as IList
            IList descRows,       // List<CardDescRow>  as IList
            string locale = null  // "ko"|"en" (null이면 시스템 언어)
        )
        {
            var result = new List<CardDefinition>();
            if (cardDataRows == null) return result;

            // 로컬라이즈 맵
            var nameMap = ToTextMap(nameRows);
            var descMap = ToTextMap(descRows);
            var lang = NormalizeLocale(locale);

            foreach (var row in cardDataRows.Cast<object>())
            {
                try
                {
                    var enabled = GetBool(row, true, "Enabled", "IsEnabled", "Enable");
                    if (!enabled) continue;

                    var id = GetString(row, "Id");
                    var nameId = GetString(row, "NameId");
                    var descId = GetString(row, "DescId");

                    var def = new CardDefinition
                    {
                        Id = id,
                        Enabled = enabled,

                        NameId = nameId,
                        DescId = descId,
                        DisplayName = ResolveText(nameMap, nameId, lang),
                        DisplayDesc = ResolveText(descMap, descId, lang),

                        Type = GetEnum<CardType>(row, "Type", CardType.Unknown),
                        Class = GetEnum<CardClass>(row, "Class", CardClass.Unknown, normalize: NormalizeClass),
                        Rarity = GetEnum<CardRarity>(row, "Rarity", CardRarity.Unknown),

                        CharId = GetString(row, "CharId"),
                        Cost = GetInt(row, "Cost", 0),

                        TargetType = GetEnum<TargetType>(row, "TargetType", TargetType.Unknown),

                        PosUse = GetFlags<PositionUseFlags>(row, "PosUse", ParseUseFlags),
                        PosHit = GetFlags<PositionHitFlags>(row, "PosHit", ParseHitFlags),

                        // 이펙트: 최소 구현(Damage만) - 여러 후보 필드명 지원
                        Effects = EffectJsonParser.Parse(
                            FirstNonEmpty(
                                GetString(row, "EffectsJSON"),
                                GetString(row, "EffectsJson"),
                                GetString(row, "EffectsText"),
                                GetString(row, "Effects")
                            ) ?? string.Empty
                        ),

                        Upgradable = GetBool(row, true, "Enabled", "IsEnabled", "Enable"),
                        UpgradeStep = GetInt(row, "UpgradeStep", 0),
                        UpgradeRefId = GetString(row, "UpgradeRefId"),

                        Tags = GetTags(row, "Tags"),
                    };

                    result.Add(def);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CardFactory] Skip row due to error: {ex.Message}");
                }
            }

            return result;
        }

        // -------------------- Reflection helpers --------------------
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
            var v = GetRaw(obj, names);
            if (v == null) return string.Empty;
            if (v is string s) return s;
            return v.ToString();
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

            // 이미 enum
            if (v is TEnum e) return e;

            // 숫자 기반
            if (v is IConvertible)
            {
                try
                {
                    var i = Convert.ToInt32(v);
                    if (Enum.IsDefined(typeof(TEnum), i))
                        return (TEnum)Enum.ToObject(typeof(TEnum), i);
                }
                catch { /* ignore */ }
            }

            // 문자열
            var s = v.ToString();
            if (string.IsNullOrWhiteSpace(s)) return def;
            if (normalize != null) s = normalize(s);
            return Enum.TryParse<TEnum>(s, true, out var parsed) ? parsed : def;
        }

        static TFlags GetFlags<TFlags>(object obj, string name, Func<string, TFlags> parseFromString) where TFlags : struct
        {
            var v = GetRaw(obj, name);
            if (v == null) return parseFromString(string.Empty);

            if (v is TFlags en) return en; // 이미 Flags enum

            if (v is IConvertible)
            {
                try { return (TFlags)Enum.ToObject(typeof(TFlags), Convert.ToInt32(v)); }
                catch { /* ignore */ }
            }

            return parseFromString(v.ToString()); // 문자열 파싱
        }

        static string FirstNonEmpty(params string[] ss)
        {
            foreach (var s in ss) if (!string.IsNullOrWhiteSpace(s)) return s;
            return null;
        }

        // -------------------- Domain parsers --------------------
        static string NormalizeClass(string s)
        {
            // 과거 표기 보정: Mythic → Myth
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

            // 문자열 → 구분자 파싱
            if (v is string s)
                return SplitTokens(s).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Flags enum → 켜진 비트 라벨 추출
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

        // -------------------- Localization helpers --------------------
        static Dictionary<string, (string ko, string en)> ToTextMap(IList rows)
        {
            var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            if (rows == null) return map;

            foreach (var any in rows)
            {
                switch (any)
                {
                    case CardNameRow nr: map[nr.Id] = (nr.Ko ?? "", nr.En ?? ""); break;
                    case CardDescRow dr: map[dr.Id] = (dr.Ko ?? "", dr.En ?? ""); break;
                    default:
                        Debug.LogWarning("[CardFactory] Unknown text row type: " + any?.GetType().Name);
                        break;
                }
            }
            return map;
        }

        static string NormalizeLocale(string loc)
        {
            if (string.IsNullOrEmpty(loc))
                return (Application.systemLanguage == SystemLanguage.Korean) ? "ko" : "en";

            loc = loc.ToLowerInvariant();
            if (loc.StartsWith("ko")) return "ko";
            return "en";
        }

        static string ResolveText(Dictionary<string, (string ko, string en)> map, string id, string lang)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            if (!map.TryGetValue(id, out var t)) return id; // 키 없으면 키 자체 반환(디버그에 유용)
            return lang == "ko" ? t.ko : t.en;
        }

        // -------------------- Shared tokenization --------------------
        static IEnumerable<string> SplitTokens(string s)
            => s.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());
    }
}