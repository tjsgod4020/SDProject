using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Core.FSM
{
    /// <summary>
    /// Minimal FSM with conditional transitions.
    /// </summary>
    public class StateMachine
    {
        private IState _current;

        private class Transition
        {
            public IState From;
            public IState To;
            public Func<bool> Condition;
        }

        private readonly List<Transition> _transitions = new();
        private readonly List<Transition> _currentTransitions = new();

        public void AddTransition(IState from, IState to, Func<bool> condition)
        {
            _transitions.Add(new Transition { From = from, To = to, Condition = condition });
        }

        public void SetState(IState next)
        {
            if (_current == next) return;

            _current?.Exit();
            _current = next;

            // rebuild currentTransitions
            _currentTransitions.Clear();
            foreach (var t in _transitions)
                if (t.From == _current) _currentTransitions.Add(t);

#if UNITY_EDITOR
            Debug.Log($"[FSM] Switched to: {_current?.GetType().Name}");
#endif
            _current?.Enter();
        }

        public void Tick(float dt)
        {
            // check transitions first
            for (int i = 0; i < _currentTransitions.Count; i++)
            {
                if (_currentTransitions[i].Condition())
                {
                    SetState(_currentTransitions[i].To);
                    break; // only first match
                }
            }
            _current?.Tick(dt);
        }

        public IState Current => _current;
    }
}
