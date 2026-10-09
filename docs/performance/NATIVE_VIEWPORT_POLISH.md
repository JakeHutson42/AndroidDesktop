# Native viewport polish — 9 October 2026

The native emulator stays inside the app. The user-facing standalone-window toggle is removed; standalone rendering remains available to the explicit comparison harness. Native launch now uses the installed emulator's `-qt-hide-window` option; the owned Qt window is attached before waiting for Android to boot. Windows parent/show event hooks recover unexpected detachment. Normal Stop hides the child and separates its input queue, keeps it parented throughout graceful shutdown, and releases the host reference after emulator exit. Recovery detachment restores a hidden window, never a visible desktop window.

The host uses a private native window class that paints the active `ViewportBrush`, replacing Windows' light STATIC background. GPU rendering and aspect-preserving sizing remain intact. Rotation now uses the [emulator console's rotate command](https://developer.android.com/studio/run/emulator-console) and Android's `wm user-rotation` command. The latter handles Android's disabled auto-rotate setting. The host changes its aspect ratio for landscape. The previous Android rotation policy is captured and restored before graceful shutdown; individual apps retain their own orientation requirements.

The bottom device-message strip is removed. Checking acceleration, starting the emulator, booting Android and connecting the display appear in a centered viewport panel. Each stage completes on its actual event. The progress bar animates stage transitions; within a stage it approaches a bounded estimate and waits for completion. Successful startup durations update a rolling estimate for each device/transport/resource configuration. Initial timing is approximate, based on measured cold boots; it is not an exact countdown. Slow stages show elapsed time instead of a misleading countdown. Native content stays hidden until the bar reaches 100%, then appears after the completion animation. Errors and stopped-device messages appear in the viewport; logs remain available in Diagnostics. Compact layouts keep the progress bar and timing visible, with scrolling available for larger text.

## Measured checks

[Raw native verification](native-containment-phase-2.json) used the existing persistent device and NVIDIA RTX 5080 renderer. No game installation, update, wipe or consent interaction occurred.

| Check | Result |
| --- | --- |
| Cold startup to connected display | 17.98 seconds |
| Progress at readiness | 100% |
| Native window hidden before attachment | Passed |
| Visible detached-window events while attached/shutting down | 0 |
| Native margin pixels match theme | Passed |
| Android rotation / host bounds | 90°, 180°, 270°, 0° all passed |
| Resize, navigation, fullscreen, minimize/restore | Passed at 96 DPI |
| Graceful shutdown | 20.35 seconds; all owned process handles released |
| Shutdown UI timer | 185 updates; longest interval 124.92 ms for a 100 ms timer |
| Existing Tap Titans 2 package | Version and original install/update timestamps retained |

The 20-second emulator exit delay remains. The measured WPF timer continued throughout it; there was no observed UI stall, and the emulator remained hidden/contained. This verifies responsiveness during the wait, not an instantaneous shutdown or every possible shutdown path.

A short 10-second Home CPU sample averaged 7.73% across all sampled processes, including 7.46% in QEMU and 0.18% in WPF. This is a supplemental observation, not a new matched three-mode comparison or a gameplay/FPS claim. Keep the earlier [matched comparison and outstanding acceptance gates](PHASE_2.md).

Standard and composition navigation/layout smokes pass. Standard-mode startup screenshots were reviewed at 1440 × 900 and 850 × 600. Full keyboard forwarding, physical scaled/mixed-monitor DPI, game frame pacing, native audio and automation remain unverified or incomplete. Native mode remains opt-in, with controller fallback. Private game-save integrity has not been independently inspected.

Loading-panel layout fixtures: [desktop](startup-1440.png) and [compact](startup-850.png). These exercise the startup presentation without starting a device; the header's Stopped state is fixture context.

## Next phase prompt

> Implement Phase 3 using docs/PERFORMANCE_PLAN.md, docs/performance/PHASE_2.md and docs/performance/NATIVE_VIEWPORT_POLISH.md. Resolve the missing native key-down event, then verify physical pointer/keyboard input, focus-loss releases, continuous resizing, scaled and mixed-monitor DPI, fullscreen shortcuts and remaining tab hover flicker. Preserve embedded-only presentation, centered startup progress and contained shutdown. Investigate the measured 20-second emulator exit delay without risking saved data. Profile longer Home and game workloads, measure presented FPS and input-to-visible latency, and compare standalone/native/controller fairly. Preserve apps and saves, retain controller fallback, and keep native mode opt-in until its acceptance gates pass. Finish with measured results and a prompt for the next phase.
