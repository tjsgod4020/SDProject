
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// State pattern for import flow: Idle -> LoadWorkbook -> ParseSheets -> Completed/Failed
/// Keeps logs at key points. Uses UnityEvents from config.
/// </summary>
public class ExcelImportStateMachine
{
    // ---------- States ----------
    // Note: Keep nested states private; only the state machine itself uses them.
    private abstract class State
    {
        protected readonly ExcelImportStateMachine ctx;
        protected State(ExcelImportStateMachine c) { ctx = c; }
        public virtual void Enter() { }
        public virtual void Tick() { }
    }

    private class IdleState : State
    {
        public IdleState(ExcelImportStateMachine c) : base(c) { }
        public override void Enter() { ctx.Log("Idle."); }
    }

    private class LoadWorkbookState : State
    {
        public LoadWorkbookState(ExcelImportStateMachine c) : base(c) { }
        public override void Enter()
        {
            ctx.Log("Loading workbook...");
            try
            {
                ctx.fullPath = Path.GetFullPath(ctx.assetPath);
                if (!File.Exists(ctx.fullPath)) throw new FileNotFoundException(ctx.fullPath);
                ctx.TransitionTo(new ParseSheetsState(ctx)); // internal transition
            }
            catch (Exception ex)
            {
                ctx.Fail($"LoadWorkbook failed: {ex.Message}");
            }
        }
    }

    private class ParseSheetsState : State
    {
        public ParseSheetsState(ExcelImportStateMachine c) : base(c) { }
        public override void Enter()
        {
            ctx.Log("Parsing sheets...");
            try
            {
                foreach (var s in ctx.settings)
                {
                    if (s.rowMapper == null || s.dataSink == null)
                    {
                        ctx.LogWarn($"Sheet '{s.sheetName}' skipped: mapper or sink not assigned.");
                        continue;
                    }

                    var rows = ExcelReader.ReadSheet(ctx.fullPath, s.sheetName, s.headerRowIndex, s.dataStartRowIndex);

                    // Infer T from IExcelRowMapper<T>
                    var mapperType = s.rowMapper.GetType();
                    var mapperIface = Array.Find(mapperType.GetInterfaces(), i =>
                        i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IExcelRowMapper"));
                    if (mapperIface == null)
                    {
                        ctx.LogError($"Mapper {mapperType.Name} does not implement IExcelRowMapper<T>.");
                        continue;
                    }
                    var modelType = mapperIface.GetGenericArguments()[0];

                    var sinkType = s.dataSink.GetType();
                    var sinkIface = Array.Find(sinkType.GetInterfaces(), i =>
                        i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IExcelDataSink"));
                    if (sinkIface == null || sinkIface.GetGenericArguments()[0] != modelType)
                    {
                        ctx.LogError($"Sink {sinkType.Name} is not IExcelDataSink<{modelType.Name}>.");
                        continue;
                    }

                    // Clear sink
                    sinkType.GetMethod("Clear").Invoke(s.dataSink, null);

                    // Map rows -> List<T>
                    var mappedList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(modelType));
                    var tryMap = mapperType.GetMethod("TryMap");

                    foreach (var row in rows)
                    {
                        object[] args = new object[] { row, null };
                        bool ok = (bool)tryMap.Invoke(s.rowMapper, args);
                        if (ok) mappedList.Add(args[1]); // out T result
                    }

                    // Persist
                    sinkType.GetMethod("AddRange").Invoke(s.dataSink, new object[] { mappedList });
                    sinkType.GetMethod("SaveDirty").Invoke(s.dataSink, null);

                    ctx.Log($"Sheet '{s.sheetName}': {mappedList.Count} rows imported → {s.dataSink.name}");
                }

                ctx.TransitionTo(new CompletedState(ctx, true, "Import completed."));
            }
            catch (Exception ex)
            {
                ctx.Fail($"ParseSheets failed: {ex}");
            }
        }
    }

    private class CompletedState : State
    {
        private readonly bool success;
        private readonly string message;
        public CompletedState(ExcelImportStateMachine c, bool success, string message) : base(c)
        { this.success = success; this.message = message; }
        public override void Enter()
        {
            ctx.Log($"Completed. success={success} message={message}");
            ctx.events?.OnCompleted?.Invoke(success, message);
        }
    }

    // ---------- Context ----------
    private State _state;
    private readonly ExcelImportConfig config;
    private readonly List<ExcelImportConfig.SheetImportSetting> settings;
    private string assetPath => config.xlsxAssetPath;
    private string _fullPath;
    private ExcelImportConfig.ExcelImportEvents events => config.events;

    public string fullPath { get => _fullPath; set => _fullPath = value; }

    public ExcelImportStateMachine(ExcelImportConfig cfg)
    {
        config = cfg;
        settings = cfg.sheets;
        _state = new IdleState(this);
    }

    /// <summary>Entry point from EditorWindow/UI.</summary>
    public void Start()
    {
        events?.OnProgress?.Invoke("Import started.");
        TransitionTo(new LoadWorkbookState(this));
    }

    // ✅ FIX: make this private (or internal) so its parameter accessibility matches.
    private void TransitionTo(State next)
    {
        _state = next;
        _state.Enter();
    }

    public void Log(string msg)
    {
        Debug.Log($"[ExcelImport] {msg}");
        events?.OnProgress?.Invoke(msg);
    }

    public void LogWarn(string msg)
    {
        Debug.LogWarning($"[ExcelImport] {msg}");
        events?.OnProgress?.Invoke("WARN: " + msg);
    }

    public void LogError(string msg)
    {
        Debug.LogError($"[ExcelImport] {msg}");
        events?.OnProgress?.Invoke("ERROR: " + msg);
    }

    public void Fail(string message)
    {
        LogError(message);
        TransitionTo(new CompletedState(this, false, message));
    }
}
#endif
