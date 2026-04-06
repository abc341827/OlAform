namespace OlAform
{
    internal sealed class ActionTemplate
    {
        public ActionTemplate(ActionCategory category, string displayName, ScriptAction template)
        {
            Category = category;
            DisplayName = displayName;
            Template = template;
        }

        public ActionCategory Category { get; }

        public string DisplayName { get; }

        public ScriptAction Template { get; }

        public string Description => Template.Description;

        public ScriptAction CreateAction()
        {
            var action = Template.Clone();
            action.Name = ActionVisuals.GetDisplayName(action.ActionType);
            return action;
        }

        public override string ToString() => DisplayName;
    }
}
