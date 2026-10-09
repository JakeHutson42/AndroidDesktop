using AndroidDesktop.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace AndroidDesktop.Adapters.Viewport;

/// <summary>Experimental host for an explicitly owned emulator window. Never owns its lifetime.</summary>
public sealed class NativeEmulatorHost : HwndHost
{
    private IntPtr _container, _child, _originalParent;
    private long _originalStyle, _originalExtendedStyle;
    private RectI _originalBounds;
    private ActiveDeviceLease.Owner? _owner;
    private static readonly WindowProc ContainerProc = PaintContainer;
    private static ushort _containerClass;
    private double _portraitAspect = 9d / 16;
    private bool _presented;
    private IntPtr _parentHook, _showHook;
    private WinEventProc? _windowEvents;
    public int ContainmentRecoveries { get; private set; }
    public bool VisibleBeforeAttach { get; private set; }
    public int VisibleDetachedEvents { get; private set; }
    public bool DiagnosticKeyboardSubmitted { get; private set; }
    public string DiagnosticKeyboardSuppression { get; private set; } = "";
    public int ForwardedKeyDowns { get; private set; }
    public int ForwardedKeyUps { get; private set; }
    public NativeInputClient? Input { get; private set; }
    private bool _pointerDown;
    private int _rotation;
    public async Task ConnectInputAsync(int port, string token, CancellationToken ct)
    {
        if (Input is not null) await DisconnectInputAsync();
        Input = await NativeInputClient.ConnectAsync(port, token, ct);
        Input.Fault += InputFault;
        // Disabled Qt children remain visible, but mouse hit testing falls through
        // to the owned WPF container. This is the only input delivery path.
        EnableWindow(_child, false);
        _portraitAspect = Input.Width / (double)Input.Height;
        _aspect = _rotation % 180 == 0 ? _portraitAspect : 1 / _portraitAspect;
        _last = default; ResizeChild();
    }
    public event Action<string>? InputFailed;
    private void InputFault(string message) => Dispatcher.BeginInvoke(() => InputFailed?.Invoke(message));
    public async Task DisconnectInputAsync()
    {
        var input = Input; Input = null;
        if (input is not null) { input.Fault -= InputFault; await input.DisposeAsync(); }
    }
    public void ReleaseInput()
    {
        Input?.ReleaseAll(); _pointerDown = false;
        if (GetCapture() == _container) ReleaseCapture();
    }
    protected override bool TranslateAcceleratorCore(ref MSG msg, System.Windows.Input.ModifierKeys modifiers)
        => HandleKey((uint)msg.message, msg.wParam, msg.lParam);
    private bool HandleKey(uint message, IntPtr key, IntPtr flags)
    {
        if (Input is null || message is not (0x100 or 0x101 or 0x104 or 0x105)) return false;
        var code = key.ToInt32();
        if (code is 0x7A or 0x1B) {
            // F11 and Escape belong to the application, including native focus.
            if (message is 0x100 or 0x104 && (flags.ToInt64() & (1L << 30)) == 0) {
                ReleaseInput();
                var window = Window.GetWindow(this);
                var routed = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,
                    PresentationSource.FromVisual(this), Environment.TickCount,
                    code == 0x7A ? System.Windows.Input.Key.F11 : System.Windows.Input.Key.Escape)
                    { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent };
                window?.RaiseEvent(routed);
            }
            return true;
        }
        // Alt+F4 keeps standard Windows close behavior.
        if (code == 0x73 && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0) return false;
        var scan = (int)((flags.ToInt64() >> 16) & 0xff);
        if ((flags.ToInt64() & (1L << 24)) != 0) scan |= 0xE000;
        if (scan == 0) return false;
        Input.Key(scan, message is 0x100 or 0x104);
        if (message is 0x100 or 0x104) ForwardedKeyDowns++; else ForwardedKeyUps++;
        return true;
    }
    protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (hwnd == _container && Input is not null) {
            if (HandleKey((uint)msg, wParam, lParam)) { handled = true; return IntPtr.Zero; }
            if (msg is 0x8 or 0x1F) ReleaseInput(); // focus loss / cancel mode
            if (msg == 0x215 && wParam != _container) ReleaseInput();
            if (msg is 0x201 or 0x202 or 0x200) {
                if (msg == 0x201) {
                    var clickX = (short)(lParam.ToInt64() & 0xffff); var clickY = (short)((lParam.ToInt64() >> 16) & 0xffff);
                    GetClientRect(_container, out var area); var fitted = Fit(area.Right, area.Bottom, _aspect);
                    if (clickX < fitted.X || clickX >= fitted.X + fitted.Width || clickY < fitted.Y || clickY >= fitted.Y + fitted.Height) { handled = true; return IntPtr.Zero; }
                    FocusNative(); SetCapture(_container); _pointerDown = true;
                }
                if (_pointerDown) {
                    var x = (short)(lParam.ToInt64() & 0xffff); var y = (short)((lParam.ToInt64() >> 16) & 0xffff);
                    GetClientRect(_container, out var client); var fit = Fit(client.Right, client.Bottom, _aspect);
                    if (fit.Width > 0 && fit.Height > 0)
                        Input.Pointer((x - fit.X) / (double)fit.Width, (y - fit.Y) / (double)fit.Height, _rotation, msg != 0x202, move: msg == 0x200);
                    if (msg == 0x202) { _pointerDown = false; ReleaseCapture(); }
                }
                handled = true;
            }
        }
        return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
    }
    public int ResizeCount { get; private set; }
    public bool Attached => _child != IntPtr.Zero && IsWindow(_child) && GetParent(_child) == _container;
    public uint ChildDpi => _child == IntPtr.Zero ? 0 : GetDpiForWindow(_child);
    public event Action<string>? Status;
    public static (int X, int Y, int Width, int Height) Fit(int width, int height, double aspect)
    {
        if (width <= 0 || height <= 0 || !double.IsFinite(aspect) || aspect <= 0) return (0, 0, 0, 0);
        var fittedWidth = Math.Min(width, Math.Max(1, (int)Math.Round(height * aspect)));
        var fittedHeight = Math.Min(height, Math.Max(1, (int)Math.Round(fittedWidth / aspect)));
        return ((width - fittedWidth) / 2, (height - fittedHeight) / 2, fittedWidth, fittedHeight);
    }
    private double _aspect = 9d / 16;
    private (int X, int Y, int Width, int Height) _last;
    protected override HandleRef BuildWindowCore(HandleRef parent)
    {
        if (_containerClass == 0) {
            var cls = new WindowClass { Procedure = ContainerProc, ClassName = "AndroidDesktop.NativeViewport" };
            _containerClass = RegisterClass(ref cls);
            if (_containerClass == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        _container = CreateWindowEx(0, "AndroidDesktop.NativeViewport", "", 0x50000000 | 0x02000000 | 0x04000000,
            0, 0, 1, 1, parent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (_container == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new HandleRef(this, _container);
    }
    protected override void DestroyWindowCore(HandleRef hwnd) { Detach(); DestroyWindow(hwnd.Handle); _container = IntPtr.Zero; }
    protected override void OnWindowPositionChanged(Rect rectangle) { base.OnWindowPositionChanged(rectangle); ResizeChild(); }
    protected override bool TabIntoCore(System.Windows.Input.TraversalRequest request) => FocusNative();
    public async Task AttachAsync(Func<IReadOnlyList<ActiveDeviceLease.Owner>> owners, CancellationToken token)
    {
        Detach();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            token.ThrowIfCancellationRequested();
            if (_container != IntPtr.Zero)
            {
                var allowed = owners(); IntPtr candidate = IntPtr.Zero; long area = 0;
                EnumWindows((window, _) => {
                    GetWindowThreadProcessId(window, out var pid);
                    var owner = allowed.FirstOrDefault(o => o.Pid == pid);
                    if (owner is null || !Matches(owner)) return true;
                    var name = new StringBuilder(256); GetClassName(window, name, name.Capacity);
                    if (!name.ToString().StartsWith("Qt", StringComparison.Ordinal)) return true;
                    GetClientRect(window, out var rect); var size = (long)rect.Right * rect.Bottom;
                    if (rect.Right >= 200 && rect.Bottom >= 200 && size > area) { candidate = window; area = size; }
                    return true;
                }, IntPtr.Zero);
                if (candidate != IntPtr.Zero)
                {
                    VisibleBeforeAttach = IsWindowVisible(candidate);
                    GetWindowThreadProcessId(candidate, out var pid);
                    _owner = allowed.First(o => o.Pid == pid);
                    // Cross-process SetParent can reset the child's DPI mode. Reject
                    // incompatible contexts instead of silently accepting that reset.
                    if (!AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(_container), GetWindowDpiAwarenessContext(candidate)))
                        throw new InvalidOperationException("Native window DPI awareness differs from the WPF host. Use controller display while native DPI integration is evaluated.");
                    _child = candidate; _originalParent = GetParent(candidate);
                    _originalStyle = GetWindowLongPtr(candidate, -16).ToInt64();
                    _originalExtendedStyle = GetWindowLongPtr(candidate, -20).ToInt64();
                    GetWindowRect(candidate, out _originalBounds); GetClientRect(candidate, out var client);
                    // Qt can restore a stale desktop-window shape. The authenticated display configuration sets the actual panel aspect at connection.
                    ShowWindow(candidate, 0);
                    SetWindowLongPtr(candidate, -16, new IntPtr((_originalStyle & ~0x80CF0000L) | 0x40000000L));
                    SetWindowLongPtr(candidate, -20, new IntPtr(_originalExtendedStyle & ~0x00040000L));
                    Marshal.SetLastPInvokeError(0); var previous = SetParent(candidate, _container);
                    var parentError = Marshal.GetLastWin32Error();
                    if (previous == IntPtr.Zero && parentError != 0) { Detach(); throw new Win32Exception(parentError); }
                    _last = default; ResizeChild();
                    _windowEvents = ContainWindow;
                    _parentHook = SetWinEventHook(0x800F, 0x800F, IntPtr.Zero, _windowEvents, (uint)pid, 0, 0);
                    _showHook = SetWinEventHook(0x8002, 0x8002, IntPtr.Zero, _windowEvents, (uint)pid, 0, 0);
                    Status?.Invoke("Native emulator window attached; no screenshot transport.");
                    return;
                }
            }
            await Task.Delay(100, token);
        }
        throw new TimeoutException("No Qt display window belonging to the owned emulator was found. Stop the device and select controller display.");
    }
    public void Present() { _presented = true; if (Attached) { ShowWindow(_child, 4); ResizeChild(); } }
    private void ContainWindow(IntPtr hook, uint evt, IntPtr hwnd, int objectId, int childId, uint thread, uint time)
    {
        if (hwnd != _child || objectId != 0 || _container == IntPtr.Zero || _owner is null || !Matches(_owner)) return;
        if (GetParent(hwnd) != _container) {
            if (IsWindowVisible(hwnd)) VisibleDetachedEvents++;
            ShowWindow(hwnd, 0);
            SetWindowLongPtr(hwnd, -16, new IntPtr((_originalStyle & ~0x90CF0000L) | 0x40000000L));
            SetParent(hwnd, _container); _last = default; ResizeChild(); ContainmentRecoveries++;
            if (_presented) ShowWindow(hwnd, 4);
        } else if (!_presented) ShowWindow(hwnd, 0);
    }
    public void RefreshBackground() { if (_container != IntPtr.Zero) InvalidateRect(_container, IntPtr.Zero, true); }
    public bool BackgroundMatchesTheme() {
        if (_container == IntPtr.Zero) return false;
        var dc = GetDC(_container);
        try {
            var color = (Application.Current.TryFindResource("ViewportBrush") as System.Windows.Media.SolidColorBrush)!.Color;
            return GetPixel(dc, 2, 2) == (uint)(color.R | color.G << 8 | color.B << 16);
        } finally { ReleaseDC(_container, dc); }
    }
    public void SetRotation(int degrees) {
        ReleaseInput(); _rotation = degrees;
        _aspect = degrees % 180 == 0 ? _portraitAspect : 1 / _portraitAspect;
        _last = default; ResizeChild();
    }
    public void Suspend() {
        ReleaseInput();
        _presented = false;
        if (_child != IntPtr.Zero && IsWindow(_child) && _owner is not null && Matches(_owner)) ShowWindow(_child, 0);
    }
    public void ResizeChild()
    {
        if (!Attached || _owner is null || !Matches(_owner)) return;
        GetClientRect(_container, out var client);
        var target = Fit(client.Right, client.Bottom, _aspect);
        if (target.Width == 0 || (target == _last && GeometryMatches())) return;
        if (!SetWindowPos(_child, IntPtr.Zero, target.X, target.Y, target.Width, target.Height, 0x0014))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        _last = target; ResizeCount++;
    }
    public object GeometrySnapshot()
    {
        GetClientRect(_container, out var client); GetWindowRect(_child, out var child);
        var origin = new PointI(); ClientToScreen(_container, ref origin); var target = Fit(client.Right, client.Bottom, _aspect);
        return new { containerWidth = client.Right, containerHeight = client.Bottom, x = child.Left - origin.X, y = child.Top - origin.Y,
            width = child.Right-child.Left, height = child.Bottom-child.Top, targetX=target.X, targetY=target.Y, targetWidth=target.Width,targetHeight=target.Height };
    }
    public bool GeometryMatches()
    {
        if (!Attached) return false;
        GetClientRect(_container, out var client); GetWindowRect(_child, out var child);
        var origin = new PointI(); ClientToScreen(_container, ref origin);
        var target = Fit(client.Right, client.Bottom, _aspect);
        return child.Left == origin.X + target.X && child.Top == origin.Y + target.Y
            && child.Right - child.Left == target.Width && child.Bottom - child.Top == target.Height;
    }
    public bool FocusNative()
    {
        if (!Attached || Input is null) return false;
        SetFocus(_container);
        return GetFocus() == _container;
    }
    public void DiagnosticKey(int scanCode, bool down)
    {
        if (!Attached || Input is null) throw new InvalidOperationException("Owned native input unavailable.");
        FocusNative();
        var flags = (long)(scanCode & 0xff) << 16;
        if ((scanCode & 0xE000) != 0) flags |= 1L << 24;
        if (!down) flags |= 3L << 30;
        PostMessage(_container, down ? 0x100u : 0x101u, new IntPtr(0x41), new IntPtr(flags));
    }
    public void DiagnosticLoseFocus() => SetFocus(GetAncestor(_container, 2));
    public async Task DiagnosticPointerAsync(double x, double y, bool focusRelease = false)
    {
        if (!Attached || Input is null) throw new InvalidOperationException("Owned native input unavailable.");
        GetClientRect(_container, out var bounds); var fit = Fit(bounds.Right, bounds.Bottom, _aspect);
        var coordinates = new IntPtr(((fit.Y + (int)(fit.Height * y)) << 16) | ((fit.X + (int)(fit.Width * x)) & 0xffff));
        PostMessage(_container, 0x201, new IntPtr(1), coordinates); await Task.Delay(80);
        if (focusRelease) DiagnosticLoseFocus(); else PostMessage(_container, 0x202, IntPtr.Zero, coordinates);
        await Task.Delay(150);
    }
    public async Task DiagnosticInputAsync()
    {
        if (!Attached || _owner is null || !Matches(_owner)) throw new InvalidOperationException("Owned native window unavailable.");
        FocusNative(); GetClientRect(_container, out var bounds);
        var fit = Fit(bounds.Right, bounds.Bottom, _aspect);
        var coordinates = new IntPtr(((fit.Y + fit.Height / 2) << 16) | ((fit.X + fit.Width / 2) & 0xffff));
        PostMessage(_container, 0x201, new IntPtr(1), coordinates);
        await Task.Delay(80);
        PostMessage(_container, 0x202, IntPtr.Zero, coordinates);
        // Qt does not necessarily process fabricated keyboard window messages.
        // Generate a navigation pair only when this application's root window is
        // foreground and the owned emulator has acquired focus. Never target another app.
        var root = GetAncestor(_child, 2);
        GetWindowThreadProcessId(root, out var rootPid);
        if (rootPid != Environment.ProcessId) { DiagnosticKeyboardSuppression = "Host root is not owned by this application"; return; }
        SetForegroundWindow(root); await Task.Delay(150);
        if (GetForegroundWindow() != root || !FocusNative()) { DiagnosticKeyboardSuppression = "Owned host could not acquire foreground/focus"; return; }
        var keys = new[] {
            new WinInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Scan = 0x50, Flags = 9 } } },
            new WinInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Scan = 0x50, Flags = 11 } } }
        };
        if (SendInput(1, [keys[0]], Marshal.SizeOf<WinInput>()) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error());
        // Let Qt observe the down state before releasing; always release generated input.
        try { await Task.Delay(80); }
        finally {
            DiagnosticKeyboardSubmitted = SendInput(1, [keys[1]], Marshal.SizeOf<WinInput>()) == 1;
            if (!DiagnosticKeyboardSubmitted) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
    public void Detach()
    {
        if (_parentHook != IntPtr.Zero) { UnhookWinEvent(_parentHook); _parentHook = IntPtr.Zero; }
        if (_showHook != IntPtr.Zero) { UnhookWinEvent(_showHook); _showHook = IntPtr.Zero; }
        Suspend();
        if (_child != IntPtr.Zero && IsWindow(_child) && _owner is not null && Matches(_owner))
        {
            EnableWindow(_child, true);
            SetParent(_child, _originalParent);
            // Restore ownership without ever presenting a detached desktop window.
            SetWindowLongPtr(_child, -16, new IntPtr(_originalStyle & ~0x10000000L));
            SetWindowLongPtr(_child, -20, new IntPtr(_originalExtendedStyle));
            SetWindowPos(_child, IntPtr.Zero, _originalBounds.Left, _originalBounds.Top,
                _originalBounds.Right - _originalBounds.Left, _originalBounds.Bottom - _originalBounds.Top, 0x0014 | 0x0020);
        }
        _child = IntPtr.Zero; _owner = null;
        _windowEvents = null;
    }
    private static IntPtr PaintContainer(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == 0x14) {
            var color = (Application.Current?.TryFindResource("ViewportBrush") as System.Windows.Media.SolidColorBrush)?.Color
                ?? System.Windows.Media.Color.FromRgb(16, 18, 22);
            var brush = CreateSolidBrush((uint)(color.R | color.G << 8 | color.B << 16));
            try { GetClientRect(hwnd, out var rect); FillRect(wParam, ref rect, brush); }
            finally { DeleteObject(brush); }
            return new IntPtr(1);
        }
        return DefWindowProc(hwnd, message, wParam, lParam);
    }
    private delegate IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    private delegate void WinEventProc(IntPtr hook, uint evt, IntPtr hwnd, int objectId, int childId, uint thread, uint time);
    [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint first, uint last, IntPtr module, WinEventProc callback, uint pid, uint thread, uint flags);
    [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, bool erase);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern uint GetPixel(IntPtr dc, int x, int y);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct WindowClass {
        public uint Style; public WindowProc Procedure; public int ClassExtra, WindowExtra;
        public IntPtr Instance, Icon, Cursor, Background;
        public string? MenuName; public string ClassName;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClass(ref WindowClass cls);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr dc, ref RectI rect, IntPtr brush);
    private static bool Matches(ActiveDeviceLease.Owner owner)
    {
        try { using var process = Process.GetProcessById(owner.Pid); return !process.HasExited && process.StartTime.ToUniversalTime().Ticks == owner.StartedUtcTicks; }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or Win32Exception) { return false; }
    }
    [StructLayout(LayoutKind.Sequential)] private struct RectI { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct PointI { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct GuiThreadInfo { public int Size, Flags; public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret; public RectI CaretRect; }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Explicit)] private struct InputData { [FieldOffset(0)] public KeyboardInput Keyboard; [FieldOffset(0)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct WinInput { public uint Type; public InputData Data; }
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, WinInput[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref PointI point);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint first, uint second, bool attach);
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
    [DllImport("user32.dll")] private static extern bool IsChild(IntPtr parent, IntPtr child);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hwnd, ref PointI point);
    [DllImport("user32.dll")] private static extern IntPtr ChildWindowFromPointEx(IntPtr hwnd, PointI point, uint flags);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    private delegate bool EnumProc(IntPtr hwnd, IntPtr value);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWindowEx(int ex, string cls, string text, int style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc callback, IntPtr param);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out int pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int count);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out RectI rect);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RectI rect);
    [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetParent(IntPtr hwnd, IntPtr parent);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int w, int h, uint flags);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);
    [DllImport("user32.dll")] private static extern bool EnableWindow(IntPtr hwnd, bool enable);
    [DllImport("user32.dll")] private static extern IntPtr GetFocus();
    [DllImport("user32.dll")] private static extern IntPtr SetCapture(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr GetCapture();
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hwnd);
}
