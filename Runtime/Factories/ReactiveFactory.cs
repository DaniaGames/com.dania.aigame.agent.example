using AIGame.Core;
using UnityEngine;

namespace AIGame.Examples.ReactiveAI
{
    /// <summary>
    /// Factory that spawns ReactiveAI agents.
    /// Creates a full team of agents using the ReactiveAI behaviour.
    /// </summary>
    [RegisterFactory("01. Reactive AI")]
    class ReactiveFactory : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the ReactiveAI type.</returns>
        protected override System.Type[] GetAgentTypes()
        {
            return new System.Type[] { typeof(ReactiveAI) };
        }
    }
}