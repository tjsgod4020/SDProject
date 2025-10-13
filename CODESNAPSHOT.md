# Code Snapshot - 2025-10-13
Commit: e2cc52d

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

