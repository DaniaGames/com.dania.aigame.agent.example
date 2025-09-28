namespace AIGame.Examples.BeheaviourTree
{
    public class Inverter : Node
    {
        private Node child;

        public Inverter(Blackboard blackboard, Node child) : base(blackboard)
        {
            this.child = child;
        }

        public override NodeState Evaluate()
        {
            NodeState result = child.Evaluate();

            switch (result)
            {
                case NodeState.Success:
                    return NodeState.Failure;
                case NodeState.Failure:
                    return NodeState.Success;
                default:
                    return result; // Running stays Running
            }
        }
    }
}