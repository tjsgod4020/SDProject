using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SD.Gameplay.Battle.Presentation
{
    /// <summary>
    /// ������ �״�� �����ϰ�,
    /// DataTable(TableRegistry)���� CharacterData/EnemyData�� �о� ������� �����Ѵ�.
    /// </summary>
    public sealed class PartySlotSpawner : MonoBehaviour
    {
        [Header("Layouts")]
        [SerializeField] private PartySlotLayout _playerLayout;
        [SerializeField] private PartySlotLayout _enemyLayout;

        [Header("Table Ids")]
        [SerializeField] private string _characterTableId = "CharacterData";
        [SerializeField] private string _enemyTableId = "EnemyData";

        [Header("Fallback Unit Prefab (optional)")]
        [SerializeField] private GameObject _fallbackUnitPrefab;

        [Header("Options")]
        [SerializeField] private bool _spawnOnStart = true;

        [Tooltip("DataTableLoader/Registry �غ� ��ٸ��� �ִ� ������ ��")]
        [SerializeField] private int _maxWaitFrames = 60;

        private void Start()
        {
            if (_spawnOnStart)
                StartCoroutine(SpawnWhenReady());
        }

        private IEnumerator SpawnWhenReady()
        {
            // 1������ ���� (DataTableLoader/Awake ���� �̽� ����)
            yield return null;

            // Registry�� ���̺��� �ö�� ������ ��� ���(Ÿ�Ӿƿ� ����)
            for (int i = 0; i < _maxWaitFrames; i++)
            {
                var chars = global::SD.DataTable.TableRegistry.Get(_characterTableId);
                var enem = global::SD.DataTable.TableRegistry.Get(_enemyTableId);

                bool ready = (chars != null) && (enem != null); // �� �� �ϳ��� �ö���� ����(�α׷� �Ǵ�)
                if (ready) break;

                yield return null;
            }

            SpawnAll();
        }

        [ContextMenu("SpawnAll Now")]
        public void SpawnAll()
        {
            if (_playerLayout == null || _enemyLayout == null)
            {
                Debug.LogError("[PartySlotSpawner] Layout missing. Assign PlayerLayout/EnemyLayout.");
                return;
            }

            // Layout의 Start()에서 이미 슬롯이 생성되었으므로, 여기서는 건드리지 않음
            // 만약 슬롯이 없다면 경고만 출력하고 빌드
            if (_playerLayout.Slots == null || _playerLayout.Slots.Count == 0)
            {
                Debug.LogWarning("[PartySlotSpawner] PlayerLayout has no slots. Building now...");
                _playerLayout.BuildSlots(rebuild: true);
            }
            if (_enemyLayout.Slots == null || _enemyLayout.Slots.Count == 0)
            {
                Debug.LogWarning("[PartySlotSpawner] EnemyLayout has no slots. Building now...");
                _enemyLayout.BuildSlots(rebuild: true);
            }

            var chars = global::SD.DataTable.TableRegistry.Get(_characterTableId);
            var enem = global::SD.DataTable.TableRegistry.Get(_enemyTableId);

            Debug.Log($"[PartySlotSpawner] Reading tables: CharacterData={(chars != null ? chars.Count : 0)} rows, EnemyData={(enem != null ? enem.Count : 0)} rows");

            int pc = SpawnSide(chars, _playerLayout, isEnemy: false, _characterTableId);
            int ec = SpawnSide(enem, _enemyLayout, isEnemy: true, _enemyTableId);

            Debug.Log($"[PartySlotSpawner] Spawned Players={pc}, Enemies={ec} (Tables: '{_characterTableId}', '{_enemyTableId}')");
        }

        private int SpawnSide(System.Collections.IList rows, PartySlotLayout layout, bool isEnemy, string tableId)
        {
            if (layout.Slots == null || layout.Slots.Count == 0)
            {
                Debug.LogWarning($"[PartySlotSpawner] '{layout.name}' has 0 slots.");
                return 0;
            }

            if (rows == null)
            {
                Debug.LogWarning($"[PartySlotSpawner] Table '{tableId}' not found or unused. (Check DataTableConfig Id)");
                return 0;
            }

            if (rows.Count == 0)
            {
                Debug.LogWarning($"[PartySlotSpawner] Table '{tableId}' is empty.");
                return 0;
            }

            int spawnCount = 0;
            int slotIndex = 0;
            int enabledCount = 0;

            for (int i = 0; i < rows.Count && slotIndex < layout.Slots.Count; i++)
            {
                object row = rows[i];
                if (row == null)
                {
                    Debug.LogWarning($"[PartySlotSpawner] {tableId} row#{i + 1}: null row -> skip");
                    continue;
                }

                string id = GetString(row, "Id");
                string assetId = GetString(row, "PrefabKey");
                bool enabled = GetBool(row, "Enabled", defaultValue: true);

                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning($"[PartySlotSpawner] {tableId} row#{i + 1}: empty Id -> skip");
                    continue;
                }

                // Enabled가 false인 경우 스킵
                if (!enabled)
                {
                    Debug.Log($"[PartySlotSpawner] {tableId} '{id}': Enabled=false -> skip");
                    continue;
                }

                enabledCount++;

                Debug.Log($"[PartySlotSpawner] Spawning {(isEnemy ? "Enemy" : "Player")} '{id}' at slot {slotIndex} (AssetId: '{assetId}')");

                var go = InstantiateUnit(assetId, isEnemy, id);
                if (go != null)
                {
                    layout.Slots[slotIndex].Attach(go);
                    go.name = $"{(isEnemy ? "EN" : "PL")}_{id}";
                    spawnCount++;
                    slotIndex++;
                }
                else
                {
                    Debug.LogWarning($"[PartySlotSpawner] Failed to instantiate unit '{id}'");
                }
            }

            Debug.Log($"[PartySlotSpawner] {tableId}: {enabledCount} enabled rows, {spawnCount} spawned to slots");
            return spawnCount;
        }

        private GameObject InstantiateUnit(string assetId, bool isEnemy, string id)
        {
            // 1) PrefabPath (Resources) �켱
            if (!string.IsNullOrWhiteSpace(assetId))
            {
                GameObject prefab = null;

#if UNITY_EDITOR
                // 에디터: AssetDatabase를 사용하여 AssetId로 prefab 찾기
                prefab = FindPrefabByAssetId(assetId);
#else
                // 런타임: Resources 폴더에서 로드 (Resources/Battle/Units/PF_TestUnit 형식)
                string resourcesPath = ConvertAssetIdToResourcesPath(assetId);
                prefab = Resources.Load<GameObject>(resourcesPath);
#endif

                if (prefab != null)
                {
                    return Instantiate(prefab);
                }

                Debug.LogWarning($"[PartySlotSpawner] Prefab not found for AssetId: '{assetId}' (id:{id}). Fallback used.");
            }
            else
            {
                Debug.LogWarning($"[PartySlotSpawner] AssetId empty (id:{id}). Fallback used.");
            }

            // 2) fallback
            if (_fallbackUnitPrefab != null)
                return Instantiate(_fallbackUnitPrefab);

            // 3) ������: �� ������Ʈ(�� ����)
            return new GameObject($"Unit_Empty_{(isEnemy ? "EN" : "PL")}_{id}");
        }

#if UNITY_EDITOR
        /// <summary>
        /// AssetId로 prefab을 찾습니다. 이름으로 검색하거나 GUID로 직접 로드합니다.
        /// </summary>
        private static GameObject FindPrefabByAssetId(string assetId)
        {
            // 방법 1: GUID인 경우 직접 로드 시도
            if (IsGuid(assetId))
            {
                string path = AssetDatabase.GUIDToAssetPath(assetId);
                if (!string.IsNullOrEmpty(path))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) return prefab;
                }
            }

            // 방법 2: 이름으로 검색 (예: "PF_TestUnit")
            string[] guids = AssetDatabase.FindAssets($"{assetId} t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && (prefab.name == assetId || path.Contains(assetId)))
                {
                    return prefab;
                }
            }

            // 방법 3: Resources 폴더에서 시도
            string resourcesPath = ConvertAssetIdToResourcesPath(assetId);
            var resourcesPrefab = Resources.Load<GameObject>(resourcesPath);
            if (resourcesPrefab != null) return resourcesPrefab;

            return null;
        }

        /// <summary>
        /// 문자열이 GUID 형식인지 확인합니다.
        /// </summary>
        private static bool IsGuid(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length != 32) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(str, @"^[0-9a-fA-F]{32}$");
        }
