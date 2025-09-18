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
        private StPlayerTurn _stPlayer;
        private StEnemyTurn _stEnemy;

        private void Awake()
        {
            _fsm = new StateMachine();
            _stPlayer = new StPlayerTurn(this);
            _stEnemy = new StEnemyTurn(this);

            _fsm.AddTransition(_stPlayer, _stEnemy, SpacePressed);
            _fsm.AddTransition(_stEnemy, _stPlayer, () => _stEnemy.IsFinished);

        }
        private void Start()
        {
            StartCoroutine(BootFSMNextFrame());
        }

        private System.Collections.IEnumerator BootFSMNextFrame()
        {
            yield return null;             // 모든 Awake/OnEnable/Start 끝난 뒤
            _fsm.SetState(_stPlayer);
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
            Debug.Log("[Battle] DrawNewHand()");

            if (_hand == null) { Debug.LogError("[Battle] HandRuntime is NULL"); return; }
            if (_deck == null) { Debug.LogError("[Battle] DeckRuntime is NULL (cannot draw)"); _handView?.Render(_hand); return; }

            _hand.Clear();

            var drawn = _deck.Draw(5); // deckList.initialDeck에서 5장 뽑음
            if (drawn == null) { Debug.LogError("[Battle] DeckRuntime.Draw returned NULL"); _handView?.Render(_hand); return; }

            var added = _hand.AddCards(drawn, _deck.HandMax);
            Debug.Log($"[Battle] Draw request=5, returned={drawn.Count}, added={added}, now hand={_hand.Count}");

            // ✅ 이벤트 의존 말고 즉시 반영
            _handView?.Render(_hand);
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
