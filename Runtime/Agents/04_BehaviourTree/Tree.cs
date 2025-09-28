namespace AIGame.Examples.BeheaviourTree
{
    public class Tree
    {
        private Node root;
        private Blackboard blackboard;

        public Tree(Node root, Blackboard blackboard)
        {
            this.root = root;
            this.blackboard = blackboard;
        }

        public NodeState Tick()
        {
            return root.Evaluate();
        }
    }
}