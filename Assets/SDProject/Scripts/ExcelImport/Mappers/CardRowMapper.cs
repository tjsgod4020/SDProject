
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
[Unity ���� ���̵�]
- Project ��Ŭ�� �� Create �� Game/Excel/RowMapper/Card �� ���� ����.
- ��Ʈ �÷���� ��Ȯ�� ��ġ�ؾ� ��(Id/Name/Cost/Rarity/Tags). ���� ����� �����ּ���.
*/
