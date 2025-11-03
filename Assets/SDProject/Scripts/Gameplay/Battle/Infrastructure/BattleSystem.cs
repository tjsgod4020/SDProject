using SD.Gameplay.Battle.Domain;
using UnityEngine;

namespace SD.Gameplay.Battle.Infrastructure
{
    /// 오케스트레이터: 전투 시작/입력 라우팅용
    public sealed class BattleSystem : MonoBehaviour
    {
        [SerializeField] private TurnController _turn;
        [SerializeField] private CardRuntimeRepository _repo;

        private void Reset()
        {
            if (_turn == null) _turn = FindAnyObjectByType<TurnController>();
            if (_repo == null) _repo = FindAnyObjectByType<CardRuntimeRepository>();
        }

        private void Awake()
        {
            if (_turn != null)
            {
                _turn.OnTurnStarted += side => Debug.Log($"[Battle] TurnStart: {side}");
                _turn.OnTurnEnded += side => Debug.Log($"[Battle] TurnEnd: {side}");
            }
            if (_repo != null)
            {
                _repo.OnHandChanged += _ => Debug.Log($"[Battle] Hand: {_repo.Hand.Count} cards");
                _repo.OnPileChanged += (d, c) => Debug.Log($"[Battle] Piles: Draw={d}, Discard={c}");
            }
        }

        private void Start() => _turn?.StartBattle();

        public void EndTurnButton() => _turn?.EndTurn();

        public bool TryPlayCard(int handIndex)
        {
            if (_repo == null) return false;
            if (handIndex < 0 || handIndex >= _repo.Hand.Count) return false;
            var inst = _repo.Hand[handIndex];
            var ok = _repo.TryPlay(inst);
            if (ok) Debug.Log($"[Battle] Played {inst.Id}");
            return ok;
        }
    }
}
