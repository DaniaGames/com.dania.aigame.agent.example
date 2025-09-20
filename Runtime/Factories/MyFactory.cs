using UnityEngine;
using AIGame.Core;

namespace Awesome.AI
{
    [CreateAssetMenu(menuName = "Factories/MyFactory")]
    class MyFactory : AgentFactory
    {
        protected override BaseAI[] CreateAgents(GameObject agentPrefab, int teamSize)
        {
            var created = new BaseAI[teamSize];

            for (int i = 0; i < created.Length; i++)
            {
                created[i] = GameObject.Instantiate(agentPrefab)
                                        .AddComponent<ReactiveAI>();
            }

            return created;
        }
    }
}
