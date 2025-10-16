// Assets/SDProject/Scripts/Combat/Targeting/UnitClickForwarder.cs
using UnityEngine;

namespace SDProject.Combat
{
    /// <summary>
    /// For manual targeting: forward OnMouseDown to TargetingSystem.
    /// Requires a Collider on the unit.
    /// </summary>
    public sealed class UnitClickForwarder : MonoBehaviour
    {
        [SerializeField] private TargetingSystem _targeting;

        private void Reset()
        {
            _targeting = FindFirstObjectByType<TargetingSystem>(FindObjectsInactive.Include);
        }

        private void OnMouseDown()
        {
            if (_targeting) _targeting.ProvideManualSingle(gameObject);
        }
    }
}
