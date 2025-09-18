// Assets/SDProject/Scripts/UI/HandView.cs
using System.Linq;
using UnityEngine;
using SDProject.Combat;
using SDProject.Core.Messaging;

namespace SDProject.UI
{
    /// Renders player's hand at the bottom using a prefab per card.
    /// Listens to GameEvents.OnHandChanged and rebuilds.
    /// SRP: hand -> UI sync only.
    public class HandView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private HandRuntime handRuntime;    // 씬에서 연결 or Render로 주입
        [SerializeField] private RectTransform content;      // HandPanel의 컨텐츠
        [SerializeField] private GameObject cardItemPrefab;  // CardItemView 달린 프리팹

        private void OnEnable() => GameEvents.OnHandChanged += Rebuild;
        private void OnDisable() => GameEvents.OnHandChanged -= Rebuild;

        private void Start()
        {
            // 초기 카드가 이미 있으면 그리기
            Rebuild(handRuntime ? handRuntime.Count : 0);
        }

        private void ClearChildren()
        {
            if (!content) return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        private void Rebuild(int _)
        {
            if (!handRuntime || handRuntime.Cards == null || !content || !cardItemPrefab)
                return;

            ClearChildren();

            foreach (var card in handRuntime.Cards.ToList())
            {
                var go = Instantiate(cardItemPrefab, content);

                // ✅ 제네릭 누락 수정
                var view = go.GetComponent<CardItemView>();
                if (view != null)
                    view.Bind(card, handRuntime);
            }
        }

        // ✅ BattleController에서 호출해 handRuntime을 명시 주입 + 즉시 그리기
        public void Render(HandRuntime hand)
        {
            handRuntime = hand;
            Rebuild(hand != null ? hand.Count : 0);
        }
    }
}
