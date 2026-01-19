using TMPro;
using UnityEngine;
using SD.Gameplay.Battle.Infrastructure;

namespace SD.Gameplay.Battle.Presentation
{
    /// ��/������ ���� ǥ��. Repository �̺�Ʈ ����.
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
            EnsureRepo();
            if (_repo != null)
            {
                _repo.OnPileChanged += OnPilesChanged;

                // �ʱ� ǥ�ô� ����ΰ�, Repository���� ù �̺�Ʈ�� �� �� ����.
                // (���ϸ� �Ʒ� �� �ٷ� 0,0 �ӽ� ǥ�� ����)
                OnPilesChanged(0, 0);
            }
        }

        private void OnDisable()
        {
            if (_repo != null)
                _repo.OnPileChanged -= OnPilesChanged;
        }

        private void EnsureRepo()
        {
            if (_repo == null) _repo = FindAnyObjectByType<CardRuntimeRepository>();
        }

        private void OnPilesChanged(int draw, int discard)
        {
            if (_drawText) _drawText.text = $"Draw ({draw})";
            if (_discardText) _discardText.text = $"Discard ({discard})";
        }
    }
}
