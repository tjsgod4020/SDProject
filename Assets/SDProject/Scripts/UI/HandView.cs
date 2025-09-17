using System.Linq;
using UnityEngine;
using SDProject.Combat;
using SDProject.Core.Messaging;

namespace SDProject.UI
{
    /// <summary>
    /// Renders player's hand at the bottom using a prefab per card.
    /// Listens to GameEvents.OnHandChanged and rebuilds.
    /// SRP: hand -> UI sync only.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private HandRuntime handRuntime;           // assign in scene
        [SerializeField] private RectTransform content;             // bottom container
        [SerializeField] private GameObject cardItemPrefab;         // prefab with CardItemView

        private void OnEnable()
        {
            GameEvents.OnHandChanged += Rebuild;
        }

        private void OnDisable()
        {
            GameEvents.OnHandChanged -= Rebuild;
        }

        private void Start()
        {
            // initial build if hand already has cards
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
                var view = go.GetComponent<CardItemView>();
                if (view) view.Bind(card, handRuntime);
            }
        }
    }
}
