using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// ������ �� ������ ǥ���մϴ�.
    /// - A��: ��Ÿ�ӿ��� ��/�ε����� �ο����� �� �ֵ��� ����(assignAtRuntime).
    /// - ������ �ܰ迡�� team/index�� ����ΰ�, BoardLayout�� Instantiate �� Configure�� �����մϴ�.
    /// </summary>
    public class CharacterSlot : MonoBehaviour
    {
        [Header("Assignment Mode")]
        [Tooltip("true�� �����տ��� team/index�� ����ΰ�, ��Ÿ�ӿ� BoardLayout�� Configure�� ���� �����մϴ�.")]
        public bool assignAtRuntime = true;

        [Header("Identity (assignAtRuntime=false�� ���� ���)")]
        public TeamSide Team = TeamSide.Ally; // �� TeamSide�� ���� ���Ǹ� �����մϴ�(�ߺ� ���� ����).
        public int Index = 0;

        [Header("Mount (���� ���� ����)")]
        [Tooltip("������ �� ���Կ� ���� �� ��ġ�� ���� ���� Transform�Դϴ�. ������ ���� Transform�� ����մϴ�.")]
        public Transform mount;

        /// <summary>
        /// BoardLayout�� ������ ������ �� ȣ���Ͽ� ��/�ε����� Ȯ���մϴ�.
        /// </summary>
        public void Configure(TeamSide newTeam, int newIndex)
        {
            Team = newTeam;
            Index = newIndex;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // assignAtRuntime == true�̸� �����Ϳ��� team/index�� ���� �������� �ʽ��ϴ�.
            // (�������� '������' ���·� �� �� �ְ� �ϱ� ����)
        }
#endif
    }
}