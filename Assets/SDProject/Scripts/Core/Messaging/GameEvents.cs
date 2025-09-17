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
