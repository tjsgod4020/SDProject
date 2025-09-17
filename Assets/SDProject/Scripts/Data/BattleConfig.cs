using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Battle Config", fileName = "BattleConfig")]
    public class BattleConfig : ScriptableObject
    {
        [Header("Party")]
        [Min(1)] public int partySize = 3;
        [Min(0)] public int partyAPMax = 3;

        [Header("Enemies")]
        [Min(1)] public int enemyMaxCount = 5;

        [Header("Rules")]
        public bool allowPositioning = true;
    }
}
