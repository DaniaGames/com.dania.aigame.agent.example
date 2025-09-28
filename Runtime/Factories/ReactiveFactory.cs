using AIGame.Core;
using UnityEngine;

namespace AIGame.Examples.ReactiveAI
{
    /// <summary>
    /// Factory that spawns EasyAI agents.
    /// Creates a full team of agents using the EasyAI behaviour.
    /// </summary>
    [CreateAssetMenu(menuName = "Factories/ReactiveFactory")]
    [RegisterFactory("01. Reactive AI")]
    class ReactiveFactory : AgentFactory
    {
        /// <summary>
        /// Creates a set of EasyAI agents for one team.
        /// </summary>
        /// <param name="agentPrefab">The prefab used for each agent.</param>
        /// <param name="teamSize">The number of agents per team.</param>
        /// <returns>An array containing the spawned EasyAI agents.</returns>
        protected override BaseAI[] CreateAgents(GameObject agentPrefab, int teamSize)
        {
            // Allocate an array for the number of agents per team
            var created = new BaseAI[teamSize];

            // Instantiate agents and attach EasyAI behaviour
            for (int i = 0; i < created.Length; i++)
            {
                created[i] = GameObject.Instantiate(agentPrefab).AddComponent<ReactiveAI>();
            }

            return created;
        }
    }
}