using System;
using UnityEngine;
using UnityEngine.Events;

namespace SDProject.Combat.Cards
{
    [Serializable] public class TargetSelectionRequestEvent : UnityEvent<TargetType> { }
    [Serializable] public class TargetSelectionProvidedEvent : UnityEvent<GameObject[]> { }

    public class TargetingSystem : MonoBehaviour
    {
        public BoardRuntime Board;

        public TargetSelectionRequestEvent OnTargetSelectionRequested = new();
        public TargetSelectionProvidedEvent OnTargetSelectionProvided = new();

        public GameObject[] AutoPickFrontMostEnemy()
        {
            var t = Board?.GetFrontMostEnemyUnit();
            if (t == null) return Array.Empty<GameObject>();
            Debug.Log($"[Targeting] Auto-picked enemy: {t.name}");
            return new[] { t };
        }

        // Hook from UI when the player taps/clicks a valid unit
        public void ProvideManualSingle(GameObject picked)
        {
            if (picked == null) OnTargetSelectionProvided.Invoke(Array.Empty<GameObject>());
            else OnTargetSelectionProvided.Invoke(new[] { picked });
        }
    }
}