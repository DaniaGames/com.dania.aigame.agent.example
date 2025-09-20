using System.Collections.Generic;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Represents the execution state of a behavior tree node.
    /// </summary>
    public enum NodeState
    {
        /// <summary>Node is currently executing and not yet complete.</summary>
        Running,

        /// <summary>Node completed successfully.</summary>
        Success,

        /// <summary>Node failed to complete.</summary>
        Failure
    }

    /// <summary>
    /// Abstract base class for all behavior tree nodes.
    /// Provides core functionality for tree traversal, blackboard access, and execution state management.
    /// </summary>
    public abstract class BehaviorTreeNode
    {
        /// <summary>
        /// Reference to the shared blackboard for data storage and communication.
        /// </summary>
        protected Blackboard blackboard;

        /// <summary>
        /// Parent node in the tree hierarchy. Null for root node.
        /// </summary>
        protected BehaviorTreeNode parent;

        /// <summary>
        /// Child nodes of this node. Empty for leaf nodes.
        /// </summary>
        protected List<BehaviorTreeNode> children;

        /// <summary>
        /// Current execution state of this node.
        /// </summary>
        protected NodeState state;

        /// <summary>
        /// Reference to the AI agent that owns this behavior tree.
        /// </summary>
        protected BaseAI agent;

        /// <summary>
        /// Creates a new behavior tree node.
        /// </summary>
        /// <param name="blackboard">Shared data storage.</param>
        /// <param name="agent">AI agent reference.</param>
        public BehaviorTreeNode(Blackboard blackboard, BaseAI agent)
        {
            this.blackboard = blackboard;
            this.agent = agent;
            this.children = new List<BehaviorTreeNode>();
            this.state = NodeState.Failure;
        }

        /// <summary>
        /// Gets the current state of this node.
        /// </summary>
        public NodeState State => state;

        /// <summary>
        /// Gets the parent node, if any.
        /// </summary>
        public BehaviorTreeNode Parent => parent;

        /// <summary>
        /// Gets the list of child nodes.
        /// </summary>
        public IReadOnlyList<BehaviorTreeNode> Children => children;

        /// <summary>
        /// Adds a child node to this node.
        /// </summary>
        /// <param name="child">Child node to add.</param>
        public virtual void AddChild(BehaviorTreeNode child)
        {
            if (child != null)
            {
                child.parent = this;
                children.Add(child);
            }
        }

        /// <summary>
        /// Removes a child node from this node.
        /// </summary>
        /// <param name="child">Child node to remove.</param>
        public virtual void RemoveChild(BehaviorTreeNode child)
        {
            if (child != null && children.Contains(child))
            {
                child.parent = null;
                children.Remove(child);
            }
        }

        /// <summary>
        /// Main execution method called each frame.
        /// This method handles the node's execution logic and returns the current state.
        /// </summary>
        /// <returns>Current execution state after this tick.</returns>
        public NodeState Tick()
        {
            if (state == NodeState.Running)
            {
                state = Execute();
            }
            else
            {
                OnEnter();
                state = Execute();
            }

            if (state != NodeState.Running)
            {
                OnExit();
            }

            return state;
        }

        /// <summary>
        /// Called when the node starts executing.
        /// Override to implement initialization logic.
        /// </summary>
        protected virtual void OnEnter() { }

        /// <summary>
        /// Core execution logic for this node.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <returns>Execution state after this frame.</returns>
        protected abstract NodeState Execute();

        /// <summary>
        /// Called when the node finishes executing (success or failure).
        /// Override to implement cleanup logic.
        /// </summary>
        protected virtual void OnExit() { }

        /// <summary>
        /// Resets the node to its initial state.
        /// Useful for restarting behavior trees or subtrees.
        /// </summary>
        public virtual void Reset()
        {
            state = NodeState.Failure;
            foreach (var child in children)
            {
                child.Reset();
            }
        }

        /// <summary>
        /// Gets a debug-friendly name for this node.
        /// Override to provide meaningful names for debugging.
        /// </summary>
        /// <returns>Node name for debugging purposes.</returns>
        public virtual string GetDebugName()
        {
            return GetType().Name;
        }

        /// <summary>
        /// Utility method to check if any enemies are visible.
        /// Commonly used by condition nodes.
        /// </summary>
        /// <returns>True if enemies are in sight.</returns>
        protected bool HasVisibleEnemies()
        {
            var enemies = blackboard.GetVisibleEnemies();
            return enemies != null && enemies.Count > 0;
        }

        /// <summary>
        /// Utility method to check if the agent has a current target.
        /// </summary>
        /// <returns>True if a target is available.</returns>
        protected bool HasCurrentTarget()
        {
            return blackboard.Get<PerceivedAgent?>("CurrentTarget").HasValue;
        }

        /// <summary>
        /// Utility method to get the agent's current position from the blackboard.
        /// </summary>
        /// <returns>Agent's world position.</returns>
        protected UnityEngine.Vector3 GetAgentPosition()
        {
            return blackboard.Get<UnityEngine.Vector3>("MyPosition");
        }
    }
}