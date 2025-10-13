using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>Maps (team, index) to logical lane flags per our design.</summary>
    public static class PositionResolver
    {
        // Ally: [Back(0), Mid2(1), Mid1(2), Front(3)]
        // Enemy: [Front(0), Mid1(1), Mid2(2), Mid3(3), Back(4)]
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
            else
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

        public static bool LaneMatches(PositionFlags lane, PositionFlags mask) => (mask & lane) != 0;
    }
}
