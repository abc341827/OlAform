namespace OlAform
{
    internal static class WorkflowActionCatalog
    {
        public static IReadOnlyList<ActionTemplate> CreateDefaultTemplates()
        {
            return new List<ActionTemplate>
            {
                new(ActionCategory.Window, ActionVisuals.GetTitle(ActionType.BindWindow), new ScriptAction { Name = "绑定窗口", ActionType = ActionType.BindWindow, Description = "按句柄、坐标或进程名解析目标窗口，并将绑定对象保存到输出变量，供后续步骤通过目标对象使用。", OutputVariable = "win1", BindWindowResolveMode = BindWindowResolveMode.DirectHandle, WindowHandle = "0x123456", ProcessName = "notepad", X = 100, Y = 100, UseRootWindow = true }),
                new(ActionCategory.Utility, ActionVisuals.GetTitle(ActionType.SetVariable), new ScriptAction { Name = "设置变量", ActionType = ActionType.SetVariable, Description = "将文本值写入变量，支持 ${变量名} 模板。", OutputVariable = "var1", TextValue = "value" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.If), new ScriptAction { Name = "条件判断", ActionType = ActionType.If, Description = "条件成立时继续执行，否则跳到否则分支或结束条件。", ConditionLeft = "${var1}", ConditionOperator = ConditionOperatorType.Equals, ConditionRight = "value" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.Else), new ScriptAction { Name = "否则分支", ActionType = ActionType.Else, Description = "条件不成立时执行的分支开始。" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.EndIf), new ScriptAction { Name = "结束条件", ActionType = ActionType.EndIf, Description = "结束条件分支结构。" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.LoopStart), new ScriptAction { Name = "循环开始", ActionType = ActionType.LoopStart, Description = "开始固定次数循环，可把当前轮次写入变量。", RepeatCount = 3, OutputVariable = "loopIndex" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.EndLoop), new ScriptAction { Name = "循环结束", ActionType = ActionType.EndLoop, Description = "结束循环并返回到循环开始。" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.BreakLoop), new ScriptAction { Name = "跳出循环", ActionType = ActionType.BreakLoop, Description = "跳出最近一层循环。" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.GotoStep), new ScriptAction { Name = "跳转步骤", ActionType = ActionType.GotoStep, Description = "跳转到目标步骤编号。", TargetStep = "step_target" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.CallStep), new ScriptAction { Name = "调用步骤", ActionType = ActionType.CallStep, Description = "调用目标步骤编号，遇到返回步骤后返回。", TargetStep = "subroutine" }),
                new(ActionCategory.Flow, ActionVisuals.GetTitle(ActionType.ReturnStep), new ScriptAction { Name = "返回步骤", ActionType = ActionType.ReturnStep, Description = "从调用步骤中返回。" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.MouseMove), new ScriptAction { Name = "鼠标移动", ActionType = ActionType.MouseMove, Description = "移动鼠标到目标坐标。", X = 100, Y = 100, TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.LeftClick), new ScriptAction { Name = "左键单击", ActionType = ActionType.LeftClick, Description = "移动到坐标后执行左键点击。", X = 100, Y = 100, TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.LeftDoubleClick), new ScriptAction { Name = "左键双击", ActionType = ActionType.LeftDoubleClick, Description = "移动到坐标后执行左键双击。", X = 100, Y = 100, TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.LeftDown), new ScriptAction { Name = "左键按下", ActionType = ActionType.LeftDown, Description = "移动到坐标后按下左键，不自动释放。", X = 100, Y = 100, TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.LeftUp), new ScriptAction { Name = "左键松开", ActionType = ActionType.LeftUp, Description = "释放当前左键按下状态。", TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.MouseDrag), new ScriptAction { Name = "鼠标拖拽", ActionType = ActionType.MouseDrag, Description = "按下左键后拖动到终点并释放。", X = 100, Y = 100, EndX = 200, EndY = 200, PollIntervalMs = 100, TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.RightClick), new ScriptAction { Name = "右键单击", ActionType = ActionType.RightClick, Description = "移动到坐标后执行右键点击。", X = 100, Y = 100, TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.RightDown), new ScriptAction { Name = "右键按下", ActionType = ActionType.RightDown, Description = "移动到坐标后按下右键，不自动释放。", X = 100, Y = 100, TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.RightUp), new ScriptAction { Name = "右键松开", ActionType = ActionType.RightUp, Description = "释放当前右键按下状态。", TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.MiddleClick), new ScriptAction { Name = "中键单击", ActionType = ActionType.MiddleClick, Description = "移动到坐标后执行中键点击。", X = 100, Y = 100, TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.WheelUp), new ScriptAction { Name = "滚轮上滚", ActionType = ActionType.WheelUp, Description = "在当前鼠标位置执行滚轮向上。", TargetObject = "win1" }),
                new(ActionCategory.Mouse, ActionVisuals.GetTitle(ActionType.WheelDown), new ScriptAction { Name = "滚轮下滚", ActionType = ActionType.WheelDown, Description = "在当前鼠标位置执行滚轮向下。", TargetObject = "win1" }),
                new(ActionCategory.Keyboard, ActionVisuals.GetTitle(ActionType.KeyPress), new ScriptAction { Name = "按键输入", ActionType = ActionType.KeyPress, Description = "发送单个按键或按键串。", Key = "A", TargetObject = "win1" }),
                new(ActionCategory.Keyboard, ActionVisuals.GetTitle(ActionType.InputText), new ScriptAction { Name = "文本输入", ActionType = ActionType.InputText, Description = "向目标窗口输入文本。", TextValue = "hello", TargetObject = "win1" }),
                new(ActionCategory.Keyboard, ActionVisuals.GetTitle(ActionType.SetClipboard), new ScriptAction { Name = "设置剪贴板", ActionType = ActionType.SetClipboard, Description = "设置系统剪贴板内容。", TextValue = "hello" }),
                new(ActionCategory.Keyboard, ActionVisuals.GetTitle(ActionType.SendPaste), new ScriptAction { Name = "发送粘贴", ActionType = ActionType.SendPaste, Description = "向当前绑定窗口发送粘贴命令。", TargetObject = "win1" }),
                new(ActionCategory.Recognition, ActionVisuals.GetTitle(ActionType.OCR), new ScriptAction { Name = "文字识别", ActionType = ActionType.OCR, Description = "识别指定区域的文字。", X = 100, Y = 100, Width = 100, Height = 100, TargetObject = "win1" }),
                new(ActionCategory.Recognition, ActionVisuals.GetTitle(ActionType.FindImage), new ScriptAction { Name = "查找图片", ActionType = ActionType.FindImage, Description = "在指定区域查找图片。", X = 0, Y = 0, Width = 300, Height = 300, ImagePath = "sample.png", MatchThreshold = 0.8, TargetObject = "win1" }),
                new(ActionCategory.Recognition, ActionVisuals.GetTitle(ActionType.ClickImage), new ScriptAction { Name = "点击图片", ActionType = ActionType.ClickImage, Description = "找到图片后点击其中心点。", X = 0, Y = 0, Width = 300, Height = 300, ImagePath = "sample.png", MatchThreshold = 0.8, TargetObject = "win1" }),
                new(ActionCategory.Recognition, ActionVisuals.GetTitle(ActionType.WaitImage), new ScriptAction { Name = "等待图片", ActionType = ActionType.WaitImage, Description = "轮询指定区域直到图片出现。", X = 0, Y = 0, Width = 300, Height = 300, ImagePath = "sample.png", MatchThreshold = 0.8, TimeoutMs = 3000, PollIntervalMs = 200, TargetObject = "win1" }),
                new(ActionCategory.Recognition, ActionVisuals.GetTitle(ActionType.FindColor), new ScriptAction { Name = "查找颜色", ActionType = ActionType.FindColor, Description = "在区域内查找颜色范围。", X = 0, Y = 0, Width = 300, Height = 300, ColorStart = "000000", ColorEnd = "FFFFFF", SearchDirection = 0, TargetObject = "win1" }),
                new(ActionCategory.Recognition, ActionVisuals.GetTitle(ActionType.ClickColor), new ScriptAction { Name = "点击颜色", ActionType = ActionType.ClickColor, Description = "找到颜色后点击命中的坐标。", X = 0, Y = 0, Width = 300, Height = 300, ColorStart = "000000", ColorEnd = "FFFFFF", SearchDirection = 0, TargetObject = "win1" }),
                new(ActionCategory.Recognition, ActionVisuals.GetTitle(ActionType.WaitColor), new ScriptAction { Name = "等待颜色", ActionType = ActionType.WaitColor, Description = "轮询指定区域直到颜色出现。", X = 0, Y = 0, Width = 300, Height = 300, ColorStart = "000000", ColorEnd = "FFFFFF", SearchDirection = 0, TimeoutMs = 3000, PollIntervalMs = 200, TargetObject = "win1" }),
                new(ActionCategory.Utility, ActionVisuals.GetTitle(ActionType.Capture), new ScriptAction { Name = "截图保存", ActionType = ActionType.Capture, Description = "将指定区域截图保存到文件。", X = 0, Y = 0, Width = 300, Height = 300, ImagePath = "capture.png", TargetObject = "win1" }),
                new(ActionCategory.Window, ActionVisuals.GetTitle(ActionType.WindowActivate), new ScriptAction { Name = "激活窗口", ActionType = ActionType.WindowActivate, Description = "激活当前绑定窗口。", TargetObject = "win1" }),
                new(ActionCategory.Window, ActionVisuals.GetTitle(ActionType.WindowHide), new ScriptAction { Name = "隐藏窗口", ActionType = ActionType.WindowHide, Description = "隐藏当前绑定窗口。", TargetObject = "win1" }),
                new(ActionCategory.Window, ActionVisuals.GetTitle(ActionType.WindowShow), new ScriptAction { Name = "显示窗口", ActionType = ActionType.WindowShow, Description = "显示当前绑定窗口。", TargetObject = "win1" }),
                new(ActionCategory.Window, ActionVisuals.GetTitle(ActionType.WindowSetSize), new ScriptAction { Name = "设置窗口大小", ActionType = ActionType.WindowSetSize, Description = "设置当前绑定窗口的大小。", Width = 1280, Height = 720, TargetObject = "win1" }),
                new(ActionCategory.Utility, ActionVisuals.GetTitle(ActionType.Delay), new ScriptAction { Name = "延时等待", ActionType = ActionType.Delay, Description = "等待指定毫秒数。", DelayMs = 500 })
            };
        }
    }
}
