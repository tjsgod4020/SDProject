using System.Collections.Generic;
using UnityEngine;

namespace SD.Gameplay.Cards.Domain.Effects
{
    /// <summary>
    /// 런타임에서 사용되는 표준 이펙트 포맷(정규화 결과).
    /// </summary>
    [System.Serializable]
    public struct EffectSpec
    {
        public string effect;   // "Damage" 만 사용(v1)
        public float value;     // 수치
        public float duration;  // 선택
        public string arg;      // 선택
    }

    // 입력 JSON을 받기 위한 "raw" 구조체 (type/effect 둘 다 허용)
    [System.Serializable]
    internal struct EffectSpecRaw
    {
        public string type;     // 예: "Damage"
        public string effect;   // 예: "Damage"
        public float value;
        public float duration;
        public string arg;

        public EffectSpec Normalize()
        {
            var eff = string.IsNullOrWhiteSpace(effect) ? type : effect;
            return new EffectSpec
            {
                effect = eff,
                value = value,
                duration = duration,
                arg = arg
            };
        }
    }

    // JsonUtility는 루트 배열을 직접 못 읽어서 래핑
    [System.Serializable]
    internal class EffectArrayWrapper { public EffectSpecRaw[] items; }

    public static class EffectJsonParser
    {
        /// <summary>
        /// 최소 동작: [{"type":"Damage","value":1}] 또는 [{"effect":"Damage","value":1}]
        /// - 허용: 배열/단일객체
        /// - "Damage" 외 타입은 무시
        /// - 파싱 실패 시 빈 리스트 반환(에러 대신 경고)
        /// </summary>
        public static List<EffectSpec> Parse(string json)
        {
            var list = new List<EffectSpec>();
            if (string.IsNullOrWhiteSpace(json)) return list;

            try
            {
                var trimmed = json.TrimStart();

                if (trimmed.StartsWith("["))
                {
                    // 배열 → 래핑
                    var wrapped = "{\"items\":" + json + "}";
                    var rawArr = JsonUtility.FromJson<EffectArrayWrapper>(wrapped);
                    if (rawArr?.items != null)
                    {
                        foreach (var r in rawArr.items)
                        {
                            var spec = r.Normalize();
                            if (IsSupported(spec)) list.Add(spec);
                        }
                    }
                }
                else
                {
                    // 단일 객체
                    var raw = JsonUtility.FromJson<EffectSpecRaw>(json);
                    var spec = raw.Normalize();
                    if (IsSupported(spec)) list.Add(spec);
                }
            }
            catch
            {
                Debug.LogWarning($"[Effect] Parse failed: {json}");
            }

            return list;
        }

        // v1에서는 Damage만 허용(나머지는 무시)
        private static bool IsSupported(EffectSpec spec)
            => !string.IsNullOrWhiteSpace(spec.effect) &&
               spec.effect.Equals("Damage", System.StringComparison.OrdinalIgnoreCase);
    }
}
