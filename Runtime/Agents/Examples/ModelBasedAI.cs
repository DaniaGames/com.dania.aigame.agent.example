using AIGame.Core;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AIGame.Examples
{
    public class ModelBasedAI : BaseAI
    {
        private List<PerceivedAgent> memory = new List<PerceivedAgent>();
        private float memoryDuration = 10f; // How long to remember agents (seconds)

        protected override void ConfigureStats()
        {
            // Allocate 20 stat points - balanced distribution
            AllocateStat(StatType.Speed, 4);
            AllocateStat(StatType.VisionRange, 4);
            AllocateStat(StatType.ProjectileRange, 4);
            AllocateStat(StatType.ReloadSpeed, 4);
            AllocateStat(StatType.DodgeCooldown, 4);
        }

        protected override void ExecuteAI()
        {
            // Add currently visible agents to memory
            var enemies = GetVisibleEnemiesSnapshot();
            var allies = GetVisibleAlliesSnapshot();

            foreach (var enemy in enemies)
            {
                UpdateMemory(enemy);
            }

            foreach (var ally in allies)
            {
                UpdateMemory(ally);
            }

            // Clean up old memories
            CleanOldMemories();

            // Make decisions based on memory
            MakeDecision();
        }

        protected override void StartAI()
        {
            // Initialize any needed components
        }

        private void UpdateMemory(PerceivedAgent agent)
        {
            // Find existing memory of this agent
            for (int i = 0; i < memory.Count; i++)
            {
                if (memory[i].Id == agent.Id)
                {
                    // Update existing memory with new position and frame
                    memory[i] = agent;
                    return;
                }
            }

            // Agent not in memory, add it
            memory.Add(agent);
        }

        private void CleanOldMemories()
        {
            // Remove agents we haven't seen for too long
            int currentFrame = Time.frameCount;
            float maxFrameAge = memoryDuration * 60f; // Assuming ~60 FPS

            memory.RemoveAll(agent => currentFrame - agent.Frame > maxFrameAge);
        }

        

        private void MakeDecision()
        {
            var visibleEnemies = GetVisibleEnemiesSnapshot();
            var visibleAllies = GetVisibleAlliesSnapshot();

            // Get visible enemies (highest priority)
            var currentEnemies = visibleEnemies.ToList();

            // Get remembered enemies that are NOT currently visible
            var rememberedEnemies = memory.Where(agent =>
                !visibleEnemies.Any(visible => visible.Id == agent.Id) && // Not currently visible
                !visibleAllies.Any(visible => visible.Id == agent.Id) &&   // Not an ally
                agent.Id != AgentID                                        // Not myself
            ).ToList();

            PerceivedAgent targetEnemy;
            bool isTargetVisible = false;

            if (currentEnemies.Count > 0)
            {
                // Prioritize visible enemies - find closest visible enemy
                targetEnemy = currentEnemies
                    .OrderBy(enemy => Vector3.Distance(transform.position, enemy.Position))
                    .First();
                isTargetVisible = true;
            }
            else if (rememberedEnemies.Count > 0)
            {
                // Fall back to remembered enemies - find closest remembered enemy
                targetEnemy = rememberedEnemies
                    .OrderBy(enemy => Vector3.Distance(transform.position, enemy.Position))
                    .First();
                isTargetVisible = false;
            }
            else
            {
                // No enemies known - patrol
                Patrol();
                return;
            }

            float distance = Vector3.Distance(transform.position, targetEnemy.Position);

            // Move towards target enemy
            MoveTo(targetEnemy.Position);

            // Only attack if target is visible and in range
            if (isTargetVisible && distance <= ProjectileRange)
            {
                ThrowBallAt(targetEnemy);
            }
        }

        private void Patrol()
        {
            // Simple patrol behavior
            if (!NavMeshAgent.hasPath)
            {
                Vector3 randomDirection = Random.insideUnitSphere * 15f;
                randomDirection += transform.position;
                randomDirection.y = transform.position.y; // Keep same height
                MoveTo(randomDirection);
            }
        }
    }
}

