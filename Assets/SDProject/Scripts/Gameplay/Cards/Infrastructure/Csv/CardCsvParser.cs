using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SD.Gameplay.Cards.Domain;
using UnityEngine;

namespace SD.Gameplay.Cards.Infrastructure.Csv
{
    internal static class CardCsvParser
    {
        public static List<CardDataModel> Parse(TextAsset csv)
        {
            using var sr = new StringReader(csv.text);
            var rows = CsvReader.ReadAll(sr);
            if (rows.Count == 0) return new();

            var header = rows[0].Select(h => h.Trim()).ToArray();
            int Index(string name) => Array.FindIndex(header, h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

            int idxId = Index("Id");
            int idxEnabled = Index("Enabled");
            int idxNameId = Index("NameId");
            int idxDescId = Index("DescId");
            int idxType = Index("Type");
            int idxClass = Index("Class");
            int idxRarity = Index("Rarity");
            int idxCharId = Index("CharId");
            int idxCost = Index("Cost");
            int idxTargetType = Index("TargetType");
            int idxPosUse = Index("PosUse");
            int idxPosHit = Index("PosHit");
            int idxEffects = Index("EffectsJSON");   // 문자열 JSON 컬럼
            int idxUpgradable = Index("Upgradable");
            int idxUpgradeStep = Index("UpgradeStep");
            int idxUpgradeRef = Index("UpgradeRefId");
            int idxTags = Index("Tags");

            // 최소 필수 컬럼
            var required = new[] { idxId, idxEnabled, idxType, idxClass, idxCost, idxTargetType };
            if (required.Any(i => i < 0))
                throw new InvalidDataException("CardData.csv 필수 헤더 누락(Id, Enabled, Type, Class, Cost, TargetType)");

            var list = new List<CardDataModel>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 1; r < rows.Count; r++)
            {
                var line = rows[r];
                string Get(int i) => (i >= 0 && i < line.Length) ? line[i]?.Trim() ?? "" : "";

                try
                {
                    var id = Get(idxId);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        Debug.LogWarning($"[CardCsvParser] 행 {r + 1}: 빈 Id → 스킵");
                        continue;
                    }
                    if (!ids.Add(id))
                    {
                        Debug.LogWarning($"[CardCsvParser] 행 {r + 1}: 중복 Id: {id} → 스킵");
                        continue;
                    }

                    bool enabled = ParseBool(Get(idxEnabled));
                    if (!enabled) continue; // 비활성은 스킵

                    var nameId = Get(idxNameId);
                    var descId = Get(idxDescId);
                    var type = ParseEnum<CardType>(Get(idxType));
                    var @class = ParseEnum<CardClass>(Get(idxClass));
                    var rarity = ParseEnumSafe<CardRarity>(Get(idxRarity), CardRarity.Common);
                    var charId = string.IsNullOrEmpty(Get(idxCharId)) ? "Public" : Get(idxCharId);
                    int cost = ParseInt(Get(idxCost), 0);
                    var target = ParseEnum<TargetType>(Get(idxTargetType));
                    var posUse = ParseFlags<PositionUseFlags>(Get(idxPosUse));
                    var posHit = ParseFlags<PositionHitFlags>(Get(idxPosHit));
                    var effectsJson = Get(idxEffects); // 문자열 그대로 저장
                    bool upgradable = ParseBool(Get(idxUpgradable));
                    int upgradeStep = ParseInt(Get(idxUpgradeStep), 0);
                    var upgradeRefId = Get(idxUpgradeRef);
                    var tags = ParseFlags<CardTagFlags>(Get(idxTags));

                    // --- 생성자 사용 금지: 기본 생성 + 멤버 세팅(유연) ---
                    var model = Activator.CreateInstance<CardDataModel>();

                    // 문자열/스칼라
                    SetMember(model, "Id", id);
                    SetMember(model, "Enabled", enabled);
                    SetMember(model, "NameId", nameId);
                    SetMember(model, "DescId", descId);
                    SetMember(model, "Class", @class);
                    SetMember(model, "Type", type);
                    SetMember(model, "Rarity", rarity);
                    SetMember(model, "CharId", charId);
                    SetMember(model, "Cost", cost);
                    SetMember(model, "TargetType", target);
                    SetMember(model, "PosUse", posUse);
                    SetMember(model, "PosHit", posHit);
                    SetMember(model, "Upgradable", upgradable);
                    SetMember(model, "UpgradeStep", upgradeStep);
                    SetMember(model, "UpgradeRefId", upgradeRefId);
                    SetMember(model, "Tags", tags);

                    // 이펙트: 우선 문자열 JSON 컬럼 우선 (CardFactory에서 파싱)
                    if (!string.IsNullOrWhiteSpace(effectsJson))
                    {
                        // 모델에 EffectsJSON(string)이 있으면 그쪽으로 저장
                        if (!SetMember(model, "EffectsJSON", effectsJson))
                        {
                            // (옵션) 모델에 Effects(List<EffectSpec>)가 있다면 주석 해제하여 즉시 파싱 저장
                            // try
                            // {
                            //     var specs = SD.Gameplay.Cards.Domain.Effects.EffectJsonParser.Parse(effectsJson);
                            //     SetMember(model, "Effects", specs);
                            // }
                            // catch { /* 파싱 실패 무시, 팩토리에서 다시 시도 */ }
                        }
                    }

                    list.Add(model);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CardCsvParser] 행 {r + 1} 파싱 실패: {ex.Message}");
                }
            }

