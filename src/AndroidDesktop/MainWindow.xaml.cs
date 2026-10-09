using AndroidDesktop.Adapters.Viewport;
using AndroidDesktop.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AndroidDesktop.Models;
using System.Windows.Navigation;
using System.Diagnostics;
namespace AndroidDesktop;
public partial class MainWindow : Window
{
 private readonly PrototypeViewModel _viewModel; private readonly IDeviceViewport _viewport;
 private readonly DesktopViewModel _desktop;
 private bool _restoring;
 private bool _canClose, _closing, _fullscreen;
 private WindowState _savedState; private WindowStyle _savedStyle;
 public MainWindow(DesktopViewModel desktop, IDeviceViewport viewport)
 {
  _desktop = desktop; _viewModel = desktop.Device; _viewport = viewport;
  InitializeComponent(); DataContext = desktop; ViewportHost.Content = viewport.Control;
  Loaded += (_, _) => UpdateWorkspaceLayout();
  ShellRoot.SizeChanged += (_, _) => UpdateWorkspaceLayout();
  SourceInitialized += (_, _) => ApplyTitleBarTheme();
  desktop.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(desktop.Theme)) { ApplyTitleBarTheme(); UpdateWorkspaceLayout(); (viewport as WebRtcViewport)?.NativeHost?.RefreshBackground(); } };
  _viewModel.ExitFullscreen += () => { if (_fullscreen) ToggleFullscreen(); };
  desktop.RestoreBounds += RestoreWindow;
  desktop.ShowSetup += show => ShellNavigation.SelectedIndex = show ? 1 : 0;
 }
 private async void OnClosing(object? sender, CancelEventArgs e)
 {
  if (_canClose) return; e.Cancel = true; if (_closing) return; _closing = true;
  try { await _desktop.CancelAndWaitAsync(); var stopped = await _viewModel.CloseAsync(); SaveWindowBounds(); await _desktop.FlushAsync(); if (stopped) { await _viewport.DisposeAsync(); _canClose = true; _ = Dispatcher.BeginInvoke(Close); } }
  finally { _closing = false; }
 }
 private void OnDeactivated(object? sender, EventArgs e) => _viewModel.ReleaseAllInput();
 private void ApplyTitleBarTheme()
 {
  var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
  if (handle == IntPtr.Zero) return;
  var dark = 1; _ = DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
  var palette = AndroidDesktop.Services.ThemeService.GetPalette(_desktop.Theme);
  foreach (var (attribute, key) in new[] { (35, "PanelBrush"), (36, "TextPrimaryBrush"), (34, "BorderBrush") })
  {
   var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(palette[key]);
   var value = color.R | color.G << 8 | color.B << 16;
   _ = DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
  }
 }
 [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
 private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
 private void OnPageChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
 {
  if (ReferenceEquals(e.OriginalSource, ShellNavigation)) _viewModel.ReleaseAllInput();
 }
 private void OnFullscreen(object sender, RoutedEventArgs e) => ToggleFullscreen();
 private void OnApkDragOver(object sender, DragEventArgs e)
 {
  e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && _viewModel.CanAcceptDrop ? DragDropEffects.Copy : DragDropEffects.None;
  e.Handled = true;
 }
 private async void OnApkDrop(object sender, DragEventArgs e)
 {
  e.Handled = true;
  if (!_viewModel.CanAcceptDrop) { _viewModel.AddMessage("Wait for the current operation or cancel it before opening another APK."); return; }
  if (e.Data.GetData(DataFormats.FileDrop) is string[] files) await _viewModel.ImportFilesCommand.ExecuteAsync(files);
  else _viewModel.AddMessage("Drop one APK or APKM bundle from Explorer.");
 }
 private void OnKeyDown(object sender, KeyEventArgs e)
 {
  if (e.Key == Key.F11) { ToggleFullscreen(); e.Handled = true; }
  if (e.Key == Key.Escape) { _viewModel.ReleaseAllInput(); if (_fullscreen) ToggleFullscreen(); e.Handled = true; }
 }
 private void ToggleFullscreen()
 {
  _viewModel.ReleaseAllInput();
  if (!_fullscreen) { SaveWindowBounds(); _fullscreen = true; _savedStyle = WindowStyle; _savedState = WindowState; WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; ShellNavigation.SelectedIndex = 0; }
  else { WindowState = WindowState.Normal; WindowStyle = _savedStyle; WindowState = _savedState; _fullscreen = false; SaveWindowBounds(); }
  UpdateWorkspaceLayout();
 }
 private bool _libraryOpen, _automationOpen;
 private int _layoutMode = -1;
 private void OnProfileSettings(object sender, RoutedEventArgs e) => ShellNavigation.SelectedIndex = 1;
 public async Task<bool> CheckNativeHostAsync(AndroidDesktop.Adapters.Viewport.NativeEmulatorHost host)
 {
  var savedWidth = Width; var savedHeight = Height; var savedPage = ShellNavigation.SelectedIndex;
  var retained = true;
  try {
   for (var cycle = 0; cycle < 5; cycle++)
   foreach (var size in new[] { (850, 600), (1180, 800), (1440, 900) }) {
    Width = size.Item1; Height = size.Item2; UpdateLayout();
    await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    retained &= host.Attached && host.GeometryMatches();
    for (var page = 0; page < 3; page++) { ShellNavigation.SelectedIndex = page; UpdateLayout(); retained &= host.Attached && host.GeometryMatches(); }
   }
   ShellNavigation.SelectedIndex = 0; ToggleFullscreen(); UpdateLayout(); retained &= host.Attached && host.GeometryMatches();
   ToggleFullscreen(); UpdateLayout(); retained &= host.Attached && host.GeometryMatches();
   WindowState = WindowState.Minimized; await Task.Delay(500); WindowState = WindowState.Normal; UpdateLayout(); retained &= host.Attached && host.GeometryMatches();
   return retained;
  } finally { Width = savedWidth; Height = savedHeight; ShellNavigation.SelectedIndex = savedPage; UpdateLayout(); }
 }
 private void OnLibraryToggle(object sender, RoutedEventArgs e)
 {
  _libraryOpen = !_libraryOpen;
  if (_layoutMode == 2 && _libraryOpen) _automationOpen = false;
  _viewModel.ReleaseAllInput(); UpdateWorkspaceLayout();
  if (!_libraryOpen) LibraryToggle.Focus();
 }
 private void OnAutomationToggle(object sender, RoutedEventArgs e)
 {
  _automationOpen = !_automationOpen;
  if (_layoutMode == 2 && _automationOpen) _libraryOpen = false;
  _viewModel.ReleaseAllInput(); UpdateWorkspaceLayout();
  if (!_automationOpen) AutomationToggle.Focus();
 }
 private void UpdateWorkspaceLayout()
 {
  if (LibraryColumn is null) return;
  var width = ShellRoot.ActualWidth > 0 ? ShellRoot.ActualWidth : Width;
  var mode = width >= 1280 ? 0 : width >= 1050 ? 1 : 2;
  if (mode != _layoutMode)
  {
   _libraryOpen = false; _automationOpen = false; _layoutMode = mode;
   _viewModel.ReleaseAllInput();
  }
  var library = !_fullscreen && (mode < 2 || _libraryOpen);
  var automation = !_fullscreen && (mode == 0 || _automationOpen);
  // A drawer occupies a real Grid column: never overlay the native WebView HWND.
  LibraryColumn.Width = new GridLength(library ? mode == 0 ? 260 : 236 : 0);
  AutomationColumn.Width = new GridLength(automation ? 280 : 0);
  LibraryPanel.Visibility = library ? Visibility.Visible : Visibility.Collapsed;
  AutomationPanel.Visibility = automation ? Visibility.Visible : Visibility.Collapsed;
  DisplayArea.Margin = new Thickness(0, 0, automation ? 16 : 0, 0);
  PanelToggles.Visibility = !_fullscreen && mode > 0 ? Visibility.Visible : Visibility.Collapsed;
  LibraryToggle.Visibility = mode == 2 ? Visibility.Visible : Visibility.Collapsed;
  LibraryToggle.Content = _libraryOpen ? "Close APK library" : "APK library";
  AutomationToggle.Content = _automationOpen ? "Close automation" : "Automation";
  DisplayHeading.Visibility = _fullscreen ? Visibility.Collapsed : Visibility.Visible;
  var largeText = FontSize > 21;
  var compact = width < 850;
  HostHeader.Visibility = _fullscreen || largeText || compact ? Visibility.Collapsed : Visibility.Visible;
  ShellNavigation.Tag = _fullscreen ? "Fullscreen" : largeText ? "LargeText" : compact ? "Compact" : null;
  DisplayDetailsLabel.Visibility = !_fullscreen && !largeText ? Visibility.Visible : Visibility.Collapsed;
 }
 private void OnBoundsChanged(object? sender, EventArgs e)
 {
  if (IsLoaded) UpdateWorkspaceLayout();
  if (IsLoaded && !_restoring) SaveWindowBounds();
 }
 private void SaveWindowBounds()
 {
  if (_fullscreen || _desktop is null || WindowState == WindowState.Minimized) return;
  var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
  if (!bounds.IsEmpty && bounds.Width >= MinWidth && bounds.Height >= MinHeight)
   _desktop.UpdateBounds(new(bounds.Left, bounds.Top, bounds.Width, bounds.Height, WindowState == WindowState.Maximized));
 }
 private void RestoreWindow(WindowBounds bounds)
 {
  _restoring = true;
  try
  {
   var width = double.IsFinite(bounds.Width) ? Math.Max(MinWidth, bounds.Width) : 1180;
   var height = double.IsFinite(bounds.Height) ? Math.Max(MinHeight, bounds.Height) : 800;
   Width = Math.Min(width, SystemParameters.VirtualScreenWidth); Height = Math.Min(height, SystemParameters.VirtualScreenHeight);
   Left = double.IsFinite(bounds.Left) ? bounds.Left : 100; Top = double.IsFinite(bounds.Top) ? bounds.Top : 100;
   // MonitorFromRect checks actual monitor rectangles rather than gaps in the virtual desktop.
   var pixel = new MonitorRect();
   var source = PresentationSource.FromVisual(this); var scale = source?.CompositionTarget?.TransformToDevice ?? System.Windows.Media.Matrix.Identity;
   pixel.Left = (int)(Left * scale.M11); pixel.Top = (int)(Top * scale.M22);
   pixel.Right = pixel.Left + (int)(Width * scale.M11); pixel.Bottom = pixel.Top + (int)(34 * scale.M22);
   if (MonitorFromRect(ref pixel, 0) == IntPtr.Zero)
   { Left = SystemParameters.WorkArea.Left + 24; Top = SystemParameters.WorkArea.Top + 24; }
   if (bounds.Maximized) WindowState = WindowState.Maximized;
  }
  finally { _restoring = false; }
 }
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
 private struct MonitorRect { public int Left, Top, Right, Bottom; }
 [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr MonitorFromRect(ref MonitorRect rect, uint flags);
 private void OnNavigate(object sender, RequestNavigateEventArgs e)
 {
  try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
  catch (Exception error) { _viewModel.AddMessage("Could not open setup documentation: " + error.Message); }
  e.Handled = true;
 }
 private void CaptureShell(string directory, string name)
 {
  System.IO.Directory.CreateDirectory(directory);
  UpdateLayout();
  var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(ShellRoot.ActualWidth), (int)Math.Ceiling(ShellRoot.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
  bitmap.Render(ShellRoot);
  var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
  encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
  using var stream = System.IO.File.Create(System.IO.Path.Combine(directory, name + ".png")); encoder.Save(stream);
 }
 public async Task<object> CheckShellSmokeAsync(string? previewDirectory = null)
 {
  _desktop.NewProfileName = "Isolated smoke profile";
  await _desktop.AddProfileCommand.ExecuteAsync(null);
  var added = _desktop.Profiles.Count == 2;
  var backupRequiresShutdown = !_desktop.BackupDeviceCommand.CanExecute(null) && !_desktop.RestoreDeviceCommand.CanExecute(null);
  _viewModel.PlaybackActive = true;
  var conflictingCommandsBlocked = !_desktop.SwitchProfileCommand.CanExecute(null) && !_desktop.AddProfileCommand.CanExecute(null);
  conflictingCommandsBlocked &= !_desktop.VerifyBackupCommand.CanExecute(null) && !_desktop.BackupDeviceCommand.CanExecute(null) && !_desktop.RestoreDeviceCommand.CanExecute(null);
  await _desktop.SwitchProfileCommand.ExecuteAsync(null);
  conflictingCommandsBlocked &= _desktop.ActiveProfileName == "Default device";
  _viewModel.PlaybackActive = false;
  await _desktop.SwitchProfileCommand.ExecuteAsync(null);
  var switchedProfile = _desktop.ActiveProfileName == "Isolated smoke profile" && _viewModel.Selection is null && _viewModel.Library.Count == 0;
  _desktop.SelectedProfile = _desktop.Profiles.Single(p => p.Id == "default");
  await _desktop.SwitchProfileCommand.ExecuteAsync(null);
  var defaultRestored = _desktop.ActiveProfileName == "Default device";
  await _desktop.CheckSetupCommand.ExecuteAsync(null);
  var viewportUnloads = 0;
  RoutedEventHandler unloaded = (_, _) => viewportUnloads++;
  _viewport.Control.Unloaded += unloaded;
  for (var cycle = 0; cycle < 20; cycle++)
  for (var page = 0; page < ShellNavigation.Items.Count; page++) {
   ShellNavigation.SelectedIndex = page;
   UpdateLayout();
   await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
  }
  _viewport.Control.Unloaded -= unloaded;
  var viewportRetainedAcrossNavigation = viewportUnloads == 0;
  if (!viewportRetainedAcrossNavigation) throw new InvalidOperationException("Shell navigation detached the viewport.");
  var pages = new List<string>();
  for (var i = 0; i < ShellNavigation.Items.Count; i++)
  {
   ShellNavigation.SelectedIndex = i; UpdateLayout();
   pages.Add(((System.Windows.Controls.TabItem)ShellNavigation.Items[i]).Header.ToString()!);
  }
  _desktop.Theme = "Arctic";
  var switched = ((System.Windows.Media.SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color.ToString() == "#FFB7F5FF";
  _desktop.Theme = "Graphite"; ShellNavigation.SelectedIndex = 0; UpdateLayout();
  await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
  // Render-only fixture exercises selected rows without importing an APK or saving settings.
  if (previewDirectory is not null)
  {
   var fixture = new ApkSelection(new ApkMetadata("diagnostic.apk", "com.androiddesktop.diagnostic", 26, ["x86_64"], "", "Diagnostic App", 1, "1.0"), "default", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
   _viewModel.RestoreLibrary([fixture]); _viewModel.LibrarySelection = fixture; _viewModel.RestoreSelection(fixture);
  }
  var adaptiveLayoutVerified = true;
  foreach (var size in new[] { (1440, 900), (1180, 800), (850, 600) })
  {
   Width = size.Item1; Height = size.Item2; UpdateLayout(); UpdateWorkspaceLayout(); UpdateLayout();
   adaptiveLayoutVerified &= DisplayArea.ActualWidth >= 350 && ViewportHost.ActualHeight > 150;
   if (previewDirectory is not null) CaptureShell(previewDirectory, $"device-{size.Item1}");
  }
  OnAutomationToggle(this, new RoutedEventArgs()); UpdateLayout();
  adaptiveLayoutVerified &= AutomationPanel.IsVisible && !LibraryPanel.IsVisible && DisplayArea.ActualWidth >= 350;
  if (previewDirectory is not null) CaptureShell(previewDirectory, "automation-850");
  OnLibraryToggle(this, new RoutedEventArgs()); UpdateLayout();
  adaptiveLayoutVerified &= LibraryPanel.IsVisible && !AutomationPanel.IsVisible;
  if (previewDirectory is not null) CaptureShell(previewDirectory, "library-850");
  Width = 1440; Height = 900; UpdateLayout(); UpdateWorkspaceLayout();
  if (previewDirectory is not null) {
   _viewModel.IsStarting = true; _viewModel.StartupProgress = 65;
   _viewModel.StartupMessage = "Waiting for Android to finish starting";
   _viewModel.StartupTiming = "About 12s remaining · 18s elapsed";
   for (var step = 0; step < 4; step++) _viewModel.StartupSteps[step].State = step < 2 ? "Complete" : step == 2 ? "Current" : "Waiting";
   UpdateLayout(); await Task.Delay(300); CaptureShell(previewDirectory, "startup-1440");
   Width = 850; Height = 600; UpdateLayout(); UpdateWorkspaceLayout(); UpdateLayout(); CaptureShell(previewDirectory, "startup-850");
   _viewModel.IsStarting = false; Width = 1440; Height = 900; UpdateLayout(); UpdateWorkspaceLayout();
  }
  for (var i = 1; i < ShellNavigation.Items.Count; i++)
  {
   ShellNavigation.SelectedIndex = i; UpdateLayout();
   if (previewDirectory is not null) CaptureShell(previewDirectory, i == 1 ? "setup" : "settings");
  }
  ShellNavigation.SelectedIndex = 0;
  ToggleFullscreen(); UpdateLayout();
  adaptiveLayoutVerified &= !LibraryPanel.IsVisible && !AutomationPanel.IsVisible && !HostHeader.IsVisible;
  if (previewDirectory is not null) CaptureShell(previewDirectory, "fullscreen");
  ToggleFullscreen();
  if (previewDirectory is not null)
  {
   Width = 850; Height = 600;
   foreach (var (name, size) in new[] { ("BodyFontSize", 28.0), ("SmallFontSize", 26.0), ("SectionFontSize", 32.0), ("HeadingFontSize", 36.0), ("PageFontSize", 40.0) })
    Application.Current.Resources[name] = size;
   UpdateLayout(); UpdateWorkspaceLayout();
   await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
   CaptureShell(previewDirectory, "text-scale-200");
   OnAutomationToggle(this, new RoutedEventArgs()); UpdateLayout();
   CaptureShell(previewDirectory, "automation-text-scale-200");
   OnLibraryToggle(this, new RoutedEventArgs()); UpdateLayout();
   CaptureShell(previewDirectory, "library-text-scale-200");
   adaptiveLayoutVerified &= ApkLibraryList.ActualHeight > 40;
   LibraryScroll.ScrollToVerticalOffset(220); UpdateLayout();
   CaptureShell(previewDirectory, "library-text-scale-200-scrolled");
   ShellNavigation.SelectedIndex = 1; UpdateLayout();
   CaptureShell(previewDirectory, "setup-text-scale-200");
   new AndroidDesktop.Services.ThemeService().Apply("Graphite");
   ShellNavigation.SelectedIndex = 0;
   _viewModel.RestoreSelection(null); _viewModel.RestoreLibrary([]);
  }
  RestoreWindow(new(double.NaN, double.PositiveInfinity, 100, 100));
  await _viewModel.ImportFilesAsync([System.IO.Path.Combine(AppContext.BaseDirectory, "__missing_import_smoke__.apk")], CancellationToken.None);
  UpdateLayout();
  var operationErrorPresented = !string.IsNullOrWhiteSpace(_viewModel.OperationError) && _viewModel.Status == _viewModel.OperationError;
  if (previewDirectory is not null) CaptureShell(previewDirectory, "import-error");
  _viewModel.OperationError = "";
  return new { pages, themeSwitchApplied = switched, adaptiveLayoutVerified, operationErrorPresented, minimumBoundsRestored = Width >= MinWidth && Height >= MinHeight,
   setupCheckComplete = !string.IsNullOrWhiteSpace(_desktop.SetupReport) && !_desktop.SetupBusy, viewportRetainedAcrossNavigation,
   isolatedProfileAdded = added, profileSwitched = switchedProfile, defaultProfileRestored = defaultRestored, conflictingProfileCommandsBlocked = conflictingCommandsBlocked,
   backupRequiresGracefulShutdown = backupRequiresShutdown };
 }
}
