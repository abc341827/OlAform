namespace OlAform;

internal static class ActionCategoryVisuals
{
    internal static string GetDisplayName(ActionCategory category)
    {
        return category switch
        {
            ActionCategory.Window => "窗口",
            ActionCategory.Flow => "流程",
            ActionCategory.Mouse => "鼠标",
            ActionCategory.Keyboard => "输入",
            ActionCategory.Recognition => "识别",
            ActionCategory.Utility => "工具",
            _ => category.ToString()
        };
    }

    internal static string GetIcon(ActionCategory category)
    {
        return category switch
        {
            ActionCategory.Window => "🪟",
            ActionCategory.Flow => "🔀",
            ActionCategory.Mouse => "🖱",
            ActionCategory.Keyboard => "⌨",
            ActionCategory.Recognition => "🔎",
            ActionCategory.Utility => "🧰",
            _ => "•"
        };
    }
}
