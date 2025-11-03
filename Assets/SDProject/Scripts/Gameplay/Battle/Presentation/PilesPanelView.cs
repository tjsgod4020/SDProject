using TMPro;
using UnityEngine;
using SD.Gameplay.Battle.Infrastructure;

namespace SD.Gameplay.Battle.Presentation
{
    /// 덱/버린패 개수 표시. Repository 이벤트 구독.
    public sealed class PilesPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _drawText;     // "Draw (..)"
        [SerializeField] private TMP_Text _discardText;  // "Discard (..)"
        [SerializeField] private CardRuntimeRepository _repo;

        private void Reset()
        {
            if (_repo == null) _repo = FindAnyObjectByType<CardRuntimeRepository>();
        }

        private void OnEnable()
        {
            if (_repo == null) _repo = FindAnyObjectByType<CardRuntimeRepository>();
            if (_repo != null)
            {
                _repo.OnPileChanged += OnPilesChanged;

                // 초기 표시는 비워두고, Repository에서 첫 이벤트가 올 때 갱신.
                // (원하면 아래 한 줄로 0,0 임시 표기 가능)
                OnPilesChanged(0, 0);
            }
        }

        private void OnDisable()
        {
            if (_repo != null)
                _repo.OnPileChanged -= OnPilesChanged;
        }

        private void OnPilesChanged(int draw, int discard)
        {
            if (_drawText) _drawText.text = $"Draw ({draw})";
            if (_discardText) _discardText.text = $"Discard ({discard})";
        }
    }
}
