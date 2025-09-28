using AIGame.Core;
using AIGame.Examples.BeheaviourTree;
using AIGame.Examples.FSM;
using System.Collections.Generic;
using System.Security.Principal;
using Unity.VisualScripting;
using UnityEngine;

namespace AIGame.Examples.ModelBased
{
    /// <summary>
    /// Factory that spawns ModelBasedAI agents.
    /// Creates a full team of agents using the ModelBasedAI behaviour.
    /// </summary>
    [RegisterFactory("03. Model Based")]
    class ModelBasedFactory : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the ModelBasedAI type.</returns>
        protected override System.Type[] GetAgentTypes()
        {
            return new System.Type[] { typeof(ModelBasedAI) };
        }
    }
}


