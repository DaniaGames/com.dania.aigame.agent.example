using System.Collections.Generic;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Condition node that checks if any enemies are currently visible to the agent.
    /// Returns SUCCESS if one or more enemies are in sight, FAILURE otherwise.
    ///
    /// This node leverages the existing perception system from BaseAI and uses
    /// the blackboard's cached enemy data for efficient checking.
    /// </summary>
    public class EnemyVisibleCondition : BehaviorTreeNode
    {
        /// <summary>
        /// Minimum number of enemies required to consider the condition met.
        /// Defaults to 1, but can be configured for different behaviors.
        /// </summary>
        private int minimumEnemyCount;

        /// <summary>
        /// Whether to update the blackboard with a preferred target when enemies are found.
        /// Useful for follow-up action nodes that need a specific target.
        /// </summary>
        private bool selectBestTarget;

        /// <summary>
        /// Creates a new enemy visible condition.
        /// </summary>
        /// <param name="blackboard">Shared blackboard instance.</param>
        /// <param name="agent">AI agent reference.</param>
        /// <param name="minimumCount">Minimum enemies required (default: 1).</param>
        /// <param name="selectTarget">Whether to select best target (default: true).</param>
        public EnemyVisibleCondition(Blackboard blackboard, BaseAI agent,
            int minimumCount = 1, bool selectTarget = true)
            : base(blackboard, agent)
        {
            minimumEnemyCount = UnityEngine.Mathf.Max(1, minimumCount);
            selectBestTarget = selectTarget;
        }

        /// <summary>
        /// Called when the condition starts being evaluated.
        /// </summary>
        protected override void OnEnter()
        {
            LogDebug($"Checking for visible enemies (need at least {minimumEnemyCount})");
        }

        /// <summary>
        /// Main condition evaluation logic.
        /// Checks blackboard for visible enemies and optionally selects best target.
        /// </summary>
        /// <returns>SUCCESS if enough enemies visible, FAILURE otherwise.</returns>
        protected override NodeState Execute()
        {
            // Use agent's perception directly for most reliable results
            var agentEnemies = agent.GetVisibleEnemiesSnapshot();
            LogDebug($"Agent sees {agentEnemies.Count} enemies");

            // Check if we have enough enemies
            if (agentEnemies.Count < minimumEnemyCount)
            {
                LogDebug($"Not enough enemies visible: {agentEnemies.Count}/{minimumEnemyCount}");

                // Clear any existing target since no enemies are visible
                if (agentEnemies.Count == 0)
                {
                    blackboard.SetCurrentTarget(null);
                }

                return NodeState.Failure;
            }

            LogDebug($"Found {agentEnemies.Count} visible enemies");

            // Optionally select the best target for follow-up actions
            if (selectBestTarget)
            {
                var bestTarget = SelectBestTarget(agentEnemies);
                if (bestTarget.HasValue)
                {
                    blackboard.SetCurrentTarget(bestTarget.Value);
                    LogDebug($"Selected target: Enemy {bestTarget.Value.Id} at {bestTarget.Value.Position}");
                }
            }

            return NodeState.Success;
        }

        /// <summary>
        /// Called when the condition evaluation completes.
        /// </summary>
        protected override void OnExit()
        {
            LogDebug($"Enemy visibility check completed (result: {state})");
        }

        /// <summary>
        /// Selects the best target from available enemies.
        /// Current strategy: closest enemy within projectile range, or closest overall.
        /// </summary>
        /// <param name="enemies">Collection of visible enemies.</param>
        /// <returns>Best target, or null if none suitable.</returns>
        private PerceivedAgent? SelectBestTarget(IReadOnlyList<PerceivedAgent> enemies)
        {
            if (enemies.Count == 0)
                return null;

            var myPosition = blackboard.GetMyPosition();
            PerceivedAgent? bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (var enemy in enemies)
            {
                float distance = UnityEngine.Vector3.Distance(myPosition, enemy.Position);

                // Prefer enemies within projectile range
                float score = distance;
                if (distance <= agent.ProjectileRange)
                {
                    score *= 0.5f; // Halve the score for in-range enemies (prefer them)
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Gets a debug name for this condition.
        /// </summary>
        /// <returns>Debug-friendly name with parameters.</returns>
        public override string GetDebugName()
        {
            return $"EnemyVisible(≥{minimumEnemyCount})";
        }

        /// <summary>
        /// Logs debug information if debug mode is enabled.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogDebug(string message)
        {
            if (blackboard.Get("DebugMode", false))
            {
                UnityEngine.Debug.Log($"[EnemyVisibleCondition] {message}");
            }
        }
    }
}