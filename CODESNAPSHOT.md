# Code Snapshot - 2025-10-17 14:00:00
Commit: 1625caa

## Assets\SDProject\Scripts\Bootstrap\UnitAutoSpawner.cs
```csharp
using System.Collections;
using System.Linq;
using UnityEngine;
using SDProject.Combat.Board; // TeamSide, CharacterSlot

namespace SDProject.Boot
{
    /// <summary>
    /// 보드의 슬롯을 스캔하여 Ally/Enemy 유닛을 자동 스폰.
    /// - BoardLayout가 슬롯을 만든 뒤에 동작하도록 1프레임 대기.
    /// - count=-1 이면 슬롯 전부 채움.
    /// - 유닛 프리팹에 SimpleUnitBinder가 있으면 팀/인덱스를 세팅하고 Rebind.
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
            // BoardLayout/BoardRuntime가 초기화될 시간을 준다.
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

                // Binder 있으면 팀/인덱스 세팅 후 즉시 바인드
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
```

## Assets\SDProject\Scripts\Combat\Board\BoardLayout.cs
```csharp
using UnityEngine;


namespace SDProject.Combat.Board
{
    /// <summary>
    /// Creates and places slots at runtime and assigns Team/Index (Plan A).
    /// - Uses a single shared Slot prefab (with CharacterSlot). Recommended: assignAtRuntime=true.
    /// - Spawning/binding units is out of scope (SRP). Other systems should handle units.
    /// </summary>
    public class BoardLayout : MonoBehaviour
    {
        [Header("Common Slot Prefab")]
        [Tooltip("A shared slot prefab that contains CharacterSlot component.")]
        public GameObject slotPrefab;

        [Header("Ally Layout (4 lanes)")]
        public int allySlotCount = 4;                    // Back(0), Mid2(1), Mid1(2), Front(3)
        public Vector3 allyStart = new Vector3(-6f, 0f, 0f);
        public float allyGap = 1.8f;
        public Transform allyRoot;                       // Parent for ally slots (defaults to this)

        [Header("Enemy Layout (5 lanes)")]
        public int enemySlotCount = 5;                   // Front(0), Mid1(1), Mid2(2), Mid3(3), Back(4)
        public Vector3 enemyStart = new Vector3(6f, 0f, 0f);
        public float enemyGap = 1.8f;                    // Place from right to left by using -enemyGap in X
        public Transform enemyRoot;                      // Parent for enemy slots (defaults to this)

        [Header("Naming")]
        public string allyNamePrefix = "Ally_";
        public string enemyNamePrefix = "Enemy_";

        private void Awake()
        {
            if (!slotPrefab)
            {
                Debug.LogError("[BoardLayout] slotPrefab is missing.");
                return;
            }

            if (!allyRoot) allyRoot = this.transform;
            if (!enemyRoot) enemyRoot = this.transform;

            BuildAllySlots();
            BuildEnemySlots();

            // ★ One-line reinforcement: after slots are created, ask BoardRuntime to rescan immediately.
            UnityEngine.Object.FindFirstObjectByType<SDProject.Combat.Board.BoardRuntime>(FindObjectsInactive.Include)?.RefreshFromScene();
        }

        private void BuildAllySlots()
        {
            for (int i = 0; i < allySlotCount; i++)
            {
                // Left -> Right: Back(0) → Mid2(1) → Mid1(2) → Front(3)
                Vector3 pos = allyStart + new Vector3(i * allyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, allyRoot);
                go.name = $"{allyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // Assign team/index at runtime
                slot.Configure(TeamSide.Ally, i);

                // Default mount fallback
                if (!slot.mount) slot.mount = slot.transform;
            }
        }

        private void BuildEnemySlots()
        {
            for (int i = 0; i < enemySlotCount; i++)
            {
                // Place from right to left in world space: Front(0) → Mid1(1) → Mid2(2) → Mid3(3) → Back(4)
                Vector3 pos = enemyStart + new Vector3(i * -enemyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, enemyRoot);
                go.name = $"{enemyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // Assign team/index at runtime
                slot.Configure(TeamSide.Enemy, i);

                // Default mount fallback
                if (!slot.mount) slot.mount = slot.transform;
            }
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Board\BoardRuntime.cs
```csharp
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

        /// <summary>Returns first alive enemy by priority (enemy indices ascending == Front→Back).</summary>
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
```

## Assets\SDProject\Scripts\Combat\Board\CharacterSlot.cs
```csharp
using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// 보드의 한 슬롯을 표현합니다.
    /// - A안: 런타임에서 팀/인덱스를 부여받을 수 있도록 설계(assignAtRuntime).
    /// - 프리팹 단계에서 team/index를 비워두고, BoardLayout이 Instantiate 후 Configure로 세팅합니다.
    /// </summary>
    public class CharacterSlot : MonoBehaviour
    {
        [Header("Assignment Mode")]
        [Tooltip("true면 프리팹에서 team/index를 비워두고, 런타임에 BoardLayout이 Configure로 값을 세팅합니다.")]
        public bool assignAtRuntime = true;

        [Header("Identity (assignAtRuntime=false일 때만 사용)")]
        public TeamSide Team = TeamSide.Ally; // ★ TeamSide는 기존 정의를 재사용합니다(중복 정의 금지).
        public int Index = 0;

        [Header("Mount (유닛 스냅 지점)")]
        [Tooltip("유닛이 이 슬롯에 있을 때 위치를 맞출 기준 Transform입니다. 없으면 슬롯 Transform을 사용합니다.")]
        public Transform mount;

        /// <summary>
        /// BoardLayout이 슬롯을 생성할 때 호출하여 팀/인덱스를 확정합니다.
        /// </summary>
        public void Configure(TeamSide newTeam, int newIndex)
        {
            Team = newTeam;
            Index = newIndex;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // assignAtRuntime == true이면 에디터에서 team/index를 강제 변경하지 않습니다.
            // (프리팹을 '미지정' 상태로 둘 수 있게 하기 위함)
        }
#endif
    }
}
```

## Assets\SDProject\Scripts\Combat\Board\DummyCharacter.cs
```csharp
// Assets/SDProject/Scripts/Combat/Board/DummyCharacter.cs
using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// Minimal dummy actor: just a sprite colored by team.
    /// SRP: visuals only for prototype (no stats/ai yet).
    /// </summary>
    [DisallowMultipleComponent]
    public class DummyCharacter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void Bind(TeamSide team, int index)
        {
            if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (!spriteRenderer) return;

            // simple color code
            spriteRenderer.color = team == TeamSide.Ally ? new Color(0.55f, 0.75f, 1f) : new Color(1f, 0.55f, 0.55f);

#if UNITY_EDITOR
            name = $"{team}_Unit_{index:00}";
            Debug.Log($"[DummyCharacter] Spawned {name}");
#endif
        }

        private void Reset()
        {
            if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Board\Positioning.cs
```csharp
using System;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// Logical lane flags for position filters.
    /// </summary>
    [Flags]
    public enum PositionFlags
    {
        None = 0,
        Front = 1 << 0,
        Mid1 = 1 << 1,
        Mid2 = 1 << 2,
        Mid3 = 1 << 3,
        Back = 1 << 4
    }

    /// <summary>
    /// Maps (team, index) to a PositionFlags lane according to our board design:
    /// Ally indices: 0=Back, 1=Mid2, 2=Mid1, 3=Front
    /// Enemy indices: 0=Front, 1=Mid1, 2=Mid2, 3=Mid3, 4=Back
    /// </summary>
    public static class PositionResolver
    {
        public static PositionFlags ToLane(TeamSide team, int index)
        {
            if (team == TeamSide.Ally)
            {
                return index switch
                {
                    0 => PositionFlags.Back,
                    1 => PositionFlags.Mid2,
                    2 => PositionFlags.Mid1,
                    3 => PositionFlags.Front,
                    _ => PositionFlags.None
                };
            }
            else // Enemy
            {
                return index switch
                {
                    0 => PositionFlags.Front,
                    1 => PositionFlags.Mid1,
                    2 => PositionFlags.Mid2,
                    3 => PositionFlags.Mid3,
                    4 => PositionFlags.Back,
                    _ => PositionFlags.None
                };
            }
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Board\TeamSide.cs
```csharp
// Assets/SDProject/Scripts/Combat/Board/TeamSide.cs
namespace SDProject.Combat.Board
{
    public enum TeamSide { Ally, Enemy }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Board\SimpleUnit.cs
```csharp
using UnityEngine;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Minimal demo unit: HP/AP and current slot reference.
    /// Implements IDamageable & IApConsumer as properties + methods.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleUnit : MonoBehaviour, IDamageable, IApConsumer
    {
        [Header("Identity")]
        public TeamSide Team;
        public int Index;

        [Header("Vitals")]
        [Min(1)] public int MaxHp = 30;
        public int CurrentHp;

        // IMPORTANT: Interface requires a *property*, not a field.
        // Use auto-property with private setter to satisfy IApConsumer.
        [Min(0)] public int CurrentAp { get; private set; } = 3;

        [Header("Board Link (resolved by runtime/binder)")]
        public CharacterSlot CurrentSlot;

        private void Awake()
        {
            CurrentHp = MaxHp;
        }

        // --- IDamageable ---
        public void ApplyDamage(int dmg)
        {
            int v = Mathf.Max(0, dmg);
            CurrentHp = Mathf.Max(0, CurrentHp - v);
            Debug.Log($"[HP] {name} takes {v}. {CurrentHp}/{MaxHp}");
            if (CurrentHp <= 0) Debug.Log($"[Unit] {name} defeated.");
        }

        public bool IsAlive() => CurrentHp > 0;

        // --- IApConsumer ---
        public bool TryConsumeAp(int amount)
        {
            if (amount <= 0) return true;
            if (CurrentAp < amount) return false;
            CurrentAp -= amount;
            Debug.Log($"[AP] {name} consumes {amount} => {CurrentAp}");
            return true;
        }

        // Optional helper to refill AP (not in interface, but handy in tests)
        public void SetAp(int value)
        {
            CurrentAp = Mathf.Max(0, value);
        }

        public void AddAp(int delta)
        {
            CurrentAp = Mathf.Max(0, CurrentAp + delta);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Board\SimpleUnitBinder.cs
```csharp
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SDProject.Combat.Board;

namespace SDProject.Combat
{
    /// <summary>
    /// Binds a unit to a board slot at runtime.
    /// - Exposes read-only properties Team/Index for external usage (binder.Team / binder.Index).
    /// - If Index < 0, finds the nearest slot of the given Team and snaps to it.
    /// - If BoardRuntime exists, tries to register the unit there (via direct ref or reflection fallback).
    /// - If BoardRuntime is missing, still snaps to the slot's mount for safe v1 usage.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleUnitBinder : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private TeamSide team = TeamSide.Ally;
        [SerializeField] private int index = -1; // -1 = auto (nearest)
        [Tooltip("Wait one frame before binding to ensure BoardLayout/Runtime are ready.")]
        [SerializeField] private bool waitOneFrame = true;

        [Header("Snap")]
        [Tooltip("If true, snaps the transform to slot mount after binding.")]
        [SerializeField] private bool snapToMount = true;

        // ---- Public read-only properties (권장안 핵심) ----
        public TeamSide Team => team;
        public int Index => index;

        // ---- Runtime references (optional/diagnostics) ----
        public CharacterSlot CurrentSlot { get; private set; }
        public MonoBehaviour BoardRuntimeRef { get; private set; } // keep as MonoBehaviour to avoid hard dependency
        private Component _boardRuntimeComp; // cached component (any type named "BoardRuntime")
        private Transform _cachedMount;
        private Transform _t;

        private void Awake()
        {
            _t = transform;
        }

        private void OnEnable()
        {
            if (waitOneFrame) StartCoroutine(CoBindNextFrame());
            else TryBindNow();
        }

        private IEnumerator CoBindNextFrame()
        {
            yield return null; // let BoardLayout create slots & BoardRuntime scan them
            TryBindNow();
        }

        /// <summary>
        /// External setter with optional rebind.
        /// </summary>
        public void SetBinding(TeamSide newTeam, int newIndex, bool rebind = true)
        {
            team = newTeam;
            index = newIndex;
            if (rebind && isActiveAndEnabled)
            {
                TryBindNow();
            }
        }

        /// <summary>
        /// Re-run binding with current team/index.
        /// </summary>
        [ContextMenu("Rebind")]
        public void Rebind()
        {
            TryBindNow();
        }

        private void TryBindNow()
        {
            // 1) Find BoardRuntime (optional)
            _boardRuntimeComp = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(mb => mb != null && mb.GetType().Name == "BoardRuntime");
            BoardRuntimeRef = _boardRuntimeComp as MonoBehaviour;

            // 2) Resolve target slot index
            var targetIndex = index >= 0 ? index : FindNearestSlotIndex(team);

            if (targetIndex < 0)
            {
                Debug.LogWarning($"[SimpleUnitBinder] No valid slot found for Team={team}.", this);
                return;
            }

            index = targetIndex; // lock-in chosen index

            // 3) Get the CharacterSlot we are going to occupy/snap to
            var slot = GetSlotByTeamIndex(team, targetIndex);
            if (slot == null)
            {
                Debug.LogWarning($"[SimpleUnitBinder] Slot not found Team={team}, Index={targetIndex}.", this);
                return;
            }

            CurrentSlot = slot;
            _cachedMount = slot.mount != null ? slot.mount : slot.transform;

            // 4) Register to BoardRuntime if available; otherwise just snap
            bool registered = TryRegisterToBoardRuntime(_boardRuntimeComp, team, targetIndex);

            if (!registered)
            {
                // Fallback: just snap to mount so v1 keeps working
                if (snapToMount && _cachedMount)
                {
                    _t.position = _cachedMount.position;
                    _t.rotation = _cachedMount.rotation;
                }
                Debug.Log($"[SimpleUnitBinder] Fallback snap only. (No BoardRuntime or method not found) Team={team}, Index={index}, Unit={name}", this);
            }
            else
            {
                if (snapToMount && _cachedMount)
                {
                    _t.position = _cachedMount.position;
                    _t.rotation = _cachedMount.rotation;
                }
                Debug.Log($"[SimpleUnitBinder] Registered to BoardRuntime. Team={team}, Index={index}, Unit={name}", this);
            }
        }

        /// <summary>
        /// Finds nearest slot index by distance to this transform among the given team.
        /// Returns -1 if none found.
        /// </summary>
        private int FindNearestSlotIndex(TeamSide side)
        {
            var allSlots = FindObjectsByType<CharacterSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(s => s != null && s.Team == side)
                .ToList();

            if (allSlots.Count == 0) return -1;

            var nearest = allSlots
                .OrderBy(s => (s.transform.position - _t.position).sqrMagnitude)
                .First();

            return nearest.Index;
        }

        /// <summary>
        /// Fetch slot by (team, index) using scene scan. Returns null if not found.
        /// </summary>
        private CharacterSlot GetSlotByTeamIndex(TeamSide side, int idx)
        {
            return FindObjectsByType<CharacterSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(s => s != null && s.Team == side && s.Index == idx);
        }

        /// <summary>
        /// Attempts to call BoardRuntime.RegisterUnit(this.gameObject, team, index) or RegisterUnit(SimpleUnit, TeamSide, int)
        /// via reflection so we don't hard depend on a specific signature.
        /// Returns true if any registration method was successfully invoked.
        /// </summary>
        private bool TryRegisterToBoardRuntime(Component boardRuntime, TeamSide side, int idx)
        {
            if (boardRuntime == null) return false;

            var brType = boardRuntime.GetType();

            // Try some common signatures by reflection (no hard dependency):
            // 1) RegisterUnit(GameObject, TeamSide, int)
            // 2) RegisterUnit(Component/MonoBehaviour, TeamSide, int)
            // 3) RegisterUnit(object, TeamSide, int)
            // 4) RegisterUnit(TeamSide, int, GameObject)

            var args1 = new object[] { this.gameObject, side, idx };
            var args2 = new object[] { this, side, idx };

            // (a) exact by name with 3 params
            var mi = brType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "RegisterUnit") return false;
                    var ps = m.GetParameters();
                    if (ps.Length != 3) return false;
                    return true;
                });

            if (mi != null)
            {
                try
                {
                    // try with (GameObject, TeamSide, int)
                    var parameters = mi.GetParameters();
                    object[] useArgs = null;

                    // Heuristic match
                    if (parameters[0].ParameterType == typeof(GameObject))
                        useArgs = args1;
                    else if (parameters[0].ParameterType.IsAssignableFrom(this.GetType()))
                        useArgs = args2;
                    else
                        useArgs = args1; // last resort

                    mi.Invoke(boardRuntime, useArgs);
                    return true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[SimpleUnitBinder] RegisterUnit reflection failed: {ex.Message}", this);
                }
            }

            // (b) alternative signature: RegisterUnit(TeamSide, int, GameObject)
            mi = brType.GetMethod("RegisterUnit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                   null, new[] { typeof(TeamSide), typeof(int), typeof(GameObject) }, null);
            if (mi != null)
            {
                try
                {
                    mi.Invoke(boardRuntime, new object[] { side, idx, this.gameObject });
                    return true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[SimpleUnitBinder] RegisterUnit(TeamSide,int,GameObject) failed: {ex.Message}", this);
                }
            }

            return false;
        }

        // Optional: clean-up hook – try to unregister if method exists
        private void OnDisable()
        {
            TryUnregister();
        }

        private void OnDestroy()
        {
            TryUnregister();
        }

        private void TryUnregister()
        {
            if (_boardRuntimeComp == null) return;

            var brType = _boardRuntimeComp.GetType();
            var mi = brType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "UnregisterUnit") return false;
                    var ps = m.GetParameters();
                    return ps.Length == 1; // try UnregisterUnit(GameObject) or (Component)
                });

            if (mi != null)
            {
                try
                {
                    mi.Invoke(_boardRuntimeComp, new object[] { this.gameObject });
                }
                catch { /* ignore */ }
            }
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Core\CardPlayController.cs
```csharp
// Assets/SDProject/Scripts/Combat/Cards/Core/CardPlayController.cs
using UnityEngine;
using TMPro;
using SDProject.Data;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// v1 미니멀: 카드 클릭 → 전열(Front-most) 적 1명 자동 타겟 → 사용 처리.
    /// - CardData 스키마: cardId, displayName, apCost 만 사용
    /// - BoardRuntime: GetFrontMostEnemyUnit() 만 사용
    /// - 타겟팅/효과/JSON/레인필터는 이후 단계에서 확장
    /// </summary>
    public sealed class CardPlayController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BoardRuntime _board;     // 전열 적 찾기에 필요
        [SerializeField] private HandRuntime _hand;       // 사용 처리(손패에서 제거)

        [Header("UI (optional)")]
        [SerializeField] private TMP_Text _errorLabel;    // 간단 에러 라벨(v1 고정문구)

        private void Awake()
        {
            if (_board == null) _board = FindFirstObjectByType<BoardRuntime>(FindObjectsInactive.Include);
            if (_hand == null) _hand = FindFirstObjectByType<HandRuntime>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// CardView.OnClick() 에서 호출됨.
        /// </summary>
        public void PlayCard(CardData card, GameObject caster)
        {
            ClearError();

            if (card == null || caster == null)
            {
                EmitError("사용 조건 불일치");
                Debug.LogWarning("[CardPlay] Null card/caster.");
                return;
            }

            if (_board == null)
            {
                EmitError("보드 없음");
                Debug.LogWarning("[CardPlay] BoardRuntime missing.");
                return;
            }

            // v1: 전열(Front-most) 적 자동 선택
            var target = _board.GetFrontMostEnemyUnit();
            if (target == null)
            {
                EmitError("대상 없음");
                Debug.LogWarning("[CardPlay] No front-most enemy found.");
                return;
            }

            // (추가 효과/데미지 시스템이 아직 없으므로) 로그만 남김
            Debug.Log($"[CardPlay] '{(string.IsNullOrEmpty(card.displayName) ? card.name : card.displayName)}' AP:{card.apCost} → Target:{target.name}");

            // 손패에서 제거(버림 처리는 BattleController에서 HandRuntime.OnUsed를 구독해 Discard로 이동시키는 구조 권장)
            if (_hand != null)
            {
                _hand.MarkUsed(card);
            }

        }

        private void EmitError(string msg)
        {
            if (_errorLabel) _errorLabel.text = msg;
        }

        private void ClearError()
        {
            if (_errorLabel) _errorLabel.text = string.Empty;
        }
    }

    /// <summary>
    /// TurnPhase enum 값이 프로젝트마다 달라도 안전하게 이벤트를 발행하기 위한 헬퍼.
    /// </summary>
    internal static class TurnPhaseEventHelper
    {
        /*public static void RaiseTurnPhaseChangedLabelSafe(this GameEvents _, string label)
        {
            // enum이 있으면 써주고, 없으면 조용히 스킵
            if (System.Enum.TryParse<SDProject.Core.TurnPhase>(label, out var phase))
            {
                GameEvents.RaiseTurnPhaseChanged(phase);
            }
        }
        */
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Core\Enums.cs
```csharp
using System;

