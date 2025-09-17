using SDProject.Core.FSM;
using SDProject.Core.Messaging;
using SDProject.Data;
using UnityEngine;

namespace SDProject.Combat
{
    /// <summary>
    /// Orchestrates battle phases with a minimal FSM.
    /// This step: Player turn draws cards at start, press Space to end turn.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("Config")]
        public BattleConfig battleConfig;   // (±âÁ¸) AP µî
        [Header("Deck")]
        public DeckRuntime deck;            // assign in scene
        public HandRuntime hand;            // assign in scene

        private StateMachine _fsm;
        private IState _stSetup, _stPlayerStart, _stPlayerMain, _stPlayerEnd, _stEnemy;

        private int _partyAP;

        private void Awake()
        {
            _fsm = new StateMachine();
            _stSetup = new StSetup(this);
            _stPlayerStart = new StPlayerStart(this);
            _stPlayerMain = new StPlayerMain(this);
            _stPlayerEnd = new StPlayerEnd(this);
            _stEnemy = new StEnemy(this);
        }

        private void Start()
        {
            _fsm.SetState(_stSetup);
        }

        private void Update()
        {
            _fsm.Tick(Time.deltaTime);
        }

        #region API
        public void InitBattle()
        {
            _partyAP = battleConfig ? battleConfig.partyAPMax : 3;
            GameEvents.RaisePartyAPChanged(_partyAP, battleConfig ? battleConfig.partyAPMax : 3);
            GameEvents.RaiseBattleStart();
#if UNITY_EDITOR
            Debug.Log("[Battle] Init");
#endif
        }
        public void RefillAP()
        {
            _partyAP = battleConfig ? battleConfig.partyAPMax : 3;
            GameEvents.RaisePartyAPChanged(_partyAP, battleConfig ? battleConfig.partyAPMax : 3);
        }
        public void ToPlayerStart() => _fsm.SetState(_stPlayerStart);
        public void ToPlayerMain() => _fsm.SetState(_stPlayerMain);
        public void ToPlayerEnd() => _fsm.SetState(_stPlayerEnd);
        public void ToEnemy() => _fsm.SetState(_stEnemy);
        #endregion

        #region States
        private class StSetup : IState
        {
            private readonly BattleController c;
            public StSetup(BattleController c) => this.c = c;
            public void Enter()
            {
                c.InitBattle();
                c.deck?.ResetFromList();
                c.hand?.Clear();
                c.ToPlayerStart();
            }
            public void Tick(float dt) { }
            public void Exit() { }
        }

        private class StPlayerStart : IState
        {
            private readonly BattleController c;
            public StPlayerStart(BattleController c) => this.c = c;
            public void Enter()
            {
                GameEvents.RaiseTurnPhaseChanged(TurnPhase.PlayerStart);
                c.RefillAP();

                // Draw at turn start
                int draw = c.deck ? c.deck.DrawPerTurn : 5;
                var cards = c.deck?.Draw(draw);
                if (cards != null)
                    c.hand?.AddCards(cards, c.deck.HandMax);

#if UNITY_EDITOR
                Debug.Log($"[Battle] PlayerStart: Draw {draw}, Hand={c.hand?.Count}");
#endif
                c.ToPlayerMain();
            }
            public void Tick(float dt) { }
            public void Exit() { }
        }

        private class StPlayerMain : IState
        {
            private readonly BattleController c;
            public StPlayerMain(BattleController c) => this.c = c;

            public void Enter()
            {
                GameEvents.RaiseTurnPhaseChanged(TurnPhase.PlayerMain);
#if UNITY_EDITOR
                Debug.Log("[Battle] PlayerMain: press Space to End Turn");
#endif
            }

            public void Tick(float dt)
            {
                // Support both Input Systems
#if ENABLE_INPUT_SYSTEM
                // New Input System
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame)
                    c.ToPlayerEnd();
#else
    // Old Input Manager
    if (Input.GetKeyDown(KeyCode.Space))
        c.ToPlayerEnd();
#endif
            }

            public void Exit() { }
        }

        private class StPlayerEnd : IState
        {
            private readonly BattleController c;
            public StPlayerEnd(BattleController c) => this.c = c;
            public void Enter()
            {
                GameEvents.RaiseTurnPhaseChanged(TurnPhase.PlayerEnd);
                // For now: discard all hand at end
                if (c.deck != null && c.hand != null)
                {
                    c.deck.Discard(c.hand.Cards);
                    c.hand.Clear();
                }
                c.ToEnemy();
            }
            public void Tick(float dt) { }
            public void Exit() { }
        }

        private class StEnemy : IState
        {
            private readonly BattleController c;
            private float t;
            public StEnemy(BattleController c) => this.c = c;
            public void Enter()
            {
                GameEvents.RaiseTurnPhaseChanged(TurnPhase.EnemyTurn);
#if UNITY_EDITOR
                Debug.Log("[Battle] EnemyTurn...");
#endif
                t = 1.0f; // fake think time
            }
            public void Tick(float dt)
            {
            #if ENABLE_INPUT_SYSTEM
                            var kb = UnityEngine.InputSystem.Keyboard.current;
                            if (kb != null && kb.spaceKey.wasPressedThisFrame)
                                c.ToPlayerEnd();
            #else
                if (Input.GetKeyDown(KeyCode.Space))
                    c.ToPlayerEnd();
            #endif
            }
            public void Exit() { }
        }
        #endregion
    }
}
