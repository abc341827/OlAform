namespace OlAform
{
    internal sealed class ActionDragData
    {
        public ActionDragData(ActionTemplate? template, int sourceIndex)
        {
            Template = template;
            SourceIndex = sourceIndex;
        }

        public ActionTemplate? Template { get; }

        public int SourceIndex { get; }

        public bool IsFromWorkflow => SourceIndex >= 0;
    }
}