namespace SDProject.Combat.Cards
{
    public enum CardType { Attack, Defense, Support, Move }
    public enum CardClass { Common, Character, Mythic }
    public enum CardRarity { Common, Rare }

    public enum TargetType
    {
        Self,
        Ally,
        AllyAll,
        Enemy,
        EnemyAll,
        AllyThenEnemy,    // v1.1 계획
        EnemyFrontMost,   // v1 자동 단일
        SingleManual      // v1 수동 단일
    }

    [Flags]
    public enum PositionFlags
    {
        None = 0,
        Front = 1 << 0,
        Mid1 = 1 << 1,
        Mid2 = 1 << 2,
        Mid3 = 1 << 3, // 적측만 운용
        Back = 1 << 4,
        All = Front | Mid1 | Mid2 | Mid3 | Back
    }

    public enum ErrorLabel
    {
        None,
        ERR_AP_LACK,
        ERR_POSUSE_MISMATCH,
        ERR_NO_TARGET,
        ERR_UNIT_DISABLED
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Core\RuntimeInterfaces.cs
```csharp
using UnityEngine;

namespace SDProject.Combat.Cards
{
    // Shared combat runtime interfaces

    public interface IDamageable
    {
        void ApplyDamage(int dmg);
        bool IsAlive();
    }

    public interface IApConsumer
    {
        int CurrentAp { get; }
        bool TryConsumeAp(int amount);
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Data\CardDefinition.cs
```csharp
using UnityEngine;

namespace SDProject.Combat.Cards
{
    [CreateAssetMenu(menuName = "SDProject/Card Definition", fileName = "CardDefinition")]
    public class CardDefinition : ScriptableObject
    {
        [Header("Data Table Fields")]
        public string Id;
        public bool Enabled = true;
        public string NameId;
        public string DescId;
        public CardType Type;
        public CardClass Class;
        public CardRarity Rarity;
        public string CharId = "Public";
        [Min(0)] public int Cost = 1;

