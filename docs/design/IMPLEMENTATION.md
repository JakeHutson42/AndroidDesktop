# Graphite theme implementation

Implemented 7 October 2026 in the rebuilt WPF app. Start using `scripts/Start-LocalAndroidApp.ps1` or `dotnet run --project src/AndroidDesktop -c Release`. Historical installer and publish artifacts have not been regenerated.

## What changed

- A semantic theme system supplies neutral surfaces, readable text, blue primary actions, interaction states, selection, focus, status colors and viewport letterboxing. Graphite is the new default; the old default Cyberpunk setting migrates to Graphite on initialization. Other existing palette names remain available with neutral surfaces and softer accent variations.
- Shared control templates cover buttons, inputs, selectors, navigation, APK rows, checkboxes, expanders, tooltips, scrollbars, progress, menus and grid controls. Buttons use 4 DIP corners, panels use 6, and controls use a 36 DIP minimum height.
- Device is a library / viewport / automation workspace. Device commands use the original bindings and safeguards; inactive contextual actions collapse, and advanced settings and diagnostics remain reachable through expanders.
- Setup and Settings use the same typography, surfaces and form treatment. The native title bar uses Windows dark caption attributes. Vector app and navigation icons remain crisp under DPI scaling.
- Small windows expose panels through buttons; panels displace the viewport rather than covering native HWND content. Only one panel opens at the narrowest breakpoint. Fullscreen retains device controls and the exit hint while hiding navigation and side panels.
- Font tokens read the Windows accessibility text scale on startup and theme application. Large-text layouts remove the duplicate header, give navigation full width, and collapse the short-window device status section; faults expand it. The library and automation remain scrollable.
- Audio and connection labels reflect viewport telemetry rather than decorative sample states. Guest pixels retain aspect ratio through the existing `object-fit: contain` viewport and input mapping.

## Deliberate adaptations

The generated image is a style reference, not an authoritative product state. The app retains its real commands, profile-switch workflow and safety messages. Profile management opens Setup from the active-profile header; it does not switch live devices automatically. Runtime metadata is shown only when known. Diagnostic/sample entries appear only in isolated rendering fixtures; they are never seeded into users' libraries.

Muted text is #A4AFBC instead of the initial #929DAB because contrast tests found the earlier value below 4.5:1 on selected rows. The toolbar reserves 60 DIPs including its gutter and scrollbar to keep 36 DIP controls accessible in short windows.

## Verification

- Release build: zero warnings and errors.
- 121 .NET tests passed, including contrast checks for normal and selected text, primary interaction states, and focus across all five palettes.
- 30 browser tests passed, including portrait/landscape letterboxing, input coordinates, toolbar actions, recording/playback and audio lifecycle.
- Both standard and composition WebView2 host smoke tests passed, including theme switching, 1440×900 / 1180×800 / 850×600 layouts, compact panel exclusivity, fullscreen, restored minimum bounds, profile isolation, command-conflict guards and graceful-shutdown backup requirements.
- Rendered screenshots reviewed for Device, Setup, Settings, narrow panel layouts, fullscreen and a simulated 200% text size. The large-text library fixture checks that list rows retain a nonzero usable height and can be scrolled into view.

Reports and screenshots are in `validation/`. Run `scripts/Test-Theme.ps1` after a Release build to reproduce the native and visual checks. WebView2 required execution outside this session's restricted sandbox; the isolated tests neither start Android nor save real settings. Temporary browser profiles were removed after testing.

These checks do not certify physical multi-monitor DPI transitions or fresh real Android media/hardware acceptance. The theme previews capture WPF chrome and use a temporary diagnostic-library fixture; no live Android session was started for the overhaul.
