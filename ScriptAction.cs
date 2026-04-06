using System.ComponentModel;

namespace OlAform
{
    public enum ActionType
    {
        BindWindow,
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

    [TypeDescriptionProvider(typeof(ScriptActionTypeDescriptionProvider))]
    public class ScriptAction
    {
        [DisplayName("步骤名称")]
        public string Name { get; set; } = "Action";

        [DisplayName("步骤编号")]
        public string? StepId { get; set; }

        [DisplayName("步骤类型")]
        public ActionType ActionType { get; set; }

        [DisplayName("说明")]
        public string Description { get; set; } = string.Empty;

        [DisplayName("X 坐标")]
        public int X { get; set; }

        [DisplayName("Y 坐标")]
        public int Y { get; set; }

        [DisplayName("宽度")]
        public int Width { get; set; } = 100;

        [DisplayName("高度")]
        public int Height { get; set; } = 30;

        [DisplayName("结束 X")]
        public int EndX { get; set; }

        [DisplayName("结束 Y")]
        public int EndY { get; set; }

        [DisplayName("按键")]
        public string? Key { get; set; }

        [DisplayName("文本")]
        public string? TextValue { get; set; }

        [DisplayName("图片路径")]
        public string? ImagePath { get; set; }

        [DisplayName("匹配阈值")]
        public double MatchThreshold { get; set; } = 0.8;

        [DisplayName("输出变量")]
        public string? OutputVariable { get; set; }

        [DisplayName("目标对象")]
        public string? TargetObject { get; set; }

        [DisplayName("目标步骤")]
        public string? TargetStep { get; set; }

        [DisplayName("窗口句柄")]
        public string? WindowHandle { get; set; }

        [DisplayName("绑定方式")]
        public BindWindowResolveMode BindWindowResolveMode { get; set; } = BindWindowResolveMode.DirectHandle;

        [DisplayName("进程名")]
        public string? ProcessName { get; set; }

        [DisplayName("使用根窗口")]
        public bool UseRootWindow { get; set; } = true;

        [DisplayName("条件左值")]
        public string? ConditionLeft { get; set; }

        [DisplayName("条件运算符")]
        public ConditionOperatorType ConditionOperator { get; set; } = ConditionOperatorType.Equals;

        [DisplayName("条件右值")]
        public string? ConditionRight { get; set; }

        [DisplayName("循环次数")]
        public int RepeatCount { get; set; } = 1;

        [DisplayName("起始颜色")]
        public string? ColorStart { get; set; }

        [DisplayName("结束颜色")]
        public string? ColorEnd { get; set; }

        [DisplayName("搜索方向")]
        public int SearchDirection { get; set; }

        [DisplayName("超时毫秒")]
        public int TimeoutMs { get; set; } = 3000;

        [DisplayName("轮询间隔毫秒")]
        public int PollIntervalMs { get; set; } = 200;

        [DisplayName("执行后延时毫秒")]
        public int DelayMs { get; set; } = 200;

        [DisplayName("附加信息")]
        public string? Additional { get; set; }

        public ScriptAction Clone()
        {
            return (ScriptAction)MemberwiseClone();
        }

        public override string ToString()
        {
            return ActionType switch
            {
                ActionType.BindWindow => $"{ActionVisuals.GetTitle(ActionType)} {OutputVariable} <- {WindowHandle}",
                ActionType.SetVariable => $"{ActionVisuals.GetTitle(ActionType)} {OutputVariable}={TextValue}",
                ActionType.If => $"{ActionVisuals.GetTitle(ActionType)} IF {ConditionLeft} {ConditionOperator} {ConditionRight}",
                ActionType.Else => ActionVisuals.GetTitle(ActionType),
                ActionType.EndIf => ActionVisuals.GetTitle(ActionType),
                ActionType.LoopStart => $"{ActionVisuals.GetTitle(ActionType)} x{RepeatCount}",
                ActionType.EndLoop => ActionVisuals.GetTitle(ActionType),
                ActionType.BreakLoop => ActionVisuals.GetTitle(ActionType),
                ActionType.GotoStep => $"{ActionVisuals.GetTitle(ActionType)} -> {TargetStep}",
                ActionType.CallStep => $"{ActionVisuals.GetTitle(ActionType)} => {TargetStep}",
                ActionType.ReturnStep => ActionVisuals.GetTitle(ActionType),
                ActionType.MouseMove => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y})"),
                ActionType.LeftClick => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y})"),
                ActionType.LeftDoubleClick => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y})"),
                ActionType.LeftDown => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y})"),
                ActionType.LeftUp => FormatWithTargetObject(ActionVisuals.GetTitle(ActionType)),
                ActionType.MouseDrag => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y}) -> ({EndX},{EndY})"),
                ActionType.RightClick => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y})"),
                ActionType.RightDown => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y})"),
                ActionType.RightUp => FormatWithTargetObject(ActionVisuals.GetTitle(ActionType)),
                ActionType.MiddleClick => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y})"),
                ActionType.WheelUp => FormatWithTargetObject(ActionVisuals.GetTitle(ActionType)),
                ActionType.WheelDown => FormatWithTargetObject(ActionVisuals.GetTitle(ActionType)),
                ActionType.KeyPress => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{Key}]"),
                ActionType.InputText => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{TextValue}]"),
                ActionType.SetClipboard => $"{ActionVisuals.GetTitle(ActionType)} [{TextValue}]",
                ActionType.SendPaste => FormatWithTargetObject(ActionVisuals.GetTitle(ActionType)),
                ActionType.OCR => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} ({X},{Y},{Width},{Height})"),
                ActionType.FindImage => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{ImagePath}]"),
                ActionType.ClickImage => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{ImagePath}]"),
                ActionType.WaitImage => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{ImagePath}]"),
                ActionType.FindColor => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{ColorStart}-{ColorEnd}]"),
                ActionType.ClickColor => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{ColorStart}-{ColorEnd}]"),
                ActionType.WaitColor => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{ColorStart}-{ColorEnd}]"),
                ActionType.Capture => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{ImagePath}]"),
                ActionType.WindowActivate => FormatWithTargetObject(ActionVisuals.GetTitle(ActionType)),
                ActionType.WindowHide => FormatWithTargetObject(ActionVisuals.GetTitle(ActionType)),
                ActionType.WindowShow => FormatWithTargetObject(ActionVisuals.GetTitle(ActionType)),
                ActionType.WindowSetSize => FormatWithTargetObject($"{ActionVisuals.GetTitle(ActionType)} [{Width}x{Height}]"),
                ActionType.Delay => $"{ActionVisuals.GetTitle(ActionType)} {DelayMs}ms",
                _ => Name
            };
        }

        private string FormatWithTargetObject(string text)
        {
            return string.IsNullOrWhiteSpace(TargetObject) ? text : $"{text} @{TargetObject}";
        }
    }
}
