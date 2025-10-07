
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObject sink for CardData (implements IExcelDataSink{CardData}).
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "Game/Data/CardDatabase")]
public class CardDatabase : ScriptableObject, IExcelDataSink<CardData>
{
    [SerializeField] private List<CardData> items = new List<CardData>();
    public IReadOnlyList<CardData> Items => items;

    public void Clear() => items.Clear();

    public void AddRange(IEnumerable<CardData> add)
    {
        items.AddRange(add);
    }

    public void SaveDirty()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    public Object AsUnityObject() => this;
}
#endif

/*
[Unity 적용 가이드]
- Project 우클릭 → Create → Game/Data/CardDatabase 로 자산 생성.
- 다른 테이블(캐릭터 등)도 동일 패턴으로 Database SO를 만들면 됨.
*/
