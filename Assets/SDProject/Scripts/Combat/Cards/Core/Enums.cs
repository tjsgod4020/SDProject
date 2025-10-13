using System;

namespace SDProject.Combat.Cards
{
    public enum CardType { Attack, Defense, Support, Move }
    public enum CardClass { Common, Character, Mythic }
    public enum CardRarity { Common, Rare }

    public enum TargetType
    {
        Self,
        Ally,
        AllyAll,
        Enemy,
        EnemyAll,
        AllyThenEnemy,    // v1.1 계획
        EnemyFrontMost,   // v1 자동 단일
        SingleManual      // v1 수동 단일
    }

    [Flags]
    public enum PositionFlags
    {
        None = 0,
        Front = 1 << 0,
        Mid1 = 1 << 1,
        Mid2 = 1 << 2,
        Mid3 = 1 << 3, // 적측만 운용
        Back = 1 << 4,
        All = Front | Mid1 | Mid2 | Mid3 | Back
    }

    public enum ErrorLabel
    {
        None,
        ERR_AP_LACK,
        ERR_POSUSE_MISMATCH,
        ERR_NO_TARGET,
        ERR_UNIT_DISABLED
    }
}
