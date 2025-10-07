
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SRP: Receive mapped rows and store them (e.g., into a ScriptableObject).
/// </summary>
public interface IExcelDataSink<T>
{
    void Clear();
    void AddRange(IEnumerable<T> items);
    void SaveDirty(); // mark asset dirty in Editor
    Object AsUnityObject(); // for ping/select in editor logs
}
#endif