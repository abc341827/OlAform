using System.Runtime.InteropServices;

namespace OlAform
{
    internal sealed class OlaWorker : IDisposable
    {
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _ready = new(false);
        private Control? _dispatcher;
        private ApplicationContext? _applicationContext;
        private Exception? _startupException;
        private OLAPlugServer? _ola;
        private bool _isInitialized;
        private bool _isBound;
        private long _boundWindowHandle;
        private string _version = string.Empty;
        private bool _disposed;

        public OlaWorker()
        {
            _thread = new Thread(ThreadMain)
            {
                IsBackground = true,
                Name = "OLA Worker"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            _ready.Wait();

            if (_startupException is not null)
            {
                throw new InvalidOperationException("OLA 工作线程初始化失败。", _startupException);
            }
        }

        public Task<string> BindWindowAsync(long targetWindowHandle)
        {
            return InvokeAsync(() =>
            {
                if (targetWindowHandle == 0)
                {
                    throw new InvalidOperationException("请先选择并绑定外部窗口句柄。");
                }

                EnsureInitialized();

                if (_isBound && _boundWindowHandle == targetWindowHandle)
                {
                    return _version;
                }

                if (_isBound)
                {
                    _ola!.UnBindWindow();
                    _isBound = false;
                }

                var result = _ola!.BindWindow(targetWindowHandle, "normal", "dx.mouse.api|dx.mouse.cursor", "dx.keypad.api", 0);
                if (result != 1)
                {
                    throw new InvalidOperationException($"绑定失败。ErrorId={_ola.GetLastError()}, Error={_ola.GetLastErrorString()}");
                }

                _isBound = true;
                _boundWindowHandle = targetWindowHandle;
                return _version;
            });
        }

        public Task MoveToAsync(int x, int y)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.MoveTo(x, y);
            });
        }

        public Task MoveRelativeAsync(int deltaX, int deltaY)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.MoveR(deltaX, deltaY);
            });
        }

        public Task LeftClickAsync(int x, int y)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.MoveTo(x, y);
                _ola.LeftClick();
            });
        }

        public Task LeftDoubleClickAsync(int x, int y)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.MoveTo(x, y);
                _ola.LeftDoubleClick();
            });
        }

        public Task LeftDownAsync(int x, int y)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.MoveTo(x, y);
                _ola.LeftDown();
            });
        }

        public Task LeftUpAsync()
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.LeftUp();
            });
        }

        public Task RightClickAsync(int x, int y)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.MoveTo(x, y);
                _ola.RightClick();
            });
        }

        public Task RightDownAsync(int x, int y)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.MoveTo(x, y);
                _ola.RightDown();
            });
        }

        public Task RightUpAsync()
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.RightUp();
            });
        }

        public Task MiddleClickAsync(int x, int y)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.MoveTo(x, y);
                _ola.MiddleClick();
            });
        }

        public Task WheelUpAsync()
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.WheelUp();
            });
        }

        public Task WheelDownAsync()
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.WheelDown();
            });
        }

        public Task KeyPressCharAsync(string key)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.KeyPressChar(key);
            });
        }

        public Task KeyPressStrAsync(string text, int delay)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.KeyPressStr(text, delay);
            });
        }

        public Task SetClipboardAsync(string text)
        {
            return InvokeAsync(() =>
            {
                EnsureInitialized();
                _ola!.SetClipboard(text);
            });
        }

        public Task SendPasteAsync()
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.SendPaste(_boundWindowHandle);
            });
        }

        public Task<string> OcrAsync(int x1, int y1, int x2, int y2)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                return _ola!.Ocr(x1, y1, x2, y2);
            });
        }

        public Task<MatchResult> MatchWindowsFromPathAsync(int x1, int y1, int x2, int y2, string imagePath, double matchVal, int type, double angle, double scale)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                return _ola!.MatchWindowsFromPath(x1, y1, x2, y2, imagePath, matchVal, type, angle, scale);
            });
        }

        public Task<Point?> FindColorAsync(int x1, int y1, int x2, int y2, string colorStart, string colorEnd, int dir)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                var result = _ola!.FindColor(x1, y1, x2, y2, colorStart, colorEnd, dir, out var x, out var y);
                return result == 1 ? new Point(x, y) : (Point?)null;
            });
        }

        public Task CaptureAsync(int x1, int y1, int x2, int y2, string filePath)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                var result = _ola!.Capture(x1, y1, x2, y2, filePath);
                if (result != 1)
                {
                    throw new InvalidOperationException($"截图失败。ErrorId={_ola.GetLastError()}, Error={_ola.GetLastErrorString()}");
                }
            });
        }

        public Task<byte[]> CaptureBmpBytesAsync(int x1, int y1, int x2, int y2)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();

                var result = _ola!.GetScreenDataBmp(x1, y1, x2, y2, out var dataPtr, out var dataLen);
                if (result != 1 || dataPtr == 0 || dataLen <= 0)
                {
                    throw new InvalidOperationException($"截图读取失败。ErrorId={_ola.GetLastError()}, Error={_ola.GetLastErrorString()}");
                }

                try
                {
                    var bytes = new byte[dataLen];
                    Marshal.Copy((IntPtr)dataPtr, bytes, 0, dataLen);
                    return bytes;
                }
                finally
                {
                    _ola.FreeImageData(dataPtr);
                }
            });
        }

        public Task<Size> GetBoundWindowClientSizeAsync()
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();

                var result = _ola!.GetClientSize(_boundWindowHandle, out var width, out var height);
                if (result != 1)
                {
                    throw new InvalidOperationException($"获取窗口客户区大小失败。ErrorId={_ola.GetLastError()}, Error={_ola.GetLastErrorString()}");
                }

                return new Size(width, height);
            });
        }

        public Task SetBoundWindowStateAsync(int state)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                _ola!.SetWindowState(_boundWindowHandle, state);
            });
        }

        public Task SetBoundWindowSizeAsync(int width, int height)
        {
            return InvokeAsync(() =>
            {
                ThrowIfNotBound();
                var result = _ola!.SetWindowSize(_boundWindowHandle, width, height);
                if (result != 1)
                {
                    throw new InvalidOperationException($"设置窗口大小失败。ErrorId={_ola.GetLastError()}, Error={_ola.GetLastErrorString()}");
                }
            });
        }

        private Task InvokeAsync(Action action)
        {
            return InvokeAsync<object?>(() =>
            {
                action();
                return null;
            });
        }

        private Task<T> InvokeAsync<T>(Func<T> action)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OlaWorker));
            }

            _ready.Wait();

            if (_startupException is not null)
            {
                throw new InvalidOperationException("OLA 工作线程初始化失败。", _startupException);
            }

            var dispatcher = _dispatcher;
            if (dispatcher is null || dispatcher.IsDisposed)
            {
                throw new InvalidOperationException("OLA 工作线程不可用。");
            }

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                dispatcher.BeginInvoke(new MethodInvoker(() =>
                {
                    if (_disposed)
                    {
                        tcs.TrySetException(new ObjectDisposedException(nameof(OlaWorker)));
                        return;
                    }

                    try
                    {
                        tcs.TrySetResult(action());
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        private void ThreadMain()
        {
            try
            {
                _dispatcher = new Control();
                _ = _dispatcher.Handle;
                _applicationContext = new ApplicationContext();
                _ready.Set();
                Application.Run(_applicationContext);
            }
            catch (Exception ex)
            {
                _startupException = ex;
                _ready.Set();
            }
            finally
            {
                Cleanup();

                if (_dispatcher is not null)
                {
                    _dispatcher.Dispose();
                    _dispatcher = null;
                }

                _applicationContext?.Dispose();
                _applicationContext = null;
            }
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _ola ??= new OLAPlugServer();
            var regResult = _ola.Reg(_ola.UserCode, _ola.SoftCode, _ola.FeatureList);
            if (regResult != 1)
            {
                throw new InvalidOperationException($"注册失败。ErrorId={_ola.GetLastError()}, Error={_ola.GetLastErrorString()}");
            }

            _ola.SetConfig($"{{\"WorkPath\":\"\",\"DbPath\":\"\",\"DbPassword\":\"\",\"InputLock\":true,\"EnableRealKeypad\":true,\"KeyDownInterval\":100,\"MouseClickInterval\":100,\"MouseDoubleClickInterval\":100,\"VncServer\":\"127.0.0.1\",\"VncPort\":\"5900\",\"VncPassword\":\"\",\"SimModeType\":0,\"UseAbsoluteMove\":true,\"EnableRealMouse\":true,\"RealMouseMode\":1,\"MinMouseTrajectory\":50,\"RealMouseBaseTimePer100Pixels\":200,\"RealMouseFlowFlag\":767,\"RealMouseNoise\":5.0,\"RealMouseDeviation\":25,\"RealMouseMinSteps\":150,\"RealMouseTimeToSteps\":1.5,\"RealMouseOvershoots\":3,\"MouseDriftCheckTime\":0,\"MaxOverlap\":0.5,\"MatchColorWeight\":0.7,\"CheckDisplayDeadInterval\":50,\"KeyboardHwnd\":0,\"MouseHwnd\":0}}");

            _version = _ola.Ver();
            _isInitialized = true;
        }

        private void ThrowIfNotBound()
        {
            if (!_isBound || _ola is null)
            {
                throw new InvalidOperationException("请先绑定外部窗口后再执行任务。");
            }
        }

        private void Cleanup()
        {
            if (_ola is null)
            {
                return;
            }

            try
            {
                if (_isBound)
                {
                    _ola.UnBindWindow();
                }
            }
            catch
            {
            }

            try
            {
                _ola.ReleaseObj();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Dispose(waitForExit: true);
        }

        public void Abandon()
        {
            Dispose(waitForExit: false);
        }

        private void Dispose(bool waitForExit)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _ready.Wait();

            try
            {
                var dispatcher = _dispatcher;
                if (dispatcher is not null && !dispatcher.IsDisposed)
                {
                    dispatcher.BeginInvoke(new MethodInvoker(() => _applicationContext?.ExitThread()));
                }
            }
            catch
            {
            }

            if (waitForExit)
            {
                _thread.Join();
            }

            _ready.Dispose();
        }
    }
}
