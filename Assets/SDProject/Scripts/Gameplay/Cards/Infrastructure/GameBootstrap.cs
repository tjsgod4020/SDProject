using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;               // ★ 씬 전환
using SD.Gameplay.Cards.Domain;

namespace SD.Gameplay.Cards.Infrastructure
{
    [DefaultExecutionOrder(-90)] // DataTableLoader(-100) 이후 실행
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string _locale = "ko";
        [SerializeField] private CardCatalog _catalog;

        [Header("Scene Load")]
        [SerializeField] private bool _loadBattleOnBoot = true;     // ★ 부트 완료 후 Battle로 이동할지
        [SerializeField] private string _battleSceneName = "Battle"; // ★ Scenes/Battle/Battle.unity (Build Settings에 등록)

        // 내부 상태: 카드 정상 빌드 완료 여부
        private int _builtCardCount = 0;

        private void Awake()
        {
            // 1) 현재 Registry 상태 점검
            var rowsCard = global::SD.DataTable.TableRegistry.Get("CardData");
            var rowsName = global::SD.DataTable.TableRegistry.Get("CardName");
            var rowsDesc = global::SD.DataTable.TableRegistry.Get("CardDesc");

            int cntCard = rowsCard is System.Collections.ICollection cc ? cc.Count : 0;
            int cntName = rowsName is System.Collections.ICollection nc ? nc.Count : 0;
            int cntDesc = rowsDesc is System.Collections.ICollection dc ? dc.Count : 0;
            Debug.Log($"[Bootstrap] precheck tables: CardData={cntCard}, CardName={cntName}, CardDesc={cntDesc}");

            // 2) 비었으면 씬 내 DataTableLoader 호출로 즉시 복구 시도
            if (cntCard == 0 || cntName == 0 || cntDesc == 0)
            {
                var loader = FindAnyObjectByType<global::SD.DataTable.DataTableLoader>();
                if (loader != null)
                {
                    Debug.Log("[Bootstrap] TableRegistry empty → calling DataTableLoader.LoadAll()");
                    loader.LoadAll();

                    // 다시 조회
                    rowsCard = global::SD.DataTable.TableRegistry.Get("CardData");
                    rowsName = global::SD.DataTable.TableRegistry.Get("CardName");
                    rowsDesc = global::SD.DataTable.TableRegistry.Get("CardDesc");

                    cntCard = rowsCard is System.Collections.ICollection cc2 ? cc2.Count : 0;
                    cntName = rowsName is System.Collections.ICollection nc2 ? nc2.Count : 0;
                    cntDesc = rowsDesc is System.Collections.ICollection dc2 ? dc2.Count : 0;
                    Debug.Log($"[Bootstrap] post-load tables: CardData={cntCard}, CardName={cntName}, CardDesc={cntDesc}");
                }
                else
                {
                    Debug.LogWarning("[Bootstrap] DataTableLoader not found in scene.");
                }
            }

            // null-safe
            rowsCard ??= System.Array.Empty<object>();
            rowsName ??= System.Array.Empty<object>();
            rowsDesc ??= System.Array.Empty<object>();

            // 3) 카드 빌드
            var cards = CardFactory.BuildAll(rowsCard, rowsName, rowsDesc, _locale);
            _builtCardCount = cards.Count;

            // 4) 카탈로그 주입 (없으면 생성) + 생존 보장
            if (_catalog == null)
            {
                var go = new GameObject("CardCatalog");
                _catalog = go.AddComponent<CardCatalog>();
                DontDestroyOnLoad(go); // 씬 전환 유지
            }
            _catalog.Set(cards);

            Debug.Log($"[Bootstrap] CardCatalog ready: {_builtCardCount} cards");
        }

        private void Start()
        {
            // 5) 씬 전환 (Start에서 실행: Awake 직후 로드시 간헐 이슈 방지)
            if (!_loadBattleOnBoot)
            {
                Debug.Log("[Bootstrap] _loadBattleOnBoot=false → Boot 씬에 머무름");
                return;
            }

            if (_builtCardCount <= 0)
            {
                Debug.LogWarning("[Bootstrap] No cards built. Scene load skipped.");
                return;
            }

            // 빌드 세팅에 등록 여부 점검
#if UNITY_6000_0_OR_NEWER
            // 6000에서도 CanStreamedLevelBeLoaded는 동작합니다.
#endif
            if (!Application.CanStreamedLevelBeLoaded(_battleSceneName))
            {
                Debug.LogError($"[Bootstrap] Scene '{_battleSceneName}' is not in Build Settings. Add it before playing.");
                return;
            }

            Debug.Log($"[Bootstrap] Loading scene: '{_battleSceneName}'");
            SceneManager.LoadScene(_battleSceneName, LoadSceneMode.Single);
        }
    }
}