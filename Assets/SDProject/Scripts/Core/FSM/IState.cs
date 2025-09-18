using UnityEngine;

namespace SDProject.Core.FSM
{
    /// <summary>State lifecycle hooks.</summary>
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}