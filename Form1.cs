using System.Globalization;
using System.Diagnostics;
using System.Linq;
namespace OlAform
{
    public partial class Form1 : Form
    {
        private const int TrackingRegionSize = 400;
        private const float TrackingConfidenceThreshold = 0.25f;
        private const float TrackingIouThreshold = 0.45f;
        private const float TrackingAimGain = 0.18f;
        private const int TrackingMaxStepPixels = 18;
        private const int TrackingMinStepPixels = 1;
        private const int TrackingDeadzonePixels = 6;
        private const int TrackingLoopDelayMs = 30;
        private const int TrackingPreviewIntervalMs = 80;
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
        private readonly PictureBox _picDetectionPreview = new();
        private TabPage? _tabDetectionPreview;
        private CancellationTokenSource? _trackingCts;
        private Task? _trackingTask;
        private bool _isTrackingArmed;
        private bool _isPickingWindow;
        private long _hoverWindowHandle;
        private long _highlightedWindowHandle;
        private Rectangle _highlightedBounds = Rectangle.Empty;
        private bool _isRunningWorkflow;
        private readonly ProjectStorage _projectStorage = new(AppContext.BaseDirectory);

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
            Shown += (_, _) => ArrangeWorkflowLayout();
            Resize += (_, _) => ArrangeWorkflowLayout();
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadProjectList();
            UpdateBindingStatus();
            RefreshWorkflowTree();
            ArrangeWorkflowLayout();
        }

        private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
        {
            try
            {
                StopWindowPick(false);
                _highlightOverlay.Close();
                StopTracking(waitForCompletion: false);
                _picDetectionPreview.Image?.Dispose();
                _olaWorker.Dispose();
            }
            catch
            {
            }
        }

        private async void btnTrackTarget_Click(object sender, EventArgs e)
        {
            if (_isTrackingArmed)
            {
                StopTracking(waitForCompletion: false);
                AppendOutput("目标追踪监控已手动停止。");
                return;
            }

            try
            {
                await EnsureOlaReadyAsync();

                var modelPath = Path.Combine(AppContext.BaseDirectory, "dawn1.onnx");
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException("找不到追踪模型。", modelPath);
                }

                StartTracking(modelPath);
                AppendOutput("目标追踪监控已启动。按住鼠标左键时执行检测和移动；松开左键仅暂停，点击按钮可手动停止监控。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "OLA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Task BindTargetWindowAsync()
        {
            return BindTargetWindowAsync(_targetWindowHandle);
        }

        private async Task BindTargetWindowAsync(long targetWindowHandle)
        {
            if (targetWindowHandle == 0)
            {
                throw new InvalidOperationException("请先输入并绑定外部窗口句柄。");
            }

            var version = await _olaWorker.BindWindowAsync(targetWindowHandle);
            _targetWindowHandle = targetWindowHandle;
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
            propertyGrid.SelectedObject = e.Node?.Tag is WorkflowNode node ? new ScriptActionPropertyGridAdapter(node.Action) : null;
        }

        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_treeActions.SelectedNode?.Tag is WorkflowNode node)
            {
                propertyGrid.SelectedObject = new ScriptActionPropertyGridAdapter(node.Action);
            }

            RefreshWorkflowTree();
        }

        private async void btnBindWindow_Click(object sender, EventArgs e)
        {
            try
            {
                StopWindowPick(false);
                _targetWindowHandle = WorkflowExecutionHelper.ParseWindowHandle(txtTargetHwnd.Text);

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
                using var fileDialog = new OpenFileDialog
                {
                    Title = "选择需要分析的图片",
                    Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.webp|所有文件|*.*"
                };

                if (fileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var modelPath = Path.Combine(AppContext.BaseDirectory, "dawn1.onnx");
                using var analyzer = new YoloOnnxImageAnalyzer(modelPath);
                var modelInfo = analyzer.GetModelInfo();
                var analysis = analyzer.AnalyzeImageDetailed(fileDialog.FileName, 0.25f, 0.45f);
                var detections = analysis.Detections;

                var annotatedPath = Path.Combine(
                    Path.GetDirectoryName(fileDialog.FileName) ?? AppContext.BaseDirectory,
                    $"{Path.GetFileNameWithoutExtension(fileDialog.FileName)}.result{Path.GetExtension(fileDialog.FileName)}");

                analyzer.SaveAnnotatedImage(fileDialog.FileName, annotatedPath);

                ClearOutput();
                AppendOutput($"模型: {modelPath}");
                AppendOutput($"图片: {fileDialog.FileName}");
                AppendOutput($"输入: {modelInfo.InputName} [{string.Join(", ", modelInfo.InputShape)}]");
                AppendOutput($"输出: {modelInfo.OutputName} [{string.Join(", ", modelInfo.OutputShape)}]");
                AppendOutput($"标签数: {modelInfo.Labels.Count}");
                AppendOutput($"预处理模式: {analysis.Diagnostics.PreprocessMode}");
                AppendOutput($"解析模式: {analysis.Diagnostics.ParserMode}");
                AppendOutput($"预测数: {analysis.Diagnostics.Predictions}");
                AppendOutput($"属性数: {analysis.Diagnostics.Attributes}");
                AppendOutput($"包含 Objectness: {analysis.Diagnostics.HasObjectness}");
                AppendOutput($"最大 Objectness: {analysis.Diagnostics.MaxObjectness:F6}");
                AppendOutput($"最大类别分数: {analysis.Diagnostics.MaxClassScore:F6}");
                AppendOutput($"最大综合分数: {analysis.Diagnostics.MaxConfidence:F6}");
                AppendOutput($"检测结果数量: {detections.Count}");

                foreach (var detection in detections)
                {
                    AppendOutput($"- {detection.Label} | 分数 {detection.Confidence:F3} | 框 [{detection.X1:F0}, {detection.Y1:F0}, {detection.X2:F0}, {detection.Y2:F0}]");
                }

                AppendOutput($"标注结果已保存: {annotatedPath}");
                ShowDetectionPreview(annotatedPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "OLA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveProject_Click(object sender, EventArgs e)
        {
            try
            {
                var projectName = cmbProjects.Text.Trim();
                if (string.IsNullOrWhiteSpace(projectName))
                {
                    throw new InvalidOperationException("请输入项目名称。");
                }

                SaveProject(projectName);
                LoadProjectList(projectName);
                MessageBox.Show($"项目已保存: {projectName}", "项目", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "项目", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadProject_Click(object sender, EventArgs e)
        {
            try
            {
                var projectName = cmbProjects.Text.Trim();
                if (string.IsNullOrWhiteSpace(projectName))
                {
                    throw new InvalidOperationException("请选择要加载的项目。");
                }

                LoadProject(projectName);
                MessageBox.Show($"项目已加载: {projectName}", "项目", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "项目", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteProject_Click(object sender, EventArgs e)
        {
            try
            {
                var projectName = cmbProjects.Text.Trim();
                if (string.IsNullOrWhiteSpace(projectName))
                {
                    throw new InvalidOperationException("请选择要删除的项目。");
                }

                _projectStorage.Delete(projectName);

                LoadProjectList();
                MessageBox.Show($"项目已删除: {projectName}", "项目", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "项目", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            WorkflowTreeService.InsertWorkflowNode(_workflowRoots, workflowNode, targetNode, addAsChild);
            RefreshWorkflowTree(workflowNode.Id);
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
            ClearOutput();

            var executionActions = WorkflowTreeService.FlattenWorkflow(_workflowRoots);
            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var windowObjects = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var stepIndex = WorkflowExecutionHelper.BuildStepIndex(executionActions);
            var loopStack = new Stack<(int StartIndex, int EndIndex, int RepeatCount, int Iteration, string VariableName)>();
            var callStack = new Stack<int>();

            var i = 0;
            while (i < executionActions.Count)
            {
                var a = executionActions[i];
                AppendOutput($"[{i + 1}] {a}");

                if (a.ActionType != ActionType.BindWindow && WorkflowExecutionHelper.RequiresBoundWindow(a.ActionType))
                {
                    await EnsureActionWindowContextAsync(a, variables, windowObjects);
                }

                switch (a.ActionType)
                {
                    case ActionType.BindWindow:
                        await ExecuteBindWindowStepAsync(a, variables, windowObjects);
                        break;
                    case ActionType.SetVariable:
                        WorkflowExecutionHelper.SetVariable(variables, a.OutputVariable, WorkflowExecutionHelper.ResolveValue(a.TextValue, variables));
                        break;
                    case ActionType.If:
                        if (!WorkflowExecutionHelper.EvaluateCondition(a, variables))
                        {
                            i = WorkflowExecutionHelper.FindElseOrEndIfIndex(executionActions, i) + 1;
                            continue;
                        }
                        break;
                    case ActionType.Else:
                        i = WorkflowExecutionHelper.FindEndIfIndex(executionActions, i) + 1;
                        continue;
                    case ActionType.EndIf:
                        break;
                    case ActionType.LoopStart:
                        {
                            var repeatCount = Math.Max(0, a.RepeatCount);
                            if (repeatCount <= 0)
                            {
                                i = WorkflowExecutionHelper.FindMatchingEndLoopIndex(executionActions, i) + 1;
                                continue;
                            }

                            if (loopStack.Count == 0 || loopStack.Peek().StartIndex != i)
                            {
                                var variableName = a.OutputVariable ?? string.Empty;
                                loopStack.Push((i, WorkflowExecutionHelper.FindMatchingEndLoopIndex(executionActions, i), repeatCount, 0, variableName));
                                WorkflowExecutionHelper.SetVariable(variables, variableName, "1");
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
                                WorkflowExecutionHelper.SetVariable(variables, loop.VariableName, (nextLoop.Iteration + 1).ToString(CultureInfo.InvariantCulture));
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
                        i = WorkflowExecutionHelper.ResolveTargetIndex(a, variables, stepIndex);
                        continue;
                    case ActionType.CallStep:
                        callStack.Push(i + 1);
                        i = WorkflowExecutionHelper.ResolveTargetIndex(a, variables, stepIndex);
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
                        await ExecuteKeyPressAsync(WorkflowExecutionHelper.ResolveValue(a.Key, variables));
                        break;
                    case ActionType.InputText:
                        await ExecuteTextInputAsync(WorkflowExecutionHelper.ResolveValue(a.TextValue, variables));
                        break;
                    case ActionType.SetClipboard:
                        await ExecuteSetClipboardAsync(WorkflowExecutionHelper.ResolveValue(a.TextValue, variables));
                        break;
                    case ActionType.SendPaste:
                        await _olaWorker.SendPasteAsync();
                        break;
                    case ActionType.OCR:
                        {
                            var text = await ExecuteOcrAsync(a.X, a.Y, a.Width, a.Height);
                            ShowOcrResult(a.X, a.Y, a.Width, a.Height, text);
                            AppendOutput($"OCR => {text}");
                            WorkflowExecutionHelper.SetVariable(variables, a.OutputVariable, text);
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

        private async Task ExecuteBindWindowStepAsync(ScriptAction action, Dictionary<string, string> variables, Dictionary<string, long> windowObjects)
        {
            var objectName = action.OutputVariable?.Trim();
            if (string.IsNullOrWhiteSpace(objectName))
            {
                throw new InvalidOperationException("Bind Window 步骤需要填写 Output Variable。该变量名将作为窗口对象名。");
            }

            var windowHandle = ResolveBindWindowHandle(action, variables);

            await BindTargetWindowAsync(windowHandle);

            windowObjects[objectName] = windowHandle;
            WorkflowExecutionHelper.SetVariable(variables, objectName, $"0x{windowHandle:X}");
            WorkflowExecutionHelper.SetVariable(variables, $"{objectName}.Handle", $"0x{windowHandle:X}");
            AppendOutput($"BindWindow => {objectName} -> 0x{windowHandle:X}");
        }

        private static long ResolveBindWindowHandle(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            return action.BindWindowResolveMode switch
            {
                BindWindowResolveMode.DirectHandle => ResolveBindWindowHandleFromText(action, variables),
                BindWindowResolveMode.WindowFromPoint => ResolveBindWindowHandleFromPoint(action),
                BindWindowResolveMode.ProcessName => ResolveBindWindowHandleFromProcess(action, variables),
                _ => throw new InvalidOperationException($"不支持的窗口绑定方式: {action.BindWindowResolveMode}")
            };
        }

        private static long ResolveBindWindowHandleFromText(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            var resolvedHandleText = WorkflowExecutionHelper.ResolveValue(action.WindowHandle, variables);
            return WorkflowExecutionHelper.ParseWindowHandle(resolvedHandleText);
        }

        private static long ResolveBindWindowHandleFromPoint(ScriptAction action)
        {
            var hwnd = NativeMethods.WindowFromPoint(new NativeMethods.POINT(new Point(action.X, action.Y)));
            if (hwnd == IntPtr.Zero)
            {
                throw new InvalidOperationException($"指定坐标未找到窗口: ({action.X},{action.Y})");
            }

            if (action.UseRootWindow)
            {
                hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
            }

            if (hwnd == IntPtr.Zero)
            {
                throw new InvalidOperationException($"指定坐标未找到可绑定的根窗口: ({action.X},{action.Y})");
            }

            return hwnd.ToInt64();
        }

        private static long ResolveBindWindowHandleFromProcess(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            var processName = WorkflowExecutionHelper.ResolveValue(action.ProcessName, variables).Trim();
            if (string.IsNullOrWhiteSpace(processName))
            {
                throw new InvalidOperationException("Bind Window 使用进程名模式时必须填写 Process Name。");
            }

            var normalizedName = Path.GetFileNameWithoutExtension(processName);
            var handle = Process.GetProcessesByName(normalizedName)
                .Select(process => process.MainWindowHandle)
                .FirstOrDefault(windowHandle => windowHandle != IntPtr.Zero);

            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"未找到进程窗口: {processName}");
            }

            return handle.ToInt64();
        }

        private async Task EnsureActionWindowContextAsync(ScriptAction action, IReadOnlyDictionary<string, string> variables, IDictionary<string, long> windowObjects)
        {
            var targetObject = WorkflowExecutionHelper.ResolveValue(action.TargetObject, variables).Trim();
            if (string.IsNullOrWhiteSpace(targetObject))
            {
                await EnsureOlaReadyAsync();
                return;
            }

            if (!windowObjects.TryGetValue(targetObject, out var windowHandle))
            {
                if (!variables.TryGetValue(targetObject, out var rawHandle) || string.IsNullOrWhiteSpace(rawHandle))
                {
                    throw new InvalidOperationException($"未找到窗口对象变量: {targetObject}");
                }

                windowHandle = WorkflowExecutionHelper.ParseWindowHandle(rawHandle);
                windowObjects[targetObject] = windowHandle;
            }

            await BindTargetWindowAsync(windowHandle);
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
            WorkflowExecutionHelper.SetMatchResultVariables(variables, action.OutputVariable, result);
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
                    WorkflowExecutionHelper.SetMatchResultVariables(variables, action.OutputVariable, result);
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
            WorkflowExecutionHelper.SetPointVariables(variables, action.OutputVariable, point.Value);
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
                    WorkflowExecutionHelper.SetPointVariables(variables, action.OutputVariable, point.Value);
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
            WorkflowExecutionHelper.SetPointVariables(variables, action.OutputVariable, point.Value);
        }

        private async Task ExecuteCaptureAsync(ScriptAction action, IReadOnlyDictionary<string, string> variables)
        {
            var outputPath = WorkflowExecutionHelper.ResolveValue(action.ImagePath, variables);
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
            var imagePath = WorkflowExecutionHelper.ResolveValue(action.ImagePath, variables);
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
            var colorStart = WorkflowExecutionHelper.ResolveValue(action.ColorStart, variables);
            var colorEnd = WorkflowExecutionHelper.ResolveValue(action.ColorEnd, variables);
            if (string.IsNullOrWhiteSpace(colorStart) || string.IsNullOrWhiteSpace(colorEnd))
            {
                throw new InvalidOperationException("颜色步骤缺少 Color Start 或 Color End 参数。");
            }

            var x2 = action.X + Math.Max(1, action.Width);
            var y2 = action.Y + Math.Max(1, action.Height);
            return await _olaWorker.FindColorAsync(action.X, action.Y, x2, y2, colorStart, colorEnd, action.SearchDirection);
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
                WorkflowExecutionHelper.SetMatchResultVariables(variables, action.OutputVariable, result);
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
            _lstAvailableActions.Visible = false;

            var tabAvailableActions = EnsureAvailableActionsTabControl();

            _lblWorkflowActions.AutoSize = true;
            _lblWorkflowActions.Text = "执行步骤";
            _lblWorkflowActions.Location = new Point(lstActions.Left, Math.Max(8, lstActions.Top - 18));

            _lblAvailableActions.AutoSize = true;
            _lblAvailableActions.Text = "基础功能";
            _lblAvailableActions.Location = new Point(designPanel.Left, Math.Max(8, designPanel.Top - 18));

            tabAvailableActions.Alignment = TabAlignment.Left;
            tabAvailableActions.Multiline = true;
            tabAvailableActions.SizeMode = TabSizeMode.Fixed;
            tabAvailableActions.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabAvailableActions.ShowToolTips = true;
            tabAvailableActions.ItemSize = new Size(36, 96);
            tabAvailableActions.Location = designPanel.Location;
            tabAvailableActions.Size = designPanel.Size;
            tabAvailableActions.Anchor = designPanel.Anchor;

            _treeActions.HideSelection = false;
            _treeActions.Location = lstActions.Location;
            _treeActions.Size = lstActions.Size;
            _treeActions.Anchor = lstActions.Anchor;
            _treeActions.AllowDrop = true;

            Controls.Add(_lblWorkflowActions);
            Controls.Add(_lblAvailableActions);
            if (!Controls.Contains(tabAvailableActions))
            {
                Controls.Add(tabAvailableActions);
            }
            Controls.Add(_treeActions);
            tabAvailableActions.BringToFront();
            _treeActions.BringToFront();

            btnAddMouse.Text = "删除步骤";
            btnAddKey.Text = "上移";
            btnAddOCR.Text = "下移";
            btnRun.Text = "执行流程";
            lblOcrResult.Text = "执行输出";

            _treeActions.AfterSelect += TreeActions_AfterSelect;
            tabAvailableActions.SelectedIndexChanged += (_, _) => UpdateAvailableActionDescription();
            tabAvailableActions.DrawItem -= TabAvailableActions_DrawItem;
            tabAvailableActions.DrawItem += TabAvailableActions_DrawItem;
            _treeActions.ItemDrag += TreeActions_ItemDrag;
            _treeActions.DragEnter += TreeActions_DragEnter;
            _treeActions.DragOver += TreeActions_DragOver;
            _treeActions.DragDrop += TreeActions_DragDrop;
            _treeActions.KeyDown += TreeActions_KeyDown;

            ArrangeWorkflowLayout();
        }

        private void ArrangeWorkflowLayout()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            var tabAvailableActions = EnsureAvailableActionsTabControl();

            const int gap = 6;

            var workflowWidth = 180;
            var buttonLeft = _treeActions.Right + gap;
            var buttonWidth = btnSaveProject.Width;
            var propertyGridLeft = propertyGrid.Left;
            var availableLeft = btnSaveProject.Right + gap;

            var computedWorkflowWidth = Math.Max(160, btnSaveProject.Left - lstActions.Left - gap);
            workflowWidth = computedWorkflowWidth;

            _lblWorkflowActions.Location = new Point(lstActions.Left, Math.Max(8, _treeActions.Top - 18));
            _treeActions.Location = lstActions.Location;
            _treeActions.Size = new Size(workflowWidth, lstActions.Height);

            buttonLeft = _treeActions.Right + gap;

            var projectControls = new Control[]
            {
                lblProjects, cmbProjects, btnSaveProject, btnLoadProject, btnDeleteProject,
                lblTargetHwnd, txtTargetHwnd, btnPickWindow, btnBindWindow, btnTestOcr, btnTrackTarget,
                lblBindStatus, lblOcrResult, txtOcrResult, btnAddMouse, btnAddKey, btnAddOCR, btnRun
            };

            foreach (var control in projectControls)
            {
                control.Left = buttonLeft;
            }

            availableLeft = buttonLeft + buttonWidth + gap;
            var availableWidth = Math.Max(220, propertyGridLeft - availableLeft - gap);

            _lblAvailableActions.Location = new Point(availableLeft, Math.Max(8, tabAvailableActions.Top - 18));
            tabAvailableActions.Location = new Point(availableLeft, designPanel.Top);
            tabAvailableActions.Size = new Size(availableWidth, designPanel.Height);

            btnAddMouse.BringToFront();
            btnAddKey.BringToFront();
            btnAddOCR.BringToFront();
            btnRun.BringToFront();
            btnSaveProject.BringToFront();
            btnLoadProject.BringToFront();
            btnDeleteProject.BringToFront();
        }

        private void InitializeActionCatalog()
        {
            var tabAvailableActions = EnsureAvailableActionsTabControl();

            _availableActionTemplates.Clear();
            _availableActionTemplates.AddRange(WorkflowActionCatalog.CreateDefaultTemplates());

            tabAvailableActions.TabPages.Clear();

            foreach (var group in _availableActionTemplates.GroupBy(t => t.Category).OrderBy(g => g.Key))
            {
                var listBox = new ListBox
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.None,
                    FormattingEnabled = true,
                    IntegralHeight = false,
                    ItemHeight = 15
                };

                listBox.MouseDown += AvailableActions_MouseDown;
                listBox.DoubleClick += AvailableActions_DoubleClick;
                listBox.SelectedIndexChanged += AvailableActions_SelectedIndexChanged;

                foreach (var template in group)
                {
                    listBox.Items.Add(template);
                }

                var tabPage = new TabPage(ActionCategoryVisuals.GetIcon(group.Key))
                {
                    ToolTipText = ActionCategoryVisuals.GetDisplayName(group.Key),
                    Tag = group.Key
                };
                tabPage.Controls.Add(listBox);
                tabAvailableActions.TabPages.Add(tabPage);
            }

            tabAvailableActions.TabPages.Add(EnsureDetectionPreviewTab());

            if (tabAvailableActions.TabPages.Count > 0)
            {
                tabAvailableActions.SelectedIndex = 0;
                var listBox = GetCurrentAvailableActionsList();
                if (listBox is not null && listBox.Items.Count > 0)
                {
                    listBox.SelectedIndex = 0;
                }
            }

            UpdateAvailableActionDescription();
        }

        private void AvailableActions_MouseDown(object? sender, MouseEventArgs e)
        {
            if (sender is not ListBox listBox)
            {
                return;
            }

            var index = listBox.IndexFromPoint(e.Location);
            if (index == ListBox.NoMatches)
            {
                return;
            }

            listBox.SelectedIndex = index;
            var template = (ActionTemplate)listBox.Items[index];
            listBox.DoDragDrop(new ActionDragData(template, -1), DragDropEffects.Copy);
        }

        private void AvailableActions_DoubleClick(object? sender, EventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ActionTemplate template)
            {
                var selectedNode = _treeActions.SelectedNode;
                AddAction(template.CreateAction(), selectedNode, selectedNode?.Tag is WorkflowNode node && WorkflowTreeService.CanContainChildren(node.Action.ActionType));
            }
        }

        private void AvailableActions_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateAvailableActionDescription();
        }

        private ListBox? GetCurrentAvailableActionsList()
        {
            return GetAvailableActionsTabControl()?.SelectedTab?.Controls.OfType<ListBox>().FirstOrDefault();
        }

        private TabControl EnsureAvailableActionsTabControl()
        {
            return GetAvailableActionsTabControl()
                ?? CreateAvailableActionsTabControl();
        }

        private TabControl? GetAvailableActionsTabControl()
        {
            return Controls.OfType<TabControl>().FirstOrDefault(control => control.Name == "tabAvailableActions");
        }

        private TabControl CreateAvailableActionsTabControl()
        {
            return new TabControl
            {
                Name = "tabAvailableActions"
            };
        }

        private void TabAvailableActions_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl || e.Index < 0 || e.Index >= tabControl.TabPages.Count)
            {
                return;
            }

            var tabPage = tabControl.TabPages[e.Index];
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var backColor = isSelected ? SystemColors.ControlLightLight : SystemColors.Control;

            using var backBrush = new SolidBrush(backColor);
            e.Graphics.FillRectangle(backBrush, e.Bounds);

            var icon = tabPage.Text;
            TextRenderer.DrawText(
                e.Graphics,
                icon,
                new Font(Font.FontFamily, 14F, FontStyle.Regular),
                e.Bounds,
                SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            if (isSelected)
            {
                using var pen = new Pen(SystemColors.Highlight, 2);
                e.Graphics.DrawRectangle(pen, e.Bounds.Left + 1, e.Bounds.Top + 1, e.Bounds.Width - 3, e.Bounds.Height - 3);
            }
        }

        private void UpdateAvailableActionDescription()
        {
            var listBox = GetCurrentAvailableActionsList();
            if (listBox?.SelectedItem is ActionTemplate template)
            {
                txtOcrResult.Text = $"功能: {template.DisplayName}{Environment.NewLine}{Environment.NewLine}{template.Description}";
            }
        }

        private TabPage EnsureDetectionPreviewTab()
        {
            if (_tabDetectionPreview is not null)
            {
                return _tabDetectionPreview;
            }

            _picDetectionPreview.Dock = DockStyle.Fill;
            _picDetectionPreview.BackColor = Color.Black;
            _picDetectionPreview.SizeMode = PictureBoxSizeMode.Zoom;

            _tabDetectionPreview = new TabPage("🖼")
            {
                ToolTipText = "检测预览"
            };
            _tabDetectionPreview.Controls.Add(_picDetectionPreview);
            return _tabDetectionPreview;
        }

        private void ShowDetectionPreview(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                return;
            }

            var tabAvailableActions = EnsureAvailableActionsTabControl();
            var previewTab = EnsureDetectionPreviewTab();
            if (!tabAvailableActions.TabPages.Contains(previewTab))
            {
                tabAvailableActions.TabPages.Add(previewTab);
            }

            _picDetectionPreview.Image?.Dispose();
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var image = Image.FromStream(stream);
            _picDetectionPreview.Image = new Bitmap(image);
            tabAvailableActions.SelectedTab = previewTab;
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
                    AddAction(data.Template.CreateAction(), targetNode, targetNode?.Tag is WorkflowNode workflowNode && WorkflowTreeService.CanContainChildren(workflowNode.Action.ActionType));
                }

                return;
            }

            if (e.Data?.GetDataPresent(typeof(WorkflowNodeDragData)) == true)
            {
                var data = (WorkflowNodeDragData)e.Data.GetData(typeof(WorkflowNodeDragData))!;
                WorkflowTreeService.MoveWorkflowNode(_workflowRoots, data.Node, targetNode);
                RefreshWorkflowTree(data.Node.Id);
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
                _treeActions.Nodes.Add(WorkflowTreeService.CreateTreeNode(node));
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
                var selectedNode = WorkflowTreeService.FindTreeNodeByWorkflowId(_treeActions.Nodes, selectedNodeId);
                if (selectedNode is not null)
                {
                    _treeActions.SelectedNode = selectedNode;
                    return;
                }
            }

            _treeActions.SelectedNode = _treeActions.Nodes.Count > 0 ? _treeActions.Nodes[0] : null;
        }

        private void RemoveSelectedAction()
        {
            if (_treeActions.SelectedNode?.Tag is not WorkflowNode selectedNode)
            {
                return;
            }

            WorkflowTreeService.RemoveWorkflowNode(_workflowRoots, selectedNode);
            RefreshWorkflowTree();
        }

        private void MoveSelectedAction(int offset)
        {
            if (_treeActions.SelectedNode is not TreeNode treeNode || treeNode.Tag is not WorkflowNode selectedNode)
            {
                return;
            }

            var siblings = WorkflowTreeService.GetSiblingList(_workflowRoots, treeNode);
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

        private void StartTracking(string modelPath)
        {
            StopTracking(waitForCompletion: false);

            _trackingCts = new CancellationTokenSource();
            _isTrackingArmed = true;
            UpdateTrackingButtonState();
            _trackingTask = Task.Run(() => RunTrackingLoopAsync(modelPath, _trackingCts.Token));
        }

        private void StopTracking(bool waitForCompletion)
        {
            _trackingCts?.Cancel();

            if (waitForCompletion && _trackingTask is not null)
            {
                try
                {
                    _trackingTask.Wait(500);
                }
                catch
                {
                }
            }

            _trackingCts?.Dispose();
            _trackingCts = null;
            _trackingTask = null;
            _isTrackingArmed = false;
            UpdateTrackingButtonState();
        }

        private void UpdateTrackingButtonState()
        {
            if (InvokeRequired)
            {
                BeginInvoke(UpdateTrackingButtonState);
                return;
            }

            btnTrackTarget.Text = _isTrackingArmed ? "停止追踪" : "目标追踪";
        }

        private async Task RunTrackingLoopAsync(string modelPath, CancellationToken cancellationToken)
        {
            var wasTrackingActive = false;
            var lastPreviewTick = Environment.TickCount64;
            var captureFilePath = Path.Combine(AppContext.BaseDirectory, "tracking-capture.bmp");
            var residualMoveX = 0f;
            var residualMoveY = 0f;

            try
            {
                using var analyzer = new YoloOnnxImageAnalyzer(modelPath);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var isLeftButtonDown = NativeMethods.IsLeftMouseButtonDown();
                    if (!isLeftButtonDown)
                    {
                        if (wasTrackingActive)
                        {
                            wasTrackingActive = false;
                            AppendOutput("鼠标左键已弹起，目标追踪暂停，等待下一次按下。");
                        }

                        await Task.Delay(TrackingLoopDelayMs, cancellationToken);
                        continue;
                    }

                    if (!wasTrackingActive)
                    {
                        wasTrackingActive = true;
                        AppendOutput("检测到左键按下，开始后台目标追踪。");
                    }

                    try
                    {
                        var clientSize = await _olaWorker.GetBoundWindowClientSizeAsync();
                        var captureBounds = GetCenteredCaptureBounds(clientSize, TrackingRegionSize);
                        await _olaWorker.CaptureAsync(captureBounds.Left, captureBounds.Top, captureBounds.Right, captureBounds.Bottom, captureFilePath);

                        using var capturedMat = OpenCvSharp.Cv2.ImRead(captureFilePath, OpenCvSharp.ImreadModes.Color);
                        if (capturedMat.Empty())
                        {
                            await Task.Delay(TrackingLoopDelayMs, cancellationToken);
                            continue;
                        }

                        var analysis = analyzer.AnalyzeImageDetailed(capturedMat, TrackingConfidenceThreshold, TrackingIouThreshold);
                        var target = SelectTrackingTarget(analysis.Detections, capturedMat.Width / 2f, capturedMat.Height / 2f);

                        var now = Environment.TickCount64;
                        if (now - lastPreviewTick >= TrackingPreviewIntervalMs)
                        {
                            using var previewMat = analyzer.CreateAnnotatedImage(capturedMat, analysis.Detections);
                            UpdateDetectionPreview(previewMat);
                            lastPreviewTick = now;
                        }

                        if (target is not null)
                        {
                            var targetCenterX = (target.X1 + target.X2) / 2f;
                            var targetCenterY = (target.Y1 + target.Y2) / 2f;
                            var cropCenterX = capturedMat.Width / 2f;
                            var cropCenterY = capturedMat.Height / 2f;
                            var deltaX = targetCenterX - cropCenterX;
                            var deltaY = targetCenterY - cropCenterY;

                            if (Math.Abs(deltaX) > TrackingDeadzonePixels || Math.Abs(deltaY) > TrackingDeadzonePixels)
                            {
                                var moveX = ComputeTrackingStep(deltaX, ref residualMoveX);
                                var moveY = ComputeTrackingStep(deltaY, ref residualMoveY);

                                if (moveX != 0 || moveY != 0)
                                {
                                    NativeMethods.MoveMouseRelative(moveX, moveY);
                                }
                            }
                            else
                            {
                                residualMoveX = 0f;
                                residualMoveY = 0f;
                            }
                        }
                        else
                        {
                            residualMoveX = 0f;
                            residualMoveY = 0f;
                        }
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        AppendOutput($"追踪循环异常: {ex.Message}");
                        await Task.Delay(60, cancellationToken);
                    }

                    await Task.Delay(TrackingLoopDelayMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                BeginInvoke(() => MessageBox.Show(ex.Message, "目标追踪", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                try
                {
                    if (File.Exists(captureFilePath))
                    {
                        File.Delete(captureFilePath);
                    }
                }
                catch
                {
                }

                BeginInvoke(() => StopTracking(waitForCompletion: false));
            }
        }

        private static Rectangle GetCenteredCaptureBounds(Size clientSize, int preferredSize)
        {
            var captureWidth = Math.Min(preferredSize, clientSize.Width);
            var captureHeight = Math.Min(preferredSize, clientSize.Height);
            var left = Math.Max(0, (clientSize.Width - captureWidth) / 2);
            var top = Math.Max(0, (clientSize.Height - captureHeight) / 2);
            return new Rectangle(left, top, captureWidth, captureHeight);
        }

        private static YoloDetection? SelectTrackingTarget(IReadOnlyList<YoloDetection> detections, float centerX, float centerY)
        {
            return detections
                .OrderBy(detection => Math.Sqrt(Math.Pow(((detection.X1 + detection.X2) / 2f) - centerX, 2) + Math.Pow(((detection.Y1 + detection.Y2) / 2f) - centerY, 2)))
                .ThenByDescending(detection => detection.Confidence)
                .FirstOrDefault();
        }

        private static int ComputeTrackingStep(float deltaPixels, ref float residual)
        {
            var desired = (deltaPixels * TrackingAimGain) + residual;
            desired = Math.Clamp(desired, -TrackingMaxStepPixels, TrackingMaxStepPixels);

            var step = (int)Math.Truncate(desired);
            if (step == 0 && Math.Abs(desired) >= TrackingMinStepPixels)
            {
                step = Math.Sign(desired) * TrackingMinStepPixels;
            }

            residual = desired - step;
            return step;
        }

        private void UpdateDetectionPreview(OpenCvSharp.Mat image)
        {
            var pngBytes = image.ImEncode(".png");
            using var ms = new MemoryStream(pngBytes);
            using var loadedImage = Image.FromStream(ms);
            var preview = new Bitmap(loadedImage);

            if (InvokeRequired)
            {
                BeginInvoke(() => SetPreviewBitmap(preview));
                return;
            }

            SetPreviewBitmap(preview);
        }

        private void SetPreviewBitmap(Bitmap preview)
        {
            var previous = _picDetectionPreview.Image;
            _picDetectionPreview.Image = preview;
            previous?.Dispose();

            var tabAvailableActions = EnsureAvailableActionsTabControl();
            var previewTab = EnsureDetectionPreviewTab();
            if (!tabAvailableActions.TabPages.Contains(previewTab))
            {
                tabAvailableActions.TabPages.Add(previewTab);
            }

            tabAvailableActions.SelectedTab = previewTab;
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

        private void LoadProjectList(string? selectedProject = null)
        {
            var projectNames = _projectStorage.GetProjectNames();

            cmbProjects.BeginUpdate();
            cmbProjects.Items.Clear();
            foreach (var name in projectNames)
            {
                cmbProjects.Items.Add(name);
            }
            cmbProjects.EndUpdate();

            if (!string.IsNullOrWhiteSpace(selectedProject) && projectNames.Contains(selectedProject, StringComparer.OrdinalIgnoreCase))
            {
                cmbProjects.Text = selectedProject;
            }
            else if (projectNames.Count > 0 && string.IsNullOrWhiteSpace(cmbProjects.Text))
            {
                cmbProjects.SelectedIndex = 0;
            }

            btnSaveProject.BringToFront();
            btnLoadProject.BringToFront();
            btnDeleteProject.BringToFront();
            btnSaveProject.Parent?.PerformLayout();
        }

        private void SaveProject(string projectName)
        {
            _projectStorage.Save(projectName, _workflowRoots);
        }

        private void LoadProject(string projectName)
        {
            var project = _projectStorage.Load(projectName);

            _workflowRoots.Clear();
            foreach (var node in _projectStorage.RestoreWorkflow(project))
            {
                _workflowRoots.Add(node);
            }

            _isBound = false;
            UpdateBindingStatus();
            RefreshWorkflowTree();
            cmbProjects.Text = project.Name;
        }

    }
}
