// Assets/SDProject/Scripts/UI/CardItemView.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;    // ★ 추가
using SDProject.Data;
using SDProject.Combat;

namespace SDProject.UI
{
    // IPointerClickHandler로 UI 클릭 처리
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

        // UI 클릭
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_hand == null || _data == null) return;

            // 좌클릭만 제거로 처리 (원하면 우클릭/더블클릭도 분기 가능)
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Debug.Log($"[CardItemView] Click remove: {_data.displayName}");
                _hand.Remove(_data);      // UI는 HandView가 이벤트로 재빌드
            }
        }
    }
}