using System.Collections.Generic;
using UnityEngine;
using SD.Gameplay.Cards.Domain;

namespace SD.Gameplay.Cards.Infrastructure
{
    /// <summary>런타임 카드 데이터 저장소(싱글톤).</summary>
    public sealed class CardCatalog : MonoBehaviour
    {
        public static CardCatalog Instance { get; private set; }
        private List<CardDefinition> _cards = new();
        public IReadOnlyList<CardDefinition> All => _cards;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Set(List<CardDefinition> cards) => _cards = cards ?? new();

        public CardDefinition FindById(string id) =>
            _cards.Find(c => c.Id.Equals(id, System.StringComparison.OrdinalIgnoreCase));
    }
}
