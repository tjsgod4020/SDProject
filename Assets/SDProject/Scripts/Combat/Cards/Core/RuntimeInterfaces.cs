using UnityEngine;

namespace SDProject.Combat.Cards
{
    // Shared combat runtime interfaces

    public interface IDamageable
    {
        void ApplyDamage(int dmg);
        bool IsAlive();
    }

    public interface IApConsumer
    {
        int CurrentAp { get; }
        bool TryConsumeAp(int amount);
    }
}