        [Header("Targeting & Position")]
        public TargetType TargetType = TargetType.EnemyFrontMost;
        public PositionFlags PosUse = PositionFlags.All; // caster allowed lanes
        public PositionFlags PosHit = PositionFlags.All; // target allowed lanes

        [Header("Upgrade (data only in v1)")]
        public bool Upgradable = true;
        [Min(0)] public int UpgradeStep = 0;
        public string UpgradeRefId;

        [Header("Composed Effects")]
        public ScriptableObject[] Effects; // each implements ICardEffect

        public override string ToString() =>
            $"Card[{Id}] Type={Type} Cost={Cost} Target={TargetType} Step={UpgradeStep}";
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Data\CardLibrary.cs
```csharp
// Assets/SDProject/Scripts/Combat/Cards/Data/CardLibrary.cs
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Combat.Cards
{
    [CreateAssetMenu(menuName = "SDProject/Card Library", fileName = "CardLibrary")]
    public class CardLibrary : ScriptableObject
    {
        public CardDefinition[] cards;

        private Dictionary<string, CardDefinition> _map;

        private void OnEnable()
        {
            _map = new Dictionary<string, CardDefinition>();
            if (cards == null) return;
            foreach (var c in cards)
            {
                if (!c || string.IsNullOrEmpty(c.Id)) continue;
                _map[c.Id] = c;
            }
        }

        public bool TryGet(string id, out CardDefinition def)
        {
            if (_map != null && !string.IsNullOrEmpty(id) && _map.TryGetValue(id, out def))
                return true;

            def = null; // ★ out 보장
            return false;
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Effects\DamageEffectDef.cs
```csharp
using UnityEngine;

namespace SDProject.Combat.Cards
{
    [CreateAssetMenu(menuName = "SDProject/Card Effects/Damage", fileName = "DamageEffect")]
    public class DamageEffectDef : ScriptableObject, ICardEffect
    {
        [Min(0)] public int BaseDamage = 6;
        [Min(0)] public int RandomBonusMax = 0;

