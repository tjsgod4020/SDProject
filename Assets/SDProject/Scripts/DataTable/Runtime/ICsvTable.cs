using System.Collections.Generic;

namespace SD.DataTable
{
    public interface ICsvTable<TRow> where TRow : IHasStringId
    {
        IReadOnlyDictionary<string, TRow> ById { get; }
        IReadOnlyList<TRow> Rows { get; }
        bool TryGet(string id, out TRow row);
        TRow GetOrNull(string id);
    }
}
