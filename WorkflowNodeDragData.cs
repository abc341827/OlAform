namespace OlAform
{
    internal sealed class WorkflowNodeDragData
    {
        public WorkflowNodeDragData(WorkflowNode node)
        {
            Node = node;
        }

        public WorkflowNode Node { get; }
    }
}