        public void Execute(ICardEffectContext ctx)
        {
            foreach (var t in ctx.Targets)
            {
                var hp = t.GetComponent<IDamageable>();
                if (hp == null)
                {
                    Debug.LogWarning($"[DamageEffect] {t.name} has no IDamageable.");
                    continue;
                }
                int bonus = RandomBonusMax > 0 ? ctx.Rng.Next(0, RandomBonusMax + 1) : 0;
                int dmg = BaseDamage + bonus;
                hp.ApplyDamage(dmg);
                Debug.Log($"[DamageEffect] {t.name} takes {dmg} dmg.");
            }
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Effects\ICardEffect.cs
```csharp
using UnityEngine;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    public interface ICardEffectContext
    {
        GameObject Caster { get; }
        GameObject[] Targets { get; }
        System.Random Rng { get; }
        BoardRuntime Board { get; }
    }

    public interface ICardEffect
    {
        void Execute(ICardEffectContext ctx);
    }

    public class CardEffectContext : ICardEffectContext
    {
        public GameObject Caster { get; }
        public GameObject[] Targets { get; }
        public System.Random Rng { get; }
        public BoardRuntime Board { get; }

        public CardEffectContext(GameObject caster, GameObject[] targets, BoardRuntime board, System.Random rng = null)
        {
            Caster = caster;
            Targets = targets ?? System.Array.Empty<GameObject>();
            Board = board;
            Rng = rng ?? new System.Random();
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Effects\KnockbackEffectDef.cs
```csharp
using UnityEngine;

namespace SDProject.Combat.Cards
{
    [CreateAssetMenu(menuName = "SDProject/Card Effects/Knockback", fileName = "KnockbackEffect")]
    public class KnockbackEffectDef : ScriptableObject, ICardEffect
    {
        [Min(1)] public int Cells = 1;

        public void Execute(ICardEffectContext ctx)
        {
            foreach (var t in ctx.Targets)
            {
                bool ok = ctx.Board != null && ctx.Board.TryKnockback(t, Cells);
                if (!ok) Debug.Log($"[Knockback] Fail (ignored): {t.name}, +{Cells}");
                else Debug.Log($"[Knockback] {t.name} moved +{Cells}.");
            }
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\UI\CardView.cs
```csharp
// Assets/SDProject/Scripts/Combat/Cards/UI/CardView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SDProject.Data;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Single card UI. Binds CardData and relays click to CardPlayController.
    /// Safe-guards:
    /// - Button.onClick is auto-wired in Awake.
    /// - Texts set raycastTarget=false (won't swallow clicks).
    /// - OnClick() falls back to BoardRuntime to fetch a caster if missing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _ap;
        [SerializeField] private Image _typeIcon; // optional

        // Runtime
        private CardData _card;
        private GameObject _caster;
        private CardPlayController _play;

        /// <summary>Legacy-friendly external injector.</summary>
        public GameObject Caster
        {
            get => _caster;
            set => _caster = value;
        }

        private void Awake()
        {
            // Auto-wire button
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClick);
            }

            if (_title) _title.raycastTarget = false;
            if (_ap) _ap.raycastTarget = false;
        }

        /// <summary>Bind all runtime references.</summary>
        public void Bind(CardData card, GameObject caster, CardPlayController play)
        {
            _card = card;
            _caster = caster;
            _play = play;

            if (_title) _title.text = string.IsNullOrEmpty(card.displayName) ? card.name : card.displayName;
            if (_ap) _ap.text = card.apCost.ToString();
        }

        /// <summary>UI button callback.</summary>
        public void OnClick()
        {
            // Last-chance fallback for caster
            if (_caster == null)
            {
                var board = FindFirstObjectByType<SDProject.Combat.Board.BoardRuntime>(FindObjectsInactive.Include);
                _caster = board?.GetFirstAllyUnit();
            }

            Debug.Log($"[CardView] Click '{_card?.displayName}', play={_play != null}, caster={_caster != null}");
            if (_card == null || _play == null || _caster == null)
            {
                Debug.LogWarning("[CardView] Missing Card/PlayController/Caster.");
                return;
            }

            _play.PlayCard(_card, _caster);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Cards\UI\HandView.cs
```csharp
// Assets/SDProject/Scripts/Combat/Cards/UI/HandView.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SDProject.Data;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Builds card UI under a layout parent. Does not touch layout values; lets LayoutGroup handle sizing.
    /// Safe-guards:
    /// - Auto-wire Button.onClick for each spawned card.
    /// - If caster is null, tries to fetch first ally unit from BoardRuntime.
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        [Header("Bind")]
        [SerializeField] private RectTransform _content;   // Where cards are spawned (HandPanel)
        [SerializeField] private CardView _cardPrefab;     // Card prefab (Image + Button + 2 TMP texts)

        private readonly List<CardView> _spawned = new();

        public void Rebuild(IReadOnlyList<CardData> items, GameObject caster, CardPlayController play)
        {
            if (_content == null || _cardPrefab == null)
            {
                Debug.LogWarning("[HandView] Missing _content or _cardPrefab.");
                return;
            }

            // Clear old
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i]) Destroy(_spawned[i].gameObject);
            _spawned.Clear();

            var count = items?.Count ?? 0;
            Debug.Log($"[HandView] rebuild count={count}");
            if (items == null) return;

            for (int i = 0; i < items.Count; i++)
            {
                var data = items[i];
                if (data == null) continue;

                var cv = Instantiate(_cardPrefab, _content);
                // Respect parent layout; do not override anchors/size.
                cv.Bind(data, caster, play);

                // Safety: force onClick wiring even if prefab was not set.
                var btn = cv.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(cv.OnClick);
                }

                // Fallback caster if still empty
                if (cv.Caster == null)
                {
                    var board = FindFirstObjectByType<SDProject.Combat.Board.BoardRuntime>(FindObjectsInactive.Include);
                    cv.Caster = board?.GetFirstAllyUnit();
                }

                _spawned.Add(cv);
            }
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Targeting\TargetingSystem.cs
```csharp
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
```

## Assets\SDProject\Scripts\Combat\Targeting\UnitClickForwarder.cs
```csharp
// Assets/SDProject/Scripts/Combat/Targeting/UnitClickForwarder.cs
using UnityEngine;

namespace SDProject.Combat
{
    /// <summary>
    /// For manual targeting: forward OnMouseDown to TargetingSystem.
    /// Requires a Collider on the unit.
    /// </summary>
    public sealed class UnitClickForwarder : MonoBehaviour
    {
        [SerializeField] private TargetingSystem _targeting;

        private void Reset()
        {
            _targeting = FindFirstObjectByType<TargetingSystem>(FindObjectsInactive.Include);
        }

        private void OnMouseDown()
        {
            if (_targeting) _targeting.ProvideManualSingle(gameObject);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\BattleController.cs
```csharp
// Assets/SDProject/Scripts/Combat/BattleController.cs
using System.Collections;
using UnityEngine;
using SDProject.Core.FSM;
using SDProject.Data;
using SDProject.Core.Messaging;
using SDProject.Combat.Board;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SDProject.Combat
{
    /// <summary>
    /// Minimal turn driver for v1. Ensures HandView rebuild passes default caster and controller.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private DeckRuntime _deck;
        [SerializeField] private HandRuntime _hand;
        [SerializeField] private Cards.HandView _handView;
        [SerializeField] private Cards.CardPlayController _playController;
        [SerializeField] private BoardRuntime _board;

        [Header("Caster")]
        [SerializeField] private GameObject _defaultCaster;

        private StateMachine _fsm;
        private StPlayerTurn _stPlayer;
        private StEnemyTurn _stEnemy;

        private void Awake()
        {
            if (_hand == null) _hand = FindFirstObjectByType<HandRuntime>(FindObjectsInactive.Include);
            if (_deck == null) _deck = FindFirstObjectByType<DeckRuntime>(FindObjectsInactive.Include);
            if (_handView == null) _handView = FindFirstObjectByType<Cards.HandView>(FindObjectsInactive.Include);
            if (_playController == null) _playController = FindFirstObjectByType<Cards.CardPlayController>(FindObjectsInactive.Include);
            if (_board == null) _board = FindFirstObjectByType<BoardRuntime>(FindObjectsInactive.Include);

            // Auto-assign default caster if not provided
            if (_defaultCaster == null)
            {
                _defaultCaster = _board?.GetFirstAllyUnit();
                if (_defaultCaster == null)
                    Debug.LogWarning("[Battle] No ally unit found to use as default caster.");
            }

            _hand.OnUsed += OnCardUsed;

            _fsm = new StateMachine();
            _stPlayer = new StPlayerTurn(this);
            _stEnemy = new StEnemyTurn(this);

            _fsm.AddTransition(_stPlayer, _stEnemy, SpacePressed);
            _fsm.AddTransition(_stEnemy, _stPlayer, () => _stEnemy.IsFinished);
        }

        private void Start() => StartCoroutine(BootFSMNextFrame());
        private IEnumerator BootFSMNextFrame()
        {
            yield return null;                                // 1프레임 양보
            _deck?.EnsureInitialized();                       // ← 덱 준비 보장
            _fsm.SetState(_stPlayer);
        }
        private void Update() => _fsm.Tick(Time.deltaTime);

        private bool SpacePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        // ── Turn Hooks ─────────────────────────────────────────
        public void OnPlayerTurnEnter()
        {
            DrawNewHand();
            // Ensure UI shows with caster/controller
            _handView?.Rebuild(_hand.Items, _defaultCaster, _playController);
        }

        public void OnPlayerTurnExit()
        {
            if (_deck == null || _hand == null) return;
            var rest = _hand.TakeAll();
            _deck.Discard(rest);
            GameEvents.RaiseDeckChanged(_deck.DrawCount, _deck.DiscardCount);
        }

        private void DrawNewHand()
        {
            if (_hand == null || _deck == null)
            {
                Debug.LogError("[Battle] DrawNewHand: missing refs.");
                return;
            }

            _hand.Clear();
            var drawn = _deck.Draw(_deck.DrawPerTurn);
            var added = _hand.AddCards(drawn, _deck.HandMax);
            Debug.Log($"[Battle] Draw request={_deck.DrawPerTurn}, returned={drawn.Count}, added={added}, now hand={_hand.Count}");

            GameEvents.RaiseHandChanged(_hand.Count);
            GameEvents.RaiseDeckChanged(_deck.DrawCount, _deck.DiscardCount);
        }

        private class StPlayerTurn : IState
        {
            private readonly BattleController c;
            public StPlayerTurn(BattleController ctx) => c = ctx;
            public void Enter() { Debug.Log("▶ PlayerTurn Enter"); c.OnPlayerTurnEnter(); }
            public void Tick(float dt) { }
            public void Exit() { Debug.Log("⏸ PlayerTurn Exit"); c.OnPlayerTurnExit(); }
        }

        private class StEnemyTurn : IState
        {
            private readonly BattleController c;
            public bool IsFinished { get; private set; }
            public StEnemyTurn(BattleController ctx) => c = ctx;
            public void Enter() { Debug.Log("[Battle] EnemyTurn..."); IsFinished = false; c.StartCoroutine(CoEnemy()); }
            private IEnumerator CoEnemy() { yield return new WaitForSeconds(1f); IsFinished = true; }
            public void Tick(float dt) { }
            public void Exit() { }
        }

        private void OnDestroy()
        {
            if (_hand != null) _hand.OnUsed -= OnCardUsed;
        }

        private void OnCardUsed(CardData card)
        {
            _deck?.Discard(card);
        }

        // (optional) UI button
        public void OnClickEndTurn() => _fsm?.SetState(_stEnemy);
    }
}
```

## Assets\SDProject\Scripts\Combat\DeckRuntime.cs
```csharp
// Assets/SDProject/Scripts/Combat/DeckRuntime.cs
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;
using SDProject.DataBridge; // ← 어댑터 네임스페이스

namespace SDProject.Combat
{
    public sealed class DeckRuntime : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private DeckSourceSOAdapter _deckSource;  // ← 강타입 필드로 변경

        [Header("Config")]
        [Min(1)][SerializeField] private int _drawPerTurn = 5;
        [Min(1)][SerializeField] private int _handMax = 10;

        // runtime
        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discard = new();
        private bool _initialized;

        public int DrawPerTurn => _drawPerTurn;
        public int HandMax => _handMax;
        public int DrawCount => _drawPile.Count;
        public int DiscardCount => _discard.Count;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (_initialized) return;

            if (_deckSource == null)
            {
                Debug.LogError("[Deck] _deckSource is null. Assign DeckSourceSOAdapter in Inspector.");
                return;
            }

            var init = _deckSource.GetInitialDeck();
            var cnt = init?.Count ?? 0;

            _drawPile.Clear();
            if (cnt > 0) _drawPile.AddRange(init);

            Debug.Log($"[Deck] init: drawPile={_drawPile.Count}, discard={_discard.Count} "
                    + $"(source='{_deckSource.DebugDeckName}', listCount={_deckSource.DebugDeckCount})");

            _initialized = true;
        }

        public List<CardData> Draw(int count)
        {
            EnsureInitialized();
            var result = new List<CardData>(count);
            for (int i = 0; i < count && _drawPile.Count > 0; i++)
            {
                var last = _drawPile[^1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                result.Add(last);
            }
            return result;
        }

        public void Discard(CardData c)
        {
            if (c != null) _discard.Add(c);
        }
        public void Discard(IEnumerable<CardData> cs)
        {
            if (cs == null) return;
            foreach (var c in cs) if (c != null) _discard.Add(c);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\HandRuntime.cs
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;
using SDProject.Core.Messaging;

namespace SDProject.Combat
{
    /// <summary>
    /// Holds current hand; raises events when changed.
    /// </summary>
    public sealed class HandRuntime : MonoBehaviour
    {
        private readonly List<CardData> _items = new();

        public int Count => _items.Count;
        public IReadOnlyList<CardData> Items => _items;
        // ▼ 레거시 호환(읽기 전용 별칭)
        public IReadOnlyList<CardData> Cards => _items;


        public event Action<CardData> OnAdded;
        public event Action<CardData> OnUsed;

        public int AddCards(List<CardData> drawResult, int handMax)
        {
            if (drawResult == null) return 0;
            int added = 0;
            foreach (var c in drawResult)
            {
                if (c == null) continue;
                if (_items.Count >= handMax) break;
                _items.Add(c);
                added++;
                OnAdded?.Invoke(c);
            }
            GameEvents.RaiseHandChanged(_items.Count);
            return added;
        }

        public void Clear()
        {
            _items.Clear();
            GameEvents.RaiseHandChanged(_items.Count);
        }

        public List<CardData> TakeAll()
        {
            var all = new List<CardData>(_items);
            _items.Clear();
            GameEvents.RaiseHandChanged(_items.Count);
            return all;
        }

        public void MarkUsed(CardData used)
        {
            if (_items.Remove(used))
            {
                OnUsed?.Invoke(used);
                GameEvents.RaiseHandChanged(_items.Count);
            }
        }
        public void Use(SDProject.Data.CardData card)
        {
            MarkUsed(card);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\SDProject.Combat.asmdef
```csharp
{
    "name": "SDProject.Combat",
    "rootNamespace": "SDProject.Combat",
    "references": [
        "SDProject.Core",
        "SDProject.Data",
        "Unity.InputSystem",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## Assets\SDProject\Scripts\Core\Boot\GameInstaller.cs
```csharp
// Assets/SDProject/Scripts/Core/Boot/GameInstaller.cs
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SDProject.Core.Boot
{
    /// <summary>
    /// Minimal boot installer.
    /// Single responsibility: load the next scene (e.g., "Battle") from Boot scene.
    /// Extend later for settings/saves/localization/Addressables.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameInstaller : MonoBehaviour
    {
        [SerializeField] private string battleSceneName = "Battle"; // must match the Scene name

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(battleSceneName))
            {
                Debug.LogError("[Boot] Target scene name is empty.");
                return;
            }

#if UNITY_EDITOR
            // Warn if Boot is not the first scene in Build Settings (editor only).
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0 || !scenes[0].path.EndsWith("/Boot.unity"))
                Debug.LogWarning("[Boot] Boot is not the first scene in Build Settings.");
#endif

            // Safety: let user know if the scene isn't added to Build Settings.
            if (!Application.CanStreamedLevelBeLoaded(battleSceneName))
                Debug.LogWarning($"[Boot] Scene '{battleSceneName}' is not in Build Settings (File > Build Settings…).");

            Debug.Log($"[Boot] Loading scene: {battleSceneName}");
            // KISS: blocking load is fine for now. Replace with async when you add a loading UI.
            SceneManager.LoadScene(battleSceneName);
        }

        // Example for future use:
        // private IEnumerator LoadNextAsync()
        // {
        //     var op = SceneManager.LoadSceneAsync(battleSceneName);
        //     while (!op.isDone) yield return null;
        // }
    }
}
```

## Assets\SDProject\Scripts\Core\FSM\IState.cs
```csharp
namespace SDProject.Core.FSM
{
    public interface IState
    {
        void Enter();
        void Tick(float dt);
        void Exit();
    }
}
```

## Assets\SDProject\Scripts\Core\FSM\StateMachine.cs
```csharp
// StateMachine.cs
using UnityEngine;

namespace SDProject.Core.FSM
{
    /// <summary>KISS: 간단 상태 전이 + 조건.</summary>
    public sealed class StateMachine
    {
        private IState _current;

        // 간단 조건 전이 용 래퍼
        private struct Transition
        {
            public IState from, to;
            public System.Func<bool> condition;
        }

        private readonly System.Collections.Generic.List<Transition> _transitions = new();

        public void SetState(IState next)
        {
            if (_current == next) return;
            _current?.Exit();
            _current = next;
            _current?.Enter();
#if UNITY_EDITOR
            Debug.Log($"[FSM] Switched to: {_current?.GetType().Name}");
#endif
        }

        public void AddTransition(IState from, IState to, System.Func<bool> condition)
        {
            _transitions.Add(new Transition { from = from, to = to, condition = condition });
        }

        public void Tick(float dt)
        {
            // 조건 검사
            for (int i = 0; i < _transitions.Count; i++)
            {
                var t = _transitions[i];
                if (_current == t.from && t.condition != null && t.condition())
                {
                    SetState(t.to);
                    break;
                }
            }
            _current?.Tick(dt);
        }
    }
}
```

## Assets\SDProject\Scripts\Core\Messaging\GameEvents.cs
```csharp
using System;

namespace SDProject.Core.Messaging
{
    public static class GameEvents
    {
        // Hand
        public static event Action<int> OnHandChanged;
        public static void RaiseHandChanged(int handCount) => OnHandChanged?.Invoke(handCount);

        // Deck / Discard
        public static event Action<int, int> OnDeckChanged;
        public static void RaiseDeckChanged(int drawCount, int discardCount) => OnDeckChanged?.Invoke(drawCount, discardCount);

        // Turn Phase  (Core.TurnPhase 사용)
        public static event Action<SDProject.Core.TurnPhase> OnTurnPhaseChanged;
        public static void RaiseTurnPhaseChanged(SDProject.Core.TurnPhase phase) => OnTurnPhaseChanged?.Invoke(phase);

        // Party AP
        public static event Action<int, int> OnPartyAPChanged;
        public static void RaisePartyAPChanged(int cur, int max) => OnPartyAPChanged?.Invoke(cur, max);
    }
}
```

## Assets\SDProject\Scripts\Core\SDProject.Core.asmdef
```csharp
{
  "name": "SDProject.Core",
  "rootNamespace": "SDProject.Core",
  "references": []
}
```

## Assets\SDProject\Scripts\Core\TurnPhase.cs
```csharp
namespace SDProject.Core
{
    public enum TurnPhase
    {
        None = 0,
        PlayerTurn = 10,
        PlayerActing = 11,
        EnemyTurn = 20,
    }
}
```

## Assets\SDProject\Scripts\Data\BattleConfig.cs
```csharp
using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Battle Config", fileName = "BattleConfig")]
    public class BattleConfig : ScriptableObject
    {
        [Header("Party")]
        [Min(1)] public int partySize = 3;
        [Min(0)] public int partyAPMax = 3;

        [Header("Enemies")]
        [Min(1)] public int enemyMaxCount = 5;

        [Header("Rules")]
        public bool allowPositioning = true;
    }
}
```

## Assets\SDProject\Scripts\Data\CardData.cs
```csharp
using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Card", fileName = "Card_")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string cardId;
        public string displayName;

        [Header("Cost")]
        [Min(0)] public int apCost = 1;

        // (선택) 나중에 타입/타깃/효과 JSON 등을 여기에 확장합니다.
        // public CardType type;
        // public TargetType targetType;
        // [TextArea] public string EffectsJSON;
    }
}
```

## Assets\SDProject\Scripts\Data\DeckList.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Deck List", fileName = "DeckList")]
    public class DeckList : ScriptableObject
    {
        // 초기 덱 구성 리스트(Inspector에서 CardData들을 드래그해서 채우세요)
        public List<CardData> cards = new();
    }
}
```

## Assets\SDProject\Scripts\Data\DeckSourceSOAdapter.cs
```csharp
// Assets/SDProject/Scripts/Data/DeckSourceSOAdapter.cs
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataBridge
{
    public sealed class DeckSourceSOAdapter : MonoBehaviour, IDeckSource
    {
        [Header("ScriptableObject Source")]
        [SerializeField] private SDProject.Data.DeckList _deckList;

        // 디버그용 공개 프로퍼티
        public string DebugDeckName => _deckList ? _deckList.name : "(null)";
        public int DebugDeckCount => (_deckList != null && _deckList.cards != null) ? _deckList.cards.Count : -1;

        public IReadOnlyList<SDProject.Data.CardData> GetInitialDeck()
        {
            if (_deckList == null)
            {
                Debug.LogWarning("[DeckSourceSOAdapter] DeckList not assigned.");
                return System.Array.Empty<SDProject.Data.CardData>();
            }

            if (_deckList.cards == null || _deckList.cards.Count == 0)
            {
                Debug.LogWarning($"[DeckSourceSOAdapter] '{_deckList.name}'.cards is empty.");
                return System.Array.Empty<SDProject.Data.CardData>();
            }

            var list = new List<SDProject.Data.CardData>(_deckList.cards.Count);
            foreach (var c in _deckList.cards)
            {
                if (c == null)
                {
                    Debug.LogWarning("[DeckSourceSOAdapter] Null card skipped.");
                    continue;
                }
                list.Add(c);
            }

            Debug.Log($"[DeckSourceSOAdapter] Loaded {list.Count} card(s) from '{_deckList.name}'.");
            return list;
        }
    }
}
```

## Assets\SDProject\Scripts\Data\IDeckSource.cs
```csharp
using System.Collections.Generic;
using SDProject.Data;

namespace SDProject.DataBridge
{
    /// <summary>
    /// Read-only source for initial deck (e.g., from DeckList ScriptableObject).
    /// </summary>
    public interface IDeckSource
    {
        IReadOnlyList<CardData> GetInitialDeck();
    }
}
```

## Assets\SDProject\Scripts\Data\SDProject.Data.asmdef
```csharp
{
  "name": "SDProject.Data",
  "rootNamespace": "SDProject.Data",
  "references": []
}
```

## Assets\SDProject\Scripts\DataTable\Editor\ExcelEncodingInit.cs
```csharp
#if UNITY_EDITOR
using System.Text;
using UnityEditor;

namespace SDProject.DataTable.Editor
{
    [InitializeOnLoad]
    public static class ExcelEncodingInit
    {
        static ExcelEncodingInit()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
#endif
```

## Assets\SDProject\Scripts\DataTable\Editor\XlsxAssetPostprocessor.cs
```csharp
// File: Assets/SDProject/Scripts/DataTable/Editor/XlsxAssetPostprocessor.cs
// Fix: remove bogus XamlRootSafe reference; use XlsxRoot directly.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ExcelDataReader;
using UnityEditor;
using UnityEngine;

// System.Data alias (name collision safety)
using DataTableType = global::System.Data.DataTable;
using DataColumnType = global::System.Data.DataColumn;
using DataRowType = global::System.Data.DataRow;

namespace SDProject.DataTable.Editor
{
    public class XlsxAssetPostprocessor : AssetPostprocessor
    {
        // XlsxAssetPostprocessor.cs - OnPostprocessAllAssets 안

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var targets = new List<string>();

            bool IsXlsxUnderRoot(string path)
            {
                var norm = path.Replace('\\', '/');
                return norm.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                    && norm.StartsWith(SDProject.DataTable.DataTablePaths.XlsxRoot, StringComparison.Ordinal);
            }

            void Consider(string path)
            {
                if (IsXlsxUnderRoot(path))
                    targets.Add(path.Replace('\\', '/'));
            }

            foreach (var p in imported) Consider(p);
            foreach (var p in moved) Consider(p);

            foreach (var xlsxPath in targets.Distinct())
            {
                try
                {
                    ConvertOne(xlsxPath, out var tableId, out var headers, out var rowCount);
                    Debug.Log($"[DataTable] Converted '{tableId}.xlsx' → CSV. Rows={rowCount}, Headers=[{string.Join(", ", headers)}], HeaderHash={HeaderHash(headers)}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataTable] Conversion FAILED for '{xlsxPath}': {ex.Message}");
                }
            }
        }


        private static void ConvertOne(string xlsxPath, out string tableId, out List<string> headers, out int rowCount)
        {
            string tid = Path.GetFileNameWithoutExtension(xlsxPath);
            tableId = tid;

            headers = new List<string>();
            rowCount = 0;

            var reg = LoadRegistry();
            TableSchema schema = null;
            string sheetName = null;

            if (reg != null && reg.entries != null)
            {
                var entry = reg.entries.FirstOrDefault(e =>
                    e != null &&
                    !string.IsNullOrWhiteSpace(e.tableId) &&
                    string.Equals(e.tableId, tid, StringComparison.Ordinal));

                if (entry != null)
                {
                    sheetName = string.IsNullOrWhiteSpace(entry.sheetName) ? null : entry.sheetName;
                    schema = entry.schema;
                }
            }

            using var stream = File.Open(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            DataTableType sheet = null;
            if (!string.IsNullOrEmpty(sheetName))
            {
                sheet = dataSet.Tables.Cast<DataTableType>()
                    .FirstOrDefault(t => string.Equals(t.TableName, sheetName, StringComparison.Ordinal));
                if (sheet == null)
                    throw new Exception($"Sheet '{sheetName}' not found in '{tid}.xlsx'.");
            }
            else
            {
                if (dataSet.Tables.Count == 0) throw new Exception("No sheets found.");
                if (dataSet.Tables.Count > 1)
                    Debug.LogWarning($"[DataTable] Multiple sheets in {tid}.xlsx; using first: '{dataSet.Tables[0].TableName}'.");
                sheet = (DataTableType)dataSet.Tables[0];
            }

            headers = sheet.Columns.Cast<DataColumnType>()
                .Select(c => (c.ColumnName ?? "").Trim()).ToList();

            if (headers.Count == 0) throw new Exception("Header row is empty.");
            if (headers.Count != headers.Distinct(StringComparer.Ordinal).Count())
                throw new Exception("Duplicate headers detected.");

            var genPath = DataTablePaths.GetGeneratedCsvPath(tid);
            Directory.CreateDirectory(Path.GetDirectoryName(genPath));
            using var sw = new StreamWriter(genPath, false, new UTF8Encoding(false));

            sw.WriteLine(ToCsvLine(headers));

            foreach (DataRowType row in sheet.Rows)
            {
                var cells = new string[headers.Count];
                for (int i = 0; i < headers.Count; i++)
                {
                    var v = row[i];
                    var s = (v == null || v == DBNull.Value) ? "" : v.ToString();
                    cells[i] = s ?? "";
                }

                if (schema != null) ValidateRowAgainstSchema(schema, headers, cells);

                sw.WriteLine(ToCsvLine(cells));
                rowCount++;
            }

            AssetDatabase.ImportAsset(ToRelativeAssetPath(genPath));
            EditorApplication.delayCall += TryHotReload;
        }

        private static void ValidateRowAgainstSchema(TableSchema schema, List<string> headers, string[] cells)
        {
            foreach (var col in schema.columns)
                if (col.required && !headers.Contains(col.name, StringComparer.Ordinal))
                    throw new Exception($"Required column '{col.name}' missing (schema={schema.name}).");

            for (int i = 0; i < headers.Count; i++)
            {
                if (!schema.TryGetColumn(headers[i], out var col)) continue;

                var value = (i < cells.Length) ? (cells[i] ?? "") : "";
                if (string.IsNullOrEmpty(value))
                {
                    if (col.required && schema.validationLevel == ValidationLevel.Strict)
                        throw new Exception($"Column '{col.name}' required but empty (Strict).");
                    continue;
                }

                switch (col.type)
                {
                    case ColumnType.Int:
                        if (!int.TryParse(value, out _)) FailByLevel(schema, $"Column '{col.name}' expects Int, got '{value}'.");
                        break;
                    case ColumnType.Float:
                        if (!float.TryParse(value, out _)) FailByLevel(schema, $"Column '{col.name}' expects Float, got '{value}'.");
                        break;
                    case ColumnType.Bool:
                        if (!bool.TryParse(value, out _)) FailByLevel(schema, $"Column '{col.name}' expects Bool, got '{value}'.");
                        break;
                    case ColumnType.Flags:
                        if (!int.TryParse(value, out _)) FailByLevel(schema, $"Column '{col.name}' expects Flags(Int), got '{value}'.");
                        break;
                    case ColumnType.String:
                    case ColumnType.Enum:
                    case ColumnType.Ref:
                        break;
                }
            }
        }

        private static void FailByLevel(TableSchema schema, string message)
        {
            if (schema.validationLevel == ValidationLevel.Lenient) Debug.LogWarning($"[DataTable][Lenient] {message}");
            else throw new Exception(message);
        }

        private static string ToCsvLine(IReadOnlyList<string> cells)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var s = cells[i] ?? "";
                bool quote = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
                if (quote) sb.Append('"').Append(s.Replace("\"", "\"\"")).Append('"');
                else sb.Append(s);
            }
            return sb.ToString();
        }

        private static string HeaderHash(List<string> headers)
        {
            var sorted = headers.OrderBy(h => h, StringComparer.Ordinal);
            var text = string.Join("|", sorted);
            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private static string ToRelativeAssetPath(string absolute)
        {
            var p = absolute.Replace('\\', '/');
            var idx = p.IndexOf("Assets/", StringComparison.Ordinal);
            return (idx >= 0) ? p.Substring(idx) : p;
        }

        private static TableRegistry LoadRegistry()
        {
            var guids = AssetDatabase.FindAssets("t:SDProject.DataTable.TableRegistry");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TableRegistry>(path);
        }

        private static void TryHotReload()
        {
            if (!Application.isPlaying) return;
            var loader = UnityEngine.Object.FindFirstObjectByType<DataTableLoader>();
            if (loader != null) loader.Reload();
        }
    }
}
#endif
```

## Assets\SDProject\Scripts\DataTable\Runtime\CsvLite.cs
```csharp
using System.Collections.Generic;
using System.Text;

namespace SDProject.DataTable
{
    public static class CsvLite
    {
        public struct CsvData
        {
            public List<string> headers;
            public List<string[]> rows;
        }

        public static CsvData ParseWithHeader(string csv)
        {
            var data = new CsvData { headers = new List<string>(), rows = new List<string[]>() };
            if (string.IsNullOrEmpty(csv)) return data;

            var lines = SplitLines(csv);
            if (lines.Count == 0) return data;

            data.headers = ParseRow(lines[0]);

            for (int i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                data.rows.Add(ParseRow(lines[i]).ToArray());
            }
            return data;
        }

        private static List<string> SplitLines(string text)
        {
            var list = new List<string>();
            using (var reader = new System.IO.StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    list.Add(line);
            }
            return list;
        }

        private static List<string> ParseRow(string line)
        {
            var cells = new List<string>();
            if (line == null) return cells;

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else inQuotes = !inQuotes;
                }
                else if (ch == ',' && !inQuotes)
                {
                    cells.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(ch);
            }
            cells.Add(sb.ToString());
            return cells;
        }
    }
}
```

## Assets\SDProject\Scripts\DataTable\Runtime\DataTableLoader.cs
```csharp
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SDProject.DataTable
{
    public class DataTableLoader : MonoBehaviour
    {
        public enum LoadState { Idle, Loading, Completed, Failed }

        [SerializeField] private TableRegistry registry;
        [SerializeField] private LoadState state = LoadState.Idle;
        [SerializeField] private int loadedCount;
        [SerializeField] private string lastMessage;

        public event Action<string> OnTableLoaded;
        public event Action OnAllTablesLoaded;
        public event Action<string> OnLoadFailed;

        public LoadState State => state;
        public int LoadedCount => loadedCount;

        private void Awake()
        {
            if (registry == null)
            {
                Fail("Registry not assigned.");
                return;
            }
            StartCoroutine(LoadAllRoutine());
        }

        public void Reload()
        {
            if (state == LoadState.Loading) return;
            StartCoroutine(LoadAllRoutine());
        }

        private IEnumerator LoadAllRoutine()
        {
            SetState(LoadState.Loading, "Begin loading tables.");
            loadedCount = 0;

            var entries = registry.entries
                .Where(e => e != null && e.enabled && !string.IsNullOrWhiteSpace(e.tableId))
                .OrderBy(e => e.order);

            foreach (var e in entries)
            {
                var key = DataTablePaths.GetResourcesKey(e.tableId);
                var ta = Resources.Load<TextAsset>(key);
                if (ta == null)
                {
                    Fail($"Missing generated TextAsset at Resources/{key}");
                    yield break;
                }

                // Instantiate a specific TableAsset when available: {tableId}Table : TableAsset
                var tableAsset = CreateTableAssetForId(e.tableId);
                try
                {
                    tableAsset.Apply(ta.text);
                    TableHub.Register(e.tableId, tableAsset);
                    loadedCount++;
                    Debug.Log($"[DataTableLoader] Loaded '{e.tableId}' into {tableAsset.GetType().Name}.");
                    OnTableLoaded?.Invoke(e.tableId);
                }
                catch (Exception ex)
                {
                    Fail($"Apply failed for '{e.tableId}': {ex.Message}");
                    yield break;
                }

                yield return null; // distribute frame cost
            }

            SetState(LoadState.Completed, $"Loaded {loadedCount} tables.");
            OnAllTablesLoaded?.Invoke();
        }

        private TableAsset CreateTableAssetForId(string tableId)
        {
            var typeName = tableId + "Table";
            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => SafeTypes(a))
                .FirstOrDefault(t => typeof(TableAsset).IsAssignableFrom(t) && t.Name == typeName);

            var instance = (TableAsset)(type != null
                ? ScriptableObject.CreateInstance(type)
                : ScriptableObject.CreateInstance<GenericCsvTable>());

            instance.name = typeName;
            return instance;
        }

        private static Type[] SafeTypes(Assembly a)
        {
            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
        }

        private void SetState(LoadState s, string message)
        {
            state = s;
            lastMessage = message;
            Debug.Log($"[DataTableLoader] State={s}. {message}");
        }

        private void Fail(string reason)
        {
            state = LoadState.Failed;
            lastMessage = reason;
            Debug.LogError($"[DataTableLoader] FAILED: {reason}");
            OnLoadFailed?.Invoke(reason);
        }
    }
}
```

## Assets\SDProject\Scripts\DataTable\Runtime\DataTablePaths.cs
```csharp
using UnityEngine;

namespace SDProject.DataTable
{
    public static class DataTablePaths
    {
        // Authoring .xlsx location
        public const string XlsxRoot = "Assets/SDProject/DataTable/Xlsx";

        // Generated .csv/.json under Resources
        // => Resources.Load<TextAsset>($"SDProject/DataTableGen/{tableId}")
        public const string ResourcesGenRoot = "Assets/SDProject/DataTable/Resources/DataTableGen";
        public const string ResourcesKeyPrefix = "DataTableGen/";

        // Optional place to create schema/table assets
        public const string SchemasRoot = "Assets/SDProject/DataTable/Schemas";
        public const string TablesRoot = "Assets/SDProject/DataTable/Tables";

        public static string GetXlsxPath(string tableId) =>
            $"{XlsxRoot}/{tableId}.xlsx";

        public static string GetGeneratedCsvPath(string tableId) =>
            $"{ResourcesGenRoot}/{tableId}.csv";

        public static string GetResourcesKey(string tableId) =>
            $"{ResourcesKeyPrefix}{tableId}";
    }
}
```

## Assets\SDProject\Scripts\DataTable\Runtime\GenericCsvTable.cs
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataTable
{
    [CreateAssetMenu(menuName = "SDProject/DataTable/Generic CSV Table", fileName = "GenericCsvTable")]
    public class GenericCsvTable : TableAsset
    {
        [SerializeField] private List<string> headers = new List<string>();
        [SerializeField] private List<string[]> rows = new List<string[]>();
        private Dictionary<string, int> headerIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyList<string> Headers => headers;
        public int RowCount => rows.Count;
        public string Get(int row, string header)
        {
            if (!headerIndex.TryGetValue(header, out var idx)) return string.Empty;
            if (row < 0 || row >= rows.Count) return string.Empty;
            var arr = rows[row];
            return (idx >= 0 && idx < arr.Length) ? arr[idx] : string.Empty;
        }

        public override void Apply(string rawText)
        {
            headers.Clear();
            rows.Clear();
            headerIndex.Clear();

            var parsed = CsvLite.ParseWithHeader(rawText);
            headers.AddRange(parsed.headers);
            for (int i = 0; i < headers.Count; i++)
                headerIndex[headers[i]] = i;

            rows.AddRange(parsed.rows);
            Debug.Log($"[GenericCsvTable] Applied. Rows={rows.Count}, Cols={headers.Count}");
        }
    }
}
```

## Assets\SDProject\Scripts\DataTable\Runtime\TableAsset.cs
```csharp
using UnityEngine;

namespace SDProject.DataTable
{
    public abstract class TableAsset : ScriptableObject
    {
        [TextArea][SerializeField] private string debugNote;
        public virtual string DebugNote => debugNote;

