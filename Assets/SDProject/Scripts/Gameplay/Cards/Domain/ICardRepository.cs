using System.Collections.Generic;

namespace SD.Gameplay.Cards.Domain
{
    public interface ICardRepository
    {
        bool TryGet(string id, out CardDataModel card);
        IEnumerable<CardDataModel> All { get; }
        int Count { get; }
    }
}
