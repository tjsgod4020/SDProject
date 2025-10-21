namespace SD.Gameplay.Cards.Domain
{
    public enum CardType { Unknown = 0, Attack = 1, Defense = 2, Support = 3, Move = 4 }

    public enum CardClass { Unknown = 0, Base = 1, Character = 2, Myth = 3 }

    public enum CardRarity { Unknown = 0, Common = 1, Rare = 2 }
    public enum TargetType { Unknown = 0, Self = 1, Ally = 2, AllyAll = 3, Enemy = 4, EnemyAll = 5, AllyThenEnemy = 6 }

    [System.Flags]
    public enum PositionUseFlags { None = 0, Front = 1 << 0, Mid1 = 1 << 1, Mid2 = 1 << 2, Back = 1 << 3 }

    [System.Flags]
    public enum PositionHitFlags { None = 0, Front = 1 << 0, Mid1 = 1 << 1, Mid2 = 1 << 2, Mid3 = 1 << 3, Back = 1 << 4 }

    [System.Flags]
    public enum CardTagFlags { None = 0 }
}
