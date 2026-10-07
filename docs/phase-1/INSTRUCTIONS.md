# Phase 1 — Desktop foundation instructions

Authorized 4 October 2026 after the user requested Phase 1. The user also requested a small, understandable framework, with non-critical defects addressed later.

1. Keep .NET 10/WPF, CommunityToolkit.Mvvm and the existing device adapters. Reuse Corvus's palette and control styles rather than introduce another UI framework.
2. Build a resizable shell with Device, Setup and Settings pages. Keep the Android viewport isolated from WPF overlays and expose prototype diagnostics separately.
3. Add Corvus theme switching and persistent window bounds. Retain per-monitor DPI awareness and restore windows onto a visible screen. Test mixed-DPI behavior manually.
4. Store versioned JSON under `%LOCALAPPDATA%/AndroidDesktop`. Serialize saves, debounce frequent changes, replace committed files atomically, retain a backup and surface failures. Preserve future schemas and migrate older settings without data loss.
5. Add asynchronous prerequisite discovery/checks for Android SDK tools, Java, WebView2 and the prepared gateway. Report missing prerequisites and free device storage. Verify acceleration through the official emulator tool.
6. Guide missing SDK/image installation and license acceptance through official tools. Do not accept licenses automatically or invent download sizes. Persist paths so setup can resume after cancellation or reboot.
7. Create only the app-owned AVD using an installed, explicitly selected system image. Retain any existing device and never force-overwrite, wipe, change other profiles, or rebuild a device for window resizing.
8. Show the shell before prerequisite checks finish. Report actual operation stages, support cancellation and disable conflicting actions. Keep Android operations in services and retain constructor injection.
9. Preserve the provisional viewport and Phase 0 diagnostic harness. APK drag/drop, unchanged-package launch optimization, the complete device toolbar and save/reopen acceptance belong to Phase 2.
10. Run appropriate settings, command-safety and Windows host checks; record actual results and exact manual follow-up steps. No installer, APK library, persistent recording system or instrumentation in this phase.

The user authorized advancing. This does not supply missing Phase 0 hardware evidence: real APK/media/latency acceptance is still unverified in the recorded results, and the transport remains provisional.
