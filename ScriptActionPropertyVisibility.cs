namespace OlAform
{
    internal static class ScriptActionPropertyVisibility
    {
        public static HashSet<string> GetVisibleProperties(ScriptAction action)
        {
            return GetVisibleProperties(action.ActionType, action.BindWindowResolveMode);
        }

        public static HashSet<string> GetVisibleProperties(ActionType actionType, BindWindowResolveMode bindMode)
        {
            var properties = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(ScriptAction.Name),
                nameof(ScriptAction.ActionType),
                nameof(ScriptAction.Description)
            };

            switch (actionType)
            {
                case ActionType.BindWindow:
                    properties.Add(nameof(ScriptAction.OutputVariable));
                    properties.Add(nameof(ScriptAction.BindWindowResolveMode));
                    switch (bindMode)
                    {
                        case BindWindowResolveMode.DirectHandle:
                            properties.Add(nameof(ScriptAction.WindowHandle));
                            break;
                        case BindWindowResolveMode.WindowFromPoint:
                            properties.Add(nameof(ScriptAction.X));
                            properties.Add(nameof(ScriptAction.Y));
                            properties.Add(nameof(ScriptAction.UseRootWindow));
                            break;
                        case BindWindowResolveMode.ProcessName:
                            properties.Add(nameof(ScriptAction.ProcessName));
                            break;
                    }
                    break;
                case ActionType.SetVariable:
                    properties.Add(nameof(ScriptAction.OutputVariable));
                    properties.Add(nameof(ScriptAction.TextValue));
                    break;
                case ActionType.If:
                    properties.Add(nameof(ScriptAction.ConditionLeft));
                    properties.Add(nameof(ScriptAction.ConditionOperator));
                    properties.Add(nameof(ScriptAction.ConditionRight));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.LoopStart:
                    properties.Add(nameof(ScriptAction.RepeatCount));
                    properties.Add(nameof(ScriptAction.OutputVariable));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.GotoStep:
                case ActionType.CallStep:
                    properties.Add(nameof(ScriptAction.TargetStep));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.MouseMove:
                case ActionType.LeftClick:
                case ActionType.LeftDoubleClick:
                case ActionType.LeftDown:
                case ActionType.RightClick:
                case ActionType.RightDown:
                case ActionType.MiddleClick:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.X));
                    properties.Add(nameof(ScriptAction.Y));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.LeftUp:
                case ActionType.RightUp:
                case ActionType.WheelUp:
                case ActionType.WheelDown:
                case ActionType.SendPaste:
                case ActionType.WindowActivate:
                case ActionType.WindowHide:
                case ActionType.WindowShow:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.MouseDrag:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.X));
                    properties.Add(nameof(ScriptAction.Y));
                    properties.Add(nameof(ScriptAction.EndX));
                    properties.Add(nameof(ScriptAction.EndY));
                    properties.Add(nameof(ScriptAction.PollIntervalMs));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.KeyPress:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.Key));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.InputText:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.TextValue));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.SetClipboard:
                    properties.Add(nameof(ScriptAction.TextValue));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.OCR:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.X));
                    properties.Add(nameof(ScriptAction.Y));
                    properties.Add(nameof(ScriptAction.Width));
                    properties.Add(nameof(ScriptAction.Height));
                    properties.Add(nameof(ScriptAction.OutputVariable));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.FindImage:
                case ActionType.ClickImage:
                case ActionType.WaitImage:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.X));
                    properties.Add(nameof(ScriptAction.Y));
                    properties.Add(nameof(ScriptAction.Width));
                    properties.Add(nameof(ScriptAction.Height));
                    properties.Add(nameof(ScriptAction.ImagePath));
                    properties.Add(nameof(ScriptAction.MatchThreshold));
                    properties.Add(nameof(ScriptAction.OutputVariable));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    if (actionType == ActionType.WaitImage)
                    {
                        properties.Add(nameof(ScriptAction.TimeoutMs));
                        properties.Add(nameof(ScriptAction.PollIntervalMs));
                    }
                    break;
                case ActionType.FindColor:
                case ActionType.ClickColor:
                case ActionType.WaitColor:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.X));
                    properties.Add(nameof(ScriptAction.Y));
                    properties.Add(nameof(ScriptAction.Width));
                    properties.Add(nameof(ScriptAction.Height));
                    properties.Add(nameof(ScriptAction.ColorStart));
                    properties.Add(nameof(ScriptAction.ColorEnd));
                    properties.Add(nameof(ScriptAction.SearchDirection));
                    properties.Add(nameof(ScriptAction.OutputVariable));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    if (actionType == ActionType.WaitColor)
                    {
                        properties.Add(nameof(ScriptAction.TimeoutMs));
                        properties.Add(nameof(ScriptAction.PollIntervalMs));
                    }
                    break;
                case ActionType.Capture:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.X));
                    properties.Add(nameof(ScriptAction.Y));
                    properties.Add(nameof(ScriptAction.Width));
                    properties.Add(nameof(ScriptAction.Height));
                    properties.Add(nameof(ScriptAction.ImagePath));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.WindowSetSize:
                    properties.Add(nameof(ScriptAction.TargetObject));
                    properties.Add(nameof(ScriptAction.Width));
                    properties.Add(nameof(ScriptAction.Height));
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.Delay:
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
                case ActionType.Else:
                case ActionType.EndIf:
                case ActionType.EndLoop:
                case ActionType.BreakLoop:
                case ActionType.ReturnStep:
                    properties.Add(nameof(ScriptAction.DelayMs));
                    break;
            }

            return properties;
        }
    }
}
