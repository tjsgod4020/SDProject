
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
[Unity ���� ���̵�]
- Project ��Ŭ�� �� Create �� Game/Data/CardDatabase �� �ڻ� ����.
- �ٸ� ���̺�(ĳ���� ��)�� ���� �������� Database SO�� ����� ��.
*/
