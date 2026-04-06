namespace OlAform
{
    internal sealed class ProjectDefinition
    {
        public string Name { get; set; } = string.Empty;

        public long TargetWindowHandle { get; set; }

        public List<WorkflowNodeDto> WorkflowRoots { get; set; } = new();
    }

    internal sealed class WorkflowNodeDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public ScriptAction Action { get; set; } = new();

        public List<WorkflowNodeDto> Children { get; set; } = new();
    }
}
