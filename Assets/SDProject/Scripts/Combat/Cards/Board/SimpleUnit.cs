using UnityEngine;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Minimal demo unit: HP/AP and current slot reference.
    /// Implements IDamageable & IApConsumer as properties + methods.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleUnit : MonoBehaviour, IDamageable, IApConsumer
    {
        [Header("Identity")]
        public TeamSide Team;
        public int Index;

        [Header("Vitals")]
        [Min(1)] public int MaxHp = 30;
        public int CurrentHp;

        // IMPORTANT: Interface requires a *property*, not a field.
        // Use auto-property with private setter to satisfy IApConsumer.
        [Min(0)] public int CurrentAp { get; private set; } = 3;

        [Header("Board Link (resolved by runtime/binder)")]
        public CharacterSlot CurrentSlot;

        private void Awake()
        {
            CurrentHp = MaxHp;
        }

        // --- IDamageable ---
        public void ApplyDamage(int dmg)
        {
            int v = Mathf.Max(0, dmg);
            CurrentHp = Mathf.Max(0, CurrentHp - v);
            Debug.Log($"[HP] {name} takes {v}. {CurrentHp}/{MaxHp}");
            if (CurrentHp <= 0) Debug.Log($"[Unit] {name} defeated.");
        }

        public bool IsAlive() => CurrentHp > 0;

        // --- IApConsumer ---
        public bool TryConsumeAp(int amount)
        {
            if (amount <= 0) return true;
            if (CurrentAp < amount) return false;
            CurrentAp -= amount;
            Debug.Log($"[AP] {name} consumes {amount} => {CurrentAp}");
            return true;
        }

        // Optional helper to refill AP (not in interface, but handy in tests)
        public void SetAp(int value)
        {
            CurrentAp = Mathf.Max(0, value);
        }

        public void AddAp(int delta)
        {
            CurrentAp = Mathf.Max(0, CurrentAp + delta);
        }
    }
}