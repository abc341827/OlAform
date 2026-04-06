namespace OlAform
{
    internal static class ActionVisuals
    {
        public static string GetDisplayName(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.BindWindow => "绑定窗口",
                ActionType.SetVariable => "设置变量",
                ActionType.If => "条件判断",
                ActionType.Else => "否则分支",
                ActionType.EndIf => "结束条件",
                ActionType.LoopStart => "循环开始",
                ActionType.EndLoop => "循环结束",
                ActionType.BreakLoop => "跳出循环",
                ActionType.GotoStep => "跳转步骤",
                ActionType.CallStep => "调用步骤",
                ActionType.ReturnStep => "返回步骤",
                ActionType.MouseMove => "鼠标移动",
                ActionType.LeftClick => "左键单击",
                ActionType.LeftDoubleClick => "左键双击",
                ActionType.LeftDown => "左键按下",
                ActionType.LeftUp => "左键松开",
                ActionType.MouseDrag => "鼠标拖拽",
                ActionType.RightClick => "右键单击",
                ActionType.RightDown => "右键按下",
                ActionType.RightUp => "右键松开",
                ActionType.MiddleClick => "中键单击",
                ActionType.WheelUp => "滚轮上滚",
                ActionType.WheelDown => "滚轮下滚",
                ActionType.KeyPress => "按键输入",
                ActionType.InputText => "文本输入",
                ActionType.SetClipboard => "设置剪贴板",
                ActionType.SendPaste => "发送粘贴",
                ActionType.OCR => "文字识别",
                ActionType.FindImage => "查找图片",
                ActionType.ClickImage => "点击图片",
                ActionType.WaitImage => "等待图片",
                ActionType.FindColor => "查找颜色",
                ActionType.ClickColor => "点击颜色",
                ActionType.WaitColor => "等待颜色",
                ActionType.Capture => "截图保存",
                ActionType.WindowActivate => "激活窗口",
                ActionType.WindowHide => "隐藏窗口",
                ActionType.WindowShow => "显示窗口",
                ActionType.WindowSetSize => "设置窗口大小",
                ActionType.Delay => "延时等待",
                _ => actionType.ToString()
            };
        }

        public static string GetIcon(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.BindWindow => "🔗",
                ActionType.SetVariable => "🧩",
                ActionType.If or ActionType.Else or ActionType.EndIf => "🔀",
                ActionType.LoopStart or ActionType.EndLoop or ActionType.BreakLoop => "🔁",
                ActionType.GotoStep or ActionType.CallStep or ActionType.ReturnStep => "📍",
                ActionType.MouseMove or ActionType.LeftClick or ActionType.LeftDoubleClick or ActionType.LeftDown or ActionType.LeftUp or ActionType.MouseDrag or ActionType.RightClick or ActionType.RightDown or ActionType.RightUp or ActionType.MiddleClick or ActionType.WheelUp or ActionType.WheelDown => "🖱",
                ActionType.KeyPress or ActionType.InputText => "⌨",
                ActionType.SetClipboard or ActionType.SendPaste => "📋",
                ActionType.OCR => "🔎",
                ActionType.FindImage or ActionType.ClickImage or ActionType.WaitImage => "🖼",
                ActionType.FindColor or ActionType.ClickColor or ActionType.WaitColor => "🎨",
                ActionType.Capture => "📸",
                ActionType.WindowActivate or ActionType.WindowHide or ActionType.WindowShow or ActionType.WindowSetSize => "🪟",
                ActionType.Delay => "⏱",
                _ => "•"
            };
        }

        public static string GetTitle(ActionType actionType)
        {
            return $"{GetIcon(actionType)} {GetDisplayName(actionType)}";
        }
    }
}
