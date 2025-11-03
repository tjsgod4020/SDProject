using System;
using UnityEngine;

namespace SD.Gameplay.Battle.Infrastructure
{
    public enum TurnSide { Player, Enemy }

    /// 간단 턴 컨트롤러: 플레이어 ↔ 적
    public sealed class TurnController : MonoBehaviour
    {
        [SerializeField] private CardRuntimeRepository _repo;

        public TurnSide CurrentSide { get; private set; } = TurnSide.Player;

        public event Action<TurnSide> OnTurnStarted;
        public event Action<TurnSide> OnTurnEnded;

        private void Reset()
        {
            if (_repo == null) _repo = FindAnyObjectByType<CardRuntimeRepository>();
        }

        public void StartBattle()
        {
            if (_repo == null)
            {
                Debug.LogError("[Turn] CardRuntimeRepository missing.");
                return;
            }

            _repo.InitDeckFromCatalog();

            CurrentSide = TurnSide.Player;
            OnTurnStarted?.Invoke(CurrentSide);
            _repo.StartPlayerTurn();
            Debug.Log("[Turn] Player turn started.");
        }

        public void EndTurn()
        {
            OnTurnEnded?.Invoke(CurrentSide);

            if (CurrentSide == TurnSide.Player)
            {
                _repo.EndPlayerTurn();

                CurrentSide = TurnSide.Enemy;
                OnTurnStarted?.Invoke(CurrentSide);
                Debug.Log("[Turn] Enemy turn started.");
                // TODO: 적 AI 행동
                CurrentSide = TurnSide.Player;
                OnTurnEnded?.Invoke(TurnSide.Enemy);

                OnTurnStarted?.Invoke(CurrentSide);
                _repo.StartPlayerTurn();
                Debug.Log("[Turn] Player turn started.");
            }
        }
    }
}
