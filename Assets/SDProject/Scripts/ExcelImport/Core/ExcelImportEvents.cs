
#if UNITY_EDITOR
using UnityEngine.Events;

[System.Serializable]
public class ImportProgressEvent : UnityEvent<string> { }

[System.Serializable]
public class ImportCompletedEvent : UnityEvent<bool, string> { } // success, message
#endif

/*
[Unity ���� ���̵�]
- �ٸ� �ý��۰� ���� ����: ����Ʈ ����/�ϷḦ UI�� �ΰŰ� ������ �� ����.
*/