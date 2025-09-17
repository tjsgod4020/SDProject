// Assets/SDProject/Scripts/UI/CardItemView.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;    // �� �߰�
using SDProject.Data;
using SDProject.Combat;

namespace SDProject.UI
{
    // IPointerClickHandler�� UI Ŭ�� ó��
    public class CardItemView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtCost;
        [SerializeField] private Image background;

        private CardData _data;
        private HandRuntime _hand;

        public void Bind(CardData data, HandRuntime hand)
        {
            _data = data;
            _hand = hand;
            if (txtName) txtName.text = data.displayName;
            if (txtCost) txtCost.text = $"AP {data.apCost}";
        }

        // UI Ŭ��
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_hand == null || _data == null) return;

            // ��Ŭ���� ���ŷ� ó�� (���ϸ� ��Ŭ��/����Ŭ���� �б� ����)
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Debug.Log($"[CardItemView] Click remove: {_data.displayName}");
                _hand.Remove(_data);      // UI�� HandView�� �̺�Ʈ�� �����
            }
        }
    }
}