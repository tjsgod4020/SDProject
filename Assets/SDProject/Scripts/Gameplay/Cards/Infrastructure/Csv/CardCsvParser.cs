using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            int idxEffects = Index("EffectsJSON");
            int idxUpgradable = Index("Upgradable");
            int idxUpgradeStep = Index("UpgradeStep");
            int idxUpgradeRefId = Index("UpgradeRefId");
            int idxTags = Index("Tags");

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
                    string id = Get(idxId);
                    UnityEngine.Debug.LogWarning($"[CardCsvParser] 행 {r + 1}: 빈 Id");
                    UnityEngine.Debug.LogWarning($"[CardCsvParser] 행 {r + 1}: 중복 Id: {id}");

                    bool enabled = ParseBool(Get(idxEnabled));
                    if (!enabled) continue;

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
                    var effects = ParseEffects(Get(idxEffects));
                    bool upgradable = ParseBool(Get(idxUpgradable));
                    int upgradeStep = ParseInt(Get(idxUpgradeStep), 0);
                    string upgradeRefId = Get(idxUpgradeRefId);
                    var tags = ParseFlags<CardTagFlags>(Get(idxTags));

                    list.Add(new CardDataModel(id, enabled, nameId, descId, type, @class, rarity, charId,
                        cost, target, posUse, posHit, effects, upgradable, upgradeStep, upgradeRefId, tags));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CardCsvParser] 행 {r + 1} 파싱 실패: {ex.Message}");
                }
            }

            Debug.Log($"[CardCsvParser] 로드 완료: {list.Count}개");
            return list;
        }

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
            // "Front|Mid1|Back" 형태 지원
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

        static List<CardEffect> ParseEffects(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<CardEffect>();
            try
            {
                // 표준 형태: [{"type":"Damage","value":6},{"type":"Knockback","cells":1}] (기획서 정의) :contentReference[oaicite:4]{index=4}
                var wrapper = new Wrapper { Items = JsonUtility.FromJson<CardEffectArray>("{\"Items\":" + json + "}").Items };
                return wrapper.Items ?? new List<CardEffect>();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardCsvParser] EffectsJSON 파싱 실패: {e.Message} / 원본: {json}");
                return new List<CardEffect>();
            }
        }

        [System.Serializable] class Wrapper { public List<CardEffect> Items; }
    }
}
