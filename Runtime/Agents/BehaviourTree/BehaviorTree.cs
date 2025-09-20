using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Main behavior tree executor that manages tree execution and coordination.
    /// Handles root node execution, debugging, and integration with the AI system.
    /// </summary>
    public class BehaviorTree
    {
        /// <summary>
        /// Root node of the behavior tree.
        /// </summary>
        private BehaviorTreeNode rootNode;

        /// <summary>
        /// Shared blackboard for all nodes in this tree.
        /// </summary>
        private Blackboard blackboard;

        /// <summary>
        /// Reference to the AI agent that owns this behavior tree.
        /// </summary>
        private BaseAI agent;

        /// <summary>
        /// Whether the tree is currently active and should execute.
        /// </summary>
        private bool isActive;

        /// <summary>
        /// Current execution state of the entire tree.
        /// </summary>
        private NodeState treeState;

        /// <summary>
        /// Frame counter for debugging and performance tracking.
        /// </summary>
        private int executionCount;

        /// <summary>
        /// Whether to enable debug logging for tree execution.
        /// </summary>
        private bool debugMode;

        /// <summary>
        /// Time when the tree was last executed (for performance monitoring).
        /// </summary>
        private float lastExecutionTime;

        /// <summary>
        /// Creates a new behavior tree with the specified root node.
        /// </summary>
        /// <param name="rootNode">The root node of the tree.</param>
        /// <param name="agent">The AI agent that owns this tree.</param>
        /// <param name="debugMode">Whether to enable debug output.</param>
        public BehaviorTree(BehaviorTreeNode rootNode, BaseAI agent, bool debugMode = false)
        {
            this.rootNode = rootNode;
            this.agent = agent;
            this.debugMode = debugMode;
            this.blackboard = new Blackboard();
            this.isActive = true;
            this.treeState = NodeState.Failure;
            this.executionCount = 0;

            // Subscribe to blackboard changes for debugging
            if (debugMode)
            {
                blackboard.OnDataChanged += OnBlackboardDataChanged;
            }

            LogDebug("Behavior tree initialized");
        }

        /// <summary>
        /// Alternative constructor that creates a blackboard automatically.
        /// </summary>
        /// <param name="rootNode">The root node of the tree.</param>
        /// <param name="agent">The AI agent that owns this tree.</param>
        /// <param name="blackboard">Existing blackboard to use.</param>
        /// <param name="debugMode">Whether to enable debug output.</param>
        public BehaviorTree(BehaviorTreeNode rootNode, BaseAI agent, Blackboard blackboard, bool debugMode = false)
        {
            this.rootNode = rootNode;
            this.agent = agent;
            this.blackboard = blackboard;
            this.debugMode = debugMode;
            this.isActive = true;
            this.treeState = NodeState.Failure;
            this.executionCount = 0;

            if (debugMode)
            {
                blackboard.OnDataChanged += OnBlackboardDataChanged;
            }

            LogDebug("Behavior tree initialized with existing blackboard");
        }

        /// <summary>
        /// Gets the shared blackboard instance.
        /// </summary>
        public Blackboard Blackboard => blackboard;

        /// <summary>
        /// Gets the root node of the tree.
        /// </summary>
        public BehaviorTreeNode RootNode => rootNode;

        /// <summary>
        /// Gets the current state of the tree execution.
        /// </summary>
        public NodeState TreeState => treeState;

        /// <summary>
        /// Gets whether the tree is currently active.
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// Gets the total number of times the tree has been executed.
        /// </summary>
        public int ExecutionCount => executionCount;

        /// <summary>
        /// Gets or sets debug mode for the tree.
        /// </summary>
        public bool DebugMode
        {
            get => debugMode;
            set
            {
                if (debugMode != value)
                {
                    debugMode = value;
                    if (debugMode)
                    {
                        blackboard.OnDataChanged += OnBlackboardDataChanged;
                    }
                    else
                    {
                        blackboard.OnDataChanged -= OnBlackboardDataChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Main execution method called each frame to run the behavior tree.
        /// Updates the blackboard with current agent data and executes the root node.
        /// </summary>
        /// <returns>Current state of the tree execution.</returns>
        public NodeState Tick()
        {
            if (!isActive || rootNode == null)
            {
                return NodeState.Failure;
            }

            float startTime = Time.realtimeSinceStartup;

            // Update blackboard with current agent perception data
            UpdateBlackboardFromAgent();

            // Execute the root node
            treeState = rootNode.Tick();

            executionCount++;
            lastExecutionTime = Time.realtimeSinceStartup - startTime;

            LogDebug($"Tree tick {executionCount}: {treeState} (took {lastExecutionTime * 1000:F2}ms)");

            return treeState;
        }

        /// <summary>
        /// Updates the blackboard with current data from the AI agent.
        /// This ensures nodes have access to the latest perception and state information.
        /// </summary>
        private void UpdateBlackboardFromAgent()
        {
            if (agent == null) return;

            // Update perception data
            var enemies = agent.GetVisibleEnemiesSnapshot();
            blackboard.SetVisibleEnemies(enemies);
            blackboard.SetVisibleAllies(agent.GetVisibleAlliesSnapshot());

            if (debugMode && enemies.Count > 0)
            {
                LogDebug($"Updated blackboard with {enemies.Count} visible enemies");
            }

            // Update position and basic state
            blackboard.SetMyPosition(agent.transform.position);

            // Update target information
            if (agent.TryGetTarget(out var target))
            {
                blackboard.SetCurrentTarget(target);
            }
            else
            {
                blackboard.SetCurrentTarget(null);
            }

            // Update objective position if available
            if (GameManager.Instance.Objective != null)
            {
                blackboard.SetObjectivePosition(GameManager.Instance.Objective.transform.position);
            }

            // Update game mode specific data
            if (CaptureTheFlag.Instance != null)
            {
                blackboard.Set("CTF_Instance", CaptureTheFlag.Instance);

                // Only update flag/zone data if flags have been spawned
                // This prevents null reference exceptions during game initialization
                var redFlag = CaptureTheFlag.Instance.RedFlag;
                var blueFlag = CaptureTheFlag.Instance.BlueFlag;

                if (redFlag != null && blueFlag != null)
                {
                    // Update flag information based on team
                    var isRedTeam = agent.MyDetectable.TeamID == Team.Red;
                    var friendlyFlag = isRedTeam ? redFlag : blueFlag;
                    var enemyFlag = isRedTeam ? blueFlag : redFlag;

                    blackboard.Set("FriendlyFlag", friendlyFlag);
                    blackboard.Set("EnemyFlag", enemyFlag);
                    blackboard.Set("FriendlyFlagPosition", friendlyFlag.transform.position);
                    blackboard.Set("EnemyFlagPosition", enemyFlag.transform.position);

                    // Update flag zones (these should be available when flags exist)
                    var redZone = CaptureTheFlag.Instance.RedFlagZone;
                    var blueZone = CaptureTheFlag.Instance.BlueFlagZone;

                    if (redZone != null && blueZone != null)
                    {
                        var friendlyZone = isRedTeam ? redZone : blueZone;
                        var enemyZone = isRedTeam ? blueZone : redZone;

                        blackboard.Set("FriendlyZone", friendlyZone);
                        blackboard.Set("EnemyZone", enemyZone);
                        blackboard.Set("FriendlyZonePosition", friendlyZone.transform.position);
                        blackboard.Set("EnemyZonePosition", enemyZone.transform.position);
                    }
                }
            }
        }

        /// <summary>
        /// Starts or resumes tree execution.
        /// </summary>
        public void Start()
        {
            isActive = true;
            LogDebug("Behavior tree started");
        }

        /// <summary>
        /// Stops tree execution.
        /// </summary>
        public void Stop()
        {
            isActive = false;
            LogDebug("Behavior tree stopped");
        }

        /// <summary>
        /// Resets the entire tree to its initial state.
        /// Useful for restarting behaviors or handling respawns.
        /// </summary>
        public void Reset()
        {
            if (rootNode != null)
            {
                rootNode.Reset();
            }
            treeState = NodeState.Failure;
            executionCount = 0;
            LogDebug("Behavior tree reset");
        }

        /// <summary>
        /// Changes the root node of the tree.
        /// Useful for dynamic behavior switching.
        /// </summary>
        /// <param name="newRootNode">New root node to use.</param>
        public void SetRootNode(BehaviorTreeNode newRootNode)
        {
            if (rootNode != null)
            {
                rootNode.Reset();
            }

            rootNode = newRootNode;
            treeState = NodeState.Failure;
            LogDebug($"Root node changed to: {newRootNode?.GetDebugName() ?? "null"}");
        }

        /// <summary>
        /// Gets performance statistics for the tree execution.
        /// </summary>
        /// <returns>String containing performance information.</returns>
        public string GetPerformanceStats()
        {
            return $"Executions: {executionCount}, Last: {lastExecutionTime * 1000:F2}ms, State: {treeState}";
        }

        /// <summary>
        /// Gets a debug representation of the current tree structure.
        /// </summary>
        /// <returns>String representation of the tree hierarchy.</returns>
        public string GetTreeStructure()
        {
            if (rootNode == null)
                return "Tree: No root node";

            return "Tree Structure:\n" + GetNodeStructure(rootNode, 0);
        }

        /// <summary>
        /// Recursively builds a string representation of the node structure.
        /// </summary>
        /// <param name="node">Node to represent.</param>
        /// <param name="depth">Current depth in the tree.</param>
        /// <returns>String representation of the node and its children.</returns>
        private string GetNodeStructure(BehaviorTreeNode node, int depth)
        {
            if (node == null) return "";

            string indent = new string(' ', depth * 2);
            string result = $"{indent}- {node.GetDebugName()} [{node.State}]\n";

            foreach (var child in node.Children)
            {
                result += GetNodeStructure(child, depth + 1);
            }

            return result;
        }

        /// <summary>
        /// Event handler for blackboard data changes (debug mode only).
        /// </summary>
        /// <param name="key">Key that was changed.</param>
        /// <param name="value">New value (or null if removed).</param>
        private void OnBlackboardDataChanged(string key, object value)
        {
            if (debugMode)
            {
                LogDebug($"Blackboard updated: {key} = {value?.ToString() ?? "null"}");
            }
        }

        /// <summary>
        /// Logs a debug message if debug mode is enabled.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[BehaviorTree] {message}");
            }
        }

        /// <summary>
        /// Cleanup method to unsubscribe from events.
        /// </summary>
        public void Dispose()
        {
            if (blackboard != null)
            {
                blackboard.OnDataChanged -= OnBlackboardDataChanged;
            }

            isActive = false;
            LogDebug("Behavior tree disposed");
        }
    }
}