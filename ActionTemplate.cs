namespace OlAform
{
    internal sealed class ActionTemplate
    {
        public ActionTemplate(string displayName, ScriptAction template)
        {
            DisplayName = displayName;
            Template = template;
        }

        public string DisplayName { get; }

        public ScriptAction Template { get; }

        public string Description => Template.Description;

        public ScriptAction CreateAction() => Template.Clone();

        public override string ToString() => DisplayName;
    }
}
