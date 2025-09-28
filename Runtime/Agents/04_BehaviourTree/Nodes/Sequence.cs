namespace AIGame.Examples.BeheaviourTree
{
    public class Sequence : Node
    {
        public Sequence(Blackboard blackboard) : base(blackboard) { }

        public override NodeState Evaluate()
        {
            foreach (Node child in children)
            {
                NodeState result = child.Evaluate();

                if (result == NodeState.Failure)
                {
                    return NodeState.Failure;
                }

                if (result == NodeState.Running)
                {
                    return NodeState.Running;
                }
            }

            return NodeState.Success;
        }
    }
}