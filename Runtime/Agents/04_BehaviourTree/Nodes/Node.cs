using System.Collections.Generic;
using AIGame.Core;
using UnityEngine;

namespace AIGame.Examples.BeheaviourTree
{
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    public abstract class Node
    {
        public List<Node> children = new List<Node>();
        protected Blackboard blackboard;

        public Node(Blackboard blackboard)
        {
            this.blackboard = blackboard;
        }

        public abstract NodeState Evaluate();
    }


}