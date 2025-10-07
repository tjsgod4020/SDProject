# Code Snapshot - 2025-10-07
Commit: 15ad682

## Assets\SDProject\Scripts\Combat\Board\BoardLayout.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Combat.Board
{
    public enum LayoutStyle { TwoRows, OneRowTwoGroups }

    [DisallowMultipleComponent]
    public class BoardLayout : MonoBehaviour
    {
        [Header("Layout")]
        public LayoutStyle layoutStyle = LayoutStyle.TwoRows;

        [Min(1)] public int allyCount = 4;
        [Min(1)] public int enemyCount = 5;

        [Header("TwoRows Y (world)")]
        public float allyRowY = -2.5f;
        public float enemyRowY = 2.0f;

        [Header("OneRowTwoGroups")]
        public float oneRowY = 0.0f;      // 한 줄일 때의 Y
        public float groupGap = 3.0f;     // 두 그룹 사이의 공백 (world)

        [Header("Common")]
        public float spacing = 2.0f;      // 슬롯 간 간격 (world)

        [Header("Prefabs")]
        public GameObject slotPrefab;
        public GameObject dummyCharacterPrefab;

        [Header("Parents")]
        public Transform slotsRoot;
        public Transform unitsRoot;

        private readonly List<CharacterSlot> _slots = new();

        private void Awake()
        {
            if (!slotsRoot)
            {
                var go = new GameObject("SlotsRoot");
                go.transform.SetParent(transform, false);
                slotsRoot = go.transform;
            }
            if (!unitsRoot)
            {
                var go = new GameObject("UnitsRoot");
                go.transform.SetParent(transform, false);
                unitsRoot = go.transform;
            }
        }

        private void Start()
        {
            _slots.Clear();
            switch (layoutStyle)
            {
                case LayoutStyle.OneRowTwoGroups:
                    BuildOneRowTwoGroups(allyCount, enemyCount, oneRowY, spacing, groupGap);
                    break;
                default:
                    BuildLine(TeamSide.Ally, allyCount, allyRowY);
                    BuildLine(TeamSide.Enemy, enemyCount, enemyRowY);
                    break;
            }
            SpawnDummies();
        }

        // ===== 기존 2줄(위/아래) =====
        private void BuildLine(TeamSide team, int count, float rowY)
        {
            float totalWidth = spacing * (count - 1);
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3(startX + i * spacing, rowY, 0f);
                CreateSlot(team, i, pos);
            }
        }

        // ===== 한 줄에 두 그룹(좌:아군, 우:적) =====
        private void BuildOneRowTwoGroups(int allyN, int enemyN, float y, float s, float gap)
        {
            float allyWidth = s * (Mathf.Max(allyN, 1) - 1);
            float enemyWidth = s * (Mathf.Max(enemyN, 1) - 1);

            // 전체 폭: [ally][gap][enemy]
            float total = allyWidth + gap + enemyWidth;
            float leftStartX = -total * 0.5f;           // 왼쪽 그룹(아군)의 시작 x
            float rightStartX = leftStartX + allyWidth + gap; // 오른쪽 그룹(적)의 시작 x

            for (int i = 0; i < allyN; i++)
            {
                Vector3 pos = new Vector3(leftStartX + i * s, y, 0f);
                CreateSlot(TeamSide.Ally, i, pos);
            }
            for (int i = 0; i < enemyN; i++)
            {
                Vector3 pos = new Vector3(rightStartX + i * s, y, 0f);
                CreateSlot(TeamSide.Enemy, i, pos);
            }
        }

        private void CreateSlot(TeamSide team, int idx, Vector3 pos)
        {
            var slotGO = slotPrefab ? Instantiate(slotPrefab, pos, Quaternion.identity, slotsRoot)
                                    : new GameObject($"{team}_Slot_{idx:00}");
            if (!slotPrefab)
            {
                slotGO.transform.SetParent(slotsRoot, true);
                slotGO.transform.position = pos;
            }
            var slot = slotGO.GetComponent<CharacterSlot>();
            if (!slot) slot = slotGO.AddComponent<CharacterSlot>();
            slot.team = team;
            slot.index = idx;
            if (!slot.mount) slot.mount = slot.transform;
            _slots.Add(slot);
        }

        private void SpawnDummies()
        {
            foreach (var slot in _slots)
            {
                if (!dummyCharacterPrefab) { Debug.LogWarning("[BoardLayout] Dummy prefab is missing."); continue; }
                var unit = Instantiate(dummyCharacterPrefab, slot.mount.position, Quaternion.identity, unitsRoot);
                var d = unit.GetComponent<DummyCharacter>() ?? unit.AddComponent<DummyCharacter>();
                d.Bind(slot.team, slot.index);
                
                if (slot.team == TeamSide.Enemy)
                {
                    var t = unit.transform;
                    t.localScale = new Vector3(-Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
                }

            }
        }
    }
}
```

## Assets\SDProject\Scripts\Combat\Board\CharacterSlot.cs
```csharp
// Assets/SDProject/Scripts/Combat/Board/CharacterSlot.cs
using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// A single logical position on the board (mount point for a character).
    /// SRP: identity + mount transform only.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterSlot : MonoBehaviour
    {
        public TeamSide team;
        public int index;                  // 0-based within team line
        public Transform mount;            // where character spawns (defaults to self)

        private void Reset()
        {
            if (!mount) mount = transform;
        }
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
            if (!_deck) _deck = FindObjectOfType<DeckRuntime>(true);
            if (!_hand) _hand = FindObjectOfType<HandRuntime>(true);
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
        "Unity.InputSystem"
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

## Assets\SDProject\Scripts\ExcelImport\Abstractions\ExcelRowMapperBase.cs
```csharp

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional base with helpers (GetString/GetInt/etc).
/// </summary>
public abstract class ExcelRowMapperBase<T> : ScriptableObject, IExcelRowMapper<T>
{
    protected string GetString(Dictionary<string, string> row, string key, string defaultValue = "")
    {
        return row.TryGetValue(key, out var val) ? val : defaultValue;
    }

    protected int GetInt(Dictionary<string, string> row, string key, int defaultValue = 0)
    {
        if (row.TryGetValue(key, out var val) && int.TryParse(val, out var i)) return i;
        return defaultValue;
    }

    protected float GetFloat(Dictionary<string, string> row, string key, float defaultValue = 0f)
    {
        if (row.TryGetValue(key, out var val) && float.TryParse(val, out var f)) return f;
        return defaultValue;
    }

    public abstract bool TryMap(Dictionary<string, string> row, out T result);
}
#endif
```

## Assets\SDProject\Scripts\ExcelImport\Abstractions\IExcelDataSink.cs
```csharp

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SRP: Receive mapped rows and store them (e.g., into a ScriptableObject).
/// </summary>
public interface IExcelDataSink<T>
{
    void Clear();
    void AddRange(IEnumerable<T> items);
    void SaveDirty(); // mark asset dirty in Editor
    Object AsUnityObject(); // for ping/select in editor logs
}
#endif
```

## Assets\SDProject\Scripts\ExcelImport\Abstractions\IExcelRowMapper.cs
```csharp

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SRP: One responsibility = convert a row dictionary to a strongly-typed model object.
/// </summary>
public interface IExcelRowMapper<T>
{
    /// <summary>Return true if row is valid and out is set; log errors as needed.</summary>
    bool TryMap(Dictionary<string, string> row, out T result);
}
#endif
```

## Assets\SDProject\Scripts\ExcelImport\Config\ExcelImportConfig.cs
```csharp

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExcelImportConfig", menuName = "Game/Excel/ImportConfig")]
public class ExcelImportConfig : ScriptableObject
{
    [Header("XLSX")]
    [Tooltip("Use Unity path under Assets/, e.g., Assets/DataTables/GameData.xlsx")]
    public string xlsxAssetPath;

    [Serializable]
    public class SheetImportSetting
    {
        public string sheetName = "Cards";
        [Min(0)] public int headerRowIndex = 0;
        [Min(0)] public int dataStartRowIndex = 1;

        [Header("Row Mapper (ScriptableObject implementing IExcelRowMapper<T>)")]
        public ScriptableObject rowMapper; // e.g., CardRowMapper

        [Header("Data Sink (ScriptableObject implementing IExcelDataSink<T>)")]
        public ScriptableObject dataSink;  // e.g., CardDatabase
    }

    [Header("Sheets")]
    public List<SheetImportSetting> sheets = new();

    [Header("Events")]
    public ExcelImportEvents events = new ExcelImportEvents();

    [Serializable]
    public class ExcelImportEvents
    {
        public ImportProgressEvent OnProgress = new ImportProgressEvent();
        public ImportCompletedEvent OnCompleted = new ImportCompletedEvent();
    }
}
#endif

/*
[Unity 적용 가이드]
- Project 우클릭 → Create → Game/Excel/ImportConfig 생성.
- xlsxAssetPath: 예) "Assets/DataTables/GameData.xlsx"
- Sheets에 각 시트를 추가하고, 매퍼/싱크 ScriptableObject를 연결.
- 다른 테이블(캐릭터 등)도 시트별로 항목만 추가하면 됨.
*/
```

## Assets\SDProject\Scripts\ExcelImport\Core\ExcelImportEvents.cs
```csharp

#if UNITY_EDITOR
using UnityEngine.Events;

[System.Serializable]
public class ImportProgressEvent : UnityEvent<string> { }

[System.Serializable]
public class ImportCompletedEvent : UnityEvent<bool, string> { } // success, message
#endif

/*
[Unity 적용 가이드]
- 다른 시스템과 느슨 결합: 임포트 진행/완료를 UI나 로거가 구독할 수 있음.
*/
```

## Assets\SDProject\Scripts\ExcelImport\Core\ExcelImportStateMachine.cs
```csharp

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// State pattern for import flow: Idle -> LoadWorkbook -> ParseSheets -> Completed/Failed
/// Keeps logs at key points. Uses UnityEvents from config.
/// </summary>
public class ExcelImportStateMachine
{
    // ---------- States ----------
    // Note: Keep nested states private; only the state machine itself uses them.
    private abstract class State
    {
        protected readonly ExcelImportStateMachine ctx;
        protected State(ExcelImportStateMachine c) { ctx = c; }
        public virtual void Enter() { }
        public virtual void Tick() { }
    }

    private class IdleState : State
    {
        public IdleState(ExcelImportStateMachine c) : base(c) { }
        public override void Enter() { ctx.Log("Idle."); }
    }

    private class LoadWorkbookState : State
    {
        public LoadWorkbookState(ExcelImportStateMachine c) : base(c) { }
        public override void Enter()
        {
            ctx.Log("Loading workbook...");
            try
            {
                ctx.fullPath = Path.GetFullPath(ctx.assetPath);
                if (!File.Exists(ctx.fullPath)) throw new FileNotFoundException(ctx.fullPath);
                ctx.TransitionTo(new ParseSheetsState(ctx)); // internal transition
            }
            catch (Exception ex)
            {
                ctx.Fail($"LoadWorkbook failed: {ex.Message}");
            }
        }
    }

    private class ParseSheetsState : State
    {
        public ParseSheetsState(ExcelImportStateMachine c) : base(c) { }
        public override void Enter()
        {
            ctx.Log("Parsing sheets...");
            try
            {
                foreach (var s in ctx.settings)
                {
                    if (s.rowMapper == null || s.dataSink == null)
                    {
                        ctx.LogWarn($"Sheet '{s.sheetName}' skipped: mapper or sink not assigned.");
                        continue;
                    }

                    var rows = ExcelReader.ReadSheet(ctx.fullPath, s.sheetName, s.headerRowIndex, s.dataStartRowIndex);

                    // Infer T from IExcelRowMapper<T>
                    var mapperType = s.rowMapper.GetType();
                    var mapperIface = Array.Find(mapperType.GetInterfaces(), i =>
                        i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IExcelRowMapper"));
                    if (mapperIface == null)
                    {
                        ctx.LogError($"Mapper {mapperType.Name} does not implement IExcelRowMapper<T>.");
                        continue;
                    }
                    var modelType = mapperIface.GetGenericArguments()[0];

                    var sinkType = s.dataSink.GetType();
                    var sinkIface = Array.Find(sinkType.GetInterfaces(), i =>
                        i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IExcelDataSink"));
                    if (sinkIface == null || sinkIface.GetGenericArguments()[0] != modelType)
                    {
                        ctx.LogError($"Sink {sinkType.Name} is not IExcelDataSink<{modelType.Name}>.");
                        continue;
                    }

                    // Clear sink
                    sinkType.GetMethod("Clear").Invoke(s.dataSink, null);

                    // Map rows -> List<T>
                    var mappedList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(modelType));
                    var tryMap = mapperType.GetMethod("TryMap");

                    foreach (var row in rows)
                    {
                        object[] args = new object[] { row, null };
                        bool ok = (bool)tryMap.Invoke(s.rowMapper, args);
                        if (ok) mappedList.Add(args[1]); // out T result
                    }

                    // Persist
                    sinkType.GetMethod("AddRange").Invoke(s.dataSink, new object[] { mappedList });
                    sinkType.GetMethod("SaveDirty").Invoke(s.dataSink, null);

                    ctx.Log($"Sheet '{s.sheetName}': {mappedList.Count} rows imported → {s.dataSink.name}");
                }

                ctx.TransitionTo(new CompletedState(ctx, true, "Import completed."));
            }
            catch (Exception ex)
            {
                ctx.Fail($"ParseSheets failed: {ex}");
            }
        }
    }

    private class CompletedState : State
    {
        private readonly bool success;
        private readonly string message;
        public CompletedState(ExcelImportStateMachine c, bool success, string message) : base(c)
        { this.success = success; this.message = message; }
        public override void Enter()
        {
            ctx.Log($"Completed. success={success} message={message}");
            ctx.events?.OnCompleted?.Invoke(success, message);
        }
    }

    // ---------- Context ----------
    private State _state;
    private readonly ExcelImportConfig config;
    private readonly List<ExcelImportConfig.SheetImportSetting> settings;
    private string assetPath => config.xlsxAssetPath;
    private string _fullPath;
    private ExcelImportConfig.ExcelImportEvents events => config.events;

    public string fullPath { get => _fullPath; set => _fullPath = value; }

    public ExcelImportStateMachine(ExcelImportConfig cfg)
    {
        config = cfg;
        settings = cfg.sheets;
        _state = new IdleState(this);
    }

    /// <summary>Entry point from EditorWindow/UI.</summary>
    public void Start()
    {
        events?.OnProgress?.Invoke("Import started.");
        TransitionTo(new LoadWorkbookState(this));
    }

    // ✅ FIX: make this private (or internal) so its parameter accessibility matches.
    private void TransitionTo(State next)
    {
        _state = next;
        _state.Enter();
    }

    public void Log(string msg)
    {
        Debug.Log($"[ExcelImport] {msg}");
        events?.OnProgress?.Invoke(msg);
    }

    public void LogWarn(string msg)
    {
        Debug.LogWarning($"[ExcelImport] {msg}");
        events?.OnProgress?.Invoke("WARN: " + msg);
    }

    public void LogError(string msg)
    {
        Debug.LogError($"[ExcelImport] {msg}");
        events?.OnProgress?.Invoke("ERROR: " + msg);
    }

    public void Fail(string message)
    {
        LogError(message);
        TransitionTo(new CompletedState(this, false, message));
    }
}
#endif
```

## Assets\SDProject\Scripts\ExcelImport\Core\ExcelReader.cs
```csharp
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;

public static class ExcelReader
{
    /// <summary>
    /// Reads a sheet and returns rows as dictionaries {Header -> CellText}.
    /// </summary>
    public static List<Dictionary<string, string>> ReadSheet(
        string fullXlsxPath,
        string sheetName,
        int headerRowIndex = 0,
        int dataStartRowIndex = 1)
    {
        if (!File.Exists(fullXlsxPath))
        {
            Debug.LogError($"[ExcelReader] File not found: {fullXlsxPath}");
            return new List<Dictionary<string, string>>();
        }

        using (var fs = new FileStream(fullXlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            IWorkbook workbook = new XSSFWorkbook(fs);
            var sheet = workbook.GetSheet(sheetName);
            if (sheet == null)
            {
                Debug.LogError($"[ExcelReader] Sheet not found: {sheetName}");
                return new List<Dictionary<string, string>>();
            }

            // Read headers
            var headerRow = sheet.GetRow(headerRowIndex);
            if (headerRow == null)
            {
                Debug.LogError($"[ExcelReader] Header row missing at index {headerRowIndex} (sheet: {sheetName})");
                return new List<Dictionary<string, string>>();
            }

            var headers = new List<string>();
            for (int c = 0; c < headerRow.LastCellNum; c++)
            {
                var cell = headerRow.GetCell(c);
                headers.Add(cell?.ToString()?.Trim() ?? $"Col{c}");
            }

            var rows = new List<Dictionary<string, string>>();

            for (int r = dataStartRowIndex; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool isAllEmpty = true;

                for (int c = 0; c < headers.Count; c++)
                {
                    var cell = row.GetCell(c);
                    string text = cell?.ToString()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(text)) isAllEmpty = false;
                    dict[headers[c]] = text;
                }

                if (!isAllEmpty) rows.Add(dict);
            }

            Debug.Log($"[ExcelReader] Read {rows.Count} rows from '{sheetName}' ({Path.GetFileName(fullXlsxPath)})");
            return rows;
        }
    }
}
#endif

/*
[Unity 적용 가이드]
- NPOI 설치 후(메뉴 Tools > NuGet > Manage NuGet Packages → NPOI, NPOI.OOXML) 자동으로 사용 가능.
- Editor 전용이므로 #if UNITY_EDITOR 가드가 포함됨.
*/
```

## Assets\SDProject\Scripts\ExcelImport\Data\CardData.cs
```csharp

using System;
using UnityEngine;

[Serializable]
public struct CardData
{
    public string Id;         // Unique Id
    public string Name;       // Display Name
    public int Cost;          // Mana/Energy cost
    public string Rarity;     // Common/Rare/Epic...
    public string Tags;       // CSV tags (for quick demo)
}
```

## Assets\SDProject\Scripts\ExcelImport\Data\CardDatabase.cs
```csharp

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObject sink for CardData (implements IExcelDataSink{CardData}).
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "Game/Data/CardDatabase")]
public class CardDatabase : ScriptableObject, IExcelDataSink<CardData>
{
    [SerializeField] private List<CardData> items = new List<CardData>();
    public IReadOnlyList<CardData> Items => items;

    public void Clear() => items.Clear();

    public void AddRange(IEnumerable<CardData> add)
    {
        items.AddRange(add);
    }

    public void SaveDirty()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    public Object AsUnityObject() => this;
}
#endif

/*
[Unity 적용 가이드]
- Project 우클릭 → Create → Game/Data/CardDatabase 로 자산 생성.
- 다른 테이블(캐릭터 등)도 동일 패턴으로 Database SO를 만들면 됨.
*/
```

## Assets\SDProject\Scripts\ExcelImport\Editor\ExcelImporterWindow.cs
```csharp

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ExcelImporterWindow : EditorWindow
{
    private ExcelImportConfig _config;
    private Vector2 _scroll;
    private string _logHint = "Ready.";

    [MenuItem("Tools/Excel Importer")]
    public static void Open()
    {
        GetWindow<ExcelImporterWindow>("Excel Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("XLSX Import (NPOI)", EditorStyles.boldLabel);
        _config = (ExcelImportConfig)EditorGUILayout.ObjectField("Import Config", _config, typeof(ExcelImportConfig), false);

        EditorGUILayout.Space();
        if (_config != null)
        {
            EditorGUILayout.HelpBox($"XLSX: {_config.xlsxAssetPath}", MessageType.Info);

            if (GUILayout.Button("Run Import"))
            {
                RunImport();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(120));
            EditorGUILayout.LabelField("Log:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_logHint, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("Assign an ExcelImportConfig asset.", MessageType.Warning);
        }
    }

    private void RunImport()
    {
        if (_config == null) return;

        // Wire events (temporary for window)
        _config.events.OnProgress.RemoveAllListeners();
        _config.events.OnCompleted.RemoveAllListeners();

        _config.events.OnProgress.AddListener((msg) =>
        {
            _logHint = msg;
            Repaint();
        });

        _config.events.OnCompleted.AddListener((ok, msg) =>
        {
            _logHint = (ok ? "SUCCESS: " : "FAILED: ") + msg;
            Repaint();
        });

        var sm = new ExcelImportStateMachine(_config);
        sm.Start();
    }
}
#endif

/*
[Unity 적용 가이드]
- 메뉴: Tools > Excel Importer
- Import Config 할당 후 "Run Import" 버튼 클릭 → Console & 창 내부 로그 확인.
- 필요 시 ScriptableObject 이벤트를 게임 내 TMP UI에 연결해도 됨(런타임 확인용).
*/
```

## Assets\SDProject\Scripts\ExcelImport\Mappers\CardRowMapper.cs
```csharp

#if UNITY_EDITOR
using SDProject.Data;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps row dictionary -> CardData. Columns: Id, Name, Cost, Rarity, Tags
/// </summary>
[CreateAssetMenu(fileName = "CardRowMapper", menuName = "Game/Excel/RowMapper/Card")]
public class CardRowMapper : ExcelRowMapperBase<CardData>
{
    public override bool TryMap(Dictionary<string, string> row, out CardData result)
    {
        result = new CardData
        {
            Id = GetString(row, "Id"),
            Name = GetString(row, "Name"),
            Cost = GetInt(row, "Cost", 0),
            Rarity = GetString(row, "Rarity"),
            Tags = GetString(row, "Tags")
        };

        if (string.IsNullOrEmpty(result.Id))
        {
            Debug.LogWarning("[CardRowMapper] Row skipped: Id is required.");
            return false;
        }
        return true;
    }
}
#endif

/*
[Unity 적용 가이드]
- Project 우클릭 → Create → Game/Excel/RowMapper/Card 로 매퍼 생성.
- 시트 컬럼명과 정확히 일치해야 함(Id/Name/Cost/Rarity/Tags). 엑셀 헤더를 맞춰주세요.
*/
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