        /// Apply raw text (CSV/JSON/etc) into internal structures.
        public abstract void Apply(string rawText);
    }
}
```

## Assets\SDProject\Scripts\DataTable\Runtime\TableHub.cs
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.DataTable
{
    public static class TableHub
    {
        private static readonly Dictionary<string, TableAsset> _tables =
            new Dictionary<string, TableAsset>(StringComparer.Ordinal);

        public static void Register(string tableId, TableAsset asset)
        {
            _tables[tableId] = asset;
        }

        public static TableAsset Get(string tableId)
        {
            _tables.TryGetValue(tableId, out var a);
            return a;
        }

        public static T Get<T>(string tableId) where T : TableAsset
        {
            var a = Get(tableId);
            return a as T;
        }

        public static IReadOnlyDictionary<string, TableAsset> All => _tables;
    }
}
```

## Assets\SDProject\Scripts\UI\AssignFirstAllyCaster.cs
```csharp
// Assets/SDProject/Scripts/UI/Controls/AssignFirstAllyCaster.cs
using SDProject.Combat;
using SDProject.Combat.Cards;
using SDProject.Combat.Board;  // TeamSide enum
using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CardView))]
public class AssignFirstAllyCaster : MonoBehaviour
{
    [Tooltip("Wait a frame so binders can register to BoardRuntime first.")]
    [SerializeField] private bool waitOneFrame = true;

    [Tooltip("If true, pick the ally with the highest slot index (front-most in our layout).")]
    [SerializeField] private bool preferFrontMost = true;

    private CardView _cv;

    private void Awake()
    {
        _cv = GetComponent<CardView>();
    }

    private void OnEnable()
    {
        if (waitOneFrame) StartCoroutine(AssignNextFrame());
        else TryAssignNow();
    }

    private IEnumerator AssignNextFrame()
    {
        // 슬롯 등록(바인더) 타이밍 보정을 위해 한 프레임 대기
        yield return null;
        TryAssignNow();
    }

    private void TryAssignNow()
    {
        // 이미 수동으로 Caster가 지정되어 있으면 아무것도 하지 않음
        if (_cv.Caster != null) return;

        // 씬에서 Ally 팀의 SimpleUnitBinder들을 수집
        var binders = FindObjectsByType<SimpleUnitBinder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(b => b != null && b.Team == TeamSide.Ally)
            .ToList();

        if (binders.Count == 0)
        {
            Debug.LogWarning("[AssignFirstAllyCaster] No Ally binders found in scene.");
            return;
        }

        // 캐스터 후보 선택 규칙:
        // 1) preferFrontMost=true 이면 index가 큰 순서(Front가 큰 인덱스) 우선
        // 2) 아니면 index가 작은 순서
        var ordered = preferFrontMost
            ? binders.OrderByDescending(b => b.Index)
            : binders.OrderBy(b => b.Index);

        // SimpleUnit 컴포넌트가 있는 첫 후보를 캐스터로 사용
        foreach (var b in ordered)
        {
            var su = b.GetComponent<SimpleUnit>();
            if (su != null)
            {
                _cv.Caster = su.gameObject;
                Debug.Log($"[AssignFirstAllyCaster] Caster assigned: {su.name} (team={b.Team}, index={b.Index})");
                return;
            }
        }

        Debug.LogWarning("[AssignFirstAllyCaster] Ally binders found, but no SimpleUnit component attached.");
    }
}
```

## Assets\SDProject\Scripts\UI\BattleHUD.cs
```csharp
using SDProject.Combat;
using TMPro;
using UnityEngine;
using SDProject.Core;
using SDProject.Core.Messaging;

namespace SDProject.UI
{
    /// <summary>
    /// Displays AP / Turn Phase / Hand count via TextMeshPro.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtAP;
        [SerializeField] private TMP_Text txtPhase;
        [SerializeField] private TMP_Text txtHand;

        private void OnEnable()
        {
            GameEvents.OnPartyAPChanged += OnAP;
            GameEvents.OnTurnPhaseChanged += OnPhase;
            GameEvents.OnHandChanged += OnHand;
        }

        private void OnDisable()
        {
            GameEvents.OnPartyAPChanged -= OnAP;
            GameEvents.OnTurnPhaseChanged -= OnPhase;
            GameEvents.OnHandChanged -= OnHand;
        }

        private void OnAP(int cur, int max)
        {
            if (txtAP) txtAP.text = $"AP {cur}/{max}";
        }

        private void OnPhase(TurnPhase phase)
        {
            if (txtPhase) txtPhase.text = phase.ToString();
        }

        private void OnHand(int count)
        {
            if (txtHand) txtHand.text = $"Hand {count}";
        }
    }
}
```

## Assets\SDProject\Scripts\UI\CardItemView.cs
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SDProject.Combat;
using SDProject.Data;

namespace SDProject.UI
{
    /// <summary>
    /// Displays a single card in hand. Title/cost are read dynamically (if present).
    /// </summary>
    public class CardItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text cost;
        [SerializeField] private Button button;

