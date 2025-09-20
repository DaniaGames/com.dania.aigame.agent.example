using System.Collections.Generic;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Sequence node implements AND logic for behavior trees.
    /// Executes child nodes in order until one fails or all succeed.
    ///
    /// Behavior:
    /// - Returns SUCCESS if all children succeed
    /// - Returns FAILURE if any child fails
    /// - Returns RUNNING if current child is still running
    ///
    /// Use cases:
    /// - Multi-step procedures (approach target, then attack)
    /// - Condition checking followed by action (if enemy visible, then shoot)
    /// - Sequential task execution (move to flag, pick up flag, return to base)
    /// </summary>
    public class SequenceNode : BehaviorTreeNode
    {
        /// <summary>
        /// Index of the currently executing child node.
        /// -1 indicates no child is currently running.
        /// </summary>
        private int currentChildIndex;

        /// <summary>
        /// Whether the sequence has started executing children.
        /// Used to track first execution vs continuation.
        /// </summary>
        private bool hasStarted;

        /// <summary>
        /// Creates a new sequence node.
        /// </summary>
        /// <param name="blackboard">Shared blackboard instance.</param>
        /// <param name="agent">AI agent reference.</param>
        public SequenceNode(Blackboard blackboard, BaseAI agent)
            : base(blackboard, agent)
        {
            currentChildIndex = -1;
            hasStarted = false;
        }

        /// <summary>
        /// Creates a sequence node with predefined children.
        /// </summary>
        /// <param name="blackboard">Shared blackboard instance.</param>
        /// <param name="agent">AI agent reference.</param>
        /// <param name="children">Child nodes to add to this sequence.</param>
        public SequenceNode(Blackboard blackboard, BaseAI agent, params BehaviorTreeNode[] children)
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
        /// Called when the sequence starts executing.
        /// Resets internal state for fresh execution.
        /// </summary>
        protected override void OnEnter()
        {
            currentChildIndex = -1;
            hasStarted = false;

            // Log debug info if available
            LogDebug($"Sequence entered with {children.Count} children");
        }

        /// <summary>
        /// Main execution logic for the sequence.
        /// Executes each child in order until one fails or all succeed.
        /// </summary>
        /// <returns>Current execution state.</returns>
        protected override NodeState Execute()
        {
            // No children means automatic success (empty sequence succeeds)
            if (children.Count == 0)
            {
                LogDebug("Sequence succeeded: no children (empty sequence)");
                return NodeState.Success;
            }

            // Start with first child if we haven't started yet
            if (!hasStarted)
            {
                currentChildIndex = 0;
                hasStarted = true;
                LogDebug($"Sequence starting with child {currentChildIndex}: {children[currentChildIndex].GetDebugName()}");
            }

            // Execute current child and all subsequent children until completion
            while (currentChildIndex < children.Count)
            {
                var currentChild = children[currentChildIndex];
                var childState = currentChild.Tick();

                switch (childState)
                {
                    case NodeState.Success:
                        // Child succeeded, move to next child
                        LogDebug($"Child {currentChildIndex} succeeded: {currentChild.GetDebugName()}");
                        currentChildIndex++;

                        // If we've completed all children, sequence succeeds
                        if (currentChildIndex >= children.Count)
                        {
                            LogDebug("Sequence succeeded: all children completed");
                            return NodeState.Success;
                        }

                        // Continue with next child
                        LogDebug($"Sequence moving to child {currentChildIndex}: {children[currentChildIndex].GetDebugName()}");
                        break;

                    case NodeState.Running:
                        // Child still running, sequence waits
                        LogDebug($"Sequence waiting on child {currentChildIndex}: {currentChild.GetDebugName()}");
                        return NodeState.Running;

                    case NodeState.Failure:
                        // Child failed, sequence fails immediately
                        LogDebug($"Sequence failed via child {currentChildIndex}: {currentChild.GetDebugName()}");
                        return NodeState.Failure;
                }
            }

            // Should not reach here, but if we do, consider it success
            LogDebug("Sequence completed (fallback success)");
            return NodeState.Success;
        }

        /// <summary>
        /// Called when the sequence finishes executing.
        /// Resets state for next execution cycle.
        /// </summary>
        protected override void OnExit()
        {
            LogDebug($"Sequence exited (final state: {state})");
        }

        /// <summary>
        /// Resets the sequence to its initial state.
        /// Also resets all child nodes.
        /// </summary>
        public override void Reset()
        {
            currentChildIndex = -1;
            hasStarted = false;
            base.Reset();
            LogDebug("Sequence reset");
        }

        /// <summary>
        /// Gets a debug name for this node.
        /// </summary>
        /// <returns>Debug-friendly name.</returns>
        public override string GetDebugName()
        {
            return $"Sequence({children.Count})";
        }

        /// <summary>
        /// Adds a child node to the end of the sequence.
        /// Children are executed in the order they are added.
        /// </summary>
        /// <param name="child">Child node to add.</param>
        public override void AddChild(BehaviorTreeNode child)
        {
            base.AddChild(child);
            LogDebug($"Added child to sequence: {child?.GetDebugName() ?? "null"} (total: {children.Count})");
        }

        /// <summary>
        /// Removes a child node from the sequence.
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

            LogDebug($"Removed child from sequence: {child?.GetDebugName() ?? "null"} (remaining: {children.Count})");
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
        /// Gets the progress through the sequence as a percentage.
        /// </summary>
        /// <returns>Progress from 0.0 to 1.0, or 0.0 if no children.</returns>
        public float GetProgress()
        {
            if (children.Count == 0)
                return 1.0f; // Empty sequence is complete

            if (currentChildIndex < 0)
                return 0.0f; // Not started

            if (currentChildIndex >= children.Count)
                return 1.0f; // Completed

            return (float)currentChildIndex / children.Count;
        }

        /// <summary>
        /// Checks if the sequence has completed all children successfully.
        /// </summary>
        /// <returns>True if all children have been executed successfully.</returns>
        public bool IsComplete()
        {
            return currentChildIndex >= children.Count && hasStarted;
        }

        /// <summary>
        /// Logs debug information if blackboard debug mode is enabled.
        /// </summary>
        /// <param name="message">Message to log.</param>
        private void LogDebug(string message)
        {
            // Only log if the blackboard or a debug flag indicates we should
            if (blackboard.Get("DebugMode", false))
            {
                UnityEngine.Debug.Log($"[Sequence] {message}");
            }
        }

        /// <summary>
        /// Creates a builder pattern for easy sequence construction.
        /// </summary>
        /// <param name="blackboard">Blackboard instance.</param>
        /// <param name="agent">AI agent.</param>
        /// <returns>New sequence builder.</returns>
        public static SequenceBuilder Create(Blackboard blackboard, BaseAI agent)
        {
            return new SequenceBuilder(blackboard, agent);
        }
    }

    /// <summary>
    /// Builder class for easy sequence construction with fluent API.
    /// </summary>
    public class SequenceBuilder
    {
        private readonly SequenceNode sequence;

        internal SequenceBuilder(Blackboard blackboard, BaseAI agent)
        {
            sequence = new SequenceNode(blackboard, agent);
        }

        /// <summary>
        /// Adds a child node to the sequence.
        /// </summary>
        /// <param name="child">Child node to add.</param>
        /// <returns>This builder for chaining.</returns>
        public SequenceBuilder AddChild(BehaviorTreeNode child)
        {
            sequence.AddChild(child);
            return this;
        }

        /// <summary>
        /// Adds multiple child nodes to the sequence.
        /// </summary>
        /// <param name="children">Child nodes to add.</param>
        /// <returns>This builder for chaining.</returns>
        public SequenceBuilder AddChildren(params BehaviorTreeNode[] children)
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    sequence.AddChild(child);
                }
            }
            return this;
        }

        /// <summary>
        /// Builds and returns the completed sequence.
        /// </summary>
        /// <returns>Configured sequence node.</returns>
        public SequenceNode Build()
        {
            return sequence;
        }
    }
}