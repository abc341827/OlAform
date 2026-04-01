using System.ComponentModel;

namespace OlAform
{
    public enum ActionType
    {
        SetVariable,
        If,
        Else,
        EndIf,
        LoopStart,
        EndLoop,
        BreakLoop,
        GotoStep,
        CallStep,
        ReturnStep,
        MouseMove,
        LeftClick,
        LeftDoubleClick,
        LeftDown,
        LeftUp,
        MouseDrag,
        RightClick,
        RightDown,
        RightUp,
        MiddleClick,
        WheelUp,
        WheelDown,
        KeyPress,
        InputText,
        SetClipboard,
        SendPaste,
        OCR,
        FindImage,
        ClickImage,
        WaitImage,
        FindColor,
        ClickColor,
        WaitColor,
        Capture,
        WindowActivate,
        WindowHide,
        WindowShow,
        WindowSetSize,
        Delay
    }

    public enum ConditionOperatorType
    {
        Equals,
        NotEquals,
        Contains,
        NotContains,
        StartsWith,
        EndsWith,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        IsEmpty,
        IsNotEmpty
    }

    public class ScriptAction
    {
        [DisplayName("Action Name")]
        public string Name { get; set; } = "Action";

        [DisplayName("Step Id")]
        public string? StepId { get; set; }

        [DisplayName("Type")]
        public ActionType ActionType { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; } = string.Empty;

        [DisplayName("X")]
        public int X { get; set; }

        [DisplayName("Y")]
        public int Y { get; set; }

        [DisplayName("Width")]
        public int Width { get; set; } = 100;

        [DisplayName("Height")]
        public int Height { get; set; } = 30;

        [DisplayName("End X")]
        public int EndX { get; set; }

        [DisplayName("End Y")]
        public int EndY { get; set; }

        [DisplayName("Key")]
        public string? Key { get; set; }

        [DisplayName("Text")]
        public string? TextValue { get; set; }

        [DisplayName("Image Path")]
        public string? ImagePath { get; set; }

        [DisplayName("Match Threshold")]
        public double MatchThreshold { get; set; } = 0.8;

        [DisplayName("Output Variable")]
        public string? OutputVariable { get; set; }

        [DisplayName("Target Step")]
        public string? TargetStep { get; set; }

        [DisplayName("Condition Left")]
        public string? ConditionLeft { get; set; }

        [DisplayName("Condition Operator")]
        public ConditionOperatorType ConditionOperator { get; set; } = ConditionOperatorType.Equals;

        [DisplayName("Condition Right")]
        public string? ConditionRight { get; set; }

        [DisplayName("Repeat Count")]
        public int RepeatCount { get; set; } = 1;

        [DisplayName("Color Start")]
        public string? ColorStart { get; set; }

        [DisplayName("Color End")]
        public string? ColorEnd { get; set; }

        [DisplayName("Search Direction")]
        public int SearchDirection { get; set; }

        [DisplayName("Timeout (ms)")]
        public int TimeoutMs { get; set; } = 3000;

        [DisplayName("Poll Interval (ms)")]
        public int PollIntervalMs { get; set; } = 200;

        [DisplayName("Delay (ms)")]
        public int DelayMs { get; set; } = 200;

        [DisplayName("Additional")]
        public string? Additional { get; set; }

        public ScriptAction Clone()
        {
            return (ScriptAction)MemberwiseClone();
        }

        public override string ToString()
        {
            return ActionType switch
            {
                ActionType.SetVariable => $"{Name} {OutputVariable}={TextValue}",
                ActionType.If => $"{Name} IF {ConditionLeft} {ConditionOperator} {ConditionRight}",
                ActionType.Else => Name,
                ActionType.EndIf => Name,
                ActionType.LoopStart => $"{Name} x{RepeatCount}",
                ActionType.EndLoop => Name,
                ActionType.BreakLoop => Name,
                ActionType.GotoStep => $"{Name} -> {TargetStep}",
                ActionType.CallStep => $"{Name} => {TargetStep}",
                ActionType.ReturnStep => Name,
                ActionType.MouseMove => $"{Name} ({X},{Y})",
                ActionType.LeftClick => $"{Name} ({X},{Y})",
                ActionType.LeftDoubleClick => $"{Name} ({X},{Y})",
                ActionType.LeftDown => $"{Name} ({X},{Y})",
                ActionType.LeftUp => $"{Name}",
                ActionType.MouseDrag => $"{Name} ({X},{Y}) -> ({EndX},{EndY})",
                ActionType.RightClick => $"{Name} ({X},{Y})",
                ActionType.RightDown => $"{Name} ({X},{Y})",
                ActionType.RightUp => $"{Name}",
                ActionType.MiddleClick => $"{Name} ({X},{Y})",
                ActionType.WheelUp => $"{Name}",
                ActionType.WheelDown => $"{Name}",
                ActionType.KeyPress => $"{Name} [{Key}]",
                ActionType.InputText => $"{Name} [{TextValue}]",
                ActionType.SetClipboard => $"{Name} [{TextValue}]",
                ActionType.SendPaste => $"{Name}",
                ActionType.OCR => $"{Name} ({X},{Y},{Width},{Height})",
                ActionType.FindImage => $"{Name} [{ImagePath}]",
                ActionType.ClickImage => $"{Name} [{ImagePath}]",
                ActionType.WaitImage => $"{Name} [{ImagePath}]",
                ActionType.FindColor => $"{Name} [{ColorStart}-{ColorEnd}]",
                ActionType.ClickColor => $"{Name} [{ColorStart}-{ColorEnd}]",
                ActionType.WaitColor => $"{Name} [{ColorStart}-{ColorEnd}]",
                ActionType.Capture => $"{Name} [{ImagePath}]",
                ActionType.WindowActivate => Name,
                ActionType.WindowHide => Name,
                ActionType.WindowShow => Name,
                ActionType.WindowSetSize => $"{Name} [{Width}x{Height}]",
                ActionType.Delay => $"{Name} {DelayMs}ms",
                _ => Name
            };
        }
    }
}
