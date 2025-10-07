
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExcelImportConfig", menuName = "Game/Excel/ImportConfig")]
public class ExcelImportConfig : ScriptableObject
{
    [Header("XLSX")]
    [Tooltip("Use Unity path under Assets/, e.g., Assets/DataTables/GameData.xlsx")]
    public string xlsxAssetPath;

    [Serializable]
    public class SheetImportSetting
    {
        public string sheetName = "Cards";
        [Min(0)] public int headerRowIndex = 0;
        [Min(0)] public int dataStartRowIndex = 1;

        [Header("Row Mapper (ScriptableObject implementing IExcelRowMapper<T>)")]
        public ScriptableObject rowMapper; // e.g., CardRowMapper

        [Header("Data Sink (ScriptableObject implementing IExcelDataSink<T>)")]
        public ScriptableObject dataSink;  // e.g., CardDatabase
    }

    [Header("Sheets")]
    public List<SheetImportSetting> sheets = new();

    [Header("Events")]
    public ExcelImportEvents events = new ExcelImportEvents();

    [Serializable]
    public class ExcelImportEvents
    {
        public ImportProgressEvent OnProgress = new ImportProgressEvent();
        public ImportCompletedEvent OnCompleted = new ImportCompletedEvent();
    }
}
#endif

/*
[Unity ���� ���̵�]
- Project ��Ŭ�� �� Create �� Game/Excel/ImportConfig ����.
- xlsxAssetPath: ��) "Assets/DataTables/GameData.xlsx"
- Sheets�� �� ��Ʈ�� �߰��ϰ�, ����/��ũ ScriptableObject�� ����.
- �ٸ� ���̺�(ĳ���� ��)�� ��Ʈ���� �׸� �߰��ϸ� ��.
*/
