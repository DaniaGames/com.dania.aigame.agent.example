using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIGame.Core;


namespace AIGame.Examples.FSM
{
    /// <summary>
    /// Base class for AI states in a simple finite state machine.
    /// Supports optional nested substates that are entered/executed/exited together.
    /// </summary>
    public abstract class AIState
    {
        /// <summary>
        /// The owning AI instance.
        /// </summary>
        protected FinitStateAI parent;

        /// <summary>
        /// Display name for debugging.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional nested substates that run with this state.
        /// </summary>
        protected List<AIState> subStates;

        /// <summary>
        /// Creates a new state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="name">Debug name.</param>
        /// <param name="substates">Optional substates.</param>
        public AIState(FinitStateAI parent, string name, params AIState[] substates)
        {
            this.parent = parent;
            this.Name = name;
            this.subStates = substates?.ToList();
        }

        /// <summary>
        /// Called when entering this state.
        /// Invokes <see cref="Enter"/> on all substates.
        /// </summary>
        public virtual void Enter()
        {
            if (subStates != null)
                foreach (var s in subStates) s.Enter();
        }

        /// <summary>
        /// Called when exiting this state.
        /// Invokes <see cref="Exit"/> on all substates.
        /// </summary>
        public virtual void Exit()
        {
            if (subStates != null)
                foreach (var s in subStates) s.Exit();
        }

        /// <summary>
        /// Called every update while this state is active.
        /// Invokes <see cref="Execute"/> on all substates.
        /// </summary>
        public virtual void Execute()
        {
            if (subStates != null)
                foreach (var s in subStates) s.Execute();
        }
    }

    /// <summary>
    /// A no-op idle state.
    /// </summary>
    public class Idle : AIState
    {
        /// <summary>
        /// Creates an idle state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        public Idle(FinitStateAI parent) : base(parent, "Idle") { }
    }

    /// <summary>
    /// Base state for moving to a world position, raising an event on arrival.
    /// </summary>
    public abstract class MoveToPosition : AIState
    {
        /// <summary>
        /// Raised exactly once when the destination is reached.
        /// </summary>
        public event Action DestinationReached;

        /// <summary>Current target world position.</summary>
        protected Vector3 currentDestination;

        /// <summary>True after arrival is detected.</summary>
        protected bool hasReachedDestination = false;

        /// <summary>Distance threshold to consider arrival.</summary>
        protected const float ARRIVAL_THRESHOLD = 0.5f;

        /// <summary>
        /// Creates a move state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="name">Debug name.</param>
        /// <param name="substates">Optional substates.</param>
        public MoveToPosition(FinitStateAI parent, string name, params AIState[] substates)
            : base(parent, name, substates) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            hasReachedDestination = false;
            base.Enter();
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (!parent.IsAlive || !parent.IsNavMeshAgentEnabled() || !parent.IsOnNavMesh())
                return;

            if (!hasReachedDestination)
            {
                bool arrived = false;

                if (parent.GetRemainingDistance() <= ARRIVAL_THRESHOLD)
                    arrived = true;
                else if (!parent.IsPathPending() && !parent.HasPath() &&
                         Vector3.Distance(parent.transform.position, currentDestination) <= ARRIVAL_THRESHOLD)
                    arrived = true;

                if (arrived)
                {
                    hasReachedDestination = true;
                    DestinationReached?.Invoke();
                }
            }

