namespace AIGame.Examples.BeheaviourTree
{
    public class Selector : Node
    {
        public Selector(Blackboard blackboard) : base(blackboard) { }

        public override NodeState Evaluate()
        {
            foreach (Node child in children)
            {
                NodeState result = child.Evaluate();

                if (result == NodeState.Success)
                {
                    return NodeState.Success;
                }

                if (result == NodeState.Running)
                {
                    return NodeState.Running;
                }
            }

            return NodeState.Failure;
        }
    }
}