using SDProject.Data;
using SDProject.DataTable;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SDProject.DataBridge
{
    /// <summary>
    /// CardData.csv �� �о� ī�� Ű/AP�� Ȯ���ϰ�,
    /// TextTableLoader���� NameId/DescId�� �ؼ��� ��Ÿ�� CardData�� �����.
    /// DeckRuntime._deckSource �� �����ؼ� ���.
    /// </summary>
    public sealed class DeckSourceFromCardData : MonoBehaviour, IDeckSource
    {
        [Header("Resources")]
        [SerializeField] private string _cardDataPath = "DataTableGen/CardData";

        [Header("Debug")]
        [SerializeField] private bool _logEach = false;

        public IReadOnlyList<CardData> GetInitialDeck()
        {
            TextTableLoader.EnsureLoaded();

            var ta = Resources.Load<TextAsset>(_cardDataPath);
            if (ta == null || string.IsNullOrEmpty(ta.text))
            {
                Debug.LogWarning($"[DeckSourceFromCardData] Missing or empty: {_cardDataPath}.csv");
                return System.Array.Empty<CardData>();
            }

            var rawRows = CsvUtil.Parse(ta.text);
            var list = new List<CardData>(rawRows.Count);
            int valid = 0;

            foreach (var row in rawRows)
            {
                if (!row.TryGet("Id", out var id) || string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning("[DeckSourceFromCardData] Skip row: missing Id");
                    continue;
                }
                row.TryGet("NameId", out var nameId);
                row.TryGet("DescId", out var descId);
                row.TryGet("AP", out var apStr);
                var ap = CsvUtil.ParseInt(apStr, 1);

                // �̸�/���� �ؼ�
                var disp = TextTableLoader.ResolveName(nameId);
                var desc = TextTableLoader.ResolveDesc(descId);

                // ��Ÿ�� CardData ����
                var cd = ScriptableObject.CreateInstance<CardData>();
                cd.cardId = id.Trim();
                cd.displayName = string.IsNullOrWhiteSpace(disp) ? cd.cardId : disp.Trim();
                cd.description = desc ?? string.Empty;
                cd.apCost = Mathf.Max(0, ap);

                list.Add(cd);
                valid++;

                if (_logEach)
                    Debug.Log($"[DeckSourceFromCardData] +Card id='{cd.cardId}', name='{cd.displayName}', ap={cd.apCost}");
            }

            Debug.Log($"[DeckSourceFromCardData] Built {valid} CardData(s) from '{_cardDataPath}.csv'");
            return list;
        }

        // Optional: DeckRuntime ����׿�
        public string DebugDeckName => "CardData.csv (with TextTables)";
        public int DebugDeckCount => -1;
    }
}
