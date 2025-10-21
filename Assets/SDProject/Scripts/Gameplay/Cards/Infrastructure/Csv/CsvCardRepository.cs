using System.Collections.Generic;
using System.Linq;
using SD.Gameplay.Cards.Domain;
using UnityEngine;

namespace SD.Gameplay.Cards.Infrastructure.Csv
{
    public sealed class CsvCardRepository : ICardRepository
    {
        private readonly Dictionary<string, CardDataModel> _byId;

        public CsvCardRepository(TextAsset cardDataCsv)
        {
            var list = CardCsvParser.Parse(cardDataCsv);
            _byId = new Dictionary<string, CardDataModel>(list.Count, System.StringComparer.OrdinalIgnoreCase);
            foreach (var c in list) _byId[c.Id] = c;
        }

        public bool TryGet(string id, out CardDataModel card) => _byId.TryGetValue(id, out card);
        public IEnumerable<CardDataModel> All => _byId.Values;
        public int Count => _byId.Count;
    }
}
