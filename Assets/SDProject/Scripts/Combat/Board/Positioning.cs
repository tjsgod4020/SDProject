using System;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// Logical lane flags for position filters.
    /// </summary>
    [Flags]
    public enum PositionFlags
    {
        None = 0,
        Front = 1 << 0,
        Mid1 = 1 << 1,
        Mid2 = 1 << 2,
        Mid3 = 1 << 3,
        Back = 1 << 4
    }

    /// <summary>
    /// Maps (team, index) to a PositionFlags lane according to our board design:
    /// Ally indices: 0=Back, 1=Mid2, 2=Mid1, 3=Front
    /// Enemy indices: 0=Front, 1=Mid1, 2=Mid2, 3=Mid3, 4=Back
    /// </summary>
    public static class PositionResolver
    {
        public static PositionFlags ToLane(TeamSide team, int index)
        {
            if (team == TeamSide.Ally)
            {
                return index switch
                {
                    0 => PositionFlags.Back,
                    1 => PositionFlags.Mid2,
                    2 => PositionFlags.Mid1,
                    3 => PositionFlags.Front,
                    _ => PositionFlags.None
                };
            }
            else // Enemy
            {
                return index switch
                {
                    0 => PositionFlags.Front,
                    1 => PositionFlags.Mid1,
                    2 => PositionFlags.Mid2,
                    3 => PositionFlags.Mid3,
                    4 => PositionFlags.Back,
                    _ => PositionFlags.None
                };
            }
        }
    }
}
