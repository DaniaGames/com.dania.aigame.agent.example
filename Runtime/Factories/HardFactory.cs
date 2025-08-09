using AIGame.Core;
using UnityEngine;

namespace AIGame.Examples
{

    [CreateAssetMenu(menuName = "Factories/HardFactory")]
    public class TestFactory : AgentFactory
    {

        protected override BaseAI[] CreateAgents(GameObject agentPrefab)
        {
            // 1) Create a brand new array
            var created = new BaseAI[GameManager.Instance.AgentsPerTeam];

            // 2) Fill it with new instances
            for (int i = 0; i < created.Length; i++)
            {
                created[i] = GameObject.Instantiate(agentPrefab).AddComponent<HardAI>();
            }

            return created;
        }
    }
    
}