## CODE SNAPSHOT ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))

### File Tree

로컬 디스크 볼륨에 대한 폴더 경로의 목록입니다.
볼륨 일련 번호는 3473-4281입니다.
D:\SD\SDPROJECT\ASSETS\SDPROJECT\SCRIPTS
│  Combat.meta
│  Core.meta
│  Data.meta
│  UI.meta
│  
├─Combat
│  │  BattleController.cs
│  │  BattleController.cs.meta
│  │  Board.meta
│  │  DeckRuntime.cs
│  │  DeckRuntime.cs.meta
│  │  HandRuntime.cs
│  │  HandRuntime.cs.meta
│  │  
│  └─Board
│          BoardLayout.cs
│          BoardLayout.cs.meta
│          CharacterSlot.cs
│          CharacterSlot.cs.meta
│          DummyCharacter.cs
│          DummyCharacter.cs.meta
│          TeamSide.cs
│          TeamSide.cs.meta
│          
├─Core
│  │  Boot.meta
│  │  FSM.meta
│  │  Messaging.meta
│  │  
│  ├─Boot
│  │      GameInstaller.cs
│  │      GameInstaller.cs.meta
│  │      
│  ├─FSM
│  │      IState.cs
│  │      IState.cs.meta
│  │      StateMachine.cs
│  │      StateMachine.cs.meta
│  │      
│  └─Messaging
│          GameEvents.cs
│          GameEvents.cs.meta
│          
├─Data
│      BattleConfig.cs
│      BattleConfig.cs.meta
│      CardData.cs
│      CardData.cs.meta
│      DeckList.cs
│      DeckList.cs.meta
│      
└─UI
        BattleHUD.cs
        BattleHUD.cs.meta
        CardItemView.cs
        CardItemView.cs.meta
        HandView.cs
        HandView.cs.meta
        

---

### Assets\SDProject\Scripts\Core\FSM\IState.cs
`csharp
using UnityEngine;

namespace SDProject.Core.FSM
{
    /// <summary>State lifecycle hooks.</summary>
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
` 

### Assets\SDProject\Scripts\Core\FSM\StateMachine.cs
`csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Core.FSM
{
    /// <summary>
    /// Minimal FSM with conditional transitions.
    /// </summary>
    public class StateMachine
    {
        private IState _current;

        private class Transition
        {
            public IState From;
            public IState To;
            public Func<bool> Condition;
        }

        private readonly List<Transition> _transitions = new();
        private readonly List<Transition> _currentTransitions = new();

        public void AddTransition(IState from, IState to, Func<bool> condition)
        {
            _transitions.Add(new Transition { From = from, To = to, Condition = condition });
        }

        public void SetState(IState next)
        {
            if (_current == next) return;

            _current?.Exit();
            _current = next;

            // rebuild currentTransitions
            _currentTransitions.Clear();
            foreach (var t in _transitions)
                if (t.From == _current) _currentTransitions.Add(t);

#if UNITY_EDITOR
            Debug.Log($"[FSM] Switched to: {_current?.GetType().Name}");
#endif
            _current?.Enter();
        }

        public void Tick(float dt)
        {
            // check transitions first
            for (int i = 0; i < _currentTransitions.Count; i++)
            {
                if (_currentTransitions[i].Condition())
                {
                    SetState(_currentTransitions[i].To);
                    break; // only first match
                }
            }
            _current?.Tick(dt);
        }

        public IState Current => _current;
    }
}
` 

### Assets\SDProject\Scripts\Core\Messaging\GameEvents.cs
`csharp
using System;

namespace SDProject.Core.Messaging
{
    public enum TurnPhase { PlayerStart, PlayerMain, PlayerEnd, EnemyTurn }

    public static class GameEvents
    {
        // Existing examples...
        public static event Action OnBattleStart;
        public static event Action OnBattleEnd;
        public static event Action<int, int> OnPartyAPChanged;

        // New: turn & hand
        public static event Action<TurnPhase> OnTurnPhaseChanged;
        public static event Action<int> OnHandChanged; // current hand count

        public static void RaiseBattleStart() => OnBattleStart?.Invoke();
        public static void RaiseBattleEnd() => OnBattleEnd?.Invoke();
        public static void RaisePartyAPChanged(int cur, int max) => OnPartyAPChanged?.Invoke(cur, max);

        public static void RaiseTurnPhaseChanged(TurnPhase phase) => OnTurnPhaseChanged?.Invoke(phase);
        public static void RaiseHandChanged(int count) => OnHandChanged?.Invoke(count);
    }
}
` 

### Assets\SDProject\Scripts\Core\Boot\GameInstaller.cs
`csharp
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
` 

### Assets\SDProject\Scripts\Combat\BattleController.cs
`csharp
using System.Collections;
using UnityEngine;
using SDProject.Core.FSM;
using SDProject.UI;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SDProject.Combat
{
    /// <summary>
    /// Player ↔ Enemy 턴 순환 컨트롤러.
    /// Space로 EnemyTurn 전환 → 1초 후 자동 PlayerTurn 복귀.
    /// PlayerTurn 진입 시 새 카드 5장 드로우.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private DeckRuntime _deck;   // 씬에서 할당
        [SerializeField] private HandRuntime _hand;   // 씬에서 할당
        [SerializeField] private HandView _handView;  // 씬에서 할당 (이벤트 기반이면 없어도 OK)

        private StateMachine _fsm;

        private void Awake()
        {
            _fsm = new StateMachine();

            var stPlayer = new StPlayerTurn(this);
            var stEnemy = new StEnemyTurn(this);

            _fsm.AddTransition(stPlayer, stEnemy, SpacePressed);
            _fsm.AddTransition(stEnemy, stPlayer, () => stEnemy.IsFinished);

            _fsm.SetState(stPlayer);
        }

        private void Update()
        {
            _fsm.Tick(Time.deltaTime);
        }

        // --- Helpers ---

        private bool SpacePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        /// <summary>핸드를 비우고 새로 5장 드로우.</summary>
        public void DrawNewHand()
        {
            if (_hand == null) return;

            _hand.Clear();

            if (_deck != null)
            {
                // DeckRuntime에 Draw(int)와 HandMax가 있다고 가정(이전 단계 로직 기준)
                var drawn = _deck.Draw(5);
                _hand.AddCards(drawn, _deck.HandMax);
            }
            else
            {
                Debug.LogWarning("[Battle] DeckRuntime is missing. Cannot draw.");
            }

            // HandView가 GameEvents로 갱신된다면 아래 호출은 필요 없음.
            // _handView?.Render(_hand);
        }

        // ======================
        // States
        // ======================

        private class StPlayerTurn : IState
        {
            private readonly BattleController c;
            public StPlayerTurn(BattleController ctx) => c = ctx;

            public void Enter()
            {
                Debug.Log("▶ PlayerTurn Enter");
                c.DrawNewHand();
            }

            public void Tick(float dt) { /* 카드 클릭 처리 이미 UI에서 */ }

            public void Exit()
            {
                Debug.Log("⏸ PlayerTurn Exit");
            }
        }

        private class StEnemyTurn : IState
        {
            private readonly BattleController c;
            public bool IsFinished { get; private set; }

            public StEnemyTurn(BattleController ctx) => c = ctx;

            public void Enter()
            {
                Debug.Log("[Battle] EnemyTurn...");
                IsFinished = false;
                c.StartCoroutine(CoEnemy());
            }

            private IEnumerator CoEnemy()
            {
                yield return new WaitForSeconds(1f);
                IsFinished = true;
            }

            public void Tick(float dt) { }

            public void Exit() { }
        }
    }
}
` 

### Assets\SDProject\Scripts\Combat\DeckRuntime.cs
`csharp
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;

namespace SDProject.Combat
{
    /// <summary>
    /// Runtime deck system: owns draw/discard piles, shuffling, drawing N cards.
    /// Single responsibility: deck mechanics only (no UI / no input).
    /// </summary>
    public class DeckRuntime : MonoBehaviour
    {
        [SerializeField] private DeckList deckList;

        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discardPile = new();

        private System.Random _rng;

        public int DrawPerTurn => deckList ? deckList.drawPerTurn : 5;
        public int HandMax => deckList ? deckList.handMax : 10;

        private void Awake()
        {
            _rng = new System.Random();
            ResetFromList();
        }

        public void ResetFromList()
        {
            _drawPile.Clear();
            _discardPile.Clear();
            if (deckList != null)
                _drawPile.AddRange(deckList.initialDeck);
            Shuffle(_drawPile);
        }

        public List<CardData> Draw(int count)
        {
            var result = new List<CardData>(count);
            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    // Refill from discard
                    if (_discardPile.Count == 0) break; // no more cards
                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                }
                var idx = _drawPile.Count - 1;
                var card = _drawPile[idx];
                _drawPile.RemoveAt(idx);
                result.Add(card);
            }
            return result;
        }

        public void Discard(IEnumerable<CardData> cards)
        {
            _discardPile.AddRange(cards);
        }

        private void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
` 