        private CardData _data;
        private HandRuntime _hand;

        public void Bind(CardData data, HandRuntime hand)
        {
            _data = data;
            _hand = hand;

            // Title: try common names, fallback to asset name
            if (title)
                title.text = TryGetStringByAnyName(data, out var t, "title", "Title", "displayName", "name")
                    ? t
                    : data ? data.name : "Card";

            // Cost: try common names; hide text if not found
            if (cost)
            {
                if (TryGetIntByAnyName(data, out var c, "cost", "Cost", "apCost", "AP", "Ap"))
                    cost.text = $"AP {c}";
                else
                    cost.text = string.Empty;
            }

            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (_data != null && _hand != null)
                _hand.Use(_data);   // 🔴 변경점: Remove -> Use
        }

        // ---- helpers ----
        private static bool TryGetIntByAnyName(object obj, out int value, params string[] names)
        {
            value = 0;
            if (obj == null) return false;
            var t = obj.GetType();

            // fields first
            foreach (var n in names)
            {
                var f = t.GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(int))
                {
                    value = (int)f.GetValue(obj);
                    return true;
                }
            }
            // then properties
            foreach (var n in names)
            {
                var p = t.GetProperty(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(int) && p.CanRead)
                {
                    value = (int)p.GetValue(obj);
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetStringByAnyName(object obj, out string value, params string[] names)
        {
            value = null;
            if (obj == null) return false;
            var t = obj.GetType();

            foreach (var n in names)
            {
                var f = t.GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(string))
                {
                    value = (string)f.GetValue(obj);
                    return true;
                }
            }
            foreach (var n in names)
            {
                var p = t.GetProperty(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(string) && p.CanRead)
                {
                    value = (string)p.GetValue(obj);
                    return true;
                }
            }
            return false;
        }

    }
}
```

## Assets\SDProject\Scripts\UI\HandView.cs
```csharp
// Assets/SDProject/Scripts/UI/HandView.cs
using UnityEngine;
using UnityEngine.UI;
using SDProject.Core.Messaging;
using SDProject.Combat;

namespace SDProject.UI
{
    /// <summary>HandRuntime를 화면에 그려주는 단순 뷰.</summary>
    public class HandView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private HandRuntime hand;                 // 씬의 HandRuntime
        [SerializeField] private RectTransform content;            // HandPanel의 RectTransform
        [SerializeField] private GameObject cardItemPrefab;        // CardItem.prefab (UI용)

