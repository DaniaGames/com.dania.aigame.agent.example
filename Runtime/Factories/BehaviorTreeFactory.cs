using UnityEngine;
using AIGame.Core;
using AIGame.Examples.BehaviorTree;

namespace AIGame.Examples
{
    /// <summary>
    /// Factory that spawns BehaviorTreeAI agents.
    /// Creates a full team of agents using the BehaviorTreeAI behavior with
    /// mobile combat and aggressive targeting.
    /// </summary>
    [CreateAssetMenu(menuName = "Factories/BehaviorTreeFactory")]
    public class BehaviorTreeFactory : AgentFactory
    {
        /// <summary>
        /// Creates a set of BehaviorTreeAI agents for one team.
        /// </summary>
        /// <param name="agentPrefab">The prefab used for each agent.</param>
        /// <param name="teamSize">The number of agents per team.</param>
        /// <returns>An array containing the spawned BehaviorTreeAI agents.</returns>
        protected override BaseAI[] CreateAgents(GameObject agentPrefab, int teamSize)
        {
            // Allocate an array for the number of agents per team
            var created = new BaseAI[teamSize];

            // Instantiate agents and attach BehaviorTreeAI behavior
            for (int i = 0; i < created.Length; i++)
            {
                created[i] = GameObject.Instantiate(agentPrefab).AddComponent<BehaviorTreeAI>();
            }

            return created;
        }
    }
}