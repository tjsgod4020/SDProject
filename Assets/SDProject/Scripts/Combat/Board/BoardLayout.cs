using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// Creates and places slots at runtime and assigns Team/Index (Plan A).
    /// - Uses a single shared Slot prefab (with CharacterSlot). Recommended: assignAtRuntime=true.
    /// - Spawning/binding units is out of scope (SRP). Other systems should handle units.
    /// </summary>
    public class BoardLayout : MonoBehaviour
    {
        [Header("Common Slot Prefab")]
        [Tooltip("A shared slot prefab that contains CharacterSlot component.")]
        public GameObject slotPrefab;

        [Header("Ally Layout (4 lanes)")]
        public int allySlotCount = 4;                    // Back(0), Mid2(1), Mid1(2), Front(3)
        public Vector3 allyStart = new Vector3(-6f, 0f, 0f);
        public float allyGap = 1.8f;
        public Transform allyRoot;                       // Parent for ally slots (defaults to this)

        [Header("Enemy Layout (5 lanes)")]
        public int enemySlotCount = 5;                   // Front(0), Mid1(1), Mid2(2), Mid3(3), Back(4)
        public Vector3 enemyStart = new Vector3(6f, 0f, 0f);
        public float enemyGap = 1.8f;                    // Place from right to left by using -enemyGap in X
        public Transform enemyRoot;                      // Parent for enemy slots (defaults to this)

        [Header("Naming")]
        public string allyNamePrefix = "Ally_";
        public string enemyNamePrefix = "Enemy_";

        private void Awake()
        {
            if (!slotPrefab)
            {
                Debug.LogError("[BoardLayout] slotPrefab is missing.");
                return;
            }

            if (!allyRoot) allyRoot = this.transform;
            if (!enemyRoot) enemyRoot = this.transform;

            BuildAllySlots();
            BuildEnemySlots();

            // ¡Ú One-line reinforcement: after slots are created, ask BoardRuntime to rescan immediately.
            UnityEngine.Object.FindFirstObjectByType<SDProject.Combat.Cards.BoardRuntime>(FindObjectsInactive.Include)?.RefreshFromScene();
        }

        private void BuildAllySlots()
        {
            for (int i = 0; i < allySlotCount; i++)
            {
                // Left -> Right: Back(0) ¡æ Mid2(1) ¡æ Mid1(2) ¡æ Front(3)
                Vector3 pos = allyStart + new Vector3(i * allyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, allyRoot);
                go.name = $"{allyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // Assign team/index at runtime
                slot.Configure(TeamSide.Ally, i);

                // Default mount fallback
                if (!slot.mount) slot.mount = slot.transform;
            }
        }

        private void BuildEnemySlots()
        {
            for (int i = 0; i < enemySlotCount; i++)
            {
                // Place from right to left in world space: Front(0) ¡æ Mid1(1) ¡æ Mid2(2) ¡æ Mid3(3) ¡æ Back(4)
                Vector3 pos = enemyStart + new Vector3(i * -enemyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, enemyRoot);
                go.name = $"{enemyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // Assign team/index at runtime
                slot.Configure(TeamSide.Enemy, i);

                // Default mount fallback
                if (!slot.mount) slot.mount = slot.transform;
            }
        }
    }
}
