// Assets/SDProject/Scripts/Core/FSM/StateMachine.cs
using UnityEngine;

namespace SDProject.Core.FSM
{
    /// <summary>
    /// Minimal finite state machine.
    /// Single responsibility: hold a current state and switch/tick it.
    /// </summary>
    public class StateMachine
    {
        private IState _current;

        /// <summary>Set next state. Calls Exit() on old and Enter() on new.</summary>
        public void SetState(IState next)
        {
            if (_current == next) return;

            _current?.Exit();
            _current = next;
#if UNITY_EDITOR
            Debug.Log($"[FSM] Switched to: {_current?.GetType().Name}");
#endif
            _current?.Enter();
        }

        /// <summary>Forward Update deltaTime to current state.</summary>
        public void Tick(float dt) => _current?.Tick(dt);

        /// <summary>Current state (read-only).</summary>
        public IState Current => _current;
    }
}