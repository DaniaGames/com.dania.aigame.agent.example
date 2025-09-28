using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.BeheaviourTree
{
    public class BehaviourTreeAI : BaseAI
    {
        private Tree behaviorTree;
        private Blackboard blackboard;

        protected override void StartAI()
        {
            // Get shared blackboard (only accessible to BehaviourTreeAI)
            blackboard = Blackboard.GetShared(this);

            // Build the behavior tree structure:
            // Root: Selector (try combat, investigate shared intel, or move to point)
            //   ├── Sequence: Combat (check enemy visible AND attack)
            //   │   ├── IsEnemyVisible (stores position in shared blackboard)
            //   │   └── AttackEnemy
            //   ├── InvestigateLastSeenEnemy (check shared blackboard for team intel)
            //   └── MoveToPoint (fallback behavior)

            Selector rootSelector = new Selector(blackboard);

            // Combat sequence: both conditions must succeed
            Sequence combatSequence = new Sequence(blackboard);
            combatSequence.children.Add(new IsEnemyVisible(blackboard, this));
            combatSequence.children.Add(new AttackEnemy(blackboard, this));

            // Investigation behavior: check team intel from shared blackboard
            InvestigateLastSeenEnemy investigateAction = new InvestigateLastSeenEnemy(blackboard, this);

            // Move to a specific point (final fallback)
            Vector3 targetPoint = Vector3.zero; // Center of map
            MoveToPoint moveAction = new MoveToPoint(blackboard, this, targetPoint);

            // Build the selector tree (tries in order)
            rootSelector.children.Add(combatSequence);      // Priority 1: Combat
            rootSelector.children.Add(investigateAction);   // Priority 2: Investigate team intel
            rootSelector.children.Add(moveAction);          // Priority 3: Default movement

            behaviorTree = new Tree(rootSelector, blackboard);
        }

        protected override void ConfigureStats()
        {
            // Simple balanced stats for demonstration
            AllocateStat(StatType.Speed, 4);
            AllocateStat(StatType.VisionRange, 6);
            AllocateStat(StatType.ProjectileRange, 4);
            AllocateStat(StatType.ReloadSpeed, 3);
            AllocateStat(StatType.DodgeCooldown, 3);
        }

        protected override void ExecuteAI()
        {
            // Run the behavior tree
            behaviorTree.Tick();
        }
    }
}