using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Behavior Tree-based AI implementation that uses a modular node system
    /// for decision making and action execution.
    ///
    /// This AI demonstrates the flexibility of behavior trees by combining
    /// combat, movement, and tactical behaviors in a structured hierarchy.
    /// </summary>
    public class BehaviorTreeAI : BaseAI
    {
        /// <summary>
        /// The main behavior tree instance.
        /// </summary>
        private BehaviorTree behaviorTree;

        /// <summary>
        /// Shared blackboard for all tree nodes.
        /// </summary>
        private Blackboard blackboard;

        /// <summary>
        /// Configures the AI's stat allocation for balanced performance.
        /// </summary>
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 5);
            AllocateStat(StatType.VisionRange, 5);
            AllocateStat(StatType.ProjectileRange, 5);
            AllocateStat(StatType.ReloadSpeed, 5);
        }

        /// <summary>
        /// Initializes the behavior tree and sets up the AI.
        /// </summary>
        protected override void StartAI()
        {
            // Create blackboard and enable debug mode if requested
            blackboard = new Blackboard();
            blackboard.Set("DebugMode", false);

            // Build the behavior tree based on selected type
            var rootNode = BuildBehaviorTree();

            // Create and start the behavior tree
            behaviorTree = new BehaviorTree(rootNode, this, blackboard, false);
            behaviorTree.Start();

            // Subscribe to BaseAI events (this is the correct pattern!)
            Respawned += OnRespawnedEvent;
            Death += OnDeathEvent;
            EnemyEnterVision += OnEnemyEnterVision;

        }

        /// <summary>
        /// Main AI execution method called every frame.
        /// Runs the behavior tree and handles performance monitoring.
        /// </summary>
        protected override void ExecuteAI()
        {
            if (behaviorTree == null || !behaviorTree.IsActive)
                return;

            // Refresh target acquisition - this is crucial for enemy detection
            RefreshOrAcquireTarget();

            // Execute the behavior tree
            behaviorTree.Tick();

            // Face current target if we have one (prioritize over movement direction)
            if (TryGetTarget(out var target))
            {
                FaceTarget(target.Position);
            }
        }

        /// <summary>
        /// Builds the behavior tree structure based on the selected tree type.
        /// </summary>
        /// <returns>Root node of the constructed tree.</returns>
        private BehaviorTreeNode BuildBehaviorTree()
        {
            return SelectorNode.Create(blackboard, this)
                .AddChild(AttackTargetAction.Sustained(blackboard, this))
                .AddChild(MoveToObjectiveAction.Spread(blackboard, this))
                .Build();
        }


        /// <summary>
        /// Event handler for respawn events. Resets the behavior tree.
        /// </summary>
        private void OnRespawnedEvent()
        {
            if (behaviorTree != null)
            {
                behaviorTree.Reset();
            }
        }

        /// <summary>
        /// Event handler for death events. Stops the behavior tree.
        /// </summary>
        private void OnDeathEvent()
        {
            if (behaviorTree != null)
            {
                behaviorTree.Stop();
            }
        }

        /// <summary>
        /// Event handler for when an enemy enters vision. Forces immediate attack mode.
        /// </summary>
        private void OnEnemyEnterVision()
        {
            // Force immediate target acquisition like MediumAI does
            RefreshOrAcquireTarget();

            // Set a flag in blackboard to force attack mode
            blackboard.Set("ForceAttack", true);

            // Reset the behavior tree to force re-evaluation of the selector
            if (behaviorTree != null)
            {
                behaviorTree.Reset();
            }
        }

        /// <summary>
        /// Unity lifecycle method for cleanup.
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            Respawned -= OnRespawnedEvent;
            Death -= OnDeathEvent;

            if (behaviorTree != null)
            {
                behaviorTree.Dispose();
                behaviorTree = null;
            }
        }

        /// <summary>
        /// Gets the current behavior tree for external inspection.
        /// </summary>
        /// <returns>Active behavior tree instance.</returns>
        public BehaviorTree GetBehaviorTree()
        {
            return behaviorTree;
        }

        /// <summary>
        /// Gets the shared blackboard for external inspection.
        /// </summary>
        /// <returns>Shared blackboard instance.</returns>
        public Blackboard GetBlackboard()
        {
            return blackboard;
        }
    }
}