using System.Collections.Generic;
using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// This is an example base class that all default AIs inherit from.
    /// It provides functionality for switching states based on conditions.
    /// </summary>
    public abstract class ExampleAI : BaseAI
    {
        /// <summary>
        /// Conditions for switching between states.
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
        /// The current state the AI is in.
        /// </summary>
        protected ExampleAIState currentState;

        /// <summary>
        /// A dictionary containing all state transitions.
        /// Transitions define how the AI can switch from one state to another.
        /// </summary>
        protected Dictionary<(ExampleAIState, AICondition), ExampleAIState> transitions = new();

        /// <summary>
        /// The current active condition. The AI will swap state based on this value.
        /// </summary>
        protected AICondition currentCondition = AICondition.None;

        /// <summary>
        /// Executes the state machine logic.
        /// </summary>
        protected override void ExecuteAI()
        {
            ProcessTransitions();
            currentCondition = AICondition.None;

            if (currentState != null)
                currentState.Execute();
        }

        /// <summary>
        /// Sets a condition so the AI can switch states.
        /// For example, setting AICondition.Idle will make the AI transition into the idle state if such a transition exists.
        /// </summary>
        /// <param name="condition">The condition to set, e.g., AICondition.Idle.</param>
        protected void SetCondition(AICondition condition)
        {
            currentCondition = condition;
        }

        /// <summary>
        /// Creates a transition between two states.
        /// </summary>
        /// <param name="from">The state to transition from.</param>
        /// <param name="condition">The condition that must be met for the transition to occur.</param>
        /// <param name="to">The state to transition to.</param>
        protected void AddTransition(ExampleAIState from, AICondition condition, ExampleAIState to)
        {
            transitions[(from, condition)] = to;
        }

        /// <summary>
        /// Processes transitions by checking the current condition and changing the state if a matching transition exists.
        /// </summary>
        private void ProcessTransitions()
        {
            if (transitions.TryGetValue((currentState, currentCondition), out var newState))
            {
                ChangeState(newState);
            }
        }

        /// <summary>
        /// Changes the current state to a new one.
        /// </summary>
        /// <param name="newState">The state to change into.</param>
        protected virtual void ChangeState(ExampleAIState newState)
        {
            if (currentState != null)
            {
                currentState.Exit();
            }

            currentState = newState;
            currentState.Enter();
        }
    }
}
