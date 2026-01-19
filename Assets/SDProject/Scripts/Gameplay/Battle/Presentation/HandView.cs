using System.Collections.Generic;
using UnityEngine;
using SD.Gameplay.Battle.Infrastructure;     // CardRuntimeRepository, BattleSystem

namespace SD.Gameplay.Battle.Presentation
{
    public sealed class HandView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private Transform _cardsRoot;
        [SerializeField] private CardView _cardPrefab;

        [Header("Sources (Optional: 자동 탐색)")]
        [SerializeField] private CardRuntimeRepository _repo;
        [SerializeField] private BattleSystem _battle;

        private readonly List<CardView> _spawned = new();

        private void Reset()
        {
            if (_cardsRoot == null)
            {
                var t = transform.Find("Cards");
                if (t != null) _cardsRoot = t;
            }
        }

        private void Awake()
        {
            EnsureSources();
        }

        private void OnEnable()
        {
            EnsureSources();
            if (_repo != null)
                _repo.OnHandChanged += Rebuild; // 시그니처가 정확히 일치해야 함
        }

        private void OnDisable()
        {
            if (_repo != null)
                _repo.OnHandChanged -= Rebuild;
        }

        private void EnsureSources()
        {
            if (_repo == null) _repo = FindAnyObjectByType<CardRuntimeRepository>();
            if (_battle == null) _battle = FindAnyObjectByType<BattleSystem>();
        }

        // 🔧 핵심 수정: 파라미터 타입을 Repository의 중첩 타입으로 변경
        private void Rebuild(IReadOnlyList<CardRuntimeRepository.CardInstance> hand)
        {
            Clear();

            if (_cardsRoot == null || _cardPrefab == null || hand == null) return;

            for (int i = 0; i < hand.Count; i++)
            {
                var inst = hand[i];
                if (inst?.Def == null) continue;

                var view = Instantiate(_cardPrefab, _cardsRoot);
                view.Bind(inst.Def); // inst.Def 는 CardDefinition 이어야 함

                int index = i; // 클릭 시 플레이 요청 전달
                view.OnClicked += _ =>
                {
                    if (_battle != null)
                        _battle.TryPlayCard(index);
                };

                _spawned.Add(view);
            }
        }

        private void Clear()
        {
            foreach (var v in _spawned)
                if (v != null) Destroy(v.gameObject);
            _spawned.Clear();
        }
    }
}
