using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OlAform
{
    public partial class Form1 : Form
    {
        private readonly List<WorkflowNode> _workflowRoots = new();
        private readonly List<ActionTemplate> _availableActionTemplates = new();
        private Control? _draggingControl;
        private Point _dragStart;
        private readonly OlaWorker _olaWorker = new();
        private bool _isBound;
        private long _targetWindowHandle;
        private readonly System.Windows.Forms.Timer _windowPickTimer = new();
        private readonly WindowHighlightOverlay _highlightOverlay = new();
        private readonly ListBox _lstAvailableActions = new();
        private readonly TreeView _treeActions = new();
        private readonly Label _lblAvailableActions = new();
        private readonly Label _lblWorkflowActions = new();
        private bool _isPickingWindow;
        private long _hoverWindowHandle;
        private long _highlightedWindowHandle;
        private Rectangle _highlightedBounds = Rectangle.Empty;
        private bool _isRunningWorkflow;

        public Form1()
        {
            InitializeComponent();
            ConfigureActionUi();
            InitializeActionCatalog();
            propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;

            _windowPickTimer.Interval = 50;
            _windowPickTimer.Tick += WindowPickTimer_Tick;

            Load += Form1_Load;
            FormClosed += Form1_FormClosed;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            UpdateBindingStatus();
            RefreshWorkflowTree();
        }

        private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
        {
            try
            {
                StopWindowPick(false);
                _highlightOverlay.Close();
                _olaWorker.Dispose();
            }
            catch
            {
            }
        }

        private async Task BindTargetWindowAsync()
        {
            if (_targetWindowHandle == 0)
            {
                throw new InvalidOperationException("请先输入并绑定外部窗口句柄。");
            }

            var version = await _olaWorker.BindWindowAsync(_targetWindowHandle);
            _isBound = true;
            Text = $"OLA Automation Configurator - {version}";
            UpdateBindingStatus();
        }

        private async Task EnsureOlaReadyAsync()
        {
            if (_targetWindowHandle == 0)
            {
                throw new InvalidOperationException("请先选择并绑定外部窗口句柄。");
            }

            if (!_isBound)
            {
                throw new InvalidOperationException("当前窗口尚未绑定，请先点击 Bind HWND。");
            }

            await Task.CompletedTask;
        }

        private void TreeActions_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            propertyGrid.SelectedObject = e.Node?.Tag is WorkflowNode node ? node.Action : null;
        }

        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            RefreshWorkflowTree();
        }

        private async void btnBindWindow_Click(object sender, EventArgs e)
        {
            try
            {
                StopWindowPick(false);
                _targetWindowHandle = ParseWindowHandle(txtTargetHwnd.Text);

                await BindTargetWindowAsync();

                MessageBox.Show($"已绑定窗口句柄: 0x{_targetWindowHandle:X}", "OLA", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _isBound = false;
                UpdateBindingStatus();
                MessageBox.Show(ex.Message, "OLA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnPickWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            StartWindowPick();
        }

        private void btnPickWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_isPickingWindow)
            {
                return;
            }

            StopWindowPick(true);
        }

        private async void btnTestOcr_Click(object sender, EventArgs e)
        {
            try
            {
                await ExecuteKeyPressAsync("A" ?? string.Empty);
                //await EnsureOlaReadyAsync();
                //var text = await ExecuteOcrAsync(100, 100, 100, 100);
                //ShowOcrResult(100, 100, 100, 100, text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "OLA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddMouse_Click(object sender, EventArgs e)
        {
            RemoveSelectedAction();
        }

        private void btnAddKey_Click(object sender, EventArgs e)
        {
            MoveSelectedAction(-1);
        }

        private void btnAddOCR_Click(object sender, EventArgs e)
        {
            MoveSelectedAction(1);
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            if (_isRunningWorkflow)
            {
                return;
            }

            try
            {
                SetWorkflowRunning(true);
                await RunActionsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "OLA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetWorkflowRunning(false);
            }
        }

        private void AddAction(ScriptAction action, TreeNode? targetNode = null, bool addAsChild = false)
        {
            var workflowNode = new WorkflowNode { Action = action.Clone() };
            InsertWorkflowNode(workflowNode, targetNode, addAsChild);
            RefreshWorkflowTree(workflowNode.Id);
        }

        private void InsertWorkflowNode(WorkflowNode node, TreeNode? targetNode, bool addAsChild)
        {
            if (targetNode?.Tag is not WorkflowNode targetWorkflowNode)
            {
                _workflowRoots.Add(node);
                return;
            }

            if (addAsChild && CanContainChildren(targetWorkflowNode.Action.ActionType))
            {
                if (node.Action.ActionType == ActionType.Else && targetWorkflowNode.Action.ActionType != ActionType.If)
                {
                    throw new InvalidOperationException("Else 只能添加到 If 步骤下。");
                }

                if (node.Action.ActionType == ActionType.Else && targetWorkflowNode.Children.Any(c => c.Action.ActionType == ActionType.Else))
                {
                    throw new InvalidOperationException("每个 If 步骤只能包含一个 Else 分支。");
                }

                targetWorkflowNode.Children.Add(node);
                return;
            }

            var siblings = GetSiblingList(targetNode);
            var index = siblings.IndexOf(targetWorkflowNode);
            siblings.Insert(index + 1, node);
        }

        private static bool CanContainChildren(ActionType actionType)
        {
            return actionType is ActionType.If or ActionType.Else or ActionType.LoopStart;
        }

        private void RefreshDesignPanelItems()
        {
            RefreshWorkflowTree();
        }

        private void Item_Click(object? sender, EventArgs e)
        {
        }

        private void Item_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Control c)
            {
                _draggingControl = c;
                _dragStart = e.Location;
                c.Capture = true;
            }
        }

        private void Item_MouseMove(object? sender, MouseEventArgs e)
        {
        }

        private void Item_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_draggingControl != null)
            {
                _draggingControl.Capture = false;
                _draggingControl = null;
            }
        }

        private void designPanel_MouseDown(object sender, MouseEventArgs e)
        {
            _treeActions.SelectedNode = null;
            propertyGrid.SelectedObject = null;
        }

        private void designPanel_MouseMove(object sender, MouseEventArgs e)
        {
            // nothing
        }

        private void designPanel_MouseUp(object sender, MouseEventArgs e)
        {
            // nothing
        }

        private void WindowPickTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isPickingWindow)
            {
                return;
            }

            if (!NativeMethods.IsLeftMouseButtonDown())
            {
                StopWindowPick(true);
                return;
            }

            var cursorPoint = Cursor.Position;
            var handle = NativeMethods.WindowFromPoint(new NativeMethods.POINT(cursorPoint));
            if (handle == IntPtr.Zero)
            {
                ClearWindowHighlight();
                return;
            }

            if (IsOwnWindow(handle))
            {
                ClearWindowHighlight();
                return;
            }

            _hoverWindowHandle = handle.ToInt64();
            if (NativeMethods.GetWindowRect(handle, out var rect))
            {
                UpdateWindowHighlight(_hoverWindowHandle, rect);
            }
            else
            {
                ClearWindowHighlight();
            }
        }

        private async Task RunActionsAsync()
        {
            await EnsureOlaReadyAsync();
            ClearOutput();

            var executionActions = FlattenWorkflow(_workflowRoots);
            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var stepIndex = BuildStepIndex(executionActions);
            var loopStack = new Stack<(int StartIndex, int EndIndex, int RepeatCount, int Iteration, string VariableName)>();
            var callStack = new Stack<int>();

            var i = 0;
            while (i < executionActions.Count)
            {
                var a = executionActions[i];
                AppendOutput($"[{i + 1}] {a}");

                switch (a.ActionType)
                {
                    case ActionType.SetVariable:
                        SetVariable(variables, a.OutputVariable, ResolveValue(a.TextValue, variables));
                        break;
                    case ActionType.If:
                        if (!EvaluateCondition(a, variables))
                        {
                            i = FindElseOrEndIfIndex(executionActions, i) + 1;
                            continue;
                        }
                        break;
                    case ActionType.Else:
                        i = FindEndIfIndex(executionActions, i) + 1;
                        continue;
                    case ActionType.EndIf:
                        break;
                    case ActionType.LoopStart:
                        {
                            var repeatCount = Math.Max(0, a.RepeatCount);
                            if (repeatCount <= 0)
                            {
                                i = FindMatchingEndLoopIndex(executionActions, i) + 1;
                                continue;
                            }

                            if (loopStack.Count == 0 || loopStack.Peek().StartIndex != i)
                            {
                                var variableName = a.OutputVariable ?? string.Empty;
                                loopStack.Push((i, FindMatchingEndLoopIndex(executionActions, i), repeatCount, 0, variableName));
                                SetVariable(variables, variableName, "1");
                            }
                        }
                        break;
                    case ActionType.EndLoop:
                        {
                            if (loopStack.Count == 0)
                            {
                                throw new InvalidOperationException("End Loop 没有对应的 Loop Start。");
                            }

                            var loop = loopStack.Pop();
                            if (loop.EndIndex != i)
                            {
                                throw new InvalidOperationException("Loop 结构不匹配，请检查 Loop Start / End Loop 顺序。");
                            }

                            if (loop.Iteration + 1 < loop.RepeatCount)
                            {
                                var nextLoop = (StartIndex: loop.StartIndex, EndIndex: loop.EndIndex, RepeatCount: loop.RepeatCount, Iteration: loop.Iteration + 1, VariableName: loop.VariableName);
                                loopStack.Push(nextLoop);
                                SetVariable(variables, loop.VariableName, (nextLoop.Iteration + 1).ToString(CultureInfo.InvariantCulture));
                                i = loop.StartIndex + 1;
                                continue;
                            }
                        }
                        break;
                    case ActionType.BreakLoop:
                        {
                            if (loopStack.Count == 0)
                            {
                                break;
                            }

                            var loop = loopStack.Pop();
                            i = loop.EndIndex + 1;
                            continue;
                        }
                    case ActionType.GotoStep:
                        i = ResolveTargetIndex(a, variables, stepIndex);
                        continue;
                    case ActionType.CallStep:
                        callStack.Push(i + 1);
                        i = ResolveTargetIndex(a, variables, stepIndex);
                        continue;
                    case ActionType.ReturnStep:
                        if (callStack.Count == 0)
                        {
                            return;
                        }

                        i = callStack.Pop();
                        continue;
                    case ActionType.MouseMove:
                        await _olaWorker.MoveToAsync(a.X, a.Y);
                        break;
                    case ActionType.LeftClick:
                        await _olaWorker.LeftClickAsync(a.X, a.Y);
                        break;
                    case ActionType.LeftDoubleClick:
                        await _olaWorker.LeftDoubleClickAsync(a.X, a.Y);
                        break;
                    case ActionType.LeftDown:
                        await _olaWorker.LeftDownAsync(a.X, a.Y);
                        break;
                    case ActionType.LeftUp:
                        await _olaWorker.LeftUpAsync();
                        break;
                    case ActionType.MouseDrag:
                        await ExecuteMouseDragAsync(a);
                        break;
                    case ActionType.RightClick:
                        await _olaWorker.RightClickAsync(a.X, a.Y);
                        break;
                    case ActionType.RightDown:
                        await _olaWorker.RightDownAsync(a.X, a.Y);
                        break;
                    case ActionType.RightUp:
                        await _olaWorker.RightUpAsync();
                        break;
                    case ActionType.MiddleClick:
                        await _olaWorker.MiddleClickAsync(a.X, a.Y);
                        break;
                    case ActionType.WheelUp:
                        await _olaWorker.WheelUpAsync();
                        break;
                    case ActionType.WheelDown:
                        await _olaWorker.WheelDownAsync();
                        break;
                    case ActionType.KeyPress:
                        await ExecuteKeyPressAsync(ResolveValue(a.Key, variables));
                        break;
                    case ActionType.InputText:
                        await ExecuteTextInputAsync(ResolveValue(a.TextValue, variables));
                        break;
                    case ActionType.SetClipboard:
                        await ExecuteSetClipboardAsync(ResolveValue(a.TextValue, variables));
                        break;
                    case ActionType.SendPaste:
                        await _olaWorker.SendPasteAsync();
                        break;
                    case ActionType.OCR:
                        {
                            var text = await ExecuteOcrAsync(a.X, a.Y, a.Width, a.Height);
                            ShowOcrResult(a.X, a.Y, a.Width, a.Height, text);
                            AppendOutput($"OCR => {text}");
                            SetVariable(variables, a.OutputVariable, text);
                            break;
                        }
                    case ActionType.FindImage:
                        await ExecuteFindImageAsync(a, variables);
                        break;
                    case ActionType.ClickImage:
                        await ExecuteClickImageAsync(a, variables);
                        break;
                    case ActionType.WaitImage:
                        await ExecuteWaitImageAsync(a, variables);
                        break;
                    case ActionType.FindColor:
                        await ExecuteFindColorAsync(a, variables);
                        break;
                    case ActionType.ClickColor:
                        await ExecuteClickColorAsync(a, variables);
                        break;
                    case ActionType.WaitColor:
                        await ExecuteWaitColorAsync(a, variables);
                        break;
                    case ActionType.Capture:
                        await ExecuteCaptureAsync(a, variables);
                        break;
                    case ActionType.WindowActivate:
                        await _olaWorker.SetBoundWindowStateAsync(1);
                        AppendOutput("Window => 激活");
                        break;
                    case ActionType.WindowHide:
                        await _olaWorker.SetBoundWindowStateAsync(6);
                        AppendOutput("Window => 隐藏");
                        break;
                    case ActionType.WindowShow:
                        await _olaWorker.SetBoundWindowStateAsync(7);
                        AppendOutput("Window => 显示");
                        break;
                    case ActionType.WindowSetSize:
                        await _olaWorker.SetBoundWindowSizeAsync(Math.Max(1, a.Width), Math.Max(1, a.Height));
                        AppendOutput($"Window => Size {a.Width}x{a.Height}");
                        break;
                    case ActionType.Delay:
                        await Task.Delay(Math.Max(1, a.DelayMs));
                        AppendOutput($"Delay => {a.DelayMs}ms");
                        break;
                }

                if (a.ActionType != ActionType.Delay)
                {
                    await Task.Delay(Math.Max(1, a.DelayMs));
                }

                i++;
            }
        }

        private async Task ExecuteKeyPressAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (key.Length == 1)
            {
                await _olaWorker.KeyPressCharAsync(key);
                return;
            }

            await _olaWorker.KeyPressStrAsync(key, 50);
        }

        private async Task ExecuteTextInputAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            await _olaWorker.KeyPressStrAsync(text, 50);
        }

        private async Task ExecuteSetClipboardAsync(string text)
        {
            await _olaWorker.SetClipboardAsync(text ?? string.Empty);
            AppendOutput($"Clipboard => {text}");
        }

        private async Task ExecuteMouseDragAsync(ScriptAction action)
        {
            await _olaWorker.LeftDownAsync(action.X, action.Y);
            await Task.Delay(Math.Max(1, action.PollIntervalMs));
            await _olaWorker.MoveToAsync(action.EndX, action.EndY);
            await Task.Delay(Math.Max(1, action.PollIntervalMs));
            await _olaWorker.LeftUpAsync();
            AppendOutput($"Drag => ({action.X},{action.Y}) -> ({action.EndX},{action.EndY})");
        }

        private static long ParseWindowHandle(string input)
        {
            var value = input.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("请输入外部窗口句柄。支持十进制或 0x 开头的十六进制。");
            }

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                {
                    return hexValue;
                }
            }

            if (long.TryParse(value, out var decimalValue))
            {
                return decimalValue;
            }

            if (long.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexWithoutPrefix))
            {
                return hexWithoutPrefix;
            }

            throw new InvalidOperationException("窗口句柄格式不正确，请输入十进制或十六进制句柄值。");
        }

        private async Task ExecuteClickImageAsync(ScriptAction action, Dictionary<string, string> variables)
        {
            var result = await FindImageAsync(action, variables);
            if (!result.MatchState)
            {
                throw new InvalidOperationException($"未找到图片: {action.ImagePath}");
            }

            var clickX = result.X + Math.Max(1, result.Width) / 2;
            var clickY = result.Y + Math.Max(1, result.Height) / 2;
            await _olaWorker.LeftClickAsync(clickX, clickY);
            AppendOutput($"ClickImage => ({clickX},{clickY})");
            SetMatchResultVariables(variables, action.OutputVariable, result);
        }

        private async Task ExecuteWaitImageAsync(ScriptAction action, Dictionary<string, string> variables)
        {
            var timeoutMs = Math.Max(1, action.TimeoutMs);
            var pollMs = Math.Max(50, action.PollIntervalMs);
            var start = Environment.TickCount64;

            while (Environment.TickCount64 - start < timeoutMs)
            {
                var result = await FindImageAsync(action, variables);
                if (result.MatchState)
                {
                    AppendOutput($"WaitImage => 命中 X={result.X}, Y={result.Y}");
                    SetMatchResultVariables(variables, action.OutputVariable, result);
                    return;
                }

                await Task.Delay(pollMs);
            }

            throw new InvalidOperationException($"等待图片超时: {action.ImagePath}");
        }

        private async Task ExecuteFindColorAsync(ScriptAction action, Dictionary<string, string> variables)
        {
            var point = await FindColorAsync(action, variables);
            if (point is null)
            {
                AppendOutput($"FindColor => 未找到 {action.ColorStart}-{action.ColorEnd}");
                return;
            }

            AppendOutput($"FindColor => 命中 X={point.Value.X}, Y={point.Value.Y}");
            SetPointVariables(variables, action.OutputVariable, point.Value);
        }

        private async Task ExecuteWaitColorAsync(ScriptAction action, Dictionary<string, string> variables)
        {
            var timeoutMs = Math.Max(1, action.TimeoutMs);
            var pollMs = Math.Max(50, action.PollIntervalMs);
            var start = Environment.TickCount64;

            while (Environment.TickCount64 - start < timeoutMs)
            {
                var point = await FindColorAsync(action, variables);
                if (point is not null)
                {
                    AppendOutput($"WaitColor => 命中 X={point.Value.X}, Y={point.Value.Y}");
                    SetPointVariables(variables, action.OutputVariable, point.Value);
                    return;
                }

                await Task.Delay(pollMs);
            }

            throw new InvalidOperationException($"等待颜色超时: {action.ColorStart}-{action.ColorEnd}");
        }

        private async Task ExecuteClickColorAsync(ScriptAction action, Dictionary<string, string> variables)
        {
            var point = await FindColorAsync(action, variables);
            if (point is null)
            {
                throw new InvalidOperationException($"未找到颜色: {action.ColorStart}-{action.ColorEnd}");
            }

            await _olaWorker.LeftClickAsync(point.Value.X, point.Value.Y);
            AppendOutput($"ClickColor => ({point.Value.X},{point.Value.Y})");
            SetPointVariables(variables, action.OutputVariable, point.Value);
        }

        private async Task ExecuteCaptureAsync(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            var outputPath = ResolveValue(action.ImagePath, variables);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("截图步骤缺少输出文件路径。请在 Image Path 中填写目标路径。");
            }

            var x2 = action.X + Math.Max(1, action.Width);
            var y2 = action.Y + Math.Max(1, action.Height);
            await _olaWorker.CaptureAsync(action.X, action.Y, x2, y2, outputPath);
            AppendOutput($"Capture => {outputPath}");
        }

        private async Task<MatchResult> FindImageAsync(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            var imagePath = ResolveValue(action.ImagePath, variables);
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new InvalidOperationException("找图步骤缺少 Image Path 参数。");
            }

            var x2 = action.X + Math.Max(1, action.Width);
            var y2 = action.Y + Math.Max(1, action.Height);
            var threshold = Math.Clamp(action.MatchThreshold, 0.1, 1.0);
            return await _olaWorker.MatchWindowsFromPathAsync(action.X, action.Y, x2, y2, imagePath, threshold, 2, 0, 1.0);
        }

        private async Task<Point?> FindColorAsync(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            var colorStart = ResolveValue(action.ColorStart, variables);
            var colorEnd = ResolveValue(action.ColorEnd, variables);
            if (string.IsNullOrWhiteSpace(colorStart) || string.IsNullOrWhiteSpace(colorEnd))
            {
                throw new InvalidOperationException("颜色步骤缺少 Color Start 或 Color End 参数。");
            }

            var x2 = action.X + Math.Max(1, action.Width);
            var y2 = action.Y + Math.Max(1, action.Height);
            return await _olaWorker.FindColorAsync(action.X, action.Y, x2, y2, colorStart, colorEnd, action.SearchDirection);
        }

        private static List<ScriptAction> FlattenWorkflow(IEnumerable<WorkflowNode> nodes)
        {
            var result = new List<ScriptAction>();
            foreach (var node in nodes)
            {
                FlattenWorkflowNode(node, result);
            }

            return result;
        }

        private static void FlattenWorkflowNode(WorkflowNode node, ICollection<ScriptAction> result)
        {
            switch (node.Action.ActionType)
            {
                case ActionType.If:
                    result.Add(node.Action.Clone());

                    var elseNode = node.Children.FirstOrDefault(c => c.Action.ActionType == ActionType.Else);
                    foreach (var child in node.Children)
                    {
                        if (ReferenceEquals(child, elseNode))
                        {
                            break;
                        }

                        FlattenWorkflowNode(child, result);
                    }

                    if (elseNode is not null)
                    {
                        result.Add(elseNode.Action.Clone());
                        foreach (var child in elseNode.Children)
                        {
                            FlattenWorkflowNode(child, result);
                        }
                    }

                    result.Add(new ScriptAction { Name = "End If", ActionType = ActionType.EndIf, Description = "Auto generated by tree workflow." });
                    break;

                case ActionType.LoopStart:
                    result.Add(node.Action.Clone());
                    foreach (var child in node.Children)
                    {
                        FlattenWorkflowNode(child, result);
                    }
                    result.Add(new ScriptAction { Name = "End Loop", ActionType = ActionType.EndLoop, Description = "Auto generated by tree workflow." });
                    break;

                case ActionType.Else:
                    result.Add(node.Action.Clone());
                    foreach (var child in node.Children)
                    {
                        FlattenWorkflowNode(child, result);
                    }
                    break;

                default:
                    result.Add(node.Action.Clone());
                    break;
            }
        }

        private Dictionary<string, int> BuildStepIndex(IReadOnlyList<ScriptAction> actions)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < actions.Count; index++)
            {
                var key = GetStepKey(actions[index]);
                if (!string.IsNullOrWhiteSpace(key) && !result.ContainsKey(key))
                {
                    result[key] = index;
                }
            }

            return result;
        }

        private static string GetStepKey(ScriptAction action)
        {
            return string.IsNullOrWhiteSpace(action.StepId) ? action.Name : action.StepId!;
        }

        private static string ResolveValue(string? template, IReadOnlyDictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            return Regex.Replace(template, "\\$\\{(?<name>[^}]+)\\}", match =>
            {
                var name = match.Groups["name"].Value;
                return variables.TryGetValue(name, out var value) ? value : string.Empty;
            });
        }

        private static void SetVariable(IDictionary<string, string> variables, string? name, string? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            variables[name] = value ?? string.Empty;
        }

        private static void SetPointVariables(IDictionary<string, string> variables, string? name, Point point)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            variables[name] = $"{point.X},{point.Y}";
            variables[$"{name}.X"] = point.X.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Y"] = point.Y.ToString(CultureInfo.InvariantCulture);
        }

        private static void SetMatchResultVariables(IDictionary<string, string> variables, string? name, MatchResult result)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            variables[name] = result.MatchState ? $"{result.X},{result.Y},{result.Width},{result.Height}" : string.Empty;
            variables[$"{name}.MatchState"] = result.MatchState.ToString();
            variables[$"{name}.X"] = result.X.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Y"] = result.Y.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Width"] = result.Width.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Height"] = result.Height.ToString(CultureInfo.InvariantCulture);
            variables[$"{name}.Score"] = result.MatchVal.ToString(CultureInfo.InvariantCulture);
        }

        private bool EvaluateCondition(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            var left = ResolveValue(action.ConditionLeft, variables);
            var right = ResolveValue(action.ConditionRight, variables);

            return action.ConditionOperator switch
            {
                ConditionOperatorType.Equals => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.NotEquals => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.Contains => left.Contains(right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.NotContains => !left.Contains(right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.StartsWith => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.EndsWith => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
                ConditionOperatorType.GreaterThan => CompareAsNumber(left, right) > 0,
                ConditionOperatorType.GreaterThanOrEqual => CompareAsNumber(left, right) >= 0,
                ConditionOperatorType.LessThan => CompareAsNumber(left, right) < 0,
                ConditionOperatorType.LessThanOrEqual => CompareAsNumber(left, right) <= 0,
                ConditionOperatorType.IsEmpty => string.IsNullOrWhiteSpace(left),
                ConditionOperatorType.IsNotEmpty => !string.IsNullOrWhiteSpace(left),
                _ => false
            };
        }

        private static int CompareAsNumber(string left, string right)
        {
            var leftIsNumber = double.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out var leftNumber);
            var rightIsNumber = double.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var rightNumber);

            return leftIsNumber && rightIsNumber
                ? leftNumber.CompareTo(rightNumber)
                : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private int ResolveTargetIndex(ScriptAction action, IReadOnlyDictionary<string, string> variables, IReadOnlyDictionary<string, int> stepIndex)
        {
            var target = ResolveValue(action.TargetStep, variables);
            if (string.IsNullOrWhiteSpace(target) || !stepIndex.TryGetValue(target, out var index))
            {
                throw new InvalidOperationException($"未找到目标步骤: {action.TargetStep}");
            }

            return index;
        }

        private int FindElseOrEndIfIndex(IReadOnlyList<ScriptAction> actions, int ifIndex)
        {
            var depth = 0;
            for (var index = ifIndex + 1; index < actions.Count; index++)
            {
                switch (actions[index].ActionType)
                {
                    case ActionType.If:
                        depth++;
                        break;
                    case ActionType.EndIf when depth == 0:
                    case ActionType.Else when depth == 0:
                        return index;
                    case ActionType.EndIf:
                        depth--;
                        break;
                }
            }

            throw new InvalidOperationException("If 步骤缺少匹配的 Else 或 EndIf。");
        }

        private int FindEndIfIndex(IReadOnlyList<ScriptAction> actions, int elseIndex)
        {
            var depth = 0;
            for (var index = elseIndex + 1; index < actions.Count; index++)
            {
                switch (actions[index].ActionType)
                {
                    case ActionType.If:
                        depth++;
                        break;
                    case ActionType.EndIf when depth == 0:
                        return index;
                    case ActionType.EndIf:
                        depth--;
                        break;
                }
            }

            throw new InvalidOperationException("Else 步骤缺少匹配的 EndIf。");
        }

        private int FindMatchingEndLoopIndex(IReadOnlyList<ScriptAction> actions, int loopStartIndex)
        {
            var depth = 0;
            for (var index = loopStartIndex + 1; index < actions.Count; index++)
            {
                switch (actions[index].ActionType)
                {
                    case ActionType.LoopStart:
                        depth++;
                        break;
                    case ActionType.EndLoop when depth == 0:
                        return index;
                    case ActionType.EndLoop:
                        depth--;
                        break;
                }
            }

            throw new InvalidOperationException("Loop Start 步骤缺少匹配的 End Loop。");
        }

        private async Task<string> ExecuteOcrAsync(int x, int y, int width, int height)
        {
            var x1 = x;
            var y1 = y;
            var x2 = x + Math.Max(1, width);
            var y2 = y + Math.Max(1, height);
            return await _olaWorker.OcrAsync(x1, y1, x2, y2);
        }

        private void ShowOcrResult(int x, int y, int width, int height, string text)
        {
            AppendOutput($"区域: ({x},{y},{width},{height})");
            AppendOutput(text);
        }

        private async Task ExecuteFindImageAsync(ScriptAction action, Dictionary<string, string> variables)
        {
            var result = await FindImageAsync(action, variables);

            if (result.MatchState)
            {
                AppendOutput($"FindImage => 命中 X={result.X}, Y={result.Y}, W={result.Width}, H={result.Height}, Score={result.MatchVal:F2}");
                SetMatchResultVariables(variables, action.OutputVariable, result);
            }
            else
            {
                AppendOutput($"FindImage => 未找到 {action.ImagePath}");
            }
        }

        private void ConfigureActionUi()
        {
            lstActions.Visible = false;
            designPanel.Visible = false;

            _lblWorkflowActions.AutoSize = true;
            _lblWorkflowActions.Text = "执行步骤";
            _lblWorkflowActions.Location = new Point(lstActions.Left, Math.Max(8, lstActions.Top - 18));

            _lblAvailableActions.AutoSize = true;
            _lblAvailableActions.Text = "基础功能";
            _lblAvailableActions.Location = new Point(designPanel.Left, Math.Max(8, designPanel.Top - 18));

            _lstAvailableActions.AllowDrop = false;
            _lstAvailableActions.FormattingEnabled = true;
            _lstAvailableActions.IntegralHeight = false;
            _lstAvailableActions.ItemHeight = 15;
            _lstAvailableActions.Location = designPanel.Location;
            _lstAvailableActions.Size = designPanel.Size;
            _lstAvailableActions.Anchor = designPanel.Anchor;

            _treeActions.HideSelection = false;
            _treeActions.Location = lstActions.Location;
            _treeActions.Size = lstActions.Size;
            _treeActions.Anchor = lstActions.Anchor;
            _treeActions.AllowDrop = true;

            Controls.Add(_lblWorkflowActions);
            Controls.Add(_lblAvailableActions);
            Controls.Add(_lstAvailableActions);
            Controls.Add(_treeActions);
            _lstAvailableActions.BringToFront();
            _treeActions.BringToFront();

            btnAddMouse.Text = "删除步骤";
            btnAddKey.Text = "上移";
            btnAddOCR.Text = "下移";
            btnRun.Text = "执行流程";
            lblOcrResult.Text = "执行输出";

            _treeActions.AfterSelect += TreeActions_AfterSelect;
            _lstAvailableActions.MouseDown += AvailableActions_MouseDown;
            _lstAvailableActions.DoubleClick += AvailableActions_DoubleClick;
            _lstAvailableActions.SelectedIndexChanged += AvailableActions_SelectedIndexChanged;
            _treeActions.ItemDrag += TreeActions_ItemDrag;
            _treeActions.DragEnter += TreeActions_DragEnter;
            _treeActions.DragOver += TreeActions_DragOver;
            _treeActions.DragDrop += TreeActions_DragDrop;
            _treeActions.KeyDown += TreeActions_KeyDown;
        }

        private void InitializeActionCatalog()
        {
            _availableActionTemplates.Clear();
            _availableActionTemplates.Add(new ActionTemplate("Set Variable", new ScriptAction { Name = "Set Variable", ActionType = ActionType.SetVariable, Description = "将文本值写入变量，支持 ${变量名} 模板。", OutputVariable = "var1", TextValue = "value" }));
            _availableActionTemplates.Add(new ActionTemplate("If", new ScriptAction { Name = "If", ActionType = ActionType.If, Description = "条件成立时继续执行，否则跳到 Else 或 EndIf。", ConditionLeft = "${var1}", ConditionOperator = ConditionOperatorType.Equals, ConditionRight = "value" }));
            _availableActionTemplates.Add(new ActionTemplate("Else", new ScriptAction { Name = "Else", ActionType = ActionType.Else, Description = "If 条件不成立时执行的分支开始。" }));
            _availableActionTemplates.Add(new ActionTemplate("End If", new ScriptAction { Name = "End If", ActionType = ActionType.EndIf, Description = "结束 If/Else 结构。" }));
            _availableActionTemplates.Add(new ActionTemplate("Loop Start", new ScriptAction { Name = "Loop Start", ActionType = ActionType.LoopStart, Description = "开始固定次数循环，可把当前轮次写入变量。", RepeatCount = 3, OutputVariable = "loopIndex" }));
            _availableActionTemplates.Add(new ActionTemplate("End Loop", new ScriptAction { Name = "End Loop", ActionType = ActionType.EndLoop, Description = "结束循环并返回到 Loop Start。" }));
            _availableActionTemplates.Add(new ActionTemplate("Break Loop", new ScriptAction { Name = "Break Loop", ActionType = ActionType.BreakLoop, Description = "跳出最近一层循环。" }));
            _availableActionTemplates.Add(new ActionTemplate("Goto Step", new ScriptAction { Name = "Goto Step", ActionType = ActionType.GotoStep, Description = "跳转到目标步骤 Id。", TargetStep = "step_target" }));
            _availableActionTemplates.Add(new ActionTemplate("Call Step", new ScriptAction { Name = "Call Step", ActionType = ActionType.CallStep, Description = "调用目标步骤 Id，遇到 Return Step 返回。", TargetStep = "subroutine" }));
            _availableActionTemplates.Add(new ActionTemplate("Return Step", new ScriptAction { Name = "Return Step", ActionType = ActionType.ReturnStep, Description = "从步骤调用中返回。" }));
            _availableActionTemplates.Add(new ActionTemplate("Mouse Move", new ScriptAction { Name = "Mouse Move", ActionType = ActionType.MouseMove, Description = "移动鼠标到目标坐标。", X = 100, Y = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Left Click", new ScriptAction { Name = "Left Click", ActionType = ActionType.LeftClick, Description = "移动到坐标后执行左键点击。", X = 100, Y = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Left Double Click", new ScriptAction { Name = "Left Double Click", ActionType = ActionType.LeftDoubleClick, Description = "移动到坐标后执行左键双击。", X = 100, Y = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Left Down", new ScriptAction { Name = "Left Down", ActionType = ActionType.LeftDown, Description = "移动到坐标后按下左键，不自动释放。", X = 100, Y = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Left Up", new ScriptAction { Name = "Left Up", ActionType = ActionType.LeftUp, Description = "释放当前左键按下状态。" }));
            _availableActionTemplates.Add(new ActionTemplate("Mouse Drag", new ScriptAction { Name = "Mouse Drag", ActionType = ActionType.MouseDrag, Description = "按下左键后拖动到终点并释放。", X = 100, Y = 100, EndX = 200, EndY = 200, PollIntervalMs = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Right Click", new ScriptAction { Name = "Right Click", ActionType = ActionType.RightClick, Description = "移动到坐标后执行右键点击。", X = 100, Y = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Right Down", new ScriptAction { Name = "Right Down", ActionType = ActionType.RightDown, Description = "移动到坐标后按下右键，不自动释放。", X = 100, Y = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Right Up", new ScriptAction { Name = "Right Up", ActionType = ActionType.RightUp, Description = "释放当前右键按下状态。" }));
            _availableActionTemplates.Add(new ActionTemplate("Middle Click", new ScriptAction { Name = "Middle Click", ActionType = ActionType.MiddleClick, Description = "移动到坐标后执行中键点击。", X = 100, Y = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Wheel Up", new ScriptAction { Name = "Wheel Up", ActionType = ActionType.WheelUp, Description = "在当前鼠标位置执行滚轮向上。" }));
            _availableActionTemplates.Add(new ActionTemplate("Wheel Down", new ScriptAction { Name = "Wheel Down", ActionType = ActionType.WheelDown, Description = "在当前鼠标位置执行滚轮向下。" }));
            _availableActionTemplates.Add(new ActionTemplate("Key Press", new ScriptAction { Name = "Key Press", ActionType = ActionType.KeyPress, Description = "发送单个按键或按键串。", Key = "A" }));
            _availableActionTemplates.Add(new ActionTemplate("Input Text", new ScriptAction { Name = "Input Text", ActionType = ActionType.InputText, Description = "向目标窗口输入文本。", TextValue = "hello" }));
            _availableActionTemplates.Add(new ActionTemplate("Set Clipboard", new ScriptAction { Name = "Set Clipboard", ActionType = ActionType.SetClipboard, Description = "设置系统剪贴板内容。", TextValue = "hello" }));
            _availableActionTemplates.Add(new ActionTemplate("Send Paste", new ScriptAction { Name = "Send Paste", ActionType = ActionType.SendPaste, Description = "向当前绑定窗口发送粘贴命令。" }));
            _availableActionTemplates.Add(new ActionTemplate("OCR", new ScriptAction { Name = "OCR", ActionType = ActionType.OCR, Description = "识别指定区域的文字。", X = 100, Y = 100, Width = 100, Height = 100 }));
            _availableActionTemplates.Add(new ActionTemplate("Find Image", new ScriptAction { Name = "Find Image", ActionType = ActionType.FindImage, Description = "在指定区域查找图片。", X = 0, Y = 0, Width = 300, Height = 300, ImagePath = "sample.png", MatchThreshold = 0.8 }));
            _availableActionTemplates.Add(new ActionTemplate("Click Image", new ScriptAction { Name = "Click Image", ActionType = ActionType.ClickImage, Description = "找到图片后点击其中心点。", X = 0, Y = 0, Width = 300, Height = 300, ImagePath = "sample.png", MatchThreshold = 0.8 }));
            _availableActionTemplates.Add(new ActionTemplate("Wait Image", new ScriptAction { Name = "Wait Image", ActionType = ActionType.WaitImage, Description = "轮询指定区域直到图片出现。", X = 0, Y = 0, Width = 300, Height = 300, ImagePath = "sample.png", MatchThreshold = 0.8, TimeoutMs = 3000, PollIntervalMs = 200 }));
            _availableActionTemplates.Add(new ActionTemplate("Find Color", new ScriptAction { Name = "Find Color", ActionType = ActionType.FindColor, Description = "在区域内查找颜色范围。", X = 0, Y = 0, Width = 300, Height = 300, ColorStart = "000000", ColorEnd = "FFFFFF", SearchDirection = 0 }));
            _availableActionTemplates.Add(new ActionTemplate("Click Color", new ScriptAction { Name = "Click Color", ActionType = ActionType.ClickColor, Description = "找到颜色后点击命中的坐标。", X = 0, Y = 0, Width = 300, Height = 300, ColorStart = "000000", ColorEnd = "FFFFFF", SearchDirection = 0 }));
            _availableActionTemplates.Add(new ActionTemplate("Wait Color", new ScriptAction { Name = "Wait Color", ActionType = ActionType.WaitColor, Description = "轮询指定区域直到颜色出现。", X = 0, Y = 0, Width = 300, Height = 300, ColorStart = "000000", ColorEnd = "FFFFFF", SearchDirection = 0, TimeoutMs = 3000, PollIntervalMs = 200 }));
            _availableActionTemplates.Add(new ActionTemplate("Capture", new ScriptAction { Name = "Capture", ActionType = ActionType.Capture, Description = "将指定区域截图保存到文件。", X = 0, Y = 0, Width = 300, Height = 300, ImagePath = "capture.png" }));
            _availableActionTemplates.Add(new ActionTemplate("Window Activate", new ScriptAction { Name = "Window Activate", ActionType = ActionType.WindowActivate, Description = "激活当前绑定窗口。" }));
            _availableActionTemplates.Add(new ActionTemplate("Window Hide", new ScriptAction { Name = "Window Hide", ActionType = ActionType.WindowHide, Description = "隐藏当前绑定窗口。" }));
            _availableActionTemplates.Add(new ActionTemplate("Window Show", new ScriptAction { Name = "Window Show", ActionType = ActionType.WindowShow, Description = "显示当前绑定窗口。" }));
            _availableActionTemplates.Add(new ActionTemplate("Window Set Size", new ScriptAction { Name = "Window Set Size", ActionType = ActionType.WindowSetSize, Description = "设置当前绑定窗口的大小。", Width = 1280, Height = 720 }));
            _availableActionTemplates.Add(new ActionTemplate("Delay", new ScriptAction { Name = "Delay", ActionType = ActionType.Delay, Description = "等待指定毫秒数。", DelayMs = 500 }));

            _lstAvailableActions.Items.Clear();
            foreach (var template in _availableActionTemplates)
            {
                _lstAvailableActions.Items.Add(template);
            }

            if (_lstAvailableActions.Items.Count > 0)
            {
                _lstAvailableActions.SelectedIndex = 0;
            }
        }

        private void AvailableActions_MouseDown(object? sender, MouseEventArgs e)
        {
            var index = _lstAvailableActions.IndexFromPoint(e.Location);
            if (index == ListBox.NoMatches)
            {
                return;
            }

            _lstAvailableActions.SelectedIndex = index;
            var template = (ActionTemplate)_lstAvailableActions.Items[index];
            _lstAvailableActions.DoDragDrop(new ActionDragData(template, -1), DragDropEffects.Copy);
        }

        private void AvailableActions_DoubleClick(object? sender, EventArgs e)
        {
            if (_lstAvailableActions.SelectedItem is ActionTemplate template)
            {
                var selectedNode = _treeActions.SelectedNode;
                AddAction(template.CreateAction(), selectedNode, selectedNode?.Tag is WorkflowNode node && CanContainChildren(node.Action.ActionType));
            }
        }

        private void AvailableActions_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_lstAvailableActions.SelectedItem is ActionTemplate template)
            {
                txtOcrResult.Text = $"功能: {template.DisplayName}{Environment.NewLine}{Environment.NewLine}{template.Description}";
            }
        }

        private void TreeActions_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is not TreeNode treeNode || treeNode.Tag is not WorkflowNode workflowNode)
            {
                return;
            }

            _treeActions.SelectedNode = treeNode;
            _treeActions.DoDragDrop(new WorkflowNodeDragData(workflowNode), DragDropEffects.Move);
        }

        private void TreeActions_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(ActionDragData)) == true)
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }

            if (e.Data?.GetDataPresent(typeof(WorkflowNodeDragData)) == true)
            {
                e.Effect = DragDropEffects.Move;
                return;
            }

            e.Effect = DragDropEffects.None;
        }

        private void TreeActions_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(ActionDragData)) != true && e.Data?.GetDataPresent(typeof(WorkflowNodeDragData)) != true)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            var targetNode = GetDropTargetNode(e.X, e.Y);
            if (targetNode is not null)
            {
                _treeActions.SelectedNode = targetNode;
            }

            e.Effect = e.Data?.GetDataPresent(typeof(WorkflowNodeDragData)) == true ? DragDropEffects.Move : DragDropEffects.Copy;
        }

        private void TreeActions_DragDrop(object? sender, DragEventArgs e)
        {
            var targetNode = GetDropTargetNode(e.X, e.Y);

            if (e.Data?.GetDataPresent(typeof(ActionDragData)) == true)
            {
                var data = (ActionDragData)e.Data.GetData(typeof(ActionDragData))!;
                if (data.Template is not null)
                {
                    AddAction(data.Template.CreateAction(), targetNode, targetNode?.Tag is WorkflowNode workflowNode && CanContainChildren(workflowNode.Action.ActionType));
                }

                return;
            }

            if (e.Data?.GetDataPresent(typeof(WorkflowNodeDragData)) == true)
            {
                var data = (WorkflowNodeDragData)e.Data.GetData(typeof(WorkflowNodeDragData))!;
                MoveWorkflowNode(data.Node, targetNode);
            }
        }

        private void TreeActions_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveSelectedAction();
            }
        }

        private TreeNode? GetDropTargetNode(int screenX, int screenY)
        {
            return _treeActions.GetNodeAt(_treeActions.PointToClient(new Point(screenX, screenY)));
        }

        private void RefreshWorkflowTree(string? selectedNodeId = null)
        {
            selectedNodeId ??= (_treeActions.SelectedNode?.Tag as WorkflowNode)?.Id;

            _treeActions.BeginUpdate();
            _treeActions.Nodes.Clear();
            foreach (var node in _workflowRoots)
            {
                _treeActions.Nodes.Add(CreateTreeNode(node));
            }
            _treeActions.ExpandAll();
            _treeActions.EndUpdate();

            if (_workflowRoots.Count == 0)
            {
                propertyGrid.SelectedObject = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedNodeId))
            {
                var selectedNode = FindTreeNodeByWorkflowId(_treeActions.Nodes, selectedNodeId);
                if (selectedNode is not null)
                {
                    _treeActions.SelectedNode = selectedNode;
                    return;
                }
            }

            _treeActions.SelectedNode = _treeActions.Nodes.Count > 0 ? _treeActions.Nodes[0] : null;
        }

        private TreeNode CreateTreeNode(WorkflowNode node)
        {
            var text = string.IsNullOrWhiteSpace(node.Action.StepId)
                ? node.Action.ToString()
                : $"[{node.Action.StepId}] {node.Action}";

            var treeNode = new TreeNode(text) { Tag = node };
            foreach (var child in node.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            return treeNode;
        }

        private void RemoveSelectedAction()
        {
            if (_treeActions.SelectedNode?.Tag is not WorkflowNode selectedNode)
            {
                return;
            }

            RemoveWorkflowNode(_workflowRoots, selectedNode);
            RefreshWorkflowTree();
        }

        private void MoveSelectedAction(int offset)
        {
            if (_treeActions.SelectedNode is not TreeNode treeNode || treeNode.Tag is not WorkflowNode selectedNode)
            {
                return;
            }

            var siblings = GetSiblingList(treeNode);
            var index = siblings.IndexOf(selectedNode);
            if (index < 0)
            {
                return;
            }

            var newIndex = index + offset;
            if (newIndex < 0 || newIndex >= siblings.Count)
            {
                return;
            }

            siblings.RemoveAt(index);
            siblings.Insert(newIndex, selectedNode);
            RefreshWorkflowTree(selectedNode.Id);
        }

        private List<WorkflowNode> GetSiblingList(TreeNode treeNode)
        {
            return treeNode.Parent?.Tag is WorkflowNode parentNode ? parentNode.Children : _workflowRoots;
        }

        private void MoveWorkflowNode(WorkflowNode sourceNode, TreeNode? targetTreeNode)
        {
            if (targetTreeNode?.Tag is WorkflowNode targetNode)
            {
                if (ReferenceEquals(sourceNode, targetNode) || ContainsNode(sourceNode, targetNode))
                {
                    return;
                }
            }

            RemoveWorkflowNode(_workflowRoots, sourceNode);

            if (targetTreeNode?.Tag is not WorkflowNode targetWorkflowNode)
            {
                _workflowRoots.Add(sourceNode);
                RefreshWorkflowTree(sourceNode.Id);
                return;
            }

            if (CanContainChildren(targetWorkflowNode.Action.ActionType))
            {
                if (sourceNode.Action.ActionType == ActionType.Else && targetWorkflowNode.Action.ActionType != ActionType.If)
                {
                    throw new InvalidOperationException("Else 只能移动到 If 步骤下。");
                }

                targetWorkflowNode.Children.Add(sourceNode);
            }
            else
            {
                var siblings = GetSiblingList(targetTreeNode);
                var targetIndex = siblings.IndexOf(targetWorkflowNode);
                siblings.Insert(targetIndex + 1, sourceNode);
            }

            RefreshWorkflowTree(sourceNode.Id);
        }

        private static bool RemoveWorkflowNode(ICollection<WorkflowNode> nodes, WorkflowNode target)
        {
            foreach (var node in nodes.ToList())
            {
                if (ReferenceEquals(node, target))
                {
                    nodes.Remove(node);
                    return true;
                }

                if (RemoveWorkflowNode(node.Children, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsNode(WorkflowNode parent, WorkflowNode target)
        {
            foreach (var child in parent.Children)
            {
                if (ReferenceEquals(child, target) || ContainsNode(child, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static TreeNode? FindTreeNodeByWorkflowId(TreeNodeCollection nodes, string workflowId)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is WorkflowNode workflowNode && string.Equals(workflowNode.Id, workflowId, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                var childResult = FindTreeNodeByWorkflowId(node.Nodes, workflowId);
                if (childResult is not null)
                {
                    return childResult;
                }
            }

            return null;
        }

        private void AppendOutput(string message)
        {
            if (txtOcrResult.InvokeRequired)
            {
                txtOcrResult.BeginInvoke(() => AppendOutput(message));
                return;
            }

            if (txtOcrResult.TextLength > 0)
            {
                txtOcrResult.AppendText(Environment.NewLine);
            }

            txtOcrResult.AppendText(message);
        }

        private void ClearOutput()
        {
            if (txtOcrResult.InvokeRequired)
            {
                txtOcrResult.BeginInvoke(ClearOutput);
                return;
            }

            txtOcrResult.Clear();
        }

        private void SetWorkflowRunning(bool isRunning)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => SetWorkflowRunning(isRunning));
                return;
            }

            _isRunningWorkflow = isRunning;
            btnRun.Enabled = !isRunning;
            btnRun.Text = isRunning ? "执行中..." : "执行流程";
        }

        private void StartWindowPick()
        {
            _isPickingWindow = true;
            _hoverWindowHandle = 0;
            btnPickWindow.Capture = true;
            btnPickWindow.Text = "Release To Select";
            Cursor = Cursors.Cross;
            txtOcrResult.Text = "拖住“Pick Window”按钮后移动鼠标到目标窗口，松开鼠标即可记录句柄。";
            _windowPickTimer.Start();
        }

        private void StopWindowPick(bool applySelection)
        {
            _windowPickTimer.Stop();
            _isPickingWindow = false;
            btnPickWindow.Capture = false;
            btnPickWindow.Text = "Pick Window";
            Cursor = Cursors.Default;
            ClearWindowHighlight();

            if (!applySelection || _hoverWindowHandle == 0)
            {
                UpdateBindingStatus();
                return;
            }

            if (_targetWindowHandle != _hoverWindowHandle)
            {
                _isBound = false;
            }

            _targetWindowHandle = _hoverWindowHandle;
            txtTargetHwnd.Text = $"0x{_targetWindowHandle:X}";
            txtOcrResult.Text = $"已选择窗口句柄: 0x{_targetWindowHandle:X}";
            UpdateBindingStatus();
        }

        private void UpdateWindowHighlight(long windowHandle, NativeMethods.RECT rect)
        {
            var bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                ClearWindowHighlight();
                return;
            }

            if (_highlightedWindowHandle == windowHandle && _highlightedBounds == bounds)
            {
                return;
            }

            _highlightedWindowHandle = windowHandle;
            _highlightedBounds = bounds;
            _highlightOverlay.ShowBorder(bounds);
            lblBindStatus.Text = $"选择中: 0x{_highlightedWindowHandle:X}";
        }

        private void ClearWindowHighlight()
        {
            if (_highlightedWindowHandle == 0 && _highlightedBounds == Rectangle.Empty)
            {
                return;
            }

            _highlightedWindowHandle = 0;
            _highlightedBounds = Rectangle.Empty;
            _highlightOverlay.HideOverlay();
        }

        private bool IsOwnWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            if (hwnd == Handle || hwnd == _highlightOverlay.Handle)
            {
                return true;
            }

            var current = hwnd;
            while (current != IntPtr.Zero)
            {
                if (current == Handle || current == _highlightOverlay.Handle)
                {
                    return true;
                }

                current = NativeMethods.GetParent(current);
            }

            return false;
        }

        private void UpdateBindingStatus()
        {
            if (_isBound && _targetWindowHandle != 0)
            {
                lblBindStatus.Text = $"已绑定: 0x{_targetWindowHandle:X}";
                return;
            }

            if (_targetWindowHandle != 0)
            {
                lblBindStatus.Text = $"待绑定: 0x{_targetWindowHandle:X}";
                return;
            }

            lblBindStatus.Text = "未绑定外部窗口";
        }

    }
}