            base.Execute();
        }
    }

    /// <summary>
    /// Moves near the current match objective.
    /// </summary>
    public class MoveToObjective : MoveToPosition
    {
        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="substates">Optional substates.</param>
        public MoveToObjective(FinitStateAI parent, params AIState[] substates)
            : base(parent, "MoveToObjective", substates) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            hasReachedDestination = false;
            Vector2 spread = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2f, 10f);
            Vector3 offset = new(spread.x, 0f, spread.y);
            currentDestination = GameManager.Instance.Objective.transform.position + offset;
            parent.MoveTo(currentDestination);
            base.Enter();
        }
    }

    /// <summary>
    /// State for engaging visible enemies.
    /// </summary>
    public class Combat : AIState
    {
        /// <summary>
        /// Raised when there are no visible enemies.
        /// </summary>
        public event Action NoMoreEnemies;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="substates">Optional substates.</param>
        public Combat(FinitStateAI parent, params AIState[] substates)
            : base(parent, "Combat", substates) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            parent.SetStopped(true);

            if (parent.GetVisibleEnemiesSnapshot().Count == 0)
                return;

            parent.RefreshOrAcquireTarget();
            parent.StopMoving();
            base.Enter();
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (!parent.CurrentTarget.HasValue)
            {
                if (parent.GetVisibleEnemiesSnapshot().Count > 0)
                    parent.RefreshOrAcquireTarget();
                else
                {
                    NoMoreEnemies?.Invoke();
                    return;
                }
            }

            if (parent.TryGetTarget(out var target))
            {
                parent.FaceTarget(target.Position);
                parent.ThrowBallAt(target);
            }

            base.Execute();
        }

        /// <inheritdoc/>
        public override void Exit()
        {
            parent.RemoveTarget();
            base.Exit();
        }
    }

    /// <summary>
    /// Patrols around the objective by moving to random nearby offsets.
    /// </summary>
    public class ProtectObjective : AIState
    {
        /// <summary>Current patrol destination.</summary>
        private Vector3 currentDestination;

        /// <summary>Arrival threshold for patrol hops.</summary>
        private const float ARRIVAL_THRESHOLD = 0.5f;

        /// <summary>Whether a destination has been set.</summary>
        private bool hasDestination = false;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="substates">Optional substates.</param>
        public ProtectObjective(FinitStateAI parent, params AIState[] substates)
            : base(parent, "ProtectObjective", substates) { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (!hasDestination ||
                (!parent.IsPathPending() &&
                 parent.GetRemainingDistance() <= ARRIVAL_THRESHOLD))
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2f, 5f);
                Vector3 offset = new(spread.x, 0f, spread.y);
                currentDestination = GameManager.Instance.Objective.transform.position + offset;
                parent.MoveTo(currentDestination);
                hasDestination = true;
            }

            base.Execute();
        }

        /// <inheritdoc/>
        public override void Exit()
        {
            hasDestination = false;
            base.Exit();
        }
    }

    /// <summary>
    /// Strafes left/right around current position.
    /// </summary>
    public class Strafe : AIState
    {
        /// <summary>Current strafe direction flag.</summary>
        private bool movingRight = true;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        public Strafe(FinitStateAI parent) : base(parent, "Strafe") { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (!parent.IsPathPending() &&
                parent.GetRemainingDistance() <= 0.5f) // Using typical stopping distance
            {
                movingRight = !movingRight;
                Vector3 offset = (movingRight ? parent.transform.right : -parent.transform.right) * 5f;
                parent.StrafeTo(parent.transform.position + offset);
            }

            base.Execute();
        }
    }

    /// <summary>
    /// Performs a dodge when a hostile ball is detected nearby.
    /// </summary>
    public class Dodge : AIState
    {
        /// <summary>Last detected hostile ball.</summary>
        private Ball ball;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        public Dodge(FinitStateAI parent) : base(parent, "Dodge") { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (ball != null)
            {
                float distance = Vector3.Distance(parent.transform.position, ball.transform.position);
                if (distance <= 20)
                {
                    parent.StartDodge(UnityEngine.Random.value > 0.5f ? parent.transform.right : -parent.transform.right);
                    ball = null;
                }
            }

            base.Execute();
        }

        /// <summary>
        /// AI hook for ball sightings.
        /// </summary>
        /// <param name="ball">Detected ball.</param>
        public void OnBallDetected(Ball ball)
        {
            this.ball = ball;
        }
    }

    /// <summary>
    /// Follows a visible enemy and stops at preferred engagement range.
    /// </summary>
    public class FollowEnemy : AIState
    {
        /// <summary>Whether the agent has issued a stop after entering range.</summary>
        private bool stopped = false;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="substates">Optional substates.</param>
        public FollowEnemy(FinitStateAI parent, params AIState[] substates)
            : base(parent, "Follow", substates) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            parent.RefreshOrAcquireTarget();
            base.Enter();
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (!parent.TryGetTarget(out var target))
                return;

            Vector3 myPos = parent.transform.position;
            Vector3 tgtPos = target.Position;

            float dist = Vector3.Distance(myPos, tgtPos);
            float desired = parent.ProjectileRange;
            float buffer = 0.25f;

            if (dist > desired + buffer)
            {
                Vector3 dir = (tgtPos - myPos).normalized;
                Vector3 stopPos = tgtPos - dir * (desired - 3f); // preserves original tweak
                parent.MoveTo(stopPos);
                stopped = false;
            }
            else if (!stopped)
            {
                parent.StopMoving();
                stopped = true;
            }

            base.Execute();
        }
    }


    /// <summary>
    /// State for seeking and collecting power-ups.
    /// </summary>
    /// 
    public class MoveToPowerUp : MoveToPosition
    {
        private int targetPowerUpId = -1;


        public MoveToPowerUp(FinitStateAI parent, params AIState[] substates) : base(parent, "Move to powerup", substates)
        {

        }

        public override void Enter()
        {
            // Only set a new target if we don't already have one
            if (targetPowerUpId == -1)
            {
                var powerUps = parent.GetVisiblePowerUpsSnapshot();
                if (powerUps.Count > 0)
                {
                    targetPowerUpId = powerUps[0].Id;
                    currentDestination = powerUps[0].Position;
                    parent.MoveTo(currentDestination);
                }
            }
            base.Enter();
        }

        public override void Exit()
        {
            // Reset target when leaving state
            targetPowerUpId = -1;
            base.Exit();
        }

        public override void Execute()
        {
            // Check if our target powerup still exists
            if (targetPowerUpId != -1)
            {
                var visiblePowerUps = parent.GetVisiblePowerUpsSnapshot();
                bool targetStillExists = false;

                foreach (var powerUp in visiblePowerUps)
                {
                    if (powerUp.Id == targetPowerUpId)
                    {
                        targetStillExists = true;
                        // Update destination in case it moved
                        currentDestination = powerUp.Position;
                        break;
                    }
                }

                // If target no longer exists, try to find a new one
                if (!targetStillExists)
                {
                    targetPowerUpId = -1; // Reset target
                    hasReachedDestination = false; // Reset reached flag

                    // Try to find another powerup
                    if (visiblePowerUps.Count > 0)
                    {
                        targetPowerUpId = visiblePowerUps[0].Id;
                        currentDestination = visiblePowerUps[0].Position;
                        parent.MoveTo(currentDestination);
                    }
                }
            }

            // Let base class handle arrival detection and call DestinationReached event
            base.Execute();

            // Only try to consume powerup if we've reached the destination
            if (hasReachedDestination)
            {
                parent.EatPowerUp(targetPowerUpId);
            }
        }
    }
}
