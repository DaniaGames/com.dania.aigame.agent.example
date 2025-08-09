using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples
{
    [CreateAssetMenu(menuName = "Factories/Easy Factory")]
    class EasyFactory : AgentFactory
    {
        protected override BaseAI[] CreateAgents(GameObject agentPrefab)
        {
            // 1) Create a brand new array
            var created = new BaseAI[GameManager.Instance.AgentsPerTeam];

            // 2) Fill it with new instances
            for (int i = 0; i < created.Length; i++)
            {
                created[i] = GameObject.Instantiate(agentPrefab).AddComponent<EasyAI>();
            }

            return created;
        }
    }
}