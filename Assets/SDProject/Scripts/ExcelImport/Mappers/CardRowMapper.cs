
#if UNITY_EDITOR
using SDProject.Data;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps row dictionary -> CardData. Columns: Id, Name, Cost, Rarity, Tags
/// </summary>
[CreateAssetMenu(fileName = "CardRowMapper", menuName = "Game/Excel/RowMapper/Card")]
public class CardRowMapper : ExcelRowMapperBase<CardData>
{
    public override bool TryMap(Dictionary<string, string> row, out CardData result)
    {
        result = new CardData
        {
            Id = GetString(row, "Id"),
            Name = GetString(row, "Name"),
            Cost = GetInt(row, "Cost", 0),
            Rarity = GetString(row, "Rarity"),
            Tags = GetString(row, "Tags")
        };

        if (string.IsNullOrEmpty(result.Id))
        {
            Debug.LogWarning("[CardRowMapper] Row skipped: Id is required.");
            return false;
        }
        return true;
    }
}
#endif

/*
[Unity 적용 가이드]
- Project 우클릭 → Create → Game/Excel/RowMapper/Card 로 매퍼 생성.
- 시트 컬럼명과 정확히 일치해야 함(Id/Name/Cost/Rarity/Tags). 엑셀 헤더를 맞춰주세요.
*/
