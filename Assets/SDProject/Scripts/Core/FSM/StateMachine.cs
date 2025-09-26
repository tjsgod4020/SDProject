// StateMachine.cs
using UnityEngine;

namespace SDProject.Core.FSM
{
    /// <summary>KISS: ���� ���� ���� + ����.</summary>
    public sealed class StateMachine
    {
        private IState _current;

        // ���� ���� ���� �� ����
        private struct Transition
        {
            public IState from, to;
            public System.Func<bool> condition;
        }

        private readonly System.Collections.Generic.List<Transition> _transitions = new();

        public void SetState(IState next)
        {
            if (_current == next) return;
            _current?.Exit();
            _current = next;
            _current?.Enter();
#if UNITY_EDITOR
            Debug.Log($"[FSM] Switched to: {_current?.GetType().Name}");
#endif
        }

        public void AddTransition(IState from, IState to, System.Func<bool> condition)
        {
            _transitions.Add(new Transition { from = from, to = to, condition = condition });
        }

        public void Tick(float dt)
        {
            // ���� �˻�
            for (int i = 0; i < _transitions.Count; i++)
            {
                var t = _transitions[i];
                if (_current == t.from && t.condition != null && t.condition())
                {
                    SetState(t.to);
                    break;
                }
            }
            _current?.Tick(dt);
        }
    }
}
