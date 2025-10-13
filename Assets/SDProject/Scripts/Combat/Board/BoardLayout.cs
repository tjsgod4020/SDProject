using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// 슬롯을 런타임에 생성/배치하고, 각 슬롯에 팀/인덱스를 부여합니다(A안).
    /// - 공용 Slot 프리팹 1개만 사용 가능(CharacterSlot.assignAtRuntime=true 권장).
    /// - 유닛 스폰은 책임에서 제외(SRP). 유닛은 다른 시스템에서 스폰/바인딩하세요.
    /// </summary>
    public class BoardLayout : MonoBehaviour
    {
        [Header("Common Slot Prefab")]
        [Tooltip("CharacterSlot 컴포넌트가 포함된 공용 슬롯 프리팹")]
        public GameObject slotPrefab;

        [Header("Ally Layout (4 lanes)")]
        public int allySlotCount = 4;                    // Back(0), Mid2(1), Mid1(2), Front(3)
        public Vector3 allyStart = new Vector3(-6f, 0f, 0f);
        public float allyGap = 1.8f;
        public Transform allyRoot;                       // 슬롯들을 담을 부모(없으면 this)

        [Header("Enemy Layout (5 lanes)")]
        public int enemySlotCount = 5;                   // Front(0), Mid1(1), Mid2(2), Mid3(3), Back(4)
        public Vector3 enemyStart = new Vector3(6f, 0f, 0f);
        public float enemyGap = 1.8f;                    // 오른쪽에서 왼쪽으로 배치하려면 음수 transform을 써도 되고, 좌표만 조정해도 됩니다.
        public Transform enemyRoot;                      // 슬롯들을 담을 부모(없으면 this)

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
                // 좌 -> 우로 배치: Back(0) → Mid2(1) → Mid1(2) → Front(3)
                Vector3 pos = allyStart + new Vector3(i * allyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, allyRoot);
                go.name = $"{allyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // 런타임 팀/인덱스 확정
                slot.Configure(TeamSide.Ally, i);

                // mount가 비어있으면 슬롯 Transform 자체를 기준으로 사용(선택)
                if (!slot.mount) slot.mount = slot.transform;
            }
        }

        private void BuildEnemySlots()
        {
            for (int i = 0; i < enemySlotCount; i++)
            {
                // 좌 -> 우로 배치하되, 기획 우선순위는 Front(0) → Mid1(1) → Mid2(2) → Mid3(3) → Back(4)
                // 기본값: enemyStart에서 왼쪽으로 진행하려면 x에 -enemyGap를 곱하거나, start를 오른쪽에 두고 양수 gap으로 왼쪽 이동시켜도 됩니다.
                Vector3 pos = enemyStart + new Vector3(i * -enemyGap, 0f, 0f);
                var go = Instantiate(slotPrefab, pos, Quaternion.identity, enemyRoot);
                go.name = $"{enemyNamePrefix}{i}";

                var slot = go.GetComponent<CharacterSlot>();
                if (!slot)
                {
                    Debug.LogError($"[BoardLayout] Slot prefab has no CharacterSlot: {go.name}");
                    continue;
                }

                // 런타임 팀/인덱스 확정
                slot.Configure(TeamSide.Enemy, i);

                // mount 기본값 처리
                if (!slot.mount) slot.mount = slot.transform;
            }
        }
    }
}