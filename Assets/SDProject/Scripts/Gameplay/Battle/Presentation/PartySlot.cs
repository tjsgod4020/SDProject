using UnityEngine;

namespace SD.Gameplay.Battle.Presentation
{
    public sealed class PartySlot : MonoBehaviour
    {
        [SerializeField] private int _index;
        [SerializeField] private bool _isEnemy;

        private GameObject _occupant;

        public int Index => _index;
        public bool IsEnemy => _isEnemy;
        public bool IsOccupied => _occupant != null;
        public Transform Anchor => transform;

        public void Configure(int index, bool isEnemy)
        {
            _index = index;
            _isEnemy = isEnemy;
        }

        public void Clear()
        {
            if (_occupant == null) return;
            Destroy(_occupant);
            _occupant = null;
        }

        public void Attach(GameObject unitGo)
        {
            if (unitGo == null) return;

            Clear();

            _occupant = unitGo;
            _occupant.transform.SetParent(Anchor, worldPositionStays: false);
            _occupant.transform.localPosition = Vector3.zero;
            _occupant.transform.localRotation = Quaternion.identity;
            _occupant.transform.localScale = Vector3.one;
        }
    }
}