        private void OnEnable()
        {
            GameEvents.OnHandChanged += OnHandChanged;
            // 초기 진입 시 현재 핸드로 그려주기
            if (hand != null) Render(hand);
        }

        private void OnDisable()
        {
            GameEvents.OnHandChanged -= OnHandChanged;
        }

        private void OnHandChanged(int _)
        {
            if (hand != null) Render(hand);
        }

        public void Render(HandRuntime h)
        {
            if (!content || !cardItemPrefab || h == null)
            {
                Debug.LogWarning("[HandView] Missing refs: content/prefab/hand", this);
                return;
            }

            // 1) 기존 자식 제거
            for (int i = content.childCount - 1; i >= 0; --i)
                Destroy(content.GetChild(i).gameObject);

            // 2) 카드 UI 생성
            foreach (var card in h.Cards)
            {
                var go = Instantiate(cardItemPrefab);
                go.transform.SetParent(content, false);         // ★ worldPositionStays = false
                go.transform.localScale = Vector3.one;

                // 카드 프리팹 크기 보장 (찌그러짐 방지)
                var le = go.GetComponent<LayoutElement>();
                if (!le) le = go.AddComponent<LayoutElement>();
                if (le.preferredWidth <= 0) le.preferredWidth = 280f;
                if (le.preferredHeight <= 0) le.preferredHeight = 320f;

                // (선택) 카드 내용 바인딩
                var view = go.GetComponent<CardItemView>();
                if (view) view.Bind(card, h);
            }
        }
    }
}
```

## Assets\SDProject\Scripts\UI\PileCounterView.cs
```csharp
using TMPro;
using UnityEngine;
using SDProject.Combat;
using SDProject.Core.Messaging;

namespace SDProject.UI
{
    /// <summary>
    /// Shows a title and a count; subscribes to GameEvents.OnDeckChanged.
    /// SRP: display-only.
    /// </summary>
    public class PileCounterView : MonoBehaviour
    {
        [SerializeField] private string title = "Deck";
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private TMP_Text txtCount;
        [SerializeField] private DeckRuntime deck; // optional; only for null-check/logs

        private void OnEnable()
        {
            if (txtTitle) txtTitle.text = title;
            GameEvents.OnDeckChanged += HandleDeckChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnDeckChanged -= HandleDeckChanged;
        }

        private void Start()
        {
            // 초기 표기 강제 갱신 (deck 참조 없어도 이벤트만으로 동작함)
            HandleDeckChanged(deck ? deck.DrawCount : 0, deck ? deck.DiscardCount : 0);
        }

        private void HandleDeckChanged(int drawCount, int discardCount)
        {
            if (!txtCount) return;
            txtCount.text = (title == "Deck") ? $"{drawCount}" : $"{discardCount}";
        }

        // For editor convenience
        public void SetTitle(string t)
        {
            title = t;
            if (txtTitle) txtTitle.text = title;
        }
    }
}
```

## Assets\SDProject\Scripts\UI\SDProject.UI.asmdef
```csharp
{
    "name": "SDProject.UI",
    "rootNamespace": "SDProject.UI",
    "references": [
        "SDProject.Core",
        "SDProject.Combat",
        "Unity.TextMeshPro",
        "Unity.InputSystem",
        "SDProject.Data"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

