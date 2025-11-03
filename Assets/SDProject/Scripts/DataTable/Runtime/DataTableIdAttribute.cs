using System;

namespace SD.DataTable
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DataTableIdAttribute : Attribute
    {
        public string Id { get; }
        public DataTableIdAttribute(string id) => Id = id;
    }

}