#endif

        /// <summary>
        /// AssetId를 Resources 폴더 기준 경로로 변환합니다.
        /// 예: "PF_TestUnit" → "Battle/Units/PF_TestUnit"
        /// </summary>
        private static string ConvertAssetIdToResourcesPath(string assetId)
        {
            // 이미 경로 형식인 경우 그대로 반환
            if (assetId.Contains("/"))
                return assetId.Replace("Resources/", "").Replace(".prefab", "");

            // 파일명만 있는 경우 기본 경로 시도
            // Resources/Battle/Units/ 폴더 구조를 가정
            return $"Battle/Units/{assetId}";
        }

        private static string GetString(object o, string member)
        {
            var t = o.GetType();

            var pi = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (pi != null)
            {
                try { return pi.GetValue(o)?.ToString(); } catch { }
            }

            var fi = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (fi != null)
            {
                try { return fi.GetValue(o)?.ToString(); } catch { }
            }

            return null;
        }

        private static bool GetBool(object o, string member, bool defaultValue = false)
        {
            var t = o.GetType();

            var pi = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (pi != null)
            {
                try
                {
                    var value = pi.GetValue(o);
                    if (value is bool b) return b;
                    if (value != null && bool.TryParse(value.ToString(), out bool parsed)) return parsed;
                }
                catch { }
            }

            var fi = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (fi != null)
            {
                try
                {
                    var value = fi.GetValue(o);
                    if (value is bool b) return b;
                    if (value != null && bool.TryParse(value.ToString(), out bool parsed)) return parsed;
                }
                catch { }
            }

            return defaultValue;
        }
    }
}
