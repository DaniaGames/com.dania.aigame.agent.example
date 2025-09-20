using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Condition node that checks if the agent has a valid current target.
    /// Returns SUCCESS if a target exists and is still valid, FAILURE otherwise.
    ///
    /// This condition is useful for ensuring combat actions have a valid target
    /// before attempting to engage.
    /// </summary>
    public class HasTargetCondition : BehaviorTreeNode
    {
        /// <summary>
        /// Maximum age (in seconds) for a target to be considered valid.
        /// Older targets may have moved significantly and should be refreshed.
        /// </summary>
        private float maxTargetAge;

        /// <summary>
        /// Whether to validate that the target is still within perception range.
        /// If true, targets outside vision range will be considered invalid.
        /// </summary>
        private bool requireInRange;

        /// <summary>
        /// Whether to check if the target is still in the list of visible enemies.
        /// If true, only currently visible enemies are considered valid targets.
        /// </summary>
        private bool requireVisible;

        /// <summary>
        /// Creates a new has target condition.
        /// </summary>
        /// <param name="blackboard">Shared blackboard instance.</param>
        /// <param name="agent">AI agent reference.</param>
        /// <param name="maxAge">Maximum target age in seconds (default: 5.0).</param>
        /// <param name="requireInRange">Whether target must be in range (default: false).</param>
        /// <param name="requireVisible">Whether target must be visible (default: true).</param>
        public HasTargetCondition(Blackboard blackboard, BaseAI agent,
            float maxAge = 5.0f, bool requireInRange = false, bool requireVisible = true)
            : base(blackboard, agent)
        {
            maxTargetAge = UnityEngine.Mathf.Max(0.1f, maxAge);
            this.requireInRange = requireInRange;
            this.requireVisible = requireVisible;
        }

        /// <summary>
        /// Called when the condition starts being evaluated.
        /// </summary>
        protected override void OnEnter()
        {
            LogDebug("Checking for valid target");
        }

        /// <summary>
        /// Main condition evaluation logic.
        /// Validates the current target based on configured criteria.
        /// </summary>
        /// <returns>SUCCESS if valid target exists, FAILURE otherwise.</returns>
        protected override NodeState Execute()
        {
            var currentTarget = blackboard.GetCurrentTarget();

            // No target at all
            if (!currentTarget.HasValue)
            {
                LogDebug("No current target");
                return NodeState.Failure;
            }

            var target = currentTarget.Value;
            LogDebug($"Evaluating target: Enemy {target.Id} (frame {target.Frame})");

            // Check target age
            int currentFrame = UnityEngine.Time.frameCount;
            float targetAge = (currentFrame - target.Frame) * UnityEngine.Time.deltaTime;

            if (targetAge > maxTargetAge)
            {
                LogDebug($"Target too old: {targetAge:F2}s > {maxTargetAge:F2}s");
                blackboard.SetCurrentTarget(null);
                return NodeState.Failure;
            }

            // Check if target must be visible
            if (requireVisible)
            {
                var visibleEnemies = blackboard.GetVisibleEnemies();
                bool isVisible = false;

                foreach (var enemy in visibleEnemies)
                {
                    if (enemy.Id == target.Id)
                    {
                        isVisible = true;
                        // Update target with latest position if we found a more recent sighting
                        if (enemy.Frame > target.Frame)
                        {
                            blackboard.SetCurrentTarget(enemy);
                            LogDebug($"Updated target position: {enemy.Position}");
                        }
                        break;
                    }
                }

                if (!isVisible)
                {
                    LogDebug($"Target {target.Id} no longer visible");
                    blackboard.SetCurrentTarget(null);
                    return NodeState.Failure;
                }
            }

            // Check if target must be in range
            if (requireInRange)
            {
                var myPosition = blackboard.GetMyPosition();
                float distance = UnityEngine.Vector3.Distance(myPosition, target.Position);

                if (distance > agent.ProjectileRange)
                {
                    LogDebug($"Target out of range: {distance:F1} > {agent.ProjectileRange:F1}");
                    return NodeState.Failure;
                }
            }

            LogDebug($"Valid target confirmed: Enemy {target.Id}");
            return NodeState.Success;
        }

        /// <summary>
        /// Called when the condition evaluation completes.
        /// </summary>
        protected override void OnExit()
        {
            LogDebug($"Target validation completed (result: {state})");
        }

        /// <summary>
        /// Gets a debug name for this condition.
        /// </summary>
        /// <returns>Debug-friendly name with validation criteria.</returns>
        public override string GetDebugName()
        {
            string criteria = "";
            if (requireVisible) criteria += "V";
            if (requireInRange) criteria += "R";
            if (criteria.Length > 0) criteria = $"({criteria})";

            return $"HasTarget{criteria}";
        }

        /// <summary>
        /// Creates a simple target condition that only checks for existence.
        /// </summary>
        /// <param name="blackboard">Blackboard instance.</param>
        /// <param name="agent">AI agent.</param>
        /// <returns>Basic target condition.</returns>
        public static HasTargetCondition Simple(Blackboard blackboard, BaseAI agent)
        {
            return new HasTargetCondition(blackboard, agent, 10.0f, false, false);
        }

        /// <summary>
        /// Creates a strict target condition that requires visibility and range.
        /// </summary>
        /// <param name="blackboard">Blackboard instance.</param>
        /// <param name="agent">AI agent.</param>
        /// <returns>Strict target condition.</returns>
        public static HasTargetCondition Strict(Blackboard blackboard, BaseAI agent)
        {
            return new HasTargetCondition(blackboard, agent, 2.0f, true, true);
        }

        /// <summary>
        /// Creates a visible-only target condition (default behavior).
        /// </summary>
        /// <param name="blackboard">Blackboard instance.</param>
        /// <param name="agent">AI agent.</param>
        /// <returns>Visible target condition.</returns>
        public static HasTargetCondition Visible(Blackboard blackboard, BaseAI agent)
        {
            return new HasTargetCondition(blackboard, agent, 5.0f, false, true);
        }

        /// <summary>
        /// Logs debug information if debug mode is enabled.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogDebug(string message)
        {
            if (blackboard.Get("DebugMode", false))
            {
                UnityEngine.Debug.Log($"[HasTargetCondition] {message}");
            }
        }
    }
}