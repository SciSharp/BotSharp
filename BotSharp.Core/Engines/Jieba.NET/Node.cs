namespace JiebaNet.Segmenter
{
    public class Node
    {
        public char Value { get; private set; }
        public Node Parent { get; private set; }

        public Node(char value, Node parent)
        {
            Value = value;
            Parent = parent;
        }
    }
}