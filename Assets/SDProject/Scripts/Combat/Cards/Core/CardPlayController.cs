// Assets/SDProject/Scripts/Combat/Cards/Core/CardPlayController.cs
using UnityEngine;
using TMPro;
using SDProject.Data;
using SDProject.Combat.Board;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// v1 �̴ϸ�: ī�� Ŭ�� �� ����(Front-most) �� 1�� �ڵ� Ÿ�� �� ��� ó��.
    /// - CardData ��Ű��: cardId, displayName, apCost �� ���
    /// - BoardRuntime: GetFrontMostEnemyUnit() �� ���
    /// - Ÿ����/ȿ��/JSON/�������ʹ� ���� �ܰ迡�� Ȯ��
    /// </summary>
    public sealed class CardPlayController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BoardRuntime _board;     // ���� �� ã�⿡ �ʿ�
        [SerializeField] private HandRuntime _hand;       // ��� ó��(���п��� ����)

        [Header("UI (optional)")]
        [SerializeField] private TMP_Text _errorLabel;    // ���� ���� ��(v1 ��������)

        private void Awake()
        {
            if (_board == null) _board = FindFirstObjectByType<BoardRuntime>(FindObjectsInactive.Include);
            if (_hand == null) _hand = FindFirstObjectByType<HandRuntime>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// CardView.OnClick() ���� ȣ���.
        /// </summary>
        public void PlayCard(CardData card, GameObject caster)
        {
            ClearError();

            if (card == null || caster == null)
            {
                EmitError("��� ���� ����ġ");
                Debug.LogWarning("[CardPlay] Null card/caster.");
                return;
            }

            if (_board == null)
            {
                EmitError("���� ����");
                Debug.LogWarning("[CardPlay] BoardRuntime missing.");
                return;
            }

            // v1: ����(Front-most) �� �ڵ� ����
            var target = _board.GetFrontMostEnemyUnit();
            if (target == null)
            {
                EmitError("��� ����");
                Debug.LogWarning("[CardPlay] No front-most enemy found.");
                return;
            }

            // (�߰� ȿ��/������ �ý����� ���� �����Ƿ�) �α׸� ����
            Debug.Log($"[CardPlay] '{(string.IsNullOrEmpty(card.displayName) ? card.name : card.displayName)}' AP:{card.apCost} �� Target:{target.name}");

            // ���п��� ����(���� ó���� BattleController���� HandRuntime.OnUsed�� ������ Discard�� �̵���Ű�� ���� ����)
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
    /// TurnPhase enum ���� ������Ʈ���� �޶� �����ϰ� �̺�Ʈ�� �����ϱ� ���� ����.
    /// </summary>
    internal static class TurnPhaseEventHelper
    {
        /*public static void RaiseTurnPhaseChangedLabelSafe(this GameEvents _, string label)
        {
            // enum�� ������ ���ְ�, ������ ������ ��ŵ
            if (System.Enum.TryParse<SDProject.Core.TurnPhase>(label, out var phase))
            {
                GameEvents.RaiseTurnPhaseChanged(phase);
            }
        }
        */
    }
}