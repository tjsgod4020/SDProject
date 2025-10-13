using UnityEngine;

namespace SDProject.Combat.Cards
{
    [CreateAssetMenu(menuName = "SDProject/Card Effects/Knockback", fileName = "KnockbackEffect")]
    public class KnockbackEffectDef : ScriptableObject, ICardEffect
    {
        [Min(1)] public int Cells = 1;

        public void Execute(ICardEffectContext ctx)
        {
            foreach (var t in ctx.Targets)
            {
                bool ok = ctx.Board != null && ctx.Board.TryKnockback(t, Cells);
                if (!ok) Debug.Log($"[Knockback] Fail (ignored): {t.name}, +{Cells}");
                else Debug.Log($"[Knockback] {t.name} moved +{Cells}.");
            }
        }
    }
}