using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SD.Gameplay.Battle.Domain;

namespace SD.Gameplay.Battle.Infrastructure
{
    public sealed class UnitCatalog : MonoBehaviour
    {
        public static UnitCatalog Instance { get; private set; }

        [SerializeField] private string _characterTableId = "Character";
        [SerializeField] private string _enemyTableId = "Enemy";

        private readonly List<UnitDefinition> _players = new List<UnitDefinition>();
        private readonly List<UnitDefinition> _enemies = new List<UnitDefinition>();

        public IReadOnlyList<UnitDefinition> Players { get { return _players; } }
        public IReadOnlyList<UnitDefinition> Enemies { get { return _enemies; } }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Build();
        }

        public void Build()
        {
            _players.Clear();
            _enemies.Clear();

            // 풀네임 호출 (using SD.DataTable 제거)
            IList chars = global::SD.DataTable.TableRegistry.Get(_characterTableId);
            IList enem = global::SD.DataTable.TableRegistry.Get(_enemyTableId);

            int pc = BuildList(chars, _players, _characterTableId);
            int ec = BuildList(enem, _enemies, _enemyTableId);

            Debug.Log("[Units] Catalog built → Players=" + pc + ", Enemies=" + ec);
        }

        private static int BuildList(IList rows, List<UnitDefinition> outList, string tableId)
        {
            if (rows == null)
            {
                Debug.LogWarning("[Units] Table '" + tableId + "' not found or unused.");
                return 0;
            }

            int added = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                object row = rows[i];
                if (row == null) continue;

                string id = GetString(row, "Id");
                string prefab = GetString(row, "Prefab");

                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning("[Units] " + tableId + " row#" + (i + 1) + ": empty Id -> skip");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(prefab))
                {
                    Debug.LogWarning("[Units] " + tableId + " '" + id + "': Prefab empty -> will use placeholder");
                }

                outList.Add(new UnitDefinition(id, prefab));
                added++;
            }
            return added;
        }

        private static string GetString(object o, string member)
        {
            Type t = o.GetType();
            var pi = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (pi != null) { try { var v = pi.GetValue(o); return v != null ? v.ToString() : null; } catch { } }
            var fi = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (fi != null) { try { var v = fi.GetValue(o); return v != null ? v.ToString() : null; } catch { } }
            return null;
        }
    }
}
