namespace OlAform
{
    internal sealed class WorkflowNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public ScriptAction Action { get; set; } = new();

        public List<WorkflowNode> Children { get; } = new();

        public WorkflowNode Clone()
        {
            var clone = new WorkflowNode
            {
                Id = Guid.NewGuid().ToString("N"),
                Action = Action.Clone()
            };

            foreach (var child in Children)
            {
                clone.Children.Add(child.Clone());
            }

            return clone;
        }

        public override string ToString() => Action.ToString();
    }
}
