// Assets/SDProject/Scripts/Combat/Cards/UI/CardView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SDProject.Data;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Single card UI. Binds CardData and relays click to CardPlayController.
    /// Safe-guards:
    /// - Button.onClick is auto-wired in Awake.
    /// - Texts set raycastTarget=false (won't swallow clicks).
    /// - OnClick() falls back to BoardRuntime to fetch a caster if missing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _ap;
        [SerializeField] private Image _typeIcon; // optional

        // Runtime
        private CardData _card;
        private GameObject _caster;
        private CardPlayController _play;

        /// <summary>Legacy-friendly external injector.</summary>
        public GameObject Caster
        {
            get => _caster;
            set => _caster = value;
        }

        private void Awake()
        {
            // Auto-wire button
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClick);
            }

            if (_title) _title.raycastTarget = false;
            if (_ap) _ap.raycastTarget = false;
        }

        /// <summary>Bind all runtime references.</summary>
        public void Bind(CardData card, GameObject caster, CardPlayController play)
        {
            _card = card;
            _caster = caster;
            _play = play;

            if (_title) _title.text = string.IsNullOrEmpty(card.displayName) ? card.name : card.displayName;
            if (_ap) _ap.text = card.apCost.ToString();
        }

        /// <summary>UI button callback.</summary>
        public void OnClick()
        {
            // Last-chance fallback for caster
            if (_caster == null)
            {
                var board = FindFirstObjectByType<SDProject.Combat.Board.BoardRuntime>(FindObjectsInactive.Include);
                _caster = board?.GetFirstAllyUnit();
            }

            Debug.Log($"[CardView] Click '{_card?.displayName}', play={_play != null}, caster={_caster != null}");
            if (_card == null || _play == null || _caster == null)
            {
                Debug.LogWarning("[CardView] Missing Card/PlayController/Caster.");
                return;
            }

            _play.PlayCard(_card, _caster);
        }
    }
}
