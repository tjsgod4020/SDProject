// Assets/SDProject/Scripts/Combat/Cards/Core/CardPlayController.cs
using UnityEngine;
using TMPro;
using SDProject.Data;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// v1 미니멀: 카드 클릭 → 전열(Front-most) 적 1명 자동 타겟 → 사용 처리.
    /// - CardData 스키마: cardId, displayName, apCost 만 사용
    /// - BoardRuntime: GetFrontMostEnemyUnit() 만 사용
    /// - 타겟팅/효과/JSON/레인필터는 이후 단계에서 확장
    /// </summary>
    public sealed class CardPlayController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BoardRuntime _board;     // 전열 적 찾기에 필요
        [SerializeField] private HandRuntime _hand;       // 사용 처리(손패에서 제거)

        [Header("UI (optional)")]
        [SerializeField] private TMP_Text _errorLabel;    // 간단 에러 라벨(v1 고정문구)

        private void Awake()
        {
            if (_board == null) _board = FindFirstObjectByType<BoardRuntime>(FindObjectsInactive.Include);
            if (_hand == null) _hand = FindFirstObjectByType<HandRuntime>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// CardView.OnClick() 에서 호출됨.
        /// </summary>
        public void PlayCard(CardData card, GameObject caster)
        {
            ClearError();

            if (card == null || caster == null)
            {
                EmitError("사용 조건 불일치");
                Debug.LogWarning("[CardPlay] Null card/caster.");
                return;
            }

            if (_board == null)
            {
                EmitError("보드 없음");
                Debug.LogWarning("[CardPlay] BoardRuntime missing.");
                return;
            }

            // v1: 전열(Front-most) 적 자동 선택
            var target = _board.GetFrontMostEnemyUnit();
            if (target == null)
            {
                EmitError("대상 없음");
                Debug.LogWarning("[CardPlay] No front-most enemy found.");
                return;
            }

            // (추가 효과/데미지 시스템이 아직 없으므로) 로그만 남김
            Debug.Log($"[CardPlay] '{(string.IsNullOrEmpty(card.displayName) ? card.name : card.displayName)}' AP:{card.apCost} → Target:{target.name}");

            // 손패에서 제거(버림 처리는 BattleController에서 HandRuntime.OnUsed를 구독해 Discard로 이동시키는 구조 권장)
            if (_hand != null)
            {
                _hand.MarkUsed(card);
            }

        }

        private void EmitError(string msg)
        {
            if (_errorLabel) _errorLabel.text = msg;
        }

        private void ClearError()
        {
            if (_errorLabel) _errorLabel.text = string.Empty;
        }
    }

    /// <summary>
    /// TurnPhase enum 값이 프로젝트마다 달라도 안전하게 이벤트를 발행하기 위한 헬퍼.
    /// </summary>
    internal static class TurnPhaseEventHelper
    {
        /*public static void RaiseTurnPhaseChangedLabelSafe(this GameEvents _, string label)
        {
            // enum이 있으면 써주고, 없으면 조용히 스킵
            if (System.Enum.TryParse<SDProject.Core.TurnPhase>(label, out var phase))
            {
                GameEvents.RaiseTurnPhaseChanged(phase);
            }
        }
        */
    }
}