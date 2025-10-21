using System.Collections.Generic;

namespace SD.Gameplay.Cards.Domain
{
    public enum EffectType { Damage, Heal, Shield, Buff, Debuff, Knockback, Pull, Push }

    [System.Serializable]
    public class CardEffect
    {
        public EffectType Type;
        public int Value;
        public int Duration;
        public int Cells; // 위치 이동 계열
    }

    [System.Serializable]
    public class CardEffectArray { public List<CardEffect> Items = new(); }
}
