using System.Collections.Generic;
using UnityEngine;

namespace SD.Gameplay.Battle.Presentation
{
    public sealed class PartySlotLayout : MonoBehaviour
    {
        [Header("Side")]
        [SerializeField] private bool _isEnemy;

        [Header("Slot Count")]
        [SerializeField] private int _slotCount = 4;
        [SerializeField] private bool _buildOnStart = true;
        [SerializeField] private bool _rebuildOnStart = true;

        [Header("Layout")]
        [SerializeField] private Vector2 _spacing = new(-1.5f, 0f);
        [SerializeField] private float _startX = -1f;
        [SerializeField] private float _y = 1f;
        [SerializeField] private bool _reverseX = false;

        private readonly List<PartySlot> _slots = new();
        public IReadOnlyList<PartySlot> Slots => _slots;

        private void Start()
        {
            if (_buildOnStart)
                BuildSlots(_rebuildOnStart);
        }

        public void BuildSlots(bool rebuild)
        {
            if (rebuild) ClearSlots();
            else if (_slots.Count > 0)
            {
                // rebuild=false이고 이미 슬롯이 있으면 재생성하지 않음
                return;
            }

            _slots.Clear();

            int count = Mathf.Max(0, _slotCount);
            for (int i = 0; i < count; i++)
            {
                var slotGo = new GameObject($"Slot_{i:00}");
                slotGo.transform.SetParent(transform, worldPositionStays: false);

                float x = _startX + (_reverseX ? -i : i) * _spacing.x;
                slotGo.transform.localPosition = new Vector3(x, _y, 0f);

                var slot = slotGo.AddComponent<PartySlot>();
                slot.Configure(i, _isEnemy);
                _slots.Add(slot);
            }
        }

        private void ClearSlots()
        {
            var existing = GetComponentsInChildren<PartySlot>(includeInactive: true);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null)
                    Destroy(existing[i].gameObject);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // ���Ӻ信�� �� ���̰�, �� �信���� ��ġ Ȯ�ο�
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = _isEnemy ? new Color(1f, 0.3f, 0.3f, 0.6f) : new Color(0.3f, 0.6f, 1f, 0.6f);

            int count = Mathf.Max(0, _slotCount);
            for (int i = 0; i < count; i++)
            {
                float x = _startX + (_reverseX ? -i : i) * _spacing.x;
                Gizmos.DrawWireSphere(new Vector3(x, _y, 0f), 0.25f);
            }
        }
#endif
    }
}
