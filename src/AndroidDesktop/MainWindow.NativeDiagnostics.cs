using AndroidDesktop.Adapters.Viewport;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace AndroidDesktop;
public partial class MainWindow
{
    public async Task<object> CheckNativeDpiAsync(NativeEmulatorHost host)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var positions = new List<(IntPtr Handle, MonitorInfo Info)>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr dc, ref MonitorRect rect, IntPtr data) => {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref info)) positions.Add((monitor, info)); return true;
        }, IntPtr.Zero);
        GetWindowRect(hwnd, out var original);
        var monitors = new List<object>(); var scales = new List<object>();
        try {
            foreach (var monitor in positions) {
                SetWindowPos(hwnd, IntPtr.Zero, monitor.Info.Work.Left + 24, monitor.Info.Work.Top + 24, 1000, 720, 0x14);
                await Task.Delay(600); UpdateLayout(); host.ResizeChild();
                monitors.Add(new { monitor.Info.Device, hostDpi = GetDpiForWindow(hwnd), childDpi = host.ChildDpi, host.Attached, geometry = host.GeometryMatches() });
            }
            foreach (var scale in new[] { 1d, 1.25, 1.5, 2 }) {
                ShellRoot.LayoutTransform = new ScaleTransform(scale, scale);
                UpdateLayout(); await Task.Delay(200); host.ResizeChild();
                scales.Add(new { scale, host.Attached, geometry = host.GeometryMatches(), bounds = host.GeometrySnapshot(), hostDpi = GetDpiForWindow(hwnd), childDpi = host.ChildDpi });
            }
        } finally {
            ShellRoot.LayoutTransform = Transform.Identity;
            SetWindowPos(hwnd, IntPtr.Zero, original.Left, original.Top, original.Right-original.Left, original.Bottom-original.Top, 0x14);
            UpdateLayout(); host.ResizeChild();
        }
        return new { monitors, simulatedLayoutScales = scales, note = "Layout transforms are not physical Windows DPI settings. Only monitor entries represent real DPI." };
    }
    public async Task<object> CheckTabHoverAsync(NativeEmulatorHost host)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var selected = 0; var unloaded = 0; var layout = 0; var transitions = 0;
        var resizeBefore = host.ResizeCount;
        SelectionChangedEventHandler selection = (_, e) => { if (ReferenceEquals(e.OriginalSource, ShellNavigation)) selected++; };
        RoutedEventHandler unload = (_, _) => unloaded++;
        EventHandler layoutHandler = (_, _) => layout++;
        ShellNavigation.SelectionChanged += selection; ViewportHost.Unloaded += unload; ShellNavigation.LayoutUpdated += layoutHandler;
        GetCursorPos(out var original);
        var skipped = false;
        try {
            Activate(); SetForegroundWindow(hwnd); await Task.Delay(150);
            for (var i = 0; i < 60; i++) {
                if (GetForegroundWindow() != hwnd) { skipped = true; break; }
                var tab = (TabItem)ShellNavigation.Items[i % ShellNavigation.Items.Count];
                var point = tab.PointToScreen(new Point(tab.ActualWidth / 2, tab.ActualHeight / 2));
                SetCursorPos((int)point.X, (int)point.Y);
                await Task.Delay(30);
                if (tab.IsMouseOver) transitions++;
            }
        } finally {
            if (GetForegroundWindow() == hwnd) SetCursorPos(original.X, original.Y);
            ShellNavigation.SelectionChanged -= selection; ViewportHost.Unloaded -= unload; ShellNavigation.LayoutUpdated -= layoutHandler;
        }
        return new { selected, unloaded, layoutNotifications = layout, hoverReceipts = transitions, skipped, resizeCalls = host.ResizeCount - resizeBefore, host.Attached, geometry = host.GeometryMatches(), note = "Counters detect churn; they do not prove absence of visual flicker." };
    }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] private struct MonitorInfo { public int Size; public MonitorRect Bounds, Work; public uint Flags; [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string Device; }
    private delegate bool MonitorProc(IntPtr monitor, IntPtr dc, ref MonitorRect rect, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorProc callback, IntPtr data);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out MonitorRect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [StructLayout(LayoutKind.Sequential)] private struct CursorPoint { public int X, Y; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out CursorPoint point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
}
