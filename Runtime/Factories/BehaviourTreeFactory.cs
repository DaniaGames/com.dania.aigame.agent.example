using AIGame.Core;
using AIGame.Examples.BeheaviourTree;
using UnityEngine;

namespace AIGame.Examples.BeheaviourTree
{
    /// <summary>
    /// Factory that spawns BehaviourTreeAI agents.
    /// Creates a full team of agents using the BehaviourTreeAI behaviour.
    /// </summary>
    [RegisterFactory("04. Behaviour Tree")]
    class BehaviourTreeFactory : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the BehaviourTreeAI type.</returns>
        protected override System.Type[] GetAgentTypes()
        {
            return new System.Type[] { typeof(BehaviourTreeAI) };
        }
    }
}