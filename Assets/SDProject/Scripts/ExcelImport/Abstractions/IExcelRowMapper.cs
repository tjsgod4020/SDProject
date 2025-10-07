
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SRP: One responsibility = convert a row dictionary to a strongly-typed model object.
/// </summary>
public interface IExcelRowMapper<T>
{
    /// <summary>Return true if row is valid and out is set; log errors as needed.</summary>
    bool TryMap(Dictionary<string, string> row, out T result);
}
#endif
