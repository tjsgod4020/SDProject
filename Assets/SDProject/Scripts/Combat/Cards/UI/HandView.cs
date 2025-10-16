// Assets/SDProject/Scripts/Combat/Cards/UI/HandView.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SDProject.Data;

namespace SDProject.Combat.Cards
{
    /// <summary>
    /// Builds card UI under a layout parent. Does not touch layout values; lets LayoutGroup handle sizing.
    /// Safe-guards:
    /// - Auto-wire Button.onClick for each spawned card.
    /// - If caster is null, tries to fetch first ally unit from BoardRuntime.
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        [Header("Bind")]
        [SerializeField] private RectTransform _content;   // Where cards are spawned (HandPanel)
        [SerializeField] private CardView _cardPrefab;     // Card prefab (Image + Button + 2 TMP texts)

        private readonly List<CardView> _spawned = new();

        public void Rebuild(IReadOnlyList<CardData> items, GameObject caster, CardPlayController play)
        {
            if (_content == null || _cardPrefab == null)
            {
                Debug.LogWarning("[HandView] Missing _content or _cardPrefab.");
                return;
            }

            // Clear old
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i]) Destroy(_spawned[i].gameObject);
            _spawned.Clear();

            var count = items?.Count ?? 0;
            Debug.Log($"[HandView] rebuild count={count}");
            if (items == null) return;

            for (int i = 0; i < items.Count; i++)
            {
                var data = items[i];
                if (data == null) continue;

                var cv = Instantiate(_cardPrefab, _content);
                // Respect parent layout; do not override anchors/size.
                cv.Bind(data, caster, play);

                // Safety: force onClick wiring even if prefab was not set.
                var btn = cv.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(cv.OnClick);
                }

                // Fallback caster if still empty
                if (cv.Caster == null)
                {
                    var board = FindFirstObjectByType<SDProject.Combat.Board.BoardRuntime>(FindObjectsInactive.Include);
                    cv.Caster = board?.GetFirstAllyUnit();
                }

                _spawned.Add(cv);
            }
        }
    }
}
