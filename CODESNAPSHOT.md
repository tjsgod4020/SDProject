# Code Snapshot - 2025-10-16 15:00:00
Commit: b92c4dc

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
            UnityEngine.Object.FindFirstObjectByType<SDProject.Combat.Cards.BoardRuntime>(FindObjectsInactive.Include)?.RefreshFromScene();
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

## Assets\SDProject\Scripts\Combat\Board\TeamSide.cs
```csharp
// Assets/SDProject/Scripts/Combat/Board/TeamSide.cs
namespace SDProject.Combat.Board
{
    public enum TeamSide { Ally, Enemy }
}
```

## Assets\SDProject\Scripts\Combat\Cards\Board\BoardRuntime.cs
```csharp
// Assets/SDProject/Scripts/Combat/Cards/Core/BoardRuntime.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Runtime view over CharacterSlot[] + Unit occupancy.
    /// - Builds slot lists after BoardLayout finished (next frame).
    /// - Exposes RefreshFromScene() to rebuild on demand.
    /// - Logs counts for quick diagnosis.
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

        private bool _builtOnce;

        private void OnEnable()
        {
            // 슬롯 생성(보통 BoardLayout)이 끝난 다음 프레임에 스캔
            StartCoroutine(CoBuildNextFrame());
        }

        private IEnumerator CoBuildNextFrame()
        {
            yield return null;
            RefreshFromScene();
        }

        /// <summary>
        /// Public: 외부(예: BoardLayout 끝부분)에서 호출해 강제로 재스캔.
        /// </summary>
        public void RefreshFromScene()
        {
            _allySlots.Clear();
            _enemySlots.Clear();

            var slots = FindObjectsByType<CharacterSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in slots)
            {
                if (!s) continue;
                if (s.Team == TeamSide.Ally) _allySlots.Add(s);
                else _enemySlots.Add(s);
            }
            _allySlots.Sort((a, b) => a.Index.CompareTo(b.Index));
            _enemySlots.Sort((a, b) => a.Index.CompareTo(b.Index));
            _builtOnce = true;

            Debug.Log($"[BoardRuntime] Slots built. Ally={_allySlots.Count}, Enemy={_enemySlots.Count}", this);
        }

        // ==== Register / Query ====

        public void RegisterUnit(GameObject unit, TeamSide team, int index)
        {
            var key = (team, index);
            _occ[key] = unit;

            // Try assign CurrentSlot if the unit has SimpleUnit (선택적)
            var su = unit.GetComponent<SimpleUnit>();
            if (su != null)
            {
                su.Team = team;
                su.Index = index;
                su.CurrentSlot = GetSlot(team, index);
            }

            Debug.Log($"[BoardRuntime] RegisterUnit: {unit.name} -> {team}[{index}]", unit);
        }

        public void UnregisterUnit(GameObject unit, TeamSide team, int index)
        {
            var key = (team, index);
            if (_occ.TryGetValue(key, out var u) && u == unit)
            {
                _occ.Remove(key);
                Debug.Log($"[BoardRuntime] UnregisterUnit: {unit.name} <- {team}[{index}]", unit);
            }
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

        /// <summary>
        /// First alive enemy by priority: Front -> Mid1 -> Mid2 -> Mid3 -> Back
        /// </summary>
        public GameObject GetFrontMostEnemyUnit()
        {
            if (!_builtOnce)
            {
                Debug.LogWarning("[BoardRuntime] GetFrontMostEnemyUnit called before slots were built. Forcing refresh.", this);
                RefreshFromScene();
            }

            foreach (var s in _enemySlots)
            {
                var u = GetOccupant(TeamSide.Enemy, s.Index);
                if (u == null) continue;
                var hp = u.GetComponent<IDamageable>();
                if (hp == null || !hp.IsAlive()) continue;
                return u;
            }
            Debug.LogWarning("[BoardRuntime] No valid enemy unit found in any enemy slots.", this);
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
            int targetIdx = idx + cells; // "back" has higher index

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

## Assets\SDProject\Scripts\Combat\Cards\Bridge\HandCardPlayAdapter.cs
```csharp
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
```

## Assets\SDProject\Scripts\Combat\Cards\Core\CardPlayController.cs
```csharp
// Assets/SDProject/Scripts/Combat/Cards/Core/CardPlayController.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SDProject.Data;
using SDProject.Combat.Board;
using SDProject.Core.Messaging;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Handles card play flow: select → target → resolve.
    /// Uses existing TargetType + PosHit (lane mask).
    /// </summary>
    public sealed class CardPlayController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BoardRuntime _board;
        [SerializeField] private TargetingSystem _targeting;

        [Header("UI (optional)")]
        [SerializeField] private TMP_Text _errorLabel; // fixed text v1 (optional)

        private IState _state;
        private IdleState _idle;
        private SelectingTargetsState _selecting;
        private ResolvingState _resolving;

        // working context
        private CardData _curCard;
        private GameObject _curCaster;
        private readonly List<GameObject> _curTargets = new();

        private void Awake()
        {
            _idle = new IdleState(this);
            _selecting = new SelectingTargetsState(this);
            _resolving = new ResolvingState(this);
            TransitionTo(_idle);
        }

        public void PlayCard(CardData card, GameObject caster)
        {
            if (card == null || caster == null)
            {
                EmitError(ErrorLabel.ERR_COND, "Null card/caster");
                return;
            }
            _curCard = card;
            _curCaster = caster;
            TransitionTo(_selecting);
        }

        private void Update() => _state?.Tick(Time.deltaTime);

        private void TransitionTo(IState next)
        {
            _state?.Exit();
            _state = next;
            _state?.Enter();
        }

        private void ClearError() { if (_errorLabel) _errorLabel.text = string.Empty; }

        public enum ErrorLabel { ERR_AP, ERR_COND, ERR_POS_HIT, ERR_NO_TARGET }

        public void EmitError(ErrorLabel code, string detail = null)
        {
            string msg = code switch
            {
                ErrorLabel.ERR_AP => "AP 부족",
                ErrorLabel.ERR_COND => "사용 조건 불일치",
                ErrorLabel.ERR_POS_HIT => "타격 위치 미일치",
                _ => "대상 없음"
            };
            if (!string.IsNullOrEmpty(detail)) msg += $" ({detail})";
            if (_errorLabel) _errorLabel.text = msg;
            Debug.LogWarning($"[CardPlay][{code}] {detail ?? ""}");
        }

        // ─────────────────── States ───────────────────
        private interface IState { void Enter(); void Tick(float dt); void Exit(); }

        private class IdleState : IState
        {
            private readonly CardPlayController c;
            public IdleState(CardPlayController ctx) => c = ctx;
            public void Enter() { c.ClearError(); }
            public void Tick(float dt) { }
            public void Exit() { }
        }

        private class SelectingTargetsState : IState
        {
            private readonly CardPlayController c;
            public SelectingTargetsState(CardPlayController ctx) => c = ctx;

            public void Enter()
            {
                c.ClearError();
                c._curTargets.Clear();

                // Quick AP check placeholder (v1: assume enough AP or check via event/system)
                // If AP system integrated, gate here and EmitError(ERR_AP).

                // Derive team/mode from TargetType (minimal mapping for common cases)
                c.ResolveTargetsOrBeginManual();
            }

            public void Tick(float dt) { }
            public void Exit() { }
        }

        private class ResolvingState : IState
        {
            private readonly CardPlayController c;
            public ResolvingState(CardPlayController ctx) => c = ctx;

            public void Enter()
            {
                // Execute EffectsJSON with current targets
                c.ExecuteEffects(c._curCard, c._curCaster, c._curTargets);

                // TODO: AP consume here if integrated; then raise HUD events
                GameEvents.RaiseTurnPhaseChanged(SDProject.Core.TurnPhase.PlayerActing);

                c.TransitionTo(c._idle);
            }

            public void Tick(float dt) { }
            public void Exit() { }
        }

        // ────────────────── Targeting ─────────────────
        private void ResolveTargetsOrBeginManual()
        {
            if (_board == null) { EmitError(ErrorLabel.ERR_COND, "Board missing"); TransitionTo(_idle); return; }
            if (_curCard == null || _curCaster == null) { EmitError(ErrorLabel.ERR_COND); TransitionTo(_idle); return; }

            // Minimal map: treat common target types; fallback to manual single.
            var (team, mode, all) = GuessTargeting(_curCard);

            // Lane mask filter using PosHit
            var mask = _curCard.PosHit; // assume PositionFlags-style flags in SO

            if (mode == TargetMode.Auto)
            {
                var pool = _board.EnumerateUnits(team);
                var filtered = new List<GameObject>();
                foreach (var u in pool)
                {
                    var loc = _board.GetUnitLocation(u);
                    if (loc == null) continue;
                    var lane = BoardRuntime.LaneOf(loc.Value.team, loc.Value.index);
                    if (BoardRuntime.LaneMatches(lane, mask))
                        filtered.Add(u);
                }

                // Sort by lane priority already defined by slot order (Front→Back)
                // EnumerateUnits should return in board order; if not, keep as-is.

                if (filtered.Count == 0)
                {
                    EmitError(ErrorLabel.ERR_NO_TARGET);
                    TransitionTo(_idle);
                    return;
                }

                if (all)
                {
                    _curTargets.AddRange(filtered);
                }
                else
                {
                    // front-most first
                    _curTargets.Add(filtered[0]);
                }
                TransitionTo(_resolving);
            }
            else
            {
                int need = all ? int.MaxValue : 1; // v1 manual only single officially; extend later if N-select added
                _targeting.BeginManualSelect(
                    _curCard, mask, team, need,
                    onDone: (targets) =>
                    {
                        if (targets == null || targets.Count == 0)
                        {
                            EmitError(ErrorLabel.ERR_NO_TARGET);
                            TransitionTo(_idle);
                            return;
                        }
                        _curTargets.Clear();
                        _curTargets.AddRange(targets);
                        TransitionTo(_resolving);
                    },
                    onCancel: () => TransitionTo(_idle)
                );
            }
        }

        private enum TargetMode { Auto, Manual }

        private (TeamSide team, TargetMode mode, bool all) GuessTargeting(CardData card)
        {
            // Heuristic for common built-in types. Adjust names to your enum.
            // Defaults: Enemy + Manual Single
            TeamSide team = TeamSide.Enemy;
            TargetMode mode = TargetMode.Manual;
            bool all = false;

            var tt = card.TargetType.ToString(); // enum → string
            if (tt.Contains("Ally")) team = TeamSide.Ally;
            if (tt.Contains("Enemy")) team = TeamSide.Enemy;
            if (tt.Contains("Self")) { team = team; mode = TargetMode.Auto; all = false; }

            if (tt.Contains("All")) { mode = TargetMode.Auto; all = true; }
            else if (tt.Contains("Front")) { mode = TargetMode.Auto; all = false; }
            else if (tt.Contains("Manual")) { mode = TargetMode.Manual; }

            return (team, mode, all);
        }

        // ─────────────── Effect Execution v1 ───────────────
        public void ExecuteEffects(CardData card, GameObject caster, IReadOnlyList<GameObject> primaryTargets)
        {
            if (card == null || primaryTargets == null || primaryTargets.Count == 0) return;

            // Parse EffectsJSON (minimal dynamic parsing to avoid coupling)
            // Expect common patterns: Damage, Knockback with optional targets.offsets and failPolicy.
            var json = card.EffectsJSON;
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"[Effect] No EffectsJSON on {card.name}");
                return;
            }

            try
            {
                var effects = MiniJson.Parse(json) as List<object>;
                if (effects == null) { Debug.LogWarning($"[Effect] JSON not array: {card.name}"); return; }

                foreach (var e in effects)
                {
                    var dict = e as Dictionary<string, object>;
                    if (dict == null) continue;
                    var type = dict.GetString("type");
                    switch (type)
                    {
                        case "Damage":
                            int value = dict.GetInt("value", 0);
                            var allTargets = ExpandWithOffsets(primaryTargets, dict, caster);
                            int hit = 0;
                            foreach (var t in allTargets) { if (ApplyDamage(t, value)) hit++; }
                            Debug.Log($"[Effect] Damage value={value} targets={hit}");
                            break;

                        case "Knockback":
                            int cells = dict.GetInt("cells", 1);
                            foreach (var t in primaryTargets)
                            {
                                bool ok = _board.TryKnockback(t, cells);
                                if (!ok) Debug.Log($"[Effect] Knockback fail (cells={cells})");
                            }
                            break;

                        default:
                            Debug.Log($"[Effect] Unknown type: {type}");
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Effect] Parse/Exec error: {ex.Message}");
            }
        }

        private List<GameObject> ExpandWithOffsets(IReadOnlyList<GameObject> primaries, Dictionary<string, object> dict, GameObject caster)
        {
            var result = new List<GameObject>(primaries);
            var targets = dict.GetDict("targets");
            if (targets == null) return result;

            var anchor = targets.GetString("anchor", "Primary");
            if (anchor != "Primary") return result;

            var offsets = targets.GetList("offsets");
            if (offsets == null || offsets.Count == 0) return result;

            foreach (var p in primaries)
            {
                var loc = _board.GetUnitLocation(p);
                if (loc == null) continue;
                foreach (var offObj in offsets)
                {
                    int off = (offObj is long lo) ? (int)lo : (offObj is int ii ? ii : 0);
                    int idx = loc.Value.index + off;
                    var slot = _board.GetSlot(loc.Value.team, idx);
                    if (slot == null) { /* failPolicy SkipLog */ continue; }
                    var u = _board.GetOccupant(loc.Value.team, idx);
                    if (u != null) result.Add(u);
                }
            }
            return result;
        }

        private bool ApplyDamage(GameObject unit, int value)
        {
            var hp = unit.GetComponent<IDamageable>();
            if (hp == null) return false;
            hp.TakeDamage(value);
            return true;
        }
    }

    // ─────────── small JSON helpers (MiniJson) ───────────
    internal static class MiniJson
    {
        public static object Parse(string json) => UnityEngine.JsonUtility.FromJson<Wrapper>(Wrap(json))?.items;
        [System.Serializable] private class Wrapper { public List<object> items; }
        private static string Wrap(string arrJson) => $"{{\"items\":{arrJson}}}";
    }

    internal static class DictExt
    {
        public static string GetString(this Dictionary<string, object> d, string k, string def = "")
            => d.TryGetValue(k, out var v) ? v?.ToString() ?? def : def;

        public static int GetInt(this Dictionary<string, object> d, string k, int def = 0)
            => d.TryGetValue(k, out var v) ? (v is long l ? (int)l : v is int i ? i : def) : def;

        public static Dictionary<string, object> GetDict(this Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) ? v as Dictionary<string, object> : null;

        public static List<object> GetList(this Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) ? v as List<object> : null;
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

## Assets\SDProject\Scripts\Combat\Cards\Core\PositionResolver.cs
```csharp
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>Maps (team, index) to logical lane flags per our design.</summary>
    public static class PositionResolver
    {
        // Ally: [Back(0), Mid2(1), Mid1(2), Front(3)]
        // Enemy: [Front(0), Mid1(1), Mid2(2), Mid3(3), Back(4)]
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
            else
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

        public static bool LaneMatches(PositionFlags lane, PositionFlags mask) => (mask & lane) != 0;
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

## Assets\SDProject\Scripts\Combat\Cards\Targeting\TargetingSystem.cs
```csharp
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SDProject.Combat.Cards
{

    [Serializable] public class TargetSelectionRequestEvent : UnityEvent<TargetType> { }
    [Serializable] public class TargetSelectionProvidedEvent : UnityEvent<GameObject[]> { }

    public class TargetingSystem : MonoBehaviour
    {
        public BoardRuntime Board;

        public TargetSelectionRequestEvent OnTargetSelectionRequested = new();
        public TargetSelectionProvidedEvent OnTargetSelectionProvided = new();

        public GameObject[] AutoPickFrontMostEnemy()
        {
            var t = Board?.GetFrontMostEnemyUnit();
            if (t == null) return Array.Empty<GameObject>();
            Debug.Log($"[Targeting] Auto-picked enemy: {t.name}");
            return new[] { t };
        }

        // Hook from UI when the player taps/clicks a valid unit
        public void ProvideManualSingle(GameObject picked)
        {
            if (picked == null) OnTargetSelectionProvided.Invoke(Array.Empty<GameObject>());
            else OnTargetSelectionProvided.Invoke(new[] { picked });
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
    /// Displays card title and AP; clicking the whole card plays it.
    /// CardData 스키마: int AP (확정)
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _ap;          // ← 코스트 표시는 AP 텍스트로 고정
        [SerializeField] private Image _typeIcon;        // (선택) 타입 아이콘 매핑 시 사용

        private CardData _card;
        private GameObject _caster;
        private CardPlayController _play;

        public void Bind(CardData card, GameObject caster, CardPlayController play)
        {
            _card = card;
            _caster = caster;
            _play = play;

            // v1: SO 이름 사용(로컬라이즈는 v1.1에서 전환)
            if (_title) _title.text = card != null ? card.name : "(null)";

            // AP 고정 스키마: CardData.AP (int)
            if (_ap) _ap.text = card != null ? card.AP.ToString() : "-";

            // (선택) 타입 아이콘 매핑:
            // if (_typeIcon) _typeIcon.sprite = ...
        }

        // 카드 루트에 Button을 달아 이 함수를 클릭 이벤트에 연결하거나,
        // EventTrigger로 PointerClick→이 함수를 호출하세요.
        public void OnClick()
        {
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
using SDProject.Data;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Simple hand UI: builds CardView items and wires click to play controller.
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        [SerializeField] private Transform _content;
        [SerializeField] private CardView _cardPrefab;

        public void Rebuild(IReadOnlyList<CardData> hand, GameObject caster, CardPlayController play)
        {
            if (_content == null || _cardPrefab == null) return;

            // clear
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            if (hand == null) return;

            foreach (var c in hand)
            {
                var v = Instantiate(_cardPrefab, _content);
                v.Bind(c, caster, play);
            }
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Targeting\TargetingSystem.cs
```csharp
// Assets/SDProject/Scripts/Combat/Targeting/TargetingSystem.cs
using SDProject.Combat.Board;
using SDProject.Combat.Cards;
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
using SDProject.Core.FSM; // if you have it; otherwise remove
using SDProject.Core.Messaging;
using SDProject.Data;
using SDProject.Combat.Cards;

namespace SDProject.Combat
{
    /// <summary>
    /// Turn hooks: draw at start, discard hand at end. Wires HandView with caster & play controller.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private DeckRuntime _deck;
        [SerializeField] private HandRuntime _hand;
        [SerializeField] private HandView _handView;
        [SerializeField] private CardPlayController _playController;

        [Header("Caster")]
        [SerializeField] private GameObject _defaultCaster;

        private void Awake()
        {
            if (_deck == null) _deck = FindFirstObjectByType<DeckRuntime>(FindObjectsInactive.Include);
            if (_hand == null) _hand = FindFirstObjectByType<HandRuntime>(FindObjectsInactive.Include);
            if (_handView == null) _handView = FindFirstObjectByType<HandView>(FindObjectsInactive.Include);
            if (_playController == null) _playController = FindFirstObjectByType<CardPlayController>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            _deck.Initialize();
            StartCoroutine(CoStartTurnNextFrame());
        }

        private IEnumerator CoStartTurnNextFrame()
        {
            yield return null;
            OnPlayerTurnEnter();
        }

        public void OnPlayerTurnEnter()
        {
            _hand.Clear();
            var drawn = _deck.Draw(_deck.DrawPerTurn);
            _hand.AddCards(drawn, _deck.HandMax);

            _handView?.Rebuild(_hand.Items, _defaultCaster, _playController);

            GameEvents.RaiseDeckChanged(_deck.DrawCount, _deck.DiscardCount);
            GameEvents.RaiseHandChanged(_hand.Count);
            GameEvents.RaiseTurnPhaseChanged(SDProject.Core.TurnPhase.PlayerTurn);
        }

        public void OnPlayerTurnExit()
        {
            var rest = _hand.TakeAll();
            _deck.Discard(rest);
            GameEvents.RaiseDeckChanged(_deck.DrawCount, _deck.DiscardCount);
            GameEvents.RaiseTurnPhaseChanged(SDProject.Core.TurnPhase.EnemyTurn);
        }

        // Example hook: call this from UI "End Turn" button.
        public void OnClickEndTurn()
        {
            OnPlayerTurnExit();
            // enemy act stub...
            StartCoroutine(CoEnemyThenPlayer());
        }

        private IEnumerator CoEnemyThenPlayer()
        {
            yield return new WaitForSeconds(0.5f);
            OnPlayerTurnEnter();
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\DeckRuntime.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;
using SDProject.Core.Messaging;
using SDProject.DataBridge;

namespace SDProject.Combat
{
    /// <summary>
    /// Manages draw/discard piles. Initializes from IDeckSource.
    /// </summary>
    public sealed class DeckRuntime : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private MonoBehaviour _deckSource; // IDeckSource
        [SerializeField] private int _drawPerTurn = 5;
        [SerializeField] private int _handMax = 10;

        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discard = new();

        public int DrawPerTurn => _drawPerTurn;
        public int HandMax => _handMax;
        public int DrawCount => _drawPile.Count;
        public int DiscardCount => _discard.Count;

        public void Initialize()
        {
            _drawPile.Clear();
            _discard.Clear();

            var src = _deckSource as IDeckSource;
            if (src == null)
            {
                Debug.LogError("[DeckRuntime] _deckSource is not IDeckSource.");
                return;
            }

            var initial = src.GetInitialDeck();
            _drawPile.AddRange(initial);

            // Shuffle
            for (int i = 0; i < _drawPile.Count; i++)
            {
                int j = Random.Range(i, _drawPile.Count);
                (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
            }

            Debug.Log($"[Deck] init: drawPile={_drawPile.Count}, discard=0");
            GameEvents.RaiseDeckChanged(DrawCount, DiscardCount);
        }

        public List<CardData> Draw(int count)
        {
            var result = new List<CardData>(count);
            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    // Reshuffle discard into draw
                    if (_discard.Count == 0) break;
                    _drawPile.AddRange(_discard);
                    _discard.Clear();
                    for (int k = 0; k < _drawPile.Count; k++)
                    {
                        int j = Random.Range(k, _drawPile.Count);
                        (_drawPile[k], _drawPile[j]) = (_drawPile[j], _drawPile[k]);
                    }
                }

                if (_drawPile.Count == 0) break;
                int last = _drawPile.Count - 1;
                var top = _drawPile[last];
                _drawPile.RemoveAt(last);
                result.Add(top);
            }

            GameEvents.RaiseDeckChanged(DrawCount, DiscardCount);
            return result;
        }

        public void Discard(CardData card)
        {
            if (card == null) return;
            _discard.Add(card);
            GameEvents.RaiseDeckChanged(DrawCount, DiscardCount);
        }

        public void Discard(IEnumerable<CardData> cards)
        {
            if (cards == null) return;
            foreach (var c in cards) if (c != null) _discard.Add(c);
            GameEvents.RaiseDeckChanged(DrawCount, DiscardCount);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\HandRuntime.cs
```csharp
// Assets/SDProject/Scripts/Combat/HandRuntime.cs
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
        None,
        Player,
        Enemy
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
        // 초기 덱 구성 리스트 (Inspector에서 카드들을 드래그하여 채우세요)
        public List<CardData> cards = new();
    }
}
```

## Assets\SDProject\Scripts\Data\DeckSourceSOAdapter.cs
```csharp
// Assets/SDProject/Scripts/Data/DeckSourceSOAdapter.cs
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SDProject.Data;

namespace SDProject.DataBridge
{
    /// <summary>
    /// DeckList(SO)의 cards(List<CardData>)를 읽어 초기 덱을 제공합니다.
    /// CardData에 Enabled/IsEnabled/Active 같은 불리언이 있으면 필터링, 없으면 전부 포함합니다.
    /// </summary>
    public sealed class DeckSourceSOAdapter : MonoBehaviour, IDeckSource
    {
        [Header("ScriptableObject Source")]
        [SerializeField] private DeckList _deckList;

        public IReadOnlyList<CardData> GetInitialDeck()
        {
            if (_deckList == null)
            {
                Debug.LogWarning("[DeckSourceSOAdapter] DeckList not assigned.");
                return System.Array.Empty<CardData>();
            }

            if (_deckList.cards == null || _deckList.cards.Count == 0)
            {
                Debug.LogWarning("[DeckSourceSOAdapter] DeckList.cards is empty.");
                return System.Array.Empty<CardData>();
            }

            var list = new List<CardData>(_deckList.cards.Count);
            foreach (var c in _deckList.cards)
            {
                if (c == null) { Debug.LogWarning("[DeckSourceSOAdapter] Null card skipped."); continue; }
                if (!IsEnabledIfPresent(c)) { Debug.Log($"[DeckSourceSOAdapter] Skipped(disabled?): {c.name}"); continue; }
                list.Add(c);
            }

            Debug.Log($"[DeckSourceSOAdapter] Loaded {list.Count} card(s) from DeckList.cards");
            return list;
        }

        /// <summary>
        /// CardData에 Enabled/IsEnabled/Active 불리언 멤버가 있으면 그 값을 따르고,
        /// 없으면 true를 반환하여 포함합니다.
        /// </summary>
        private static bool IsEnabledIfPresent(CardData card)
        {
            var t = card.GetType();

            // 1) Field 우선
            foreach (var name in new[] { "Enabled", "IsEnabled", "Active", "IsActive" })
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(bool))
                {
                    try { return (bool)f.GetValue(card); } catch { return true; }
                }
            }

            // 2) Property
            foreach (var name in new[] { "Enabled", "IsEnabled", "Active", "IsActive" })
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanRead && p.PropertyType == typeof(bool))
                {
                    try { return (bool)p.GetValue(card); } catch { return true; }
                }
            }

            // 멤버가 없으면 포함
            return true;
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