            Debug.Log($"[CardCsvParser] 로드 완료: {list.Count}개");
            return list;
        }

        // ----------------- 파싱 유틸 -----------------
        static bool ParseBool(string s) =>
            !string.IsNullOrEmpty(s) && (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1" || s.Equals("yes", StringComparison.OrdinalIgnoreCase));

        static int ParseInt(string s, int def) => int.TryParse(s, out var v) ? v : def;

        static T ParseEnum<T>(string s) where T : struct =>
            Enum.TryParse<T>(s, true, out var v) ? v : throw new InvalidDataException($"Enum 파싱 실패: {typeof(T).Name}='{s}'");

        static T ParseEnumSafe<T>(string s, T def) where T : struct =>
            Enum.TryParse<T>(s, true, out var v) ? v : def;

        static U ParseFlags<U>(string s) where U : struct
        {
            if (string.IsNullOrWhiteSpace(s)) return default;
            ulong acc = 0;
            foreach (var token in s.Split('|'))
            {
                var t = token.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                if (!Enum.TryParse<U>(t, true, out var part))
                    throw new InvalidDataException($"Flags 파싱 실패: {typeof(U).Name}='{t}'");
                acc |= Convert.ToUInt64(part);
            }
            return (U)Enum.ToObject(typeof(U), acc);
        }

        // ----------------- 리플렉션 세터 -----------------
        static readonly BindingFlags _bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;

        /// <summary>
        /// model의 프로퍼티/필드 중 name과 일치하는 멤버를 찾아 값을 대입. 없으면 false.
        /// enum/기본형/참조형을 모두 처리(타입 호환 시).
        /// </summary>
        static bool SetMember(object model, string name, object value)
        {
            var t = model.GetType();

            // 프로퍼티 우선
            var p = t.GetProperty(name, _bf);
            if (p != null && p.CanWrite)
            {
                var ok = TryConvert(value, p.PropertyType, out var boxed);
                if (ok)
                {
                    try { p.SetValue(model, boxed); return true; } catch { }
                }
            }

            // 필드
            var f = t.GetField(name, _bf);
            if (f != null)
            {
                var ok = TryConvert(value, f.FieldType, out var boxed);
                if (ok)
                {
                    try { f.SetValue(model, boxed); return true; } catch { }
                }
            }

            return false;
        }

        static bool TryConvert(object src, Type dst, out object boxed)
        {
            boxed = null;
            if (src == null)
            {
                if (!dst.IsValueType || Nullable.GetUnderlyingType(dst) != null) { boxed = null; return true; }
                return false;
            }

            var sType = src.GetType();
            if (dst.IsAssignableFrom(sType)) { boxed = src; return true; }

            // enum ← 숫자/문자열
            if (dst.IsEnum)
            {
                try
                {
                    if (src is string ss && Enum.TryParse(dst, ss, true, out var e1)) { boxed = e1; return true; }
                    var i = Convert.ToInt64(src);
                    boxed = Enum.ToObject(dst, i);
                    return true;
                }
                catch { return false; }
            }

            // Nullable<T>
            var u = Nullable.GetUnderlyingType(dst);
            if (u != null)
            {
                if (src is string str && string.IsNullOrWhiteSpace(str)) { boxed = null; return true; }
                return TryConvert(src, u, out boxed);
            }

            try
            {
                // 숫자/기본형 변환 시도
                boxed = Convert.ChangeType(src, dst);
                return true;
            }
            catch
            {
                // List<T> 등은 컬렉션 그대로 허용 (타입 호환 시)
                if (dst.IsGenericType && src is System.Collections.IEnumerable e &&
                    typeof(System.Collections.IEnumerable).IsAssignableFrom(dst))
                {
                    boxed = src; return true;
                }
                return false;
            }
        }
    }
}