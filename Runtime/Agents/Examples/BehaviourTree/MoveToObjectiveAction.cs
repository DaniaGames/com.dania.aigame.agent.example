using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Action node that moves the agent toward the current game objective.
    /// Returns RUNNING while moving, SUCCESS when arrived, FAILURE if movement fails.
    ///
    /// This action leverages the existing movement system from BaseAI and uses
    /// the GameManager's objective position for navigation.
    /// </summary>
    public class MoveToObjectiveAction : BehaviorTreeNode
    {
        /// <summary>
        /// Distance threshold to consider the objective reached.
        /// </summary>
        private float arrivalThreshold;

        /// <summary>
        /// Whether to add random offset around the objective to avoid clustering.
        /// </summary>
        private bool useRandomOffset;

        /// <summary>
        /// Maximum random offset distance when useRandomOffset is true.
        /// </summary>
        private float maxOffsetDistance;

        /// <summary>
        /// The actual target position we're moving to (objective + any offset).
        /// </summary>
        private Vector3 targetPosition;

        /// <summary>
        /// Whether we've issued the movement command for this execution cycle.
        /// </summary>
        private bool hasIssuedMoveCommand;

        /// <summary>
        /// Time when the movement command was last issued.
        /// Used to detect if the agent gets stuck.
        /// </summary>
        private float lastMoveCommandTime;

        /// <summary>
        /// Maximum time to wait for movement completion before considering it failed.
        /// </summary>
        private float movementTimeout;

        /// <summary>
        /// Creates a new move to objective action.
        /// </summary>
        /// <param name="blackboard">Shared blackboard instance.</param>
        /// <param name="agent">AI agent reference.</param>
        /// <param name="arrivalThreshold">Distance to consider arrived (default: 2.0f).</param>
        /// <param name="useRandomOffset">Whether to use random offset (default: true).</param>
        /// <param name="maxOffset">Maximum offset distance (default: 5.0f).</param>
        /// <param name="timeout">Movement timeout in seconds (default: 10.0f).</param>
        public MoveToObjectiveAction(Blackboard blackboard, BaseAI agent,
            float arrivalThreshold = 2.0f, bool useRandomOffset = true,
            float maxOffset = 5.0f, float timeout = 10.0f)
            : base(blackboard, agent)
        {
            this.arrivalThreshold = Mathf.Max(0.5f, arrivalThreshold);
            this.useRandomOffset = useRandomOffset;
            this.maxOffsetDistance = Mathf.Max(0.0f, maxOffset);
            this.movementTimeout = Mathf.Max(1.0f, timeout);
            this.hasIssuedMoveCommand = false;
            this.lastMoveCommandTime = 0f;
        }

        /// <summary>
        /// Called when the action starts executing.
        /// Calculates target position and prepares for movement.
        /// </summary>
        protected override void OnEnter()
        {
            hasIssuedMoveCommand = false;
            CalculateTargetPosition();
            LogDebug($"Starting move to objective at {targetPosition}");
        }

        /// <summary>
        /// Main execution logic for the movement action.
        /// Issues movement commands and checks for arrival.
        /// </summary>
        /// <returns>Current execution state.</returns>
        protected override NodeState Execute()
        {
            // Verify we have a valid target position
            if (targetPosition == Vector3.zero)
            {
                LogDebug("No valid objective position found");
                return NodeState.Failure;
            }

            // Issue movement command if we haven't already
            if (!hasIssuedMoveCommand)
            {
                agent.MoveTo(targetPosition);
                hasIssuedMoveCommand = true;
                lastMoveCommandTime = Time.time;
                LogDebug($"Issued move command to {targetPosition}");
            }

            // Check for timeout
            if (Time.time - lastMoveCommandTime > movementTimeout)
            {
                LogDebug($"Movement timed out after {movementTimeout}s");
                return NodeState.Failure;
            }

            // Check if we've arrived
            var myPosition = blackboard.GetMyPosition();
            float distanceToTarget = Vector3.Distance(myPosition, targetPosition);

            if (distanceToTarget <= arrivalThreshold)
            {
                LogDebug($"Arrived at objective (distance: {distanceToTarget:F1})");
                agent.StopMoving();
                return NodeState.Success;
            }

            // Check if the NavMeshAgent is still moving
            if (agent.NavMeshAgent != null && agent.NavMeshAgent.enabled)
            {
                // If the agent has stopped but we're not at the target, there might be a pathfinding issue
                if (!agent.NavMeshAgent.pathPending && agent.NavMeshAgent.remainingDistance <= 0.1f)
                {
                    float directDistance = Vector3.Distance(myPosition, targetPosition);
                    if (directDistance > arrivalThreshold)
                    {
                        LogDebug($"Pathfinding may have failed (remaining: {agent.NavMeshAgent.remainingDistance}, direct: {directDistance})");

                        // Try to reissue the command once
                        if (Time.time - lastMoveCommandTime > 2.0f)
                        {
                            agent.MoveTo(targetPosition);
                            lastMoveCommandTime = Time.time;
                            LogDebug("Re-issued move command due to potential pathfinding failure");
                        }
                    }
                }
            }

            LogDebug($"Moving to objective (distance: {distanceToTarget:F1})");
            return NodeState.Running;
        }

        /// <summary>
        /// Called when the action finishes executing.
        /// </summary>
        protected override void OnExit()
        {
            LogDebug($"Move to objective completed (final state: {state})");
        }

        /// <summary>
        /// Resets the action to its initial state.
        /// </summary>
        public override void Reset()
        {
            hasIssuedMoveCommand = false;
            lastMoveCommandTime = 0f;
            targetPosition = Vector3.zero;
            base.Reset();
            LogDebug("Move to objective action reset");
        }

        /// <summary>
        /// Calculates the target position based on the current objective.
        /// Adds random offset if configured to do so.
        /// </summary>
        private void CalculateTargetPosition()
        {
            // Get objective position from blackboard (updated by BehaviorTree)
            var objectivePosition = blackboard.GetObjectivePosition();

            if (objectivePosition == Vector3.zero)
            {
                // Try to get it directly from GameManager as fallback
                if (GameManager.Instance?.Objective != null)
                {
                    objectivePosition = GameManager.Instance.Objective.transform.position;
                }
            }

            targetPosition = objectivePosition;

            // Add random offset if enabled
            if (useRandomOffset && maxOffsetDistance > 0.0f)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(1.0f, maxOffsetDistance);
                Vector3 offset = new Vector3(randomCircle.x, 0f, randomCircle.y);
                targetPosition += offset;

                LogDebug($"Applied random offset: {offset} (final target: {targetPosition})");
            }

            // Store the calculated position in blackboard for other nodes
            blackboard.Set("MoveToObjectiveTarget", targetPosition);
        }

        /// <summary>
        /// Gets the current target position.
        /// </summary>
        /// <returns>Target position for movement.</returns>
        public Vector3 GetTargetPosition()
        {
            return targetPosition;
        }

        /// <summary>
        /// Gets the distance to the target position.
        /// </summary>
        /// <returns>Distance to target, or -1 if no valid target.</returns>
        public float GetDistanceToTarget()
        {
            if (targetPosition == Vector3.zero)
                return -1f;

            var myPosition = blackboard.GetMyPosition();
            return Vector3.Distance(myPosition, targetPosition);
        }

        /// <summary>
        /// Checks if the agent has arrived at the target.
        /// </summary>
        /// <returns>True if within arrival threshold.</returns>
        public bool HasArrived()
        {
            return GetDistanceToTarget() <= arrivalThreshold && GetDistanceToTarget() >= 0f;
        }

        /// <summary>
        /// Gets a debug name for this action.
        /// </summary>
        /// <returns>Debug-friendly name.</returns>
        public override string GetDebugName()
        {
            string offsetInfo = useRandomOffset ? $"±{maxOffsetDistance:F1}" : "direct";
            return $"MoveToObjective({offsetInfo})";
        }

        /// <summary>
        /// Creates a direct movement action without random offset.
        /// </summary>
        /// <param name="blackboard">Blackboard instance.</param>
        /// <param name="agent">AI agent.</param>
        /// <returns>Direct movement action.</returns>
        public static MoveToObjectiveAction Direct(Blackboard blackboard, BaseAI agent)
        {
            return new MoveToObjectiveAction(blackboard, agent, 2.0f, false, 0.0f);
        }

        /// <summary>
        /// Creates a spread movement action with random offset.
        /// </summary>
        /// <param name="blackboard">Blackboard instance.</param>
        /// <param name="agent">AI agent.</param>
        /// <param name="maxSpread">Maximum spread distance.</param>
        /// <returns>Spread movement action.</returns>
        public static MoveToObjectiveAction Spread(Blackboard blackboard, BaseAI agent, float maxSpread = 5.0f)
        {
            return new MoveToObjectiveAction(blackboard, agent, 2.0f, true, maxSpread);
        }

        /// <summary>
        /// Logs debug information if debug mode is enabled.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogDebug(string message)
        {
            if (blackboard.Get("DebugMode", false))
            {
                Debug.Log($"[MoveToObjectiveAction] {message}");
            }
        }
    }
}