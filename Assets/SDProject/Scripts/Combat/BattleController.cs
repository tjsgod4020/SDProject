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
