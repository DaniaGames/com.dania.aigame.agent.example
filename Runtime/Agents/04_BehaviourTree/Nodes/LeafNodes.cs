using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.BeheaviourTree
{
    public class IsEnemyVisible : Node
    {
        private BaseAI ai;

        public IsEnemyVisible(Blackboard blackboard, BaseAI ai) : base(blackboard)
        {
            this.ai = ai;
        }

        public override NodeState Evaluate()
        {
            var enemies = ai.GetVisibleEnemiesSnapshot();

            if (enemies.Count > 0)
            {
                ai.RefreshOrAcquireTarget();

                // Store last seen enemy position using generic blackboard methods
                var sharedBlackboard = Blackboard.GetShared(ai);
                if (sharedBlackboard != null && enemies.Count > 0)
                {
                    sharedBlackboard.SetValue("LastSeenEnemyPosition", enemies[0].Position);
                    sharedBlackboard.SetValue("LastSeenEnemyTime", Time.time);
                }

                return NodeState.Success;
            }

            return NodeState.Failure;
        }
    }

    public class AttackEnemy : Node
    {
        private BaseAI ai;

        public AttackEnemy(Blackboard blackboard, BaseAI ai) : base(blackboard)
        {
            this.ai = ai;
        }

        public override NodeState Evaluate()
        {
            if (ai.TryGetTarget(out var target))
            {
                ai.FaceTarget(target.Position);
                ai.ThrowBallAt(target);
                return NodeState.Success;
            }

            return NodeState.Failure;
        }
    }

    public class InvestigateLastSeenEnemy : Node
    {
        private BaseAI ai;

        public InvestigateLastSeenEnemy(Blackboard blackboard, BaseAI ai) : base(blackboard)
        {
            this.ai = ai;
        }

        public override NodeState Evaluate()
        {
            var sharedBlackboard = Blackboard.GetShared(ai);
            if (sharedBlackboard != null)
            {
                // Check if we have recent enemy intel using generic methods
                if (sharedBlackboard.HasKey("LastSeenEnemyPosition") && sharedBlackboard.HasKey("LastSeenEnemyTime"))
                {
                    float lastSeenTime = sharedBlackboard.GetValue<float>("LastSeenEnemyTime");

                    // Only investigate if intel is recent (within 10 seconds)
                    if (Time.time - lastSeenTime < 10f)
                    {
                        Vector3 lastSeenPosition = sharedBlackboard.GetValue<Vector3>("LastSeenEnemyPosition");
                        ai.MoveTo(lastSeenPosition);
                        return NodeState.Success;
                    }
                    else
                    {
                        // Clean up old data using generic methods
                        sharedBlackboard.RemoveKey("LastSeenEnemyPosition");
                        sharedBlackboard.RemoveKey("LastSeenEnemyTime");
                    }
                }
            }

            return NodeState.Failure;
        }
    }

    public class MoveToPoint : Node
    {
        private BaseAI ai;
        private Vector3 targetPoint;

        public MoveToPoint(Blackboard blackboard, BaseAI ai, Vector3 targetPoint) : base(blackboard)
        {
            this.ai = ai;
            this.targetPoint = targetPoint;
        }

        public override NodeState Evaluate()
        {
            ai.MoveTo(targetPoint);

            if (Vector3.Distance(ai.transform.position, targetPoint) < 2f)
            {
                return NodeState.Success;
            }

            return NodeState.Running;
        }
    }
}