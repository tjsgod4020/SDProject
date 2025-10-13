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