// Assets/SDProject/Scripts/Core/FSM/IState.cs
using UnityEngine;

namespace SDProject.Core.FSM
{
    /// <summary>
    /// Minimal state interface.
    /// Single responsibility: state's lifecycle hooks.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when entering the state.</summary>
        void Enter();

        /// <summary>Called every frame (or tick) while this state is active.</summary>
        void Tick(float deltaTime);

        /// <summary>Called once when exiting the state.</summary>
        void Exit();
    }
}