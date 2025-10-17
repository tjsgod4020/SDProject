// Assets/SDProject/Scripts/Combat/DeckRuntime.cs
using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;          // CardData
using SDProject.DataBridge;    // IDeckSource

namespace SDProject.Combat
{
    /// <summary>
    /// Runtime deck manager.
    /// - 초기화: IDeckSource에서 초기 덱을 받아 drawPile 구성(필요 시 셔플 1회).
    /// - 드로우/버림 더미 관리, 카운트 제공.
    /// - SRP: UI/HUD, Hand, Battle FSM은 외부에서 이벤트로만 연결.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeckRuntime : MonoBehaviour
    {
        [Header("Source (Adapter)")]
        [Tooltip("IDeckSource를 구현한 어댑터 컴포넌트를 연결하세요. 예) DeckSourceFromCardData")]
        [SerializeField] private MonoBehaviour _deckSource;

        [Header("Config")]
        [Min(1)][SerializeField] private int _drawPerTurn = 5;
        [Min(1)][SerializeField] private int _handMax = 10;
        [SerializeField] private bool _shuffleOnceOnInit = true;
        [SerializeField] private bool _reshuffleWhenEmpty = true;

        // Runtime
        private IDeckSource _src;
        private bool _initialized;
        private readonly List<CardData> _drawPile = new();
        private readonly List<CardData> _discard = new();

        // ── Public props for other systems (BattleController/HandRuntime 등) ──
        public int DrawPerTurn => _drawPerTurn;
        public int HandMax => _handMax;
        public int DrawCount => _drawPile.Count;
        public int DiscardCount => _discard.Count;

        private void OnValidate()
        {
            if (_drawPerTurn < 1) _drawPerTurn = 1;
            if (_handMax < 1) _handMax = 1;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>초기 덱을 소스에서 가져와 drawPile 구성. 중복 호출 안전.</summary>
        public void EnsureInitialized()
        {
            if (_initialized) return;

            _src = _deckSource as IDeckSource;
            if (_src == null)
            {
                Debug.LogError("[Deck] _deckSource is null or does not implement IDeckSource.");
                return;
            }

            var init = _src.GetInitialDeck();
            _drawPile.Clear();
            _discard.Clear();

            if (init != null && init.Count > 0)
            {
                _drawPile.AddRange(init);
                if (_shuffleOnceOnInit) FisherYatesShuffle(_drawPile);
            }

            Debug.Log($"[Deck] init: drawPile={_drawPile.Count}, discard={_discard.Count}");
            _initialized = true;
        }

        /// <summary>초기 소스 재적용(디버그/개발용).</summary>
        public void ResetFromSource()
        {
            _initialized = false;
            EnsureInitialized();
        }

        /// <summary>모든 더미 비움(디버그/개발용).</summary>
        public void ClearAll()
        {
            _drawPile.Clear();
            _discard.Clear();
            Debug.Log("[Deck] ClearAll: draw=0, discard=0");
        }

        /// <summary>count장 드로우. 부족하면 가능한 만큼만 반환. 필요 시 셔플-리필.</summary>
        public List<CardData> Draw(int count)
        {
            EnsureInitialized();

            var result = new List<CardData>(count);
            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    if (_reshuffleWhenEmpty && _discard.Count > 0)
                    {
                        // Discard -> Draw로 이동 및 셔플
                        _drawPile.AddRange(_discard);
                        _discard.Clear();
                        FisherYatesShuffle(_drawPile);
                        Debug.Log($"[Deck] Reshuffle -> draw={_drawPile.Count}, discard={_discard.Count}");
                    }
                }

                if (_drawPile.Count == 0)
                {
                    // 더 이상 뽑을 카드가 없음
                    break;
                }

                var last = _drawPile[^1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                result.Add(last);
            }

            return result;
        }

        /// <summary>단일 카드를 버림 더미로 이동.</summary>
        public void Discard(CardData c)
        {
            if (c != null) _discard.Add(c);
        }

        /// <summary>여러 장을 버림 더미로 이동.</summary>
        public void Discard(IEnumerable<CardData> cs)
        {
            if (cs == null) return;
            foreach (var c in cs)
                if (c != null) _discard.Add(c);
        }

        // ── Utils ────────────────────────────────────────────────────────────
        private static void FisherYatesShuffle<T>(IList<T> list)
        {
            // UnityEngine.Random 사용(Fisher-Yates)
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}