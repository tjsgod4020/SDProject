using UnityEngine;
using SD.Gameplay.Battle.Domain;

namespace SD.Gameplay.Battle.Infrastructure
{
    /// 씬에 배치해서 아군/적군을 스폰한다.
    /// - PlayerRoot/EnemyRoot 아래에 차례로 배치
    /// - Prefab 경로가 없거나 로드 실패 시 플레이스홀더(스프라이트) 생성
    public sealed class PartySpawner : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform _playerRoot; // Scenes/Battle/Parties/PlayerParty
        [SerializeField] private Transform _enemyRoot;  // Scenes/Battle/Parties/EnemyParty

        [Header("Visuals")]
        [SerializeField] private Vector2 _spacing = new Vector2(1.2f, 0f); // 가로 간격
        [SerializeField] private float _startXPlayer = -4f;
        [SerializeField] private float _startXEnemy = 4f;
        [SerializeField] private float _y = 0f; // 배치 y

        private void Reset()
        {
            var pr = transform.Find("PlayerRoot");
            var er = transform.Find("EnemyRoot");
            if (pr) _playerRoot = pr as Transform;
            if (er) _enemyRoot = er as Transform;
        }

        private void Start()
        {
            var cat = UnitCatalog.Instance;
            if (cat == null)
            {
                Debug.LogError("[PartySpawner] UnitCatalog.Instance is null. Ensure it exists in Boot or Battle scene.");
                return;
            }

            SpawnSide(cat.Players, _playerRoot, _startXPlayer, isEnemy: false);
            SpawnSide(cat.Enemies, _enemyRoot, _startXEnemy, isEnemy: true);
        }

        private void SpawnSide(System.Collections.Generic.IReadOnlyList<UnitDefinition> defs, Transform root, float startX, bool isEnemy)
        {
            if (root == null)
            {
                Debug.LogWarning($"[PartySpawner] Missing root for {(isEnemy ? "Enemy" : "Player")}.");
                return;
            }
            if (defs == null || defs.Count == 0) return;

            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                var go = TryInstantiate(def, root, isEnemy);
                // 좌→우 or 우→좌 정렬
                float x = startX + (isEnemy ? -i : i) * _spacing.x;
                go.transform.localPosition = new Vector3(x, _y, 0f);
                go.name = $"{(isEnemy ? "EN" : "PL")}_{def.Id}";
            }
        }

        private GameObject TryInstantiate(UnitDefinition def, Transform parent, bool isEnemy)
        {
            GameObject go = null;
            if (!string.IsNullOrWhiteSpace(def.PrefabPath))
            {
                var prefab = Resources.Load<GameObject>(def.PrefabPath);
                if (prefab != null)
                    go = Instantiate(prefab, parent);
                else
                    Debug.LogWarning($"[PartySpawner] Prefab not found: '{def.PrefabPath}' for '{def.Id}'. Using placeholder.");
            }

            if (go == null) go = CreatePlaceholder(parent, isEnemy);
            return go;
        }

        private static GameObject CreatePlaceholder(Transform parent, bool isEnemy)
        {
            var go = new GameObject("UnitPlaceholder");
            go.transform.SetParent(parent, worldPositionStays: false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateQuadSprite();
            sr.color = isEnemy ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.2f, 0.5f, 0.9f);
            go.transform.localScale = Vector3.one * 0.9f;
            return go;
        }

        // 간단한 1×1 사각형 스프라이트 생성(런타임 캐시)
        private static Sprite _quadSprite;
        private static Sprite CreateQuadSprite()
        {
            if (_quadSprite != null) return _quadSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            _quadSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            _quadSprite.name = "RuntimeWhiteQuad";
            return _quadSprite;
        }
    }
}
