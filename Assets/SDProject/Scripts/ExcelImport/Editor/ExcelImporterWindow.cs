
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ExcelImporterWindow : EditorWindow
{
    private ExcelImportConfig _config;
    private Vector2 _scroll;
    private string _logHint = "Ready.";

    [MenuItem("Tools/Excel Importer")]
    public static void Open()
    {
        GetWindow<ExcelImporterWindow>("Excel Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("XLSX Import (NPOI)", EditorStyles.boldLabel);
        _config = (ExcelImportConfig)EditorGUILayout.ObjectField("Import Config", _config, typeof(ExcelImportConfig), false);

        EditorGUILayout.Space();
        if (_config != null)
        {
            EditorGUILayout.HelpBox($"XLSX: {_config.xlsxAssetPath}", MessageType.Info);

            if (GUILayout.Button("Run Import"))
            {
                RunImport();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(120));
            EditorGUILayout.LabelField("Log:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_logHint, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("Assign an ExcelImportConfig asset.", MessageType.Warning);
        }
    }

    private void RunImport()
    {
        if (_config == null) return;

        // Wire events (temporary for window)
        _config.events.OnProgress.RemoveAllListeners();
        _config.events.OnCompleted.RemoveAllListeners();

        _config.events.OnProgress.AddListener((msg) =>
        {
            _logHint = msg;
            Repaint();
        });

        _config.events.OnCompleted.AddListener((ok, msg) =>
        {
            _logHint = (ok ? "SUCCESS: " : "FAILED: ") + msg;
            Repaint();
        });

        var sm = new ExcelImportStateMachine(_config);
        sm.Start();
    }
}
#endif

/*
[Unity ���� ���̵�]
- �޴�: Tools > Excel Importer
- Import Config �Ҵ� �� "Run Import" ��ư Ŭ�� �� Console & â ���� �α� Ȯ��.
- �ʿ� �� ScriptableObject �̺�Ʈ�� ���� �� TMP UI�� �����ص� ��(��Ÿ�� Ȯ�ο�).
*/
