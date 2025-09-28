using AIGame.Core;
using AIGame.Examples.FSM;
using UnityEngine;

namespace AIGame.Examples.FSM
{
    /// <summary>
    /// Factory that spawns FinitStateAI agents.
    /// Creates a full team of agents using the FinitStateAI behaviour.
    /// </summary>
    [RegisterFactory("02. Finite State Machine")]
    class FinitStateFactory : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the FinitStateAI type.</returns>
        protected override System.Type[] GetAgentTypes()
        {
            return new System.Type[] { typeof(FinitStateAI) };
        }
    }
}