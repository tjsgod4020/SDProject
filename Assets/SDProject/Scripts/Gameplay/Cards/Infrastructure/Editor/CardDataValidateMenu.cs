#if UNITY_EDITOR
using System.Linq;
using SD.DataTable;
using SD.Gameplay.Cards.Infrastructure.Csv; // CsvCardRepository(TextAsset csv) 사용
using UnityEditor;
using UnityEngine;

public static class CardDataValidateMenu
{
    [MenuItem("SD/Data/Validate CardData.csv")]
    public static void Validate()
    {
        var config = FindConfigAsset();
        if (config == null || config.Tables == null || config.Tables.Count == 0)
        {
            Debug.LogError("[Validate] DataTableConfig을 찾지 못했거나 테이블이 비어있습니다. Tools > DataTables > Sync All 실행/Config 연결을 확인하세요.");
            return;
        }

        // 1) CardData 항목 하나를 찾는다
        var cardEntry = config.Tables.FirstOrDefault(
            t => string.Equals(t.Id, "CardData", System.StringComparison.OrdinalIgnoreCase));

        if (cardEntry.Csv == null)
        {
            Debug.LogError("[Validate] 'CardData' 항목을 찾지 못했거나 CSV가 비어있습니다.");
            return;
        }

        // 2) CsvCardRepository는 TextAsset 하나를 받으므로 그걸 넘긴다
        var repo = new CsvCardRepository(cardEntry.Csv); // ← 핵심 수정
        var ids = repo.All.Select(c => c.Id).ToList();

        if (ids.Count == 0)
        {
            Debug.LogWarning("[Validate] CardData가 0개로 로드되었습니다. Enabled/RowTypeName/CSV 내용을 확인하세요.");
        }
        else
        {
            Debug.Log($"CardData 유효성 OK. 총 {ids.Count}개\n- 상위 10개: {string.Join(", ", ids.Take(10))}");
        }
    }

    /// 프로젝트 전체에서 DataTableConfig를 찾아 테이블 수가 가장 많은 것을 선택
    private static DataTableConfig FindConfigAsset()
    {
        var guids = AssetDatabase.FindAssets("t:DataTableConfig");
        var assets = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<DataTableConfig>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null)
            .ToList();

        if (assets.Count == 0) return null;
        if (assets.Count == 1) return assets[0];

        var chosen = assets.OrderByDescending(a => a.Tables?.Count ?? 0).First();
        var info = string.Join("\n - ", assets.Select(a => $"{AssetDatabase.GetAssetPath(a)} (Tables={a.Tables?.Count ?? 0})"));
        Debug.LogWarning($"[Validate] DataTableConfig가 여러 개입니다. 테이블이 가장 많은 에셋을 선택합니다.\n - 후보:\n - {info}\n - 선택: {AssetDatabase.GetAssetPath(chosen)}");
        return chosen;
    }
}
#endif