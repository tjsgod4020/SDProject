using System;
using System.Collections.Generic;
using UnityEngine;
using SD.Gameplay.Cards.Domain;         // CardDefinition
using SD.Gameplay.Cards.Infrastructure; // CardCatalog

namespace SD.Gameplay.Battle.Infrastructure
{
    /// <summary>
    /// 덱/손패/버림패를 관리하는 최소 런타임 저장소.
    /// - InitDeckFromCatalog: 카탈로그의 "Enabled" 카드 전부로 초기 덱 구성 (상한 없음)
    /// - StartPlayerTurn: 드로우
    /// - EndPlayerTurn: 손패 전부 버림
    /// - TryPlay: 손패에서 카드 1장 사용(버림으로 이동)
    /// - 이벤트: OnHandChanged, OnPileChanged
    /// </summary>
    public sealed class CardRuntimeRepository : MonoBehaviour
    {
        // ====== 모델 ======
        public sealed class CardInstance
        {
            public string Id;
            public CardDefinition Def;
            public CardInstance(CardDefinition def)
            {
                Def = def;
                Id = def?.Id ?? string.Empty;
            }
        }

        // ====== 설정 ======
        [SerializeField] private int _startHandCount = 5; // 전투 시작/턴 시작 시 손패 목표
        [SerializeField] private int _drawPerTurn = 5;    // 매 턴 시작 드로우 기본값

        // ====== 상태 ======
        private readonly List<CardInstance> _deck = new();
        private readonly List<CardInstance> _hand = new();
        private readonly List<CardInstance> _discard = new();

        public IReadOnlyList<CardInstance> Hand => _hand;

        // ====== 이벤트 ======
        /// <summary>손패 변경 시 호출: 현재 손패 스냅샷 전달</summary>
        public event Action<IReadOnlyList<CardInstance>> OnHandChanged;

        /// <summary>더미 수량 변경 시 호출: (Draw, Discard)</summary>
        public event Action<int, int> OnPileChanged;

        // ====== 초기화 ======
        /// <summary>
        /// 카탈로그에서 초기 덱을 구성한다.
        /// 기획 의도: Enabled=true 인 카드 "전부"를 사용(상한 없음).
        /// </summary>
        public void InitDeckFromCatalog()
        {
            _deck.Clear();
            _hand.Clear();
            _discard.Clear();

            var catalog = FindAnyObjectByType<CardCatalog>();
            if (catalog == null || catalog.All == null || catalog.All.Count == 0)
            {
                Debug.LogWarning("[Cards] CardCatalog not ready. Deck will be empty.");
                RaiseEvents();
                return;
            }

            // Enabled 카드 전부 수집
            var pool = new List<CardDefinition>();
            foreach (var def in catalog.All)
            {
                if (def != null && def.Enabled) pool.Add(def);
            }

            if (pool.Count == 0)
            {
                Debug.LogWarning("[Cards] No enabled cards in catalog.");
                RaiseEvents();
                return;
            }

            // 상한 없이 모두 덱에 편입
            Shuffle(pool);
            int take = pool.Count;
            for (int i = 0; i < take; i++)
                _deck.Add(new CardInstance(pool[i]));

            Shuffle(_deck);
            Debug.Log($"[Cards] Deck initialized: {_deck.Count} cards.");
            RaiseEvents();
        }

        // ====== 턴 제어 ======
        /// <summary>플레이어 턴 시작: 손패를 _drawPerTurn 장이 되도록 채우기.</summary>
        public void StartPlayerTurn()
        {
            int target = Mathf.Max(_drawPerTurn, _startHandCount);
            int need = Mathf.Max(0, target - _hand.Count);
            if (need > 0) Draw(need);
            RaiseEvents();
        }

        /// <summary>플레이어 턴 종료: 손패 전부 버림으로 이동.</summary>
        public void EndPlayerTurn()
        {
            DiscardAllHand();
            RaiseEvents();
        }

        // ====== 카드 사용 ======
        /// <summary>
        /// 손패에서 지정 카드를 사용. (v1: 효과 처리는 생략하고 버림으로만 이동)
        /// </summary>
        public bool TryPlay(CardInstance inst)
        {
            if (inst == null) return false;
            int idx = _hand.IndexOf(inst);
            if (idx < 0) return false;

            // TODO: EffectResolver 연동 (현재는 버림 이동만)
            var used = _hand[idx];
            _hand.RemoveAt(idx);
            _discard.Add(used);

            RaiseEvents();
            return true;
        }

        // ====== 드로우/버림 ======
        public int Draw(int count)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (_deck.Count == 0)
                {
                    // 리필: 버림 → 덱
                    if (_discard.Count == 0) break;
                    RefillDeckFromDiscard();
                }

                var top = _deck[_deck.Count - 1];
                _deck.RemoveAt(_deck.Count - 1);
                _hand.Add(top);
                drawn++;
            }
            return drawn;
        }

        public void DiscardAllHand()
        {
            if (_hand.Count == 0) return;
            _discard.AddRange(_hand);
            _hand.Clear();
        }

        private void RefillDeckFromDiscard()
        {
            _deck.AddRange(_discard);
            _discard.Clear();
            Shuffle(_deck);
        }

        // ====== 유틸 ======
        private void RaiseEvents()
        {
            OnHandChanged?.Invoke(_hand);
            OnPileChanged?.Invoke(_deck.Count, _discard.Count);
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
