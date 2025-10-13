using UnityEngine;
using System.Reflection;
using SDProject.Combat;            // HandRuntime
using SDProject.Data;              // CardData
using System;

namespace SDProject.Combat.Cards.Bridge
{
    /// <summary>
    /// Bridges HandRuntime.OnUsed(CardData) to CardPlayController via CardLibrary(Id->SO).
    /// Uses reflection to resolve CardData Id safely (v1). Replace with direct field when known.
    /// </summary>
    [DisallowMultipleComponent]
    public class HandCardPlayAdapter : MonoBehaviour
    {
        [SerializeField] private HandRuntime _hand;
        [SerializeField] private CardPlayController _controller;
        [SerializeField] private CardLibrary _library;
        [SerializeField] private GameObject _defaultCaster;

        private void Awake()
        {
            // Use Unity's official API directly (no helper to avoid CS0108)
            if (!_hand) _hand = UnityEngine.Object.FindFirstObjectByType<HandRuntime>(FindObjectsInactive.Include);
            if (_hand != null) _hand.OnUsed += OnUsed;
        }

        private void OnDestroy()
        {
            if (_hand != null) _hand.OnUsed -= OnUsed;
        }

        private void OnUsed(CardData c)
        {
            if (c == null || _controller == null || _library == null)
            {
                Debug.LogWarning("[HandBridge] Missing refs.");
                return;
            }

            string id = ResolveCardId(c);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[HandBridge] Cannot resolve CardId from CardData. Add mapping.");
                return;
            }

            if (!_library.TryGet(id, out var def))
            {
                Debug.LogWarning($"[HandBridge] No CardDefinition for Id={id}");
                return;
            }

            var caster = _defaultCaster;
            if (!caster)
            {
                var br = UnityEngine.Object.FindFirstObjectByType<BoardRuntime>(FindObjectsInactive.Include);
                if (br != null)
                {
                    // Pick first alive ally as a caster
                    for (int i = 0; i < br.AllySlots.Count; i++)
                    {
                        var u = br.GetOccupant(SDProject.Combat.Board.TeamSide.Ally, i);
                        if (!u) continue;
                        var hp = u.GetComponent<IDamageable>();
                        if (hp == null || !hp.IsAlive()) continue;
                        caster = u;
                        break;
                    }
                }
            }

            if (!caster)
            {
                Debug.LogWarning("[HandBridge] No caster found.");
                return;
            }

            _controller.PlayCard(def, caster);
        }

        // Reflection-based Id resolver (temporary until the exact field name is fixed)
        private static readonly string[] _idCandidates =
            { "Id", "ID", "CardId", "cardId", "Key", "key", "Name", "name" };

        private string ResolveCardId(CardData data)
        {
            var t = data.GetType();
            foreach (var n in _idCandidates)
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.PropertyType == typeof(string))
                {
                    var v = p.GetValue(data) as string;
                    if (!string.IsNullOrEmpty(v)) return v;
                }

                var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance);
                if (f != null && f.FieldType == typeof(string))
                {
                    var v = f.GetValue(data) as string;
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            return null;
        }
    }
}
