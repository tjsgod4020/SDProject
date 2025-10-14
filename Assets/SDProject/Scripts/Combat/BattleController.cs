using System.Collections;
using UnityEngine;
using SDProject.Core.FSM;
using SDProject.Core.Messaging;   // GameEvents only
using SDProject.Core;            // TurnPhase enum 등 (프로젝트 네임스페이스에 맞춰 사용)
using SDProject.Data;

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
            // 인스펙터에 없으면 Find로 보강(있으면 보존)
            if (!_hand) _hand = Object.FindFirstObjectByType<HandRuntime>(FindObjectsInactive.Include);
            if (!_deck) _deck = Object.FindFirstObjectByType<DeckRuntime>(FindObjectsInactive.Include);

            if (_hand != null)
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
            yield return null;
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

        // ── 턴 훅 ──────────────────────────────────────────────────────────────
        public void OnPlayerTurnEnter()
        {
            GameEvents.RaiseTurnPhaseChanged(TurnPhase.Player); // HUD 동기화
            DrawNewHand();
        }

        public void OnPlayerTurnExit()
        {
            if (_deck == null || _hand == null) return;

            var rest = _hand.TakeAll();        // 손패 싹 비우기
            _deck.Discard(rest);               // 남은 손패 전부 버림
            // HandRuntime/DeckRuntime이 내부에서 이벤트를 쏘지 않는 경우 대비해 보강 통지
            GameEvents.RaiseHandChanged(_hand.Count);
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

            // 프로젝트 API에 맞춰 호출(아래 함수명들은 샘플, 레포 시그니처에 맞춰 사용)
            var drawn = _deck.Draw(_deck.DrawPerTurn);        // List<CardData>
            var added = _hand.AddCards(drawn, _deck.HandMax); // 실제 추가된 수

            Debug.Log($"[Battle] Draw request={_deck.DrawPerTurn}, returned={drawn.Count}, added={added}, now hand={_hand.Count}");

            // HUD/카운터 동기화(내부에서 이미 쏜다면 중복 아님: GameEvents는 idempotent하게 설계 권장)
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

            public void Enter()
            {
                Debug.Log("[Battle] EnemyTurn...");
                GameEvents.RaiseTurnPhaseChanged(TurnPhase.Enemy); // HUD 동기화
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

        private void OnDestroy()
        {
            if (_hand != null)
                _hand.OnUsed -= OnCardUsed;
        }

        private void OnCardUsed(CardData card)
        {
            if (_deck == null || card == null) return;

            // ⚠️ 프로젝트 규칙 확인:
            // - HandRuntime.Use가 이미 버림까지 처리하면, 아래 Discard는 제거하세요.
            //_deck.Discard(card);

            // HUD 동기화 보강
            GameEvents.RaiseHandChanged(_hand?.Count ?? 0);
            GameEvents.RaiseDeckChanged(_deck.DrawCount, _deck.DiscardCount);
        }
    }
}