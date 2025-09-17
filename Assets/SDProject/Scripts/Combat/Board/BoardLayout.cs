using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Combat.Board
{
    public enum LayoutStyle { TwoRows, OneRowTwoGroups }

    [DisallowMultipleComponent]
    public class BoardLayout : MonoBehaviour
    {
        [Header("Layout")]
        public LayoutStyle layoutStyle = LayoutStyle.TwoRows;

        [Min(1)] public int allyCount = 4;
        [Min(1)] public int enemyCount = 5;

        [Header("TwoRows Y (world)")]
        public float allyRowY = -2.5f;
        public float enemyRowY = 2.0f;

        [Header("OneRowTwoGroups")]
        public float oneRowY = 0.0f;      // 한 줄일 때의 Y
        public float groupGap = 3.0f;     // 두 그룹 사이의 공백 (world)

        [Header("Common")]
        public float spacing = 2.0f;      // 슬롯 간 간격 (world)

        [Header("Prefabs")]
        public GameObject slotPrefab;
        public GameObject dummyCharacterPrefab;

        [Header("Parents")]
        public Transform slotsRoot;
        public Transform unitsRoot;

        private readonly List<CharacterSlot> _slots = new();

        private void Awake()
        {
            if (!slotsRoot)
            {
                var go = new GameObject("SlotsRoot");
                go.transform.SetParent(transform, false);
                slotsRoot = go.transform;
            }
            if (!unitsRoot)
            {
                var go = new GameObject("UnitsRoot");
                go.transform.SetParent(transform, false);
                unitsRoot = go.transform;
            }
        }

        private void Start()
        {
            _slots.Clear();
            switch (layoutStyle)
            {
                case LayoutStyle.OneRowTwoGroups:
                    BuildOneRowTwoGroups(allyCount, enemyCount, oneRowY, spacing, groupGap);
                    break;
                default:
                    BuildLine(TeamSide.Ally, allyCount, allyRowY);
                    BuildLine(TeamSide.Enemy, enemyCount, enemyRowY);
                    break;
            }
            SpawnDummies();
        }

        // ===== 기존 2줄(위/아래) =====
        private void BuildLine(TeamSide team, int count, float rowY)
        {
            float totalWidth = spacing * (count - 1);
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3(startX + i * spacing, rowY, 0f);
                CreateSlot(team, i, pos);
            }
        }

        // ===== 한 줄에 두 그룹(좌:아군, 우:적) =====
        private void BuildOneRowTwoGroups(int allyN, int enemyN, float y, float s, float gap)
        {
            float allyWidth = s * (Mathf.Max(allyN, 1) - 1);
            float enemyWidth = s * (Mathf.Max(enemyN, 1) - 1);

            // 전체 폭: [ally][gap][enemy]
            float total = allyWidth + gap + enemyWidth;
            float leftStartX = -total * 0.5f;           // 왼쪽 그룹(아군)의 시작 x
            float rightStartX = leftStartX + allyWidth + gap; // 오른쪽 그룹(적)의 시작 x

            for (int i = 0; i < allyN; i++)
            {
                Vector3 pos = new Vector3(leftStartX + i * s, y, 0f);
                CreateSlot(TeamSide.Ally, i, pos);
            }
            for (int i = 0; i < enemyN; i++)
            {
                Vector3 pos = new Vector3(rightStartX + i * s, y, 0f);
                CreateSlot(TeamSide.Enemy, i, pos);
            }
        }

        private void CreateSlot(TeamSide team, int idx, Vector3 pos)
        {
            var slotGO = slotPrefab ? Instantiate(slotPrefab, pos, Quaternion.identity, slotsRoot)
                                    : new GameObject($"{team}_Slot_{idx:00}");
            if (!slotPrefab)
            {
                slotGO.transform.SetParent(slotsRoot, true);
                slotGO.transform.position = pos;
            }
            var slot = slotGO.GetComponent<CharacterSlot>();
            if (!slot) slot = slotGO.AddComponent<CharacterSlot>();
            slot.team = team;
            slot.index = idx;
            if (!slot.mount) slot.mount = slot.transform;
            _slots.Add(slot);
        }

        private void SpawnDummies()
        {
            foreach (var slot in _slots)
            {
                if (!dummyCharacterPrefab) { Debug.LogWarning("[BoardLayout] Dummy prefab is missing."); continue; }
                var unit = Instantiate(dummyCharacterPrefab, slot.mount.position, Quaternion.identity, unitsRoot);
                var d = unit.GetComponent<DummyCharacter>() ?? unit.AddComponent<DummyCharacter>();
                d.Bind(slot.team, slot.index);
                
                if (slot.team == TeamSide.Enemy)
                {
                    var t = unit.transform;
                    t.localScale = new Vector3(-Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
                }

            }
        }
    }
}
