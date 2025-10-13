using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SDProject.DataTable
{
    public class DataTableLoader : MonoBehaviour
    {
        public enum LoadState { Idle, Loading, Completed, Failed }

        [SerializeField] private TableRegistry registry;
        [SerializeField] private LoadState state = LoadState.Idle;
        [SerializeField] private int loadedCount;
        [SerializeField] private string lastMessage;

        public event Action<string> OnTableLoaded;
        public event Action OnAllTablesLoaded;
        public event Action<string> OnLoadFailed;

        public LoadState State => state;
        public int LoadedCount => loadedCount;

        private void Awake()
        {
            if (registry == null)
            {
                Fail("Registry not assigned.");
                return;
            }
            StartCoroutine(LoadAllRoutine());
        }

        public void Reload()
        {
            if (state == LoadState.Loading) return;
            StartCoroutine(LoadAllRoutine());
        }

        private IEnumerator LoadAllRoutine()
        {
            SetState(LoadState.Loading, "Begin loading tables.");
            loadedCount = 0;

            var entries = registry.entries
                .Where(e => e != null && e.enabled && !string.IsNullOrWhiteSpace(e.tableId))
                .OrderBy(e => e.order);

            foreach (var e in entries)
            {
                var key = DataTablePaths.GetResourcesKey(e.tableId);
                var ta = Resources.Load<TextAsset>(key);
                if (ta == null)
                {
                    Fail($"Missing generated TextAsset at Resources/{key}");
                    yield break;
                }

                // Instantiate a specific TableAsset when available: {tableId}Table : TableAsset
                var tableAsset = CreateTableAssetForId(e.tableId);
                try
                {
                    tableAsset.Apply(ta.text);
                    TableHub.Register(e.tableId, tableAsset);
                    loadedCount++;
                    Debug.Log($"[DataTableLoader] Loaded '{e.tableId}' into {tableAsset.GetType().Name}.");
                    OnTableLoaded?.Invoke(e.tableId);
                }
                catch (Exception ex)
                {
                    Fail($"Apply failed for '{e.tableId}': {ex.Message}");
                    yield break;
                }

                yield return null; // distribute frame cost
            }

            SetState(LoadState.Completed, $"Loaded {loadedCount} tables.");
            OnAllTablesLoaded?.Invoke();
        }

        private TableAsset CreateTableAssetForId(string tableId)
        {
            var typeName = tableId + "Table";
            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => SafeTypes(a))
                .FirstOrDefault(t => typeof(TableAsset).IsAssignableFrom(t) && t.Name == typeName);

            var instance = (TableAsset)(type != null
                ? ScriptableObject.CreateInstance(type)
                : ScriptableObject.CreateInstance<GenericCsvTable>());

            instance.name = typeName;
            return instance;
        }

        private static Type[] SafeTypes(Assembly a)
        {
            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
        }

        private void SetState(LoadState s, string message)
        {
            state = s;
            lastMessage = message;
            Debug.Log($"[DataTableLoader] State={s}. {message}");
        }

        private void Fail(string reason)
        {
            state = LoadState.Failed;
            lastMessage = reason;
            Debug.LogError($"[DataTableLoader] FAILED: {reason}");
            OnLoadFailed?.Invoke(reason);
        }
    }
}