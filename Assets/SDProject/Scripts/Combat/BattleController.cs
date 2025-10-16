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
