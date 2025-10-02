using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.ModelBased
{
    public class ModelBasedAI : BaseAI
    {
        private WorldModel worldModel;
        private float lastModelUpdate = 0f;
        private const float MODEL_UPDATE_INTERVAL = 0.5f; // Update model every 0.5 seconds

        // Decision states
        private enum AIState
        {
            Exploring,
            PursuingPowerUp,
            HuntingEnemy,
            Wandering
        }

        private AIState currentState = AIState.Exploring;
        private Vector3 currentTarget;
        private float stateChangeTime;
        private const float ARRIVAL_THRESHOLD = 2f;

        protected override void StartAI()
        {
            worldModel = new WorldModel();
            stateChangeTime = Time.time;
        }

        protected override void ConfigureStats()
        {
            // Balanced stats focusing on vision for better world modeling
            AllocateStat(StatType.Speed, 3);
            AllocateStat(StatType.VisionRange, 8); // High vision for better modeling
            AllocateStat(StatType.ProjectileRange, 3);
            AllocateStat(StatType.ReloadSpeed, 3);
            AllocateStat(StatType.DodgeCooldown, 3);
        }

        protected override void ExecuteAI()
        {
            if (!IsAlive) return;

            // Update world model periodically
            if (Time.time - lastModelUpdate > MODEL_UPDATE_INTERVAL)
            {
                UpdateWorldModel();
                lastModelUpdate = Time.time;
            }

            // Make decisions based on current model
            MakeDecision();

            // Execute current action
            ExecuteCurrentState();
        }

        private void UpdateWorldModel()
        {
            // Update model with visible powerups
            var visiblePowerUps = GetVisiblePowerUpsSnapshot();
            foreach (var powerUp in visiblePowerUps)
            {
                worldModel.UpdatePowerUp(powerUp.Id, powerUp.Position);
            }

            // Update model with visible enemies
            var visibleEnemies = GetVisibleEnemiesSnapshot();
            foreach (var enemy in visibleEnemies)
            {
                worldModel.UpdateEnemy(enemy.Id, enemy.Position);
            }

            // Clean up old data
            worldModel.CleanupOldData();
        }

        private void MakeDecision()
        {
            // Decision making based on world model state
            AIState newState = currentState;

            // Priority 1: Combat if enemies are visible or recently seen
            var visibleEnemies = GetVisibleEnemiesSnapshot();
            if (visibleEnemies.Count > 0)
            {
                newState = AIState.HuntingEnemy;
                RefreshOrAcquireTarget();
            }
            else
            {
                // Check model for recent enemy positions
                Vector3? recentEnemyPos = worldModel.GetMostRecentEnemyPosition();
                if (recentEnemyPos.HasValue)
                {
                    newState = AIState.HuntingEnemy;
                    currentTarget = recentEnemyPos.Value;
                }
                else
                {
                    // Priority 2: Pursue known powerups
                    Vector3? closestPowerUp = worldModel.GetClosestKnownPowerUp(transform.position);
                    if (closestPowerUp.HasValue)
                    {
                        newState = AIState.PursuingPowerUp;
                        currentTarget = closestPowerUp.Value;
                    }
                    else
                    {
                        // Priority 3: Explore for new information
                        if (currentState != AIState.Exploring || HasReachedDestination())
                        {
                            newState = AIState.Exploring;
                            currentTarget = GetExplorationTarget();
                        }
                    }
                }
            }

            // Change state if needed
            if (newState != currentState)
            {
                currentState = newState;
                stateChangeTime = Time.time;
                Debug.Log($"ModelBasedAI: Switching to {currentState} at {stateChangeTime}");
            }
        }

        private void ExecuteCurrentState()
        {
            switch (currentState)
            {
                case AIState.HuntingEnemy:
                    ExecuteHunting();
                    break;

                case AIState.PursuingPowerUp:
                    ExecutePowerUpPursuit();
                    break;

                case AIState.Exploring:
                    ExecuteExploration();
                    break;

                case AIState.Wandering:
                    ExecuteWandering();
                    break;
            }
        }

        private void ExecuteHunting()
        {
            // Try to attack visible enemies first
            if (TryGetTarget(out var target))
            {
                FaceTarget(target.Position);
                ThrowBallAt(target);
            }
            else
            {
                // Move to last known enemy position from our model
                Vector3? enemyPos = worldModel.GetMostRecentEnemyPosition();
                if (enemyPos.HasValue)
                {
                    MoveTo(enemyPos.Value);
                }
                else
                {
                    // No enemy intel, switch to exploring
                    currentState = AIState.Exploring;
                }
            }
        }

        private void ExecutePowerUpPursuit()
        {
            // Move to the powerup location we have in our model
            MoveTo(currentTarget);

            // Check if we're close enough to collect
            if (Vector3.Distance(transform.position, currentTarget) < 2f)
            {
                // Try to collect powerup and mark it in our model
                var visiblePowerUps = GetVisiblePowerUpsSnapshot();
                foreach (var powerUp in visiblePowerUps)
                {
                    if (Vector3.Distance(powerUp.Position, currentTarget) < 3f)
                    {
                        worldModel.MarkPowerUpCollected(powerUp.Id);
                        break;
                    }
                }

                // Switch to exploring for new targets
                currentState = AIState.Exploring;
            }
        }

        private void ExecuteExploration()
        {
            MoveTo(currentTarget);

            // If we've reached our exploration target, pick a new one
            if (HasReachedDestination())
            {
                currentTarget = GetExplorationTarget();
            }
        }

        private void ExecuteWandering()
        {
            MoveTo(currentTarget);

            if (HasReachedDestination())
            {
                currentTarget = GetExplorationTarget();
            }
        }

        private Vector3 GetExplorationTarget()
        {
            // Simple exploration: move to areas we haven't been to recently
            // For demonstration, just pick random points around the map
            Vector3 basePosition = transform.position;
            Vector3 randomDirection = Random.insideUnitSphere * 15f;
            randomDirection.y = 0; // Keep on ground level

            Vector3 explorationTarget = basePosition + randomDirection;

            // Ensure target is within reasonable bounds (basic map awareness)
            explorationTarget.x = Mathf.Clamp(explorationTarget.x, -50f, 50f);
            explorationTarget.z = Mathf.Clamp(explorationTarget.z, -50f, 50f);

            return explorationTarget;
        }

        private bool HasReachedDestination()
        {
            if (GetRemainingDistance() <= ARRIVAL_THRESHOLD)
            {
                return true;
            }
            else if (!IsPathPending() && !HasPath())
            {
                return true;
            }

            return false;
        }
    }
}