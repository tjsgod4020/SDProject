using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// ������ ��Ÿ�ӿ� ����/��ġ�ϰ�, �� ���Կ� ��/�ε����� �ο��մϴ�(A��).
    /// - ���� Slot ������ 1���� ��� ����(CharacterSlot.assignAtRuntime=true ����).
    /// - ���� ������ å�ӿ��� ����(SRP). ������ �ٸ� �ý��ۿ��� ����/���ε��ϼ���.
    /// </summary>
    public class BoardLayout : MonoBehaviour
    {
        [Header("Common Slot Prefab")]
        [Tooltip("CharacterSlot ������Ʈ�� ���Ե� ���� ���� ������")]
        public GameObject slotPrefab;

        [Header("Ally Layout (4 lanes)")]
        public int allySlotCount = 4;                    // Back(0), Mid2(1), Mid1(2), Front(3)
        public Vector3 allyStart = new Vector3(-6f, 0f, 0f);
        public float allyGap = 1.8f;
        public Transform allyRoot;                       // ���Ե��� ���� �θ�(������ this)

        [Header("Enemy Layout (5 lanes)")]
        public int enemySlotCount = 5;                   // Front(0), Mid1(1), Mid2(2), Mid3(3), Back(4)
        public Vector3 enemyStart = new Vector3(6f, 0f, 0f);
        public float enemyGap = 1.8f;                    // �����ʿ��� �������� ��ġ�Ϸ��� ���� transform�� �ᵵ �ǰ�, ��ǥ�� �����ص� �˴ϴ�.
        public Transform enemyRoot;                      // ���Ե��� ���� �θ�(������ this)

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
        }

        private void BuildAllySlots()
        {
            for (int i = 0; i < allySlotCount; i++)
            {
                // �� -> ��� ��ġ: Back(0) �� Mid2(1) �� Mid1(2) �� Front(3)
                Vector3 pos = allyStart + new Vector3(i * allyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, allyRoot);
                go.name = $"{allyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // ��Ÿ�� ��/�ε��� Ȯ��
                slot.Configure(TeamSide.Ally, i);

                // mount�� ��������� ���� Transform ��ü�� �������� ���(����)
                if (!slot.mount) slot.mount = slot.transform;
            }
        }

        private void BuildEnemySlots()
        {
            for (int i = 0; i < enemySlotCount; i++)
            {
                // �� -> ��� ��ġ�ϵ�, ��ȹ �켱������ Front(0) �� Mid1(1) �� Mid2(2) �� Mid3(3) �� Back(4)
                // �⺻��: enemyStart���� �������� �����Ϸ��� x�� -enemyGap�� ���ϰų�, start�� �����ʿ� �ΰ� ��� gap���� ���� �̵����ѵ� �˴ϴ�.
                Vector3 pos = enemyStart + new Vector3(i * -enemyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, enemyRoot);
                go.name = $"{enemyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // ��Ÿ�� ��/�ε��� Ȯ��
                slot.Configure(TeamSide.Enemy, i);

                // mount �⺻�� ó��
                if (!slot.mount) slot.mount = slot.transform;
            }
        }
    }
}