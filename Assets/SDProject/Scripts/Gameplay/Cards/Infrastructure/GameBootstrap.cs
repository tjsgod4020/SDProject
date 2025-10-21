using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SD.DataTable;                          // TableRegistry
using SD.Gameplay.Cards.Domain;             // CardDefinition
using SD.Gameplay.Cards.Infrastructure;     // CardFactory, CardCatalog

namespace SD.Gameplay.Cards.Infrastructure
{
    [DefaultExecutionOrder(-100)]
    /// <summary>
    /// 씬 시작 시 Registry에서 카드 테이블을 꺼내 CardDefinition으로 빌드하고 카탈로그에 등록.
    /// DataTableLoader(Awake)가 먼저 실행되어 TableRegistry에 세팅되어 있어야 한다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string _locale = "ko";  // "ko" or "en"
        [SerializeField] private CardCatalog _catalog;   // 씬에 없으면 자동 생성

        private void Awake()
        {
            // 1) DataTable에서 읽어온 IList들
            var rowsCard = TableRegistry.Get("CardData");
            var rowsName = TableRegistry.Get("CardName");
            var rowsDesc = TableRegistry.Get("CardDesc");

            Debug.Log($"[Bootstrap] tables: CardData={(rowsCard as System.Collections.ICollection)?.Count ?? 0}, " +
                      $"CardName={(rowsName as System.Collections.ICollection)?.Count ?? 0}, " +
                      $"CardDesc={(rowsDesc as System.Collections.ICollection)?.Count ?? 0}");

            // Null-safe: 없으면 빈 리스트로
            rowsCard ??= System.Array.Empty<object>();
            rowsName ??= System.Array.Empty<object>();
            rowsDesc ??= System.Array.Empty<object>();

            // 2) CardDefinition 빌드
            var cards = CardFactory.BuildAll(rowsCard, rowsName, rowsDesc, _locale);

            // 3) 카탈로그에 등록 (없으면 자동 생성)
            if (_catalog == null)
            {
                var go = new GameObject("CardCatalog");
                _catalog = go.AddComponent<CardCatalog>();
            }
            _catalog.Set(cards);



            Debug.Log($"[Bootstrap] CardCatalog ready: {cards.Count} cards");
        }
    }
}
