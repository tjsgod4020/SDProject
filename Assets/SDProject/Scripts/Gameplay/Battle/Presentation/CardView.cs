using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SD.Gameplay.Cards.Domain;

namespace SD.Gameplay.Battle.Presentation
{
    /// 카드 1장의 UI 바인딩.
    /// - 제목/설명은 폴백 체인 적용: Display → *Id → Id
    /// - 클릭 시 이벤트 노출(OnClicked) + Button.onClick 연동
    public sealed class CardView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _desc;
        [SerializeField] private TMP_Text _cost;
        [SerializeField] private Image _art;
        [SerializeField] private Button _button;

        public event Action<CardDefinition> OnClicked;

        public CardDefinition Def { get; private set; }

        private void Awake()
        {
            // 버튼 자동 연결(없으면 무시)
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
                _button.onClick.AddListener(HandleClick);
            }
        }

        private void OnValidate()
        {
            // 에디터에서 누락시 자동 찾아 매핑 (이름 기준)
            if (_title == null) _title = transform.Find("Title")?.GetComponent<TMP_Text>();
            if (_desc == null) _desc = transform.Find("Desc")?.GetComponent<TMP_Text>();
            if (_cost == null)
            {
                var costRoot = transform.Find("CostBadge/CostText") ?? transform.Find("CostText");
                _cost = costRoot ? costRoot.GetComponent<TMP_Text>() : null;
            }
            if (_art == null) _art = transform.Find("Art")?.GetComponent<Image>();
            if (_button == null) _button = GetComponent<Button>();
        }

        public void Bind(CardDefinition def)
        {
            Def = def;

            // 🔹 제목 폴백: DisplayName → NameId → Id
            if (_title)
            {
                var title =
                    !string.IsNullOrWhiteSpace(def?.DisplayName) ? def.DisplayName :
                    !string.IsNullOrWhiteSpace(def?.NameId) ? def.NameId :
                    def?.Id ?? "";
                _title.text = title;
            }

            // 🔹 설명 폴백: DisplayDesc → DescId → ""
            if (_desc)
            {
                var desc =
                    !string.IsNullOrWhiteSpace(def?.DisplayDesc) ? def.DisplayDesc :
                    !string.IsNullOrWhiteSpace(def?.DescId) ? def.DescId :
                    "";
                _desc.text = desc;
            }

            if (_cost) _cost.text = (def != null) ? def.Cost.ToString() : "";

            // 아트는 아직 기획 미확정 → 비움(필요 시 sprite/addressables 연결)
            if (_art) _art.enabled = _art.sprite != null;
        }

        private void HandleClick()
        {
            if (Def == null) return;
            OnClicked?.Invoke(Def);
        }
    }
}
