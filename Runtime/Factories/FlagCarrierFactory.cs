using AIGame.Core;
using UnityEngine;

namespace AIGame.Examples
{
    /// <summary>
    /// Factory that spawns FlagCarrierAI agents.
    /// Creates a full team of agents that attempt to capture the enemy flag
    /// and return the friendly flag.
    /// </summary>
    [CreateAssetMenu(menuName = "Factories/FlagCarrierFactory")]
    class FlagCarrierFactory : AgentFactory
    {
        /// <summary>
        /// Creates a set of FlagCarrierAI agents for one team.
        /// </summary>
        /// <param name="agentPrefab">The prefab used for each agent.</param>
        /// <param name="teamSize">The number of agents per team.</param>
        /// <returns>An array containing the spawned FlagCarrierAI agents.</returns>
        protected override BaseAI[] CreateAgents(GameObject agentPrefab, int teamSize)
        {
            // Allocate an array for the number of agents per team
            var created = new BaseAI[teamSize];

            // Instantiate agents and attach FlagCarrierAI behaviour
            for (int i = 0; i < created.Length; i++)
            {
                created[i] = GameObject.Instantiate(agentPrefab)
                                        .AddComponent<FlagCarrierAI>();
            }

            return created;
        }
    }
}
