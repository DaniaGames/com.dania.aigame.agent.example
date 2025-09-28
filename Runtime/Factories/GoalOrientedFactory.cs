using AIGame.Core;
using AIGame.Examples.GoalOriented;
using UnityEngine;

namespace AIGame.Examples.GoalOriented
{
    /// <summary>
    /// Factory that spawns GoalOrientedAI agents.
    /// Creates a full team of agents using the GoalOrientedAI behaviour.
    /// </summary>
    [RegisterFactory("05. Goal Oriented")]
    class GoalOrientedFactory : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the GoalOrientedAI type.</returns>
        protected override System.Type[] GetAgentTypes()
        {
            return new System.Type[] { typeof(GoalOrientedAI) };
        }
    }
}