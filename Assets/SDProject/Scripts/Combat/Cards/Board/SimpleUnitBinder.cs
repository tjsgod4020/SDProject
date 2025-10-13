using UnityEngine;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Registers the unit to BoardRuntime at start.
    /// If index < 0, binds to nearest slot of the configured team.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleUnitBinder : MonoBehaviour
    {
        public SDProject.Combat.Board.TeamSide team;
        public int index = -1; // -1 => infer by nearest slot of the team

        private void Start()
        {
            // Use Unity's official API directly (no helper to avoid CS0108)
            var runtime = UnityEngine.Object.FindFirstObjectByType<BoardRuntime>(FindObjectsInactive.Include);
            if (!runtime)
            {
                Debug.LogWarning("[UnitBinder] BoardRuntime not found.");
                return;
            }

            int resolvedIdx = index;
            if (resolvedIdx < 0)
            {
                var slots = (team == SDProject.Combat.Board.TeamSide.Ally)
                    ? runtime.AllySlots : runtime.EnemySlots;

                float best = float.MaxValue;
                int bestIdx = -1;
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    var p = s.mount ? s.mount.position : s.transform.position;
                    float d = Vector3.SqrMagnitude(transform.position - p);
                    if (d < best) { best = d; bestIdx = i; }
                }
                resolvedIdx = bestIdx;
            }

            var unit = GetComponent<SimpleUnit>() ?? gameObject.AddComponent<SimpleUnit>();
            unit.Team = team;
            unit.Index = resolvedIdx;

            runtime.RegisterUnit(gameObject, team, resolvedIdx);
        }
    }
}