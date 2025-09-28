using System.Collections.Generic;
using AIGame.Core;
using UnityEngine;

namespace AIGame.Examples.FSM
{
    /// <summary>
    /// Represents the possible conditions that can trigger a state change.
    /// </summary>
    public enum AICondition
    {
        None,
        Spawned,
        Idle,
        SeesEnemy,
        MoveToObjective,
        Investigate,
        Protect,
        EnemyZone,
        EnemyFlag,
        FriendlyZone,
        FriendlyFlag,
    }

    /// <summary>
    /// Base class for example AI agents.
    /// Provides a finite state machine implementation for switching between states
    /// based on <see cref="AICondition"/> values.
    /// </summary>
    public class FSM
    {


        /// <summary>
        /// The current active state.
        /// </summary>
        protected AIState currentState;

        /// <summary>
        /// Maps a tuple of (current state, condition) to the next state.
        /// </summary>
        protected Dictionary<(AIState, AICondition), AIState> transitions = new();

        /// <summary>
        /// The condition currently set, which may trigger a state change.
        /// </summary>
        protected AICondition currentCondition = AICondition.None;

        /// <summary>
        /// Main update logic for the AI.
        /// Runs once per frame as part of <see cref="BaseAI"/> execution.
        /// </summary>
        public void Execute()
        {
            ProcessTransitions();
            currentCondition = AICondition.None;

            if (currentState != null)
                currentState.Execute();
        }

        /// <summary>
        /// Sets the current condition to be evaluated in the next update cycle.
        /// </summary>
        /// <param name="condition">The new condition value.</param>
        public void SetCondition(AICondition condition)
        {
            currentCondition = condition;
        }

        /// <summary>
        /// Adds a state transition rule.
        /// </summary>
        /// <param name="from">The starting state.</param>
        /// <param name="condition">The condition that triggers the change.</param>
        /// <param name="to">The target state.</param>
        public void AddTransition(AIState from, AICondition condition, AIState to)
        {
            transitions[(from, condition)] = to;
        }

        /// <summary>
        /// Checks if the current state and condition match any registered transition rule.
        /// Changes the state if a match is found.
        /// </summary>
        private void ProcessTransitions()
        {
            if (transitions.TryGetValue((currentState, currentCondition), out var newState))
            {
                ChangeState(newState);
            }
        }

        /// <summary>
        /// Changes the current state, calling <see cref="AIState.Exit"/> on the old state
        /// and <see cref="AIState.Enter"/> on the new state.
        /// </summary>
        /// <param name="newState">The state to switch to.</param>
        public void ChangeState(AIState newState)
        {
            if (currentState != null)
                currentState.Exit();

            currentState = newState;
            currentState.Enter();
        }
    }
}
