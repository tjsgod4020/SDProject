using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Runtime view over CharacterSlot[] + Unit occupancy.
    /// Finds all CharacterSlot in scene and maintains (team,index)->occupant mapping.
    /// Provides FrontMost, PosUse/PosHit checks, and Knockback.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoardRuntime : MonoBehaviour
    {
        // Per team, slots ordered by index ascending
        private readonly List<CharacterSlot> _allySlots = new();
        private readonly List<CharacterSlot> _enemySlots = new();

        // Occupancy: (team, index) -> unit
        private readonly Dictionary<(TeamSide team, int index), GameObject> _occ = new();

        public IReadOnlyList<CharacterSlot> AllySlots => _allySlots;
        public IReadOnlyList<CharacterSlot> EnemySlots => _enemySlots;

        private void Awake()
        {
            BuildFromScene();
        }

        private void BuildFromScene()
        {
            _allySlots.Clear();
            _enemySlots.Clear();
            _occ.Clear();

            var slots = FindObjectsByType<SDProject.Combat.Board.CharacterSlot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var s in slots)
            {
                if (s.team == SDProject.Combat.Board.TeamSide.Ally) _allySlots.Add(s);
                else _enemySlots.Add(s);
            }
            _allySlots.Sort((a, b) => a.index.CompareTo(b.index));
            _enemySlots.Sort((a, b) => a.index.CompareTo(b.index));
        }

        // ==== Register / Query ====

        public void RegisterUnit(GameObject unit, TeamSide team, int index)
        {
            var key = (team, index);
            _occ[key] = unit;

            // Try assign CurrentSlot if the unit has SimpleUnit
            var su = unit.GetComponent<SimpleUnit>();
            if (su != null)
            {
                su.Team = team;
                su.Index = index;
                su.CurrentSlot = GetSlot(team, index);
            }
        }

        public void UnregisterUnit(GameObject unit, TeamSide team, int index)
        {
            var key = (team, index);
            if (_occ.TryGetValue(key, out var u) && u == unit) _occ.Remove(key);
        }

        public CharacterSlot GetSlot(TeamSide team, int index)
        {
            var list = (team == TeamSide.Ally) ? _allySlots : _enemySlots;
            return (index >= 0 && index < list.Count) ? list[index] : null;
        }

        public GameObject GetOccupant(TeamSide team, int index)
        {
            _occ.TryGetValue((team, index), out var u);
            return u;
        }

        public (TeamSide team, int index)? GetUnitLocation(GameObject unit)
        {
            foreach (var kv in _occ)
            {
                if (kv.Value == unit) return kv.Key;
            }
            return null;
        }

        // First alive enemy by designed priority: Front -> Mid1 -> Mid2 -> Mid3 -> Back
        public GameObject GetFrontMostEnemyUnit()
        {
            foreach (var s in _enemySlots)
            {
                var u = GetOccupant(TeamSide.Enemy, s.index);
                if (u == null) continue;
                var hp = u.GetComponent<IDamageable>();
                if (hp == null || !hp.IsAlive()) continue;
                return u;
            }
            return null;
        }

        // ==== Knockback (v1: +cells only, ignore fail + log) ====

        public bool TryKnockback(GameObject unit, int cells)
        {
            if (unit == null || cells <= 0) return false;

            var loc = GetUnitLocation(unit);
            if (loc == null) return false;

            var (team, idx) = loc.Value;
            var list = (team == TeamSide.Ally) ? _allySlots : _enemySlots;
            int targetIdx = idx + cells; // "back" is higher index on both teams by our design

            if (targetIdx < 0 || targetIdx >= list.Count) return false;

            // path clear?
            for (int i = idx + 1; i <= targetIdx; i++)
            {
                if (GetOccupant(team, i) != null) return false;
            }

            // move
            _occ.Remove((team, idx));
            _occ[(team, targetIdx)] = unit;

            // snap unit to new slot mount
            var to = GetSlot(team, targetIdx);
            if (to != null)
            {
                unit.transform.position = (to.mount ? to.mount.position : to.transform.position);
                var su = unit.GetComponent<SimpleUnit>();
                if (su != null)
                {
                    su.Index = targetIdx;
                    su.CurrentSlot = to;
                }
            }
            return true;
        }

        // Utility for PosUse/PosHit checks
        public static bool LaneMatches(PositionFlags lane, PositionFlags mask) => (mask & lane) != 0;
        public static PositionFlags LaneOf(TeamSide team, int index) => PositionResolver.ToLane(team, index);
    }
}
