using UnityEngine;

namespace SDProject.Combat.Cards
{
    [CreateAssetMenu(menuName = "SDProject/Card Effects/Damage", fileName = "DamageEffect")]
    public class DamageEffectDef : ScriptableObject, ICardEffect
    {
        [Min(0)] public int BaseDamage = 6;
        [Min(0)] public int RandomBonusMax = 0;

        public void Execute(ICardEffectContext ctx)
        {
            foreach (var t in ctx.Targets)
            {
                var hp = t.GetComponent<IDamageable>();
                if (hp == null)
                {
                    Debug.LogWarning($"[DamageEffect] {t.name} has no IDamageable.");
                    continue;
                }
                int bonus = RandomBonusMax > 0 ? ctx.Rng.Next(0, RandomBonusMax + 1) : 0;
                int dmg = BaseDamage + bonus;
                hp.ApplyDamage(dmg);
                Debug.Log($"[DamageEffect] {t.name} takes {dmg} dmg.");
            }
        }
    }
}