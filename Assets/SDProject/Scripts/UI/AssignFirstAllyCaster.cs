// Assets/SDProject/Scripts/UI/Controls/AssignFirstAllyCaster.cs
using SDProject.Combat;
using SDProject.Combat.Cards;
using SDProject.Combat.Board;  // TeamSide enum
using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(CardView))]
public class AssignFirstAllyCaster : MonoBehaviour
{
    [Tooltip("Wait a frame so binders can register to BoardRuntime first.")]
    [SerializeField] private bool waitOneFrame = true;

    [Tooltip("If true, pick the ally with the highest slot index (front-most in our layout).")]
    [SerializeField] private bool preferFrontMost = true;

    private CardView _cv;

    private void Awake()
    {
        _cv = GetComponent<CardView>();
    }

    private void OnEnable()
    {
        if (waitOneFrame) StartCoroutine(AssignNextFrame());
        else TryAssignNow();
    }

    private IEnumerator AssignNextFrame()
    {
        // 슬롯 등록(바인더) 타이밍 보정을 위해 한 프레임 대기
        yield return null;
        TryAssignNow();
    }

    private void TryAssignNow()
    {
        // 이미 수동으로 Caster가 지정되어 있으면 아무것도 하지 않음
        if (_cv.Caster != null) return;

        // 씬에서 Ally 팀의 SimpleUnitBinder들을 수집
        var binders = FindObjectsByType<SimpleUnitBinder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(b => b != null && b.Team == TeamSide.Ally)
            .ToList();

        if (binders.Count == 0)
        {
            Debug.LogWarning("[AssignFirstAllyCaster] No Ally binders found in scene.");
            return;
        }

        // 캐스터 후보 선택 규칙:
        // 1) preferFrontMost=true 이면 index가 큰 순서(Front가 큰 인덱스) 우선
        // 2) 아니면 index가 작은 순서
        var ordered = preferFrontMost
            ? binders.OrderByDescending(b => b.Index)
            : binders.OrderBy(b => b.Index);

        // SimpleUnit 컴포넌트가 있는 첫 후보를 캐스터로 사용
        foreach (var b in ordered)
        {
            var su = b.GetComponent<SimpleUnit>();
            if (su != null)
            {
                _cv.Caster = su.gameObject;
                Debug.Log($"[AssignFirstAllyCaster] Caster assigned: {su.name} (team={b.Team}, index={b.Index})");
                return;
            }
        }

        Debug.LogWarning("[AssignFirstAllyCaster] Ally binders found, but no SimpleUnit component attached.");
    }
}
