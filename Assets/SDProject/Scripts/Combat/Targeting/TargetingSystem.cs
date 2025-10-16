// Assets/SDProject/Scripts/Combat/Targeting/TargetingSystem.cs
using SDProject.Combat.Board;
using SDProject.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Combat
{
    /// <summary>
    /// Manual selection flow; filters by team and PosHit lane mask.
    /// </summary>
    public sealed class TargetingSystem : MonoBehaviour
    {
        [SerializeField] private BoardRuntime _board;

        private PositionFlags _mask;
        private TeamSide _team;
        private int _needCount;
        private Action<IReadOnlyList<GameObject>> _onDone;
        private Action _onCancel;
        private readonly List<GameObject> _picked = new();

        private bool _active;

        public void BeginManualSelect(CardData card, PositionFlags mask, TeamSide team, int needCount,
                                      Action<IReadOnlyList<GameObject>> onDone,
                                      Action onCancel)
        {
            _active = true;
            _mask = mask;
            _team = team;
            _needCount = Mathf.Max(1, needCount);
            _onDone = onDone;
            _onCancel = onCancel;
            _picked.Clear();

            // TODO: highlight allowed units (optional visual layer)
            Debug.Log($"[Targeting] Begin manual: team={team}, need={_needCount}, mask={mask}");
        }

        public void ProvideManualSingle(GameObject unitGO)
        {
            if (!_active || unitGO == null) return;

            // Team & lane filter
            var loc = _board.GetUnitLocation(unitGO);
            if (loc == null || loc.Value.team != _team) return;

            var lane = BoardRuntime.LaneOf(loc.Value.team, loc.Value.index);
            if (!BoardRuntime.LaneMatches(lane, _mask)) return;

            if (_picked.Contains(unitGO)) return; // no duplicates
            _picked.Add(unitGO);

            if (_picked.Count >= _needCount)
            {
                var res = new List<GameObject>(_picked);
                End();
                _onDone?.Invoke(res);
            }
        }

        public void Cancel()
        {
            if (!_active) return;
            Debug.Log("[Targeting] Cancel");
            End();
            _onCancel?.Invoke();
        }

        private void End()
        {
            _active = false;
            _picked.Clear();
            // TODO: clear highlights (optional)
        }
    }
}
