#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SD.DataTable;

[CustomEditor(typeof(SD.DataTable.DataTableConfig))]
public class DataTableConfigInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(8);
        if (GUILayout.Button("Sync From Folder"))
        {
            ScriptableObject cfg = (ScriptableObject)target;
            SD.DataTable.Editor.DataTableConfigAutoSync.SyncOneConfigNow(cfg);
        }
    }
}
#endif