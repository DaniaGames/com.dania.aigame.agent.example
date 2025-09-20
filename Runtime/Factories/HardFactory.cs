using AIGame.Core;
using UnityEngine;

namespace AIGame.Examples
{
    /// <summary>
    /// Factory that spawns HardAI agents.
    /// Creates a full team of agents that run to objectives,
    /// engage enemies, and dodge incoming projectiles.
    /// </summary>
    [CreateAssetMenu(menuName = "Factories/HardFactory")]
    public class TestFactory : AgentFactory
    {
        /// <summary>
        /// Creates a set of HardAI agents for one team.
        /// </summary>
        /// <param name="agentPrefab">The prefab used for each agent.</param>
        /// <param name="teamSize">The number of agents per team.</param>
        /// <returns>An array containing the spawned HardAI agents.</returns>
        protected override BaseAI[] CreateAgents(GameObject agentPrefab, int teamSize)
        {
            // Allocate an array for the number of agents per team
            var created = new BaseAI[teamSize];

            // Instantiate agents and attach HardAI behaviour
            for (int i = 0; i < created.Length; i++)
            {
                created[i] = GameObject.Instantiate(agentPrefab)
                                        .AddComponent<HardAI>();
            }

            return created;
        }
    }
}
