// Assets/SDProject/Scripts/Combat/Board/BoardRuntime.cs
using SDProject.Combat.Cards;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// Runtime view over CharacterSlot[] + Unit occupancy.
    /// Keeps (team,index)->unit mapping, provides helpers for target queries and knockback.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoardRuntime : MonoBehaviour
    {
        // Slots ordered by index ascending
        private readonly List<CharacterSlot> _allySlots = new();
        private readonly List<CharacterSlot> _enemySlots = new();

        // Occupancy map
        private readonly Dictionary<(TeamSide team, int index), GameObject> _occ = new();

        public IReadOnlyList<CharacterSlot> AllySlots => _allySlots;
        public IReadOnlyList<CharacterSlot> EnemySlots => _enemySlots;

        private void Awake() => BuildFromScene();

        /// <summary>Public endpoint for BoardLayout to trigger after it instantiated slots.</summary>
        public void RefreshFromScene() => BuildFromScene();

        private void BuildFromScene()
        {
            _allySlots.Clear();
            _enemySlots.Clear();
            _occ.Clear();

            var slots = FindObjectsByType<CharacterSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in slots)
            {
                if (s.Team == TeamSide.Ally) _allySlots.Add(s);
                else _enemySlots.Add(s);
            }
            _allySlots.Sort((a, b) => a.Index.CompareTo(b.Index));
            _enemySlots.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        // ------- Register / Query -------
        public void RegisterUnit(GameObject unit, TeamSide team, int index)
        {
            _occ[(team, index)] = unit;

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
            if (_occ.TryGetValue((team, index), out var u) && u == unit)
                _occ.Remove((team, index));
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
                if (kv.Value == unit) return kv.Key;
            return null;
        }

        /// <summary>Helper: first found ally unit (front-most by index ordering).</summary>
        public GameObject GetFirstAllyUnit()
        {
            foreach (var s in _allySlots)
            {
                var u = GetOccupant(TeamSide.Ally, s.Index);
                if (u != null) return u;
            }
            return null;
        }

        /// <summary>Returns first alive enemy by priority (enemy indices ascending == Front°ÊBack).</summary>
        public GameObject GetFrontMostEnemyUnit()
        {
            foreach (var s in _enemySlots)
            {
                var u = GetOccupant(TeamSide.Enemy, s.Index);
                if (u == null) continue;
                var hp = u.GetComponent<IDamageable>();
                if (hp == null || !hp.IsAlive()) continue;
                return u;
            }
            return null;
        }

        // (v1 knockback kept minimal; ignore fail + log)
        public bool TryKnockback(GameObject unit, int cells)
        {
            if (unit == null || cells <= 0) return false;
            var loc = GetUnitLocation(unit);
            if (loc == null) return false;

            var (team, idx) = loc.Value;
            var list = (team == TeamSide.Ally) ? _allySlots : _enemySlots;
            int targetIdx = idx + cells; // back is higher index for both teams

            if (targetIdx < 0 || targetIdx >= list.Count) return false;

            for (int i = idx + 1; i <= targetIdx; i++)
                if (GetOccupant(team, i) != null) return false;

            _occ.Remove((team, idx));
            _occ[(team, targetIdx)] = unit;

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

        // Utility for PosUse/PosHit checks (used by TargetingSystem)
        public static bool LaneMatches(PositionFlags lane, PositionFlags mask) => (mask & lane) != 0;
        public static PositionFlags LaneOf(TeamSide team, int index) => PositionResolver.ToLane(team, index);

    }
}
