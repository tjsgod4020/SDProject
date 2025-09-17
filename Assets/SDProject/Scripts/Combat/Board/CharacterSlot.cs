// Assets/SDProject/Scripts/Combat/Board/CharacterSlot.cs
using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// A single logical position on the board (mount point for a character).
    /// SRP: identity + mount transform only.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterSlot : MonoBehaviour
    {
        public TeamSide team;
        public int index;                  // 0-based within team line
        public Transform mount;            // where character spawns (defaults to self)

        private void Reset()
        {
            if (!mount) mount = transform;
        }
    }
}