### Assets\SDProject\Scripts\Combat\HandRuntime.cs
`csharp
// Assets/SDProject/Scripts/Combat/HandRuntime.cs
using System.Collections.Generic;
using UnityEngine;
using SDProject.Core.Messaging;
using SDProject.Data;

namespace SDProject.Combat
{
    public class HandRuntime : MonoBehaviour
    {
        private readonly List<CardData> _cards = new();
        public IReadOnlyList<CardData> Cards => _cards;
        public int Count => _cards.Count;

        public void Add(CardData card)
        {
            _cards.Add(card);
            GameEvents.RaiseHandChanged(_cards.Count);
        }

        public void Clear()
        {
            _cards.Clear();
            GameEvents.RaiseHandChanged(_cards.Count);
        }

        // 새로 추가: 특정 카드 제거
        public void Remove(CardData card)
        {
            if (_cards.Remove(card))
            {
                GameEvents.RaiseHandChanged(_cards.Count);
            }
        }
        public int AddCards(IEnumerable<SDProject.Data.CardData> cards, int maxHand)
        {
            int added = 0;
            foreach (var c in cards)
            {
                if (_cards.Count >= maxHand) break;
                _cards.Add(c);
                added++;
            }
            GameEvents.RaiseHandChanged(_cards.Count);
            return added;
        }
    }
}
` 

### Assets\SDProject\Scripts\UI\HandView.cs
`csharp
using System.Linq;
using UnityEngine;
using SDProject.Combat;
using SDProject.Core.Messaging;

namespace SDProject.UI
{
    /// <summary>
    /// Renders player's hand at the bottom using a prefab per card.
    /// Listens to GameEvents.OnHandChanged and rebuilds.
    /// SRP: hand -> UI sync only.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private HandRuntime handRuntime;           // assign in scene
        [SerializeField] private RectTransform content;             // bottom container
        [SerializeField] private GameObject cardItemPrefab;         // prefab with CardItemView

        private void OnEnable()
        {
            GameEvents.OnHandChanged += Rebuild;
        }

        private void OnDisable()
        {
            GameEvents.OnHandChanged -= Rebuild;
        }

        private void Start()
        {
            // initial build if hand already has cards
            Rebuild(handRuntime ? handRuntime.Count : 0);
        }

        private void ClearChildren()
        {
            if (!content) return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        private void Rebuild(int _)
        {
            if (!handRuntime || handRuntime.Cards == null || !content || !cardItemPrefab)
                return;

            ClearChildren();

            foreach (var card in handRuntime.Cards.ToList())
            {
                var go = Instantiate(cardItemPrefab, content);
                var view = go.GetComponent<CardItemView>();
                if (view) view.Bind(card, handRuntime);
            }
        }
    }
}
` 

### Assets\SDProject\Scripts\UI\CardItemView.cs
`csharp
// Assets/SDProject/Scripts/UI/CardItemView.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;    // ★ 추가
using SDProject.Data;
using SDProject.Combat;

namespace SDProject.UI
{
    // IPointerClickHandler로 UI 클릭 처리
    public class CardItemView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtCost;
        [SerializeField] private Image background;

        private CardData _data;
        private HandRuntime _hand;

        public void Bind(CardData data, HandRuntime hand)
        {
            _data = data;
            _hand = hand;
            if (txtName) txtName.text = data.displayName;
            if (txtCost) txtCost.text = $"AP {data.apCost}";
        }

        // UI 클릭
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_hand == null || _data == null) return;

            // 좌클릭만 제거로 처리 (원하면 우클릭/더블클릭도 분기 가능)
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Debug.Log($"[CardItemView] Click remove: {_data.displayName}");
                _hand.Remove(_data);      // UI는 HandView가 이벤트로 재빌드
            }
        }
    }
}
` 

### Assets\SDProject\Scripts\Combat\Board\BoardLayout.cs
`csharp
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
` 

### Assets\SDProject\Scripts\Combat\Board\DummyCharacter.cs
`csharp
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
` 

