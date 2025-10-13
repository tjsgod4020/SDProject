using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SDProject.Combat.Cards
{
    public class CardView : MonoBehaviour
    {
        public CardDefinition Card;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI CostText;
        public TextMeshProUGUI DescText;
        public Button PlayButton;

        [Header("Runtime")]
        public CardPlayController PlayController;
        public GameObject Caster; // who plays the card

        private void Awake()
        {
            if (PlayButton) PlayButton.onClick.AddListener(OnClickPlay);
        }

        private void Start() => Refresh();

        public void Refresh()
        {
            if (!Card) return;
            if (TitleText) TitleText.SetText(string.IsNullOrEmpty(Card.NameId) ? Card.Id : Card.NameId);
            if (CostText) CostText.SetText(Card.Cost.ToString());
            if (DescText) DescText.SetText(string.IsNullOrEmpty(Card.DescId) ? "-" : Card.DescId);
        }

        private void OnClickPlay()
        {
            if (!Card || !PlayController || !Caster)
            {
                Debug.LogWarning("[CardView] Missing Card/PlayController/Caster.");
                return;
            }
            Debug.Log($"[CardView] Play {Card.Id}");
            PlayController.PlayCard(Card, Caster);
        }
    }
}
