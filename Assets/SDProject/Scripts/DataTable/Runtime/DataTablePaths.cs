using UnityEngine;

namespace SDProject.DataTable
{
    public static class DataTablePaths
    {
        // Authoring .xlsx location
        public const string XlsxRoot = "Assets/SDProject/DataTable/Xlsx";

        // Generated .csv/.json under Resources
        // => Resources.Load<TextAsset>($"SDProject/DataTableGen/{tableId}")
        public const string ResourcesGenRoot = "Assets/SDProject/DataTable/Resources/DataTableGen";
        public const string ResourcesKeyPrefix = "DataTableGen/";

        // Optional place to create schema/table assets
        public const string SchemasRoot = "Assets/SDProject/DataTable/Schemas";
        public const string TablesRoot = "Assets/SDProject/DataTable/Tables";

        public static string GetXlsxPath(string tableId) =>
            $"{XlsxRoot}/{tableId}.xlsx";

        public static string GetGeneratedCsvPath(string tableId) =>
            $"{ResourcesGenRoot}/{tableId}.csv";

        public static string GetResourcesKey(string tableId) =>
            $"{ResourcesKeyPrefix}{tableId}";
    }
}
