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
        // ���� ���(���δ�) Ÿ�̹� ������ ���� �� ������ ���
        yield return null;
        TryAssignNow();
    }

    private void TryAssignNow()
    {
        // �̹� �������� Caster�� �����Ǿ� ������ �ƹ��͵� ���� ����
        if (_cv.Caster != null) return;

        // ������ Ally ���� SimpleUnitBinder���� ����
        var binders = FindObjectsByType<SimpleUnitBinder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(b => b != null && b.Team == TeamSide.Ally)
            .ToList();

        if (binders.Count == 0)
        {
            Debug.LogWarning("[AssignFirstAllyCaster] No Ally binders found in scene.");
            return;
        }

        // ĳ���� �ĺ� ���� ��Ģ:
        // 1) preferFrontMost=true �̸� index�� ū ����(Front�� ū �ε���) �켱
        // 2) �ƴϸ� index�� ���� ����
        var ordered = preferFrontMost
            ? binders.OrderByDescending(b => b.Index)
            : binders.OrderBy(b => b.Index);

        // SimpleUnit ������Ʈ�� �ִ� ù �ĺ��� ĳ���ͷ� ���
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
