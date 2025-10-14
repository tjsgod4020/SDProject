using System.Collections;
using System.Linq;
using UnityEngine;
using SDProject.Combat.Board; // TeamSide, CharacterSlot

namespace SDProject.Boot
{
    /// <summary>
    /// ������ ������ ��ĵ�Ͽ� Ally/Enemy ������ �ڵ� ����.
    /// - BoardLayout�� ������ ���� �ڿ� �����ϵ��� 1������ ���.
    /// - count=-1 �̸� ���� ���� ä��.
    /// - ���� �����տ� SimpleUnitBinder�� ������ ��/�ε����� �����ϰ� Rebind.
    /// </summary>
    public class UnitAutoSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _allyPrefab;
        [SerializeField] private GameObject _enemyPrefab;

        [Header("Counts (-1 = fill all slots)")]
        [SerializeField] private int _allyCount = -1;
        [SerializeField] private int _enemyCount = -1;

        [Header("Parents (optional)")]
        [SerializeField] private Transform _allyParent;
        [SerializeField] private Transform _enemyParent;

        [Header("Clear Before Spawn")]
        [SerializeField] private bool _clearExistingAllies = true;
        [SerializeField] private bool _clearExistingEnemies = true;

        private void OnEnable() => StartCoroutine(CoSpawn());

        private IEnumerator CoSpawn()
        {
            // BoardLayout/BoardRuntime�� �ʱ�ȭ�� �ð��� �ش�.
            yield return null;

            if (_clearExistingAllies) ClearUnits(TeamSide.Ally);
            if (_clearExistingEnemies) ClearUnits(TeamSide.Enemy);

            SpawnTeam(TeamSide.Ally, _allyPrefab, _allyCount, _allyParent);
            SpawnTeam(TeamSide.Enemy, _enemyPrefab, _enemyCount, _enemyParent);
        }

        private void SpawnTeam(TeamSide team, GameObject prefab, int count, Transform parent)
        {
            if (!prefab)
            {
                Debug.Log($"[UnitAutoSpawner] Skip {team}: prefab missing.");
                return;
            }

            var slots = FindObjectsByType<CharacterSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .Where(s => s && s.Team == team)
                        .OrderBy(s => s.Index)
                        .ToList();

            if (slots.Count == 0)
            {
                Debug.LogWarning($"[UnitAutoSpawner] No slots for {team}.");
                return;
            }

            int target = (count < 0) ? slots.Count : Mathf.Min(count, slots.Count);

            for (int i = 0; i < target; i++)
            {
                var slot = slots[i];
                var pos = slot.mount ? slot.mount.position : slot.transform.position;
                var rot = slot.mount ? slot.mount.rotation : slot.transform.rotation;
                var par = parent ? parent : slot.transform.parent;

                var go = Instantiate(prefab, pos, rot, par);
                go.name = $"{team}_Unit_{i}";

                // Binder ������ ��/�ε��� ���� �� ��� ���ε�
                var binder = go.GetComponent<SDProject.Combat.SimpleUnitBinder>();
                if (binder != null)
                {
                    binder.SetBinding(team, slot.Index, rebind: true);
                }
            }

            Debug.Log($"[UnitAutoSpawner] Spawned {target} {team} unit(s).");
        }

        private void ClearUnits(TeamSide team)
        {
            var binders = FindObjectsByType<SDProject.Combat.SimpleUnitBinder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                          .Where(b => b != null && b.Team == team);
            foreach (var b in binders)
            {
                if (b) Destroy(b.gameObject);
            }
        }
    }
}
