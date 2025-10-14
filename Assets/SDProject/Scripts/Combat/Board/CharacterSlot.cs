using UnityEngine;

namespace SDProject.Combat.Board
{
    /// <summary>
    /// 보드의 한 슬롯을 표현합니다.
    /// - A안: 런타임에서 팀/인덱스를 부여받을 수 있도록 설계(assignAtRuntime).
    /// - 프리팹 단계에서 team/index를 비워두고, BoardLayout이 Instantiate 후 Configure로 세팅합니다.
    /// </summary>
    public class CharacterSlot : MonoBehaviour
    {
        [Header("Assignment Mode")]
        [Tooltip("true면 프리팹에서 team/index를 비워두고, 런타임에 BoardLayout이 Configure로 값을 세팅합니다.")]
        public bool assignAtRuntime = true;

        [Header("Identity (assignAtRuntime=false일 때만 사용)")]
        public TeamSide Team = TeamSide.Ally; // ★ TeamSide는 기존 정의를 재사용합니다(중복 정의 금지).
        public int Index = 0;

        [Header("Mount (유닛 스냅 지점)")]
        [Tooltip("유닛이 이 슬롯에 있을 때 위치를 맞출 기준 Transform입니다. 없으면 슬롯 Transform을 사용합니다.")]
        public Transform mount;

        /// <summary>
        /// BoardLayout이 슬롯을 생성할 때 호출하여 팀/인덱스를 확정합니다.
        /// </summary>
        public void Configure(TeamSide newTeam, int newIndex)
        {
            Team = newTeam;
            Index = newIndex;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // assignAtRuntime == true이면 에디터에서 team/index를 강제 변경하지 않습니다.
            // (프리팹을 '미지정' 상태로 둘 수 있게 하기 위함)
        }
#endif
    }
}