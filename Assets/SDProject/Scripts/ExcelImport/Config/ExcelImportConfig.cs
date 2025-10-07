
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
[Unity 적용 가이드]
- Project 우클릭 → Create → Game/Excel/ImportConfig 생성.
- xlsxAssetPath: 예) "Assets/DataTables/GameData.xlsx"
- Sheets에 각 시트를 추가하고, 매퍼/싱크 ScriptableObject를 연결.
- 다른 테이블(캐릭터 등)도 시트별로 항목만 추가하면 됨.
*/
