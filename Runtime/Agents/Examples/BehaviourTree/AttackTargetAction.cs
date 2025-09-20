using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Action node that attacks the current target using the agent's projectile system.
    /// Returns RUNNING while attacking, FAILURE if no valid target.
    /// </summary>
    public class AttackTargetAction : BehaviorTreeNode
    {
        /// <summary>
        /// Maximum range at which to attempt attacks.
        /// </summary>
        private float maxAttackRange;

        /// <summary>
        /// Whether we've stopped movement for this attack.
        /// </summary>
        private bool hasStoppedMoving;

        /// <summary>
        /// Creates a new attack target action.
        /// </summary>
        /// <param name="blackboard">Shared blackboard instance.</param>
        /// <param name="agent">AI agent reference.</param>
        /// <param name="maxRange">Maximum attack range, -1 for agent's projectile range (default: -1).</param>
        public AttackTargetAction(Blackboard blackboard, BaseAI agent, float maxRange = -1f)
            : base(blackboard, agent)
        {
            maxAttackRange = maxRange > 0 ? maxRange : agent.ProjectileRange;
            hasStoppedMoving = false;
        }

        /// <summary>
        /// Called when the action starts executing.
        /// </summary>
        protected override void OnEnter()
        {
            // Stop moving immediately when entering attack mode
            agent.StopMoving();
            hasStoppedMoving = true;
        }

        /// <summary>
        /// Main execution logic for the attack action.
        /// </summary>
        /// <returns>Current execution state.</returns>
        protected override NodeState Execute()
        {
            // Check if we're being forced into attack mode by vision event
            bool forceAttack = blackboard.Get("ForceAttack", false);

            // Try to get current target
            if (!agent.TryGetTarget(out var target))
            {
                // Try to acquire a target if we don't have one
                if (agent.GetVisibleEnemiesSnapshot().Count > 0 || forceAttack)
                {
                    agent.RefreshOrAcquireTarget();
                    if (!agent.TryGetTarget(out target))
                    {
                        // Clear force attack flag if we can't get a target
                        blackboard.Set("ForceAttack", false);
                        return NodeState.Failure;
                    }
                }
                else
                {
                    return NodeState.Failure;
                }
            }

            // Clear the force attack flag once we have a target
            if (forceAttack)
            {
                blackboard.Set("ForceAttack", false);
            }

            // Check if target is in range
            var myPosition = blackboard.GetMyPosition();
            float distanceToTarget = Vector3.Distance(myPosition, target.Position);

            // If target is out of range, face them and wait
            if (distanceToTarget > maxAttackRange)
            {
                // Face the target while waiting for them to get closer
                agent.FaceTarget(target.Position);
                return NodeState.Running; // Keep running, don't fail
            }

            // Get fresh target position
            var visibleEnemies = agent.GetVisibleEnemiesSnapshot();
            PerceivedAgent? freshTarget = null;
            foreach (var enemy in visibleEnemies)
            {
                if (enemy.Id == target.Id)
                {
                    freshTarget = enemy;
                    break;
                }
            }

            var targetToUse = freshTarget ?? target;

            // Now we're in range - stop moving and attack
            if (!hasStoppedMoving)
            {
                agent.StopMoving();
                hasStoppedMoving = true;
            }

            // Face and attack target
            agent.FaceTarget(targetToUse.Position);
            agent.ThrowBallAt(targetToUse);

            return NodeState.Running;
        }

        /// <summary>
        /// Called when the action finishes executing.
        /// </summary>
        protected override void OnExit()
        {
            // Attack is ending, movement will be handled by next action
        }

        /// <summary>
        /// Resets the action to its initial state.
        /// </summary>
        public override void Reset()
        {
            hasStoppedMoving = false;
            base.Reset();
        }

        /// <summary>
        /// Gets a debug name for this action.
        /// </summary>
        /// <returns>Debug-friendly name.</returns>
        public override string GetDebugName()
        {
            return $"AttackTarget(r:{maxAttackRange:F1})";
        }

        /// <summary>
        /// Creates a sustained attack action.
        /// </summary>
        /// <param name="blackboard">Blackboard instance.</param>
        /// <param name="agent">AI agent.</param>
        /// <returns>Attack action.</returns>
        public static AttackTargetAction Sustained(Blackboard blackboard, BaseAI agent)
        {
            return new AttackTargetAction(blackboard, agent);
        }

    }
}