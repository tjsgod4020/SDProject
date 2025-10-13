# Code Snapshot - 2025-10-13 19:00:00
Commit: 13cd1c4

## Assets\SDProject\Scripts\Combat\Board\BoardLayout.cs
```csharp
using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// 슬롯을 런타임에 생성/배치하고, 각 슬롯에 팀/인덱스를 부여합니다(A안).
    /// - 공용 Slot 프리팹 1개만 사용 가능(CharacterSlot.assignAtRuntime=true 권장).
    /// - 유닛 스폰은 책임에서 제외(SRP). 유닛은 다른 시스템에서 스폰/바인딩하세요.
    /// </summary>
    public class BoardLayout : MonoBehaviour
    {
        [Header("Common Slot Prefab")]
        [Tooltip("CharacterSlot 컴포넌트가 포함된 공용 슬롯 프리팹")]
        public GameObject slotPrefab;

        [Header("Ally Layout (4 lanes)")]
        public int allySlotCount = 4;                    // Back(0), Mid2(1), Mid1(2), Front(3)
        public Vector3 allyStart = new Vector3(-6f, 0f, 0f);
        public float allyGap = 1.8f;
        public Transform allyRoot;                       // 슬롯들을 담을 부모(없으면 this)

        [Header("Enemy Layout (5 lanes)")]
        public int enemySlotCount = 5;                   // Front(0), Mid1(1), Mid2(2), Mid3(3), Back(4)
        public Vector3 enemyStart = new Vector3(6f, 0f, 0f);
        public float enemyGap = 1.8f;                    // 오른쪽에서 왼쪽으로 배치하려면 음수 transform을 써도 되고, 좌표만 조정해도 됩니다.
        public Transform enemyRoot;                      // 슬롯들을 담을 부모(없으면 this)

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
        }

        private void BuildAllySlots()
        {
            for (int i = 0; i < allySlotCount; i++)
            {
                // 좌 -> 우로 배치: Back(0) → Mid2(1) → Mid1(2) → Front(3)
                Vector3 pos = allyStart + new Vector3(i * allyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, allyRoot);
                go.name = $"{allyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // 런타임 팀/인덱스 확정
                slot.Configure(TeamSide.Ally, i);

                // mount가 비어있으면 슬롯 Transform 자체를 기준으로 사용(선택)
                if (!slot.mount) slot.mount = slot.transform;
            }
        }

        private void BuildEnemySlots()
        {
            for (int i = 0; i < enemySlotCount; i++)
            {
                // 좌 -> 우로 배치하되, 기획 우선순위는 Front(0) → Mid1(1) → Mid2(2) → Mid3(3) → Back(4)
                // 기본값: enemyStart에서 왼쪽으로 진행하려면 x에 -enemyGap를 곱하거나, start를 오른쪽에 두고 양수 gap으로 왼쪽 이동시켜도 됩니다.
                Vector3 pos = enemyStart + new Vector3(i * -enemyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, enemyRoot);
                go.name = $"{enemyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // 런타임 팀/인덱스 확정
                slot.Configure(TeamSide.Enemy, i);

                // mount 기본값 처리
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
        public TeamSide team = TeamSide.Ally; // ★ TeamSide는 기존 정의를 재사용합니다(중복 정의 금지).
        public int index = 0;

        [Header("Mount (유닛 스냅 지점)")]
        [Tooltip("유닛이 이 슬롯에 있을 때 위치를 맞출 기준 Transform입니다. 없으면 슬롯 Transform을 사용합니다.")]
        public Transform mount;

        /// <summary>
        /// BoardLayout이 슬롯을 생성할 때 호출하여 팀/인덱스를 확정합니다.
        /// </summary>
        public void Configure(TeamSide newTeam, int newIndex)
        {
            team = newTeam;
            index = newIndex;
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
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    [DisallowMultipleComponent]
    public class CardPlayController : MonoBehaviour
    {
        [Header("Refs")]
        public BoardRuntime Board;
        public TargetingSystem Targeting;

        [Header("Events (v1: fixed text)")]
        public UnityEvent OnCardPlayStarted;
        public UnityEvent OnCardPlayFinished;
        public UnityEvent<string> OnHint;
        public UnityEvent<string> OnErrorLabel;

        [Header("Debug")]
        [SerializeField] private string _state = "Idle";

        private IState _st;

        private void Awake() => TransitionTo(new IdleState(this));

        public void PlayCard(CardDefinition card, GameObject caster)
        {
            if (!card || !caster) { Debug.LogWarning("[CardPlay] Missing refs."); return; }
            OnCardPlayStarted?.Invoke();
            TransitionTo(new SelectingTargetsState(this, card, caster));
        }

        private void TransitionTo(IState next)
        {
            _st?.Exit();
            _st = next;
            _state = _st.GetType().Name;
            Debug.Log($"[FSM] -> {_state}");
            _st.Enter();
        }

        // ===== States =====
        interface IState { void Enter(); void Exit(); }

        class IdleState : IState
        {
            private readonly CardPlayController c;
            public IdleState(CardPlayController c) => this.c = c;
            public void Enter() { }
            public void Exit() { }
        }

        class SelectingTargetsState : IState
        {
            private readonly CardPlayController c;
            private readonly CardDefinition card;
            private readonly GameObject caster;

            public SelectingTargetsState(CardPlayController c, CardDefinition card, GameObject caster)
            { this.c = c; this.card = card; this.caster = caster; }

            public void Enter()
            {
                // Enabled?
                if (!card.Enabled)
                {
                    c.EmitError(ErrorLabel.ERR_UNIT_DISABLED, "Card disabled.");
                    c.TransitionTo(new DoneState(c));
                    return;
                }

                // AP?
                var ap = caster.GetComponent<IApConsumer>();
                if (ap != null && !ap.TryConsumeAp(card.Cost))
                {
                    c.EmitError(ErrorLabel.ERR_AP_LACK, "Not enough AP.");
                    c.TransitionTo(new DoneState(c));
                    return;
                }

                // PosUse?
                var su = caster.GetComponent<SimpleUnit>();
                if (su == null) { c.EmitError(ErrorLabel.ERR_UNIT_DISABLED, "Caster invalid."); c.TransitionTo(new DoneState(c)); return; }

                var casterLane = PositionResolver.ToLane(su.Team, su.Index);
                if (!PositionResolver.LaneMatches(casterLane, card.PosUse))
                {
                    c.EmitError(ErrorLabel.ERR_POSUSE_MISMATCH, "Position not allowed.");
                    c.TransitionTo(new DoneState(c));
                    return;
                }

                switch (card.TargetType)
                {
                    case TargetType.EnemyFrontMost:
                        var auto = c.FilterByPosHit(c.Targeting.AutoPickFrontMostEnemy(), card);
                        c.TryResolveOrAbort(card, caster, auto);
                        break;

                    case TargetType.SingleManual:
                        c.Targeting.OnTargetSelectionProvided.AddListener(OnManual);
                        c.Targeting.OnTargetSelectionRequested.Invoke(card.TargetType);
                        c.OnHint?.Invoke("Tap a valid target.");
                        break;

                    default:
                        c.EmitError(ErrorLabel.ERR_NO_TARGET, $"Not implemented: {card.TargetType}");
                        c.TransitionTo(new DoneState(c));
                        break;
                }
            }

            private void OnManual(GameObject[] picks)
            {
                c.Targeting.OnTargetSelectionProvided.RemoveListener(OnManual);
                var filtered = c.FilterByPosHit(picks, card);
                c.TryResolveOrAbort(card, caster, filtered);
            }

            public void Exit() => c.Targeting.OnTargetSelectionProvided.RemoveListener(OnManual);
        }

        class ResolvingState : IState
        {
            private readonly CardPlayController c;
            private readonly CardDefinition card;
            private readonly GameObject caster;
            private readonly GameObject[] targets;

            public ResolvingState(CardPlayController c, CardDefinition card, GameObject caster, GameObject[] targets)
            { this.c = c; this.card = card; this.caster = caster; this.targets = targets ?? Array.Empty<GameObject>(); }

            public void Enter()
            {
                Debug.Log($"[CardPlay] Resolving {card.Id} x{targets.Length}");
                var ctx = new CardEffectContext(caster, targets, c.Board);

                foreach (var so in card.Effects)
                {
                    if (so is ICardEffect eff) eff.Execute(ctx);
                    else Debug.LogWarning($"[CardPlay] Effect not ICardEffect: {so?.name}");
                }

                c.TransitionTo(new DoneState(c));
            }

            public void Exit() { }
        }

        class DoneState : IState
        {
            private readonly CardPlayController c;
            public DoneState(CardPlayController c) => this.c = c;
            public void Enter()
            {
                Debug.Log("[CardPlay] Finished.");
                c.OnCardPlayFinished?.Invoke();
                c.TransitionTo(new IdleState(c));
            }
            public void Exit() { }
        }

        // ===== Helpers =====

        private void TryResolveOrAbort(CardDefinition card, GameObject caster, GameObject[] rawTargets)
        {
            var filtered = FilterByPosHit(rawTargets, card);
            if (filtered == null || filtered.Length == 0)
            {
                EmitError(ErrorLabel.ERR_NO_TARGET, "No valid targets.");
                TransitionTo(new DoneState(this));
                return;
            }
            TransitionTo(new ResolvingState(this, card, caster, filtered));
        }

        private GameObject[] FilterByPosHit(GameObject[] raw, CardDefinition card)
        {
            if (raw == null || raw.Length == 0) return Array.Empty<GameObject>();

            return raw.Where(t =>
            {
                var loc = Board.GetUnitLocation(t);
                if (loc == null) return false;
                var lane = PositionResolver.ToLane(loc.Value.team, loc.Value.index);
                return PositionResolver.LaneMatches(lane, card.PosHit);
            }).ToArray();
        }

        private void EmitError(ErrorLabel code, string uiText)
        {
            Debug.LogWarning($"[CardPlay][{code}] {uiText}");
            OnErrorLabel?.Invoke(uiText); // v1: 고정 텍스트
        }
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
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SDProject.Combat.Cards
{
    public class CardView : MonoBehaviour
    {
        public CardDefinition Card;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI CostText;
        public TextMeshProUGUI DescText;
        public Button PlayButton;

        [Header("Runtime")]
        public CardPlayController PlayController;
        public GameObject Caster; // who plays the card

        private void Awake()
        {
            if (PlayButton) PlayButton.onClick.AddListener(OnClickPlay);
        }

        private void Start() => Refresh();

        public void Refresh()
        {
            if (!Card) return;
            if (TitleText) TitleText.SetText(string.IsNullOrEmpty(Card.NameId) ? Card.Id : Card.NameId);
            if (CostText) CostText.SetText(Card.Cost.ToString());
            if (DescText) DescText.SetText(string.IsNullOrEmpty(Card.DescId) ? "-" : Card.DescId);
        }

        private void OnClickPlay()
        {
            if (!Card || !PlayController || !Caster)
            {
                Debug.LogWarning("[CardView] Missing Card/PlayController/Caster.");
                return;
            }
            Debug.Log($"[CardView] Play {Card.Id}");
            PlayController.PlayCard(Card, Caster);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\BattleController.cs
```csharp
using System.Collections;
using UnityEngine;
using SDProject.Core.FSM;
using SDProject.Data;
using SDProject.Core.Messaging;   // ← 이벤트만 사용

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SDProject.Combat
{
    public class BattleController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private DeckRuntime _deck;
        [SerializeField] private HandRuntime _hand;

        private StateMachine _fsm;
        private StPlayerTurn _stPlayer;
        private StEnemyTurn _stEnemy;

        private void Awake()
        {
            var hand = Object.FindFirstObjectByType<HandRuntime>(FindObjectsInactive.Include);
            var deck = Object.FindFirstObjectByType<DeckRuntime>(FindObjectsInactive.Include);
            _hand.OnUsed += OnCardUsed;

            _fsm = new StateMachine();
            _stPlayer = new StPlayerTurn(this);
            _stEnemy = new StEnemyTurn(this);

            _fsm.AddTransition(_stPlayer, _stEnemy, SpacePressed);
            _fsm.AddTransition(_stEnemy, _stPlayer, () => _stEnemy.IsFinished);
        }

        private void Start() => StartCoroutine(BootFSMNextFrame());
        private IEnumerator BootFSMNextFrame() { yield return null; _fsm.SetState(_stPlayer); }
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

        // ── 턴 훅 ──────────────────────────────────────────────────────────────
        public void OnPlayerTurnEnter() => DrawNewHand();

        public void OnPlayerTurnExit()
        {
            if (_deck == null || _hand == null) return;
            var rest = _hand.TakeAll();
            _deck.Discard(rest);
            // HandRuntime가 내부에서 GameEvents.RaiseHandChanged 호출함
            GameEvents.RaiseDeckChanged(_deck.DrawCount, _deck.DiscardCount);
        }

        public void DrawNewHand()
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

            // UI로 알림만 보냄
            GameEvents.RaiseHandChanged(_hand.Count);
            GameEvents.RaiseDeckChanged(_deck.DrawCount, _deck.DiscardCount);
        }

        // ── States ────────────────────────────────────────────────────────────
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

        private void OnCardUsed(SDProject.Data.CardData card)
        {
            // 카드를 실제로 '버림 더미'로 이동
            _deck.Discard(card);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\DeckRuntime.cs
```csharp
// ... (using 생략)

using SDProject.Core.Messaging;
using SDProject.Data;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Combat
{
    public class DeckRuntime : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private DeckList deckList;
        [SerializeField] private int drawPerTurn = 5;
        [SerializeField] private int handMax = 5;

        public int DrawPerTurn => drawPerTurn;
        public int HandMax => handMax;

        // ✅ 여기가 내부 저장소
        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discardPile = new();

        // ✅ 🔹추가: BattleController 등에서 읽어갈 공개 카운터
        public int DrawCount => _drawPile.Count;
        public int DiscardCount => _discardPile.Count;

        private void Awake()
        {
            ResetFromList();
        }

        public void ResetFromList()
        {
            _drawPile.Clear();
            _discardPile.Clear();

            if (deckList != null && deckList.cards != null)
                _drawPile.AddRange(deckList.cards);

            Shuffle(_drawPile);
            BroadcastCounts();
            Debug.Log($"[Deck] init: drawPile={_drawPile.Count}, discard={_discardPile.Count}");
        }

        public List<CardData> Draw(int count)
        {
            EnsureDrawable(count);
            var result = new List<CardData>(count);
            for (int i = 0; i < count && _drawPile.Count > 0; i++)
            {
                var top = _drawPile[^1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                result.Add(top);
            }
            BroadcastCounts();
            return result;
        }

        public void Discard(CardData card)
        {
            if (card == null) return;
            _discardPile.Add(card);
            BroadcastCounts();
        }

        public void Discard(IEnumerable<CardData> cards)
        {
            if (cards == null) return;
            _discardPile.AddRange(cards);
            BroadcastCounts();
        }

        private void EnsureDrawable(int needed)
        {
            if (_drawPile.Count >= needed) return;
            if (_discardPile.Count == 0) return;

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        private static void Shuffle(List<CardData> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = Random.Range(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void BroadcastCounts()
        {
            // UI 갱신 이벤트
            GameEvents.RaiseDeckChanged(_drawPile.Count, _discardPile.Count);
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\HandRuntime.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;
using SDProject.Core.Messaging;
using System;

namespace SDProject.Combat
{
    /// <summary>
    /// 손패 보관/변경 책임. 사용(=버림으로 보낼 후보)은 OnUsed로 통지.
    /// </summary>
    public class HandRuntime : MonoBehaviour
    {
        [SerializeField] private List<CardData> _cards = new();
        public IReadOnlyList<CardData> Cards => _cards;
        public int Count => _cards.Count;

        /// <summary>카드 한 장이 '사용됨'을 알림 (실제 버림 이동은 상위 컨트롤러 책임).</summary>
        public event Action<CardData> OnUsed;

        public int AddCards(List<CardData> add, int maxHand)
        {
            if (add == null || add.Count == 0) return 0;
            int canAdd = Mathf.Max(0, maxHand - _cards.Count);
            int take = Mathf.Min(canAdd, add.Count);
            if (take <= 0) return 0;

            for (int i = 0; i < take; i++)
                _cards.Add(add[i]);

            GameEvents.RaiseHandChanged(_cards.Count);
            return take;
        }

        public bool Remove(CardData c)
        {
            bool ok = _cards.Remove(c);
            if (ok) GameEvents.RaiseHandChanged(_cards.Count);
            return ok;
        }

        /// <summary>사용 버튼 핸들러가 호출. 내부에서 제거 후 OnUsed로 알림.</summary>
        public void Use(CardData c)
        {
            if (Remove(c))
                OnUsed?.Invoke(c);
        }

        public void Clear()
        {
            _cards.Clear();
            GameEvents.RaiseHandChanged(_cards.Count);
        }

        public List<CardData> TakeAll()
        {
            var all = new List<CardData>(_cards);
            _cards.Clear();
            GameEvents.RaiseHandChanged(_cards.Count);
            return all;
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

