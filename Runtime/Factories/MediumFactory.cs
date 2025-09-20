using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// Factory that spawns MediumAI agents.
    /// MediumAI agents run to objectives and engage enemies they encounter.
    /// </summary>
    [CreateAssetMenu(menuName = "Factories/MediumFactory")]
    public class OpponentFactory : AgentFactory
    {
        /// <summary>
        /// Creates a set of MediumAI agents for one team.
        /// </summary>
        /// <param name="agentPrefab">The prefab used for each agent.</param>
        /// <param name="teamSize">The number of agents per team.</param>
        /// <returns>An array containing the spawned MediumAI agents.</returns>
        protected override BaseAI[] CreateAgents(GameObject agentPrefab, int teamSize)
        {
            // Allocate an array for the number of agents per team
            var created = new BaseAI[teamSize];

            // Instantiate agents and attach MediumAI behaviour
            for (int i = 0; i < created.Length; i++)
            {
                created[i] = GameObject.Instantiate(agentPrefab)
                                        .AddComponent<MediumAI>();
            }

            return created;
        }
    }
}
