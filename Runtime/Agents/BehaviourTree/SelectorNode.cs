using System.Collections.Generic;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Selector node implements OR logic for behavior trees.
    /// Executes child nodes in order until one succeeds or all fail.
    ///
    /// Behavior:
    /// - Returns SUCCESS if any child succeeds
    /// - Returns FAILURE if all children fail
    /// - Returns RUNNING if current child is still running
    ///
    /// Use cases:
    /// - Choosing between different behaviors (combat vs movement)
    /// - Fallback chains (try preferred action, then backup plans)
    /// - Decision making with priority order
    /// </summary>
    public class SelectorNode : BehaviorTreeNode
    {
        /// <summary>
        /// Index of the currently executing child node.
        /// -1 indicates no child is currently running.
        /// </summary>
        private int currentChildIndex;

        /// <summary>
        /// Whether the selector has started executing children.
        /// Used to track first execution vs continuation.
        /// </summary>
        private bool hasStarted;

        /// <summary>
        /// Creates a new selector node.
        /// </summary>
        /// <param name="blackboard">Shared blackboard instance.</param>
        /// <param name="agent">AI agent reference.</param>
        public SelectorNode(Blackboard blackboard, BaseAI agent)
            : base(blackboard, agent)
        {
            currentChildIndex = -1;
            hasStarted = false;
        }

        /// <summary>
        /// Creates a selector node with predefined children.
        /// </summary>
        /// <param name="blackboard">Shared blackboard instance.</param>
        /// <param name="agent">AI agent reference.</param>
        /// <param name="children">Child nodes to add to this selector.</param>
        public SelectorNode(Blackboard blackboard, BaseAI agent, params BehaviorTreeNode[] children)
            : this(blackboard, agent)
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    AddChild(child);
                }
            }
        }

        /// <summary>
        /// Called when the selector starts executing.
        /// Resets internal state for fresh execution.
        /// </summary>
        protected override void OnEnter()
        {
            currentChildIndex = -1;
            hasStarted = false;

            // Log debug info if available
            LogDebug($"Selector entered with {children.Count} children");
        }

        /// <summary>
        /// Main execution logic for the selector.
        /// Tries each child in order until one succeeds or all fail.
        /// </summary>
        /// <returns>Current execution state.</returns>
        protected override NodeState Execute()
        {
            // No children means automatic failure
            if (children.Count == 0)
            {
                LogDebug("Selector failed: no children");
                return NodeState.Failure;
            }

            // Start with first child if we haven't started yet
            if (!hasStarted)
            {
                currentChildIndex = 0;
                hasStarted = true;
                LogDebug($"Selector starting with child {currentChildIndex}: {children[currentChildIndex].GetDebugName()}");
            }

            // Execute current child
            while (currentChildIndex < children.Count)
            {
                var currentChild = children[currentChildIndex];
                var childState = currentChild.Tick();

                switch (childState)
                {
                    case NodeState.Success:
                        // Child succeeded, selector succeeds
                        LogDebug($"Selector succeeded via child {currentChildIndex}: {currentChild.GetDebugName()}");
                        return NodeState.Success;

                    case NodeState.Running:
                        // Child still running, selector waits
                        LogDebug($"Selector waiting on child {currentChildIndex}: {currentChild.GetDebugName()}");
                        return NodeState.Running;

                    case NodeState.Failure:
                        // Child failed, try next child
                        LogDebug($"Child {currentChildIndex} failed: {currentChild.GetDebugName()}, trying next");
                        currentChildIndex++;

                        // If we have more children, continue the loop
                        if (currentChildIndex < children.Count)
                        {
                            LogDebug($"Selector moving to child {currentChildIndex}: {children[currentChildIndex].GetDebugName()}");
                        }
                        break;
                }
            }

            // All children failed
            LogDebug("Selector failed: all children failed");
            return NodeState.Failure;
        }

        /// <summary>
        /// Called when the selector finishes executing.
        /// Resets state for next execution cycle.
        /// </summary>
        protected override void OnExit()
        {
            LogDebug($"Selector exited (final state: {state})");
        }

        /// <summary>
        /// Resets the selector to its initial state.
        /// Also resets all child nodes.
        /// </summary>
        public override void Reset()
        {
            currentChildIndex = -1;
            hasStarted = false;
            base.Reset();
            LogDebug("Selector reset");
        }

        /// <summary>
        /// Gets a debug name for this node.
        /// </summary>
        /// <returns>Debug-friendly name.</returns>
        public override string GetDebugName()
        {
            return $"Selector({children.Count})";
        }

        /// <summary>
        /// Adds a child node to the end of the selection list.
        /// Children are tried in the order they are added.
        /// </summary>
        /// <param name="child">Child node to add.</param>
        public override void AddChild(BehaviorTreeNode child)
        {
            base.AddChild(child);
            LogDebug($"Added child to selector: {child?.GetDebugName() ?? "null"} (total: {children.Count})");
        }

        /// <summary>
        /// Removes a child node from the selector.
        /// May affect execution order of remaining children.
        /// </summary>
        /// <param name="child">Child node to remove.</param>
        public override void RemoveChild(BehaviorTreeNode child)
        {
            int childIndex = children.IndexOf(child);
            base.RemoveChild(child);

            // Adjust current index if necessary
            if (childIndex >= 0 && childIndex <= currentChildIndex)
            {
                currentChildIndex--;
            }

            LogDebug($"Removed child from selector: {child?.GetDebugName() ?? "null"} (remaining: {children.Count})");
        }

        /// <summary>
        /// Gets the currently executing child node, if any.
        /// </summary>
        /// <returns>Current child node, or null if none executing.</returns>
        public BehaviorTreeNode GetCurrentChild()
        {
            if (currentChildIndex >= 0 && currentChildIndex < children.Count)
            {
                return children[currentChildIndex];
            }
            return null;
        }

        /// <summary>
        /// Gets the index of the currently executing child.
        /// </summary>
        /// <returns>Current child index, or -1 if none executing.</returns>
        public int GetCurrentChildIndex()
        {
            return currentChildIndex;
        }

        /// <summary>
        /// Logs debug information if blackboard debug mode is enabled.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogDebug(string message)
        {
            // Only log if the blackboard or a debug flag indicates we should
            // This could be enhanced to check a debug flag in the blackboard
            if (blackboard.Get("DebugMode", false))
            {
                UnityEngine.Debug.Log($"[Selector] {message}");
            }
        }

        /// <summary>
        /// Creates a builder pattern for easy selector construction.
        /// </summary>
        /// <param name="blackboard">Blackboard instance.</param>
        /// <param name="agent">AI agent.</param>
        /// <returns>New selector builder.</returns>
        public static SelectorBuilder Create(Blackboard blackboard, BaseAI agent)
        {
            return new SelectorBuilder(blackboard, agent);
        }
    }

    /// <summary>
    /// Builder class for easy selector construction with fluent API.
    /// </summary>
    public class SelectorBuilder
    {
        private readonly SelectorNode selector;

        internal SelectorBuilder(Blackboard blackboard, BaseAI agent)
        {
            selector = new SelectorNode(blackboard, agent);
        }

        /// <summary>
        /// Adds a child node to the selector.
        /// </summary>
        /// <param name="child">Child node to add.</param>
        /// <returns>This builder for chaining.</returns>
        public SelectorBuilder AddChild(BehaviorTreeNode child)
        {
            selector.AddChild(child);
            return this;
        }

        /// <summary>
        /// Builds and returns the completed selector.
        /// </summary>
        /// <returns>Configured selector node.</returns>
        public SelectorNode Build()
        {
            return selector;
        }
    }
}