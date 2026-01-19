using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;               // �� �� ��ȯ
using SD.Gameplay.Cards.Domain;

namespace SD.Gameplay.Cards.Infrastructure
{
    [DefaultExecutionOrder(-90)] // DataTableLoader(-100) ���� ����
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string _locale = "ko";

        [Header("Scene Load")]
        [SerializeField] private bool _loadBattleOnBoot = true;     // �� ��Ʈ �Ϸ� �� Battle�� �̵�����
        [SerializeField] private string _battleSceneName = "Battle"; // �� Scenes/Battle/Battle.unity (Build Settings�� ���)

        // ���� ����: ī�� ���� ���� �Ϸ� ����
        private int _builtCardCount = 0;

        private void Awake()
        {
            // 1) ���� Registry ���� ����
            var rowsCard = global::SD.DataTable.TableRegistry.Get("CardData");
            var rowsName = global::SD.DataTable.TableRegistry.Get("CardName");
            var rowsDesc = global::SD.DataTable.TableRegistry.Get("CardDesc");

            int cntCard = rowsCard is System.Collections.ICollection cc ? cc.Count : 0;
            int cntName = rowsName is System.Collections.ICollection nc ? nc.Count : 0;
            int cntDesc = rowsDesc is System.Collections.ICollection dc ? dc.Count : 0;
            Debug.Log($"[Bootstrap] precheck tables: CardData={cntCard}, CardName={cntName}, CardDesc={cntDesc}");

            // 2) ������� �� �� DataTableLoader ȣ��� ��� ���� �õ�
            if (cntCard == 0 || cntName == 0 || cntDesc == 0)
            {
                var loader = FindAnyObjectByType<global::SD.DataTable.DataTableLoader>();
                if (loader != null)
                {
                    Debug.Log("[Bootstrap] TableRegistry empty �� calling DataTableLoader.LoadAll()");
                    loader.LoadAll();

                    // �ٽ� ��ȸ
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

            // 3) ī�� ����
            var cards = CardFactory.BuildAll(rowsCard, rowsName, rowsDesc, _locale);
            _builtCardCount = cards.Count;

            // 4) īŻ�α� ���� (CardCatalog 싱글톤 보장) + ���� ����
            if (CardCatalog.Instance == null)
            {
                var go = new GameObject("CardCatalog");
                go.AddComponent<CardCatalog>(); // Awake에서 Instance/DontDestroyOnLoad 처리
            }
            CardCatalog.Instance.Set(cards);

            Debug.Log($"[Bootstrap] CardCatalog ready: {_builtCardCount} cards");
        }

        private void Start()
        {
            // 5) �� ��ȯ (Start���� ����: Awake ���� �ε�� ���� �̽� ����)
            if (!_loadBattleOnBoot)
            {
                Debug.Log("[Bootstrap] _loadBattleOnBoot=false �� Boot ���� �ӹ���");
                return;
            }

            if (_builtCardCount <= 0)
            {
                Debug.LogWarning("[Bootstrap] No cards built. Scene load skipped.");
                return;
            }

            // ���� ���ÿ� ��� ���� ����
#if UNITY_6000_0_OR_NEWER
            // 6000������ CanStreamedLevelBeLoaded�� �����մϴ�.
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