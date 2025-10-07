
#if UNITY_EDITOR
using UnityEngine.Events;

[System.Serializable]
public class ImportProgressEvent : UnityEvent<string> { }

[System.Serializable]
public class ImportCompletedEvent : UnityEvent<bool, string> { } // success, message
#endif

/*
[Unity 적용 가이드]
- 다른 시스템과 느슨 결합: 임포트 진행/완료를 UI나 로거가 구독할 수 있음.
*/