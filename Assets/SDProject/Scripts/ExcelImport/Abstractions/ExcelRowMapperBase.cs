
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional base with helpers (GetString/GetInt/etc).
/// </summary>
public abstract class ExcelRowMapperBase<T> : ScriptableObject, IExcelRowMapper<T>
{
    protected string GetString(Dictionary<string, string> row, string key, string defaultValue = "")
    {
        return row.TryGetValue(key, out var val) ? val : defaultValue;
    }

    protected int GetInt(Dictionary<string, string> row, string key, int defaultValue = 0)
    {
        if (row.TryGetValue(key, out var val) && int.TryParse(val, out var i)) return i;
        return defaultValue;
    }

    protected float GetFloat(Dictionary<string, string> row, string key, float defaultValue = 0f)
    {
        if (row.TryGetValue(key, out var val) && float.TryParse(val, out var f)) return f;
        return defaultValue;
    }

    public abstract bool TryMap(Dictionary<string, string> row, out T result);
}
#endif
