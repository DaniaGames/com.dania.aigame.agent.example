using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// This class shows how to use a finit state machine to create an AI with different states
    /// </summary>
    public abstract class ExampleAIState
    {
        /// <summary>
        /// A reference to the agenmt that owns the state
        /// </summary>
        protected ExampleAI parent;

        /// <summary>
        /// The name of the state, this is just for debugging
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A list of substates, substates are states that execute as part of another state
        /// </summary>
        protected List<ExampleAIState> subStates;

        public ExampleAIState(ExampleAI parent, string name, params ExampleAIState[] substates)
        {
            this.parent = parent;
            this.Name = name;
            this.subStates = substates?.ToList();
        }

        /// <summary>
        /// Enter is called whenever the AI enters this state
        /// </summary>
        public virtual void Enter() 
        {
            if (subStates != null)
            {
                foreach (ExampleAIState state in subStates)
                {
                    state.Enter();
                }
            }  
        }

        /// <summary>
        /// Exit is executed whenever the AI is done executing the state
        /// </summary>
        public virtual void Exit() 
        {
            if (subStates != null)
            {
                foreach (ExampleAIState state in subStates)
                {
                    state.Exit();
                }
            }
        }

        /// <summary>
        /// Execute runs every update
        /// </summary>
        public virtual void Execute() 
        {
            if (subStates != null)
            {
                foreach (ExampleAIState state in subStates)
                {
                    state.Execute();
                }
            }
        }


    }

    /// <summary>
    /// A Simple idle state, that doesn't do anything
    /// </summary>
    public class Idle : ExampleAIState
    {

        public Idle(ExampleAI parent) : base(parent, "Idle")
        {

        }
    }

    /// <summary>
    /// A state that moves to a given position
    /// </summary>
    public abstract class MoveToPosition : ExampleAIState
    {
        public event Action DestinationReached;

        protected Vector3 currentDestination;
        protected bool hasReachedDestination = false;
        protected const float ARRIVAL_THRESHOLD = 0.5f;

        public MoveToPosition(ExampleAI parent, string name ,params ExampleAIState[] substates) : base(parent, name, substates)
        {

        }

        public override void Enter()
        {
            hasReachedDestination = false;
            base.Enter();
        }

        public override void Execute()
        {
            var agent = parent.NavMeshAgent;

            // Skip if agent is dead, disabled, or no destination set
            if (!parent.IsAlive || !agent.enabled || !agent.isOnNavMesh)
                return;

            if (!hasReachedDestination)
            {
                bool arrived = false;

                // Only consider arrival if there's an active path
                if (agent.remainingDistance <= ARRIVAL_THRESHOLD)
                {
                    arrived = true;
                }
                else if (!agent.pathPending && !agent.hasPath &&
                         Vector3.Distance(parent.transform.position, currentDestination) <= ARRIVAL_THRESHOLD)
                {
                    arrived = true;
                }

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
    /// This state moves to the objective
    /// </summary>
    public class MoveToObjective : MoveToPosition
    {
        public MoveToObjective(ExampleAI parent,params ExampleAIState[] substates) : base(parent, "MoveToObjective", substates)
        {

        }

        public override void Enter()
        {
            hasReachedDestination = false;
            Vector2 spread = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2f, 10f);
            Vector3 controlPointOffset = new Vector3(spread.x, 0f, spread.y);
            currentDestination = GameManager.Instance.Objective.transform.position + controlPointOffset;
            parent.MoveTo(currentDestination);

            base.Enter();
        }
    }

    /// <summary>
    /// State for being engaged in combat
    /// </summary>
    public class Combat : ExampleAIState
    {


        public event Action NoMoreEnemies;

        public Combat(ExampleAI parent, params ExampleAIState[] substates) : base(parent, "Combat", substates)
        {
        }

        public override void Enter()
        {
            parent.NavMeshAgent.isStopped = true;

            // 1) Nothing to do if no enemies
            if (parent.GetVisibleEnemiesSnapshot().Count == 0)
                return;

            // 2) Pick the first visible enemy
            parent.RefreshOrAcquireTarget();

            parent.StopMoving();

            base.Enter();
        }

        public override void Execute()
        {

            if (!parent.CurrentTarget.HasValue)
            {
                if (parent.GetVisibleEnemiesSnapshot().Count > 0)
                {
                    parent.RefreshOrAcquireTarget();
                }
                else
                {
                    Debug.Log("No more enemies");
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

        public override void Exit()
        {
            parent.RemoveTarget();

            base.Exit();
        }
    }


    /// <summary>
    /// Moves around the objective to protect it
    /// </summary>
    public class ProtectObjective : ExampleAIState
    {
        private Vector3 currentDestination;
        private const float ARRIVAL_THRESHOLD = 0.5f;
        private bool hasDestination = false;

        public ProtectObjective(ExampleAI parent, params ExampleAIState[] substates) : base(parent, "ProtectObjective", substates)
        {
        }

        public override void Execute()
        {
            if (!hasDestination ||
                (!parent.NavMeshAgent.pathPending &&
                 parent.NavMeshAgent.remainingDistance <= ARRIVAL_THRESHOLD))
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2f, 5f);
                Vector3 controlPointOffset = new Vector3(spread.x, 0f, spread.y);

                currentDestination = GameManager.Instance.Objective.transform.position + controlPointOffset;
                parent.MoveTo(currentDestination);
                hasDestination = true;
            }

            base.Execute();
        }

        public override void Exit()
        {
            hasDestination = false;
            base.Exit();

        }
    }

    /// <summary>
    /// Strafe from left to right
    /// </summary>
    public class Strafe : ExampleAIState
    {
        private bool movingRight = true;

        public Strafe(ExampleAI parent) : base(parent, "Strafe")
        {
        }

        public override void Execute()
        {
            if (!parent.NavMeshAgent.pathPending &&
                parent.NavMeshAgent.remainingDistance <= parent.NavMeshAgent.stoppingDistance)
            {
                movingRight = !movingRight;

                Vector3 offset = (movingRight ? parent.transform.right : -parent.transform.right) * 5f;
                parent.StrafeTo(parent.transform.position + offset);
            }

            base.Execute();
        }

    }

    /// <summary>
    /// Dodge if an unfriendly ball gets close
    /// </summary>
    public class Dodge : ExampleAIState
    {
        private Ball ball;

        public Dodge(ExampleAI parent) : base(parent, "Dodge")
        {
        }

        public override void Execute()
        {
            if (ball != null)
            {
                float distance = Vector3.Distance(parent.transform.position, ball.transform.position);

                if (distance <= 20)
                {
                    parent.StartDodge((UnityEngine.Random.value > 0.5f) ? parent.transform.right : -parent.transform.right);
                    ball = null;
                }
            }
            
            base.Execute();
        }

        public void OnBallDetected(Ball ball)
        {
            this.ball = ball;
        }


    }

    /// <summary>
    /// Follow an enemy
    /// </summary>
    public class FollowEnemy : ExampleAIState
    {
        private bool stopped = false;

        public FollowEnemy(ExampleAI parent, params ExampleAIState[] substates) : base(parent, "Follow", substates)
        {
        }

        public override void Enter()
        {
            parent.RefreshOrAcquireTarget();
            base.Enter();
        }

        public override void Execute()
        {
            if (!parent.TryGetTarget(out var target))
                return;

            Vector3 myPos = parent.transform.position;
            Vector3 tgtPos = target.Position;

            float dist = Vector3.Distance(myPos, tgtPos);
            float desired = parent.ProjectileRange;   // your preferred engagement distance
            float buffer = 0.25f;                    // tiny hysteresis to avoid jitter

            if (dist > desired + buffer)
            {
                // Move closer but stop at desired range (you had a -3 offset; keep if intentional)
                Vector3 dir = (tgtPos - myPos).normalized;
                Vector3 stopPos = tgtPos - dir * (desired - 3f); // keep your -3 tweak
                parent.MoveTo(stopPos);
                stopped = false;
            }
            else
            {
                // We are in range — stop once
                if (!stopped)
                {
                    parent.StopMoving();
                    stopped = true;
                }
            }

            base.Execute();
        }





    }

    /// <summary>
    /// Move to an enemy flag zone
    /// </summary>
    public class MoveToEnemyZone : MoveToPosition
    {
        public MoveToEnemyZone(ExampleAI parent, params ExampleAIState[] substates) : base(parent, "Move to enemy flag zone", substates)
        {

        }

        public override void Enter()
        {
            currentDestination = parent.MyDetectable.TeamID == Team.Red ? CaptureTheFlag.Instance.BlueFlagZone.transform.position : CaptureTheFlag.Instance.RedFlagZone.transform.position;
            parent.MoveTo(currentDestination);
            base.Enter();
        }
    }


    /// <summary>
    /// Move to a friendly flag zone
    /// </summary>
    public class MoveToFriendlyZone : MoveToPosition
    {
        public MoveToFriendlyZone(ExampleAI parent, params ExampleAIState[] substates) : base(parent, "Move to friendly flag zone", substates)
        {

        }

        public override void Enter()
        {
            currentDestination = parent.MyDetectable.TeamID != Team.Red ? CaptureTheFlag.Instance.BlueFlagZone.transform.position : CaptureTheFlag.Instance.RedFlagZone.transform.position;
            parent.MoveTo(currentDestination);
            base.Enter();
        }
    }

    /// <summary>
    /// Move to the friendly flag
    /// </summary>
    public class FollowFriendlyFlag : MoveToPosition
    {
        private Flag flag;

        public FollowFriendlyFlag(ExampleAI parent, params ExampleAIState[] substates) : base(parent, "Follow friendly flag", substates)
        {

        }

        public override void Enter()
        {

            flag = parent.MyDetectable.TeamID != Team.Red ? CaptureTheFlag.Instance.BlueFlag : CaptureTheFlag.Instance.RedFlag;
            currentDestination = flag.transform.position;
            parent.MoveTo(currentDestination);
            base.Enter();
        }

        public override void Execute()
        {
            if (!parent.NavMeshAgent.pathPending &&
            parent.NavMeshAgent.remainingDistance <= ARRIVAL_THRESHOLD)
            {
                return;
            }

            currentDestination = flag.transform.position;
            parent.MoveTo(currentDestination);
            base.Execute();
        }
    }

    /// <summary>
    /// Move to the enemy flag
    /// </summary>
    public class MoveToEnemyFlag : MoveToPosition
    {
        public MoveToEnemyFlag(ExampleAI parent, params ExampleAIState[] substates) : base(parent, "Move to enemy flag", substates)
        {

        }

        public override void Enter()
        {
            currentDestination = parent.MyDetectable.TeamID == Team.Red ? CaptureTheFlag.Instance.BlueFlag.transform.position : CaptureTheFlag.Instance.RedFlag.transform.position;
            parent.MoveTo(currentDestination);
            base.Enter();
        }
    }

    /// <summary>
    /// Chase the enemy flag carrier
    /// </summary>
    public class ChaseEnemyCarrier : MoveToPosition
    {
        private int enemyCarrierId = -1;                 // stable id to chase
        private PerceivedAgent? enemyCarrier;            // refreshed per frame
        private Flag myFlag;
        private Flag enemyFlag;

        public ChaseEnemyCarrier(ExampleAI parent, params ExampleAIState[] substates)
            : base(parent, "Chase Enemy Carrier", substates)
        {
            var blueFlag = CaptureTheFlag.Instance.BlueFlag;
            var redFlag = CaptureTheFlag.Instance.RedFlag;

            myFlag = (blueFlag.TeamID == parent.MyDetectable.TeamID) ? blueFlag : redFlag;
            enemyFlag = (myFlag == blueFlag) ? redFlag : blueFlag;
        }

        public override void Enter()
        {
            base.Enter();

            // Lock in who we're chasing by ID (may be -1 if nobody has it)
            var carrier = myFlag.FlagCarrier;
            enemyCarrierId = carrier != null ? carrier.MyAgent.AgentID : -1;

            RefreshEnemyCarrierSnapshot();

            if (enemyCarrier.HasValue)
            {
                currentDestination = enemyCarrier.Value.Position;
                parent.MoveTo(currentDestination);
            }
        }

        public override void Execute()
        {
            // Refresh snapshot for this frame (may become null if not visible)
            RefreshEnemyCarrierSnapshot();

            if (!enemyCarrier.HasValue)
                return;

            var carrierPos = enemyCarrier.Value.Position;

            float distance = Vector3.Distance(parent.transform.position, carrierPos);

            if (distance <= parent.ProjectileRange)
            {
                parent.StopMoving();
                parent.FaceTarget(carrierPos);
                parent.ThrowBallAt(enemyCarrier.Value); // your ThrowBallAt(PerceivedAgent) version
            }
            else
            {
                // Repath only if it moved meaningfully
                if (Vector3.Distance(currentDestination, carrierPos) > 0.5f)
                {
                    currentDestination = carrierPos;
                    parent.MoveTo(currentDestination);
                }
            }

            base.Execute();
        }

        private void RefreshEnemyCarrierSnapshot()
        {
            if (enemyCarrierId < 0) { enemyCarrier = null; return; }

            // Look for that ID in the current snapshot
            PerceivedAgent? found = null;
            var vis = parent.GetVisibleEnemiesSnapshot();
            for (int i = 0; i < vis.Count; i++)
            {
                if (vis[i].Id == enemyCarrierId) { found = vis[i]; break; }
            }
            enemyCarrier = found; // may be null if not visible this frame
        }
    }
}
