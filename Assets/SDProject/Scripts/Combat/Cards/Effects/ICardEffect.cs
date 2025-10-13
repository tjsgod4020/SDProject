using UnityEngine;

namespace SDProject.Combat.Cards
{
    public interface ICardEffectContext
    {
        GameObject Caster { get; }
        GameObject[] Targets { get; }
        System.Random Rng { get; }
        BoardRuntime Board { get; }
    }

    public interface ICardEffect
    {
        void Execute(ICardEffectContext ctx);
    }

    public class CardEffectContext : ICardEffectContext
    {
        public GameObject Caster { get; }
        public GameObject[] Targets { get; }
        public System.Random Rng { get; }
        public BoardRuntime Board { get; }

        public CardEffectContext(GameObject caster, GameObject[] targets, BoardRuntime board, System.Random rng = null)
        {
            Caster = caster;
            Targets = targets ?? System.Array.Empty<GameObject>();
            Board = board;
            Rng = rng ?? new System.Random();
        }
    }
}