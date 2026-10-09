# Performance overhaul

## Goal

Make the Windows interface responsive and stable, and replace the 15 fps display with a measured, efficient path targeting 60 fps on the Ryzen 9800X3D / RTX 5080 test machine. Preserve installed apps, saved data, APKM import, profiles, backups and authenticated local-only access. This is an architectural change, not an FPS-cap adjustment.

## Confirmed bottleneck

The controller gateway polls `getScreenshot(PNG)` at full device resolution, sends each image over a WebSocket and waits for acknowledgement. The browser decodes it, copies it to a canvas, and captures that canvas into a video stream. Both the gateway and canvas stream impose a 15 fps limit. This repeatedly compresses and copies large images rather than using an efficient video or shared-frame transport.

The reported 50–75% CPU consumption and tab flicker need reproduction and attribution. Do not assume all CPU usage belongs to WPF or that `-gpu auto` selected the NVIDIA hardware renderer. Mobile-native execution and Windows emulation have different overhead.

## Implementation order

Progress: [Phase 1](performance/PHASE_1.md) retains shell viewport content and records the controller baseline. [Phase 2](performance/PHASE_2.md) implements the opt-in native host and same-workload comparison. Phase 3 must harden its remaining resize/DPI/input gates, investigate native versus standalone overhead and tab hover flicker, and measure frame pacing before changing the default renderer. The numbered work areas below remain the full acceptance scope; prototype implementation does not imply all gates have passed.

1. **Measure the baseline.** Reproduce tab hover/switch flicker and Tap Titans 2 loading/gameplay. Record per-process CPU (WPF, WebView2, gateway, emulator), GPU engines, memory, frame delivery/presentation and input-to-visible latency. Include Android Home, active game, idle, hidden/minimized and tab-switch cases. Record guest resolution, RAM/cores, emulator version, acceleration and actual graphics backend. Obtain user acceptance of game terms before gameplay testing.

2. **Fix shell rendering.** Profile WPF layout/render activity during mouse movement. Check tab hit testing, selection events, native WebView composition/airspace and control lifetime. Keep the viewport instance alive across navigation; eliminate hover-driven layout churn and unnecessary rebinding. Fix the demonstrated flicker, preserve keyboard focus/accessibility, and verify resizing/fullscreen/text scaling. Do not remove hover feedback simply to hide the symptom.

3. **Prototype native emulator presentation first.** Keep WPF and replace only its viewport with a stable `HwndHost` hosting the owned emulator's native display window. Launch with native graphics enabled rather than `-no-window`; verify the actual GPU/iGPU backend and virtualization acceleration. Establish a separately running native-window baseline, then compare embedding under the same workload. Identify the display window by owned process identity, not its title alone. Prove native window attachment, styles, focus, lifetime and graceful teardown; never destroy unrelated windows. This is a feasibility gate, not an assumption that cross-process embedding is officially supported by the emulator.

4. **Harden native resizing and integrate the viewport.** Treat native resizing problems as potential risks until reproduced. Keep one host/window alive across tab changes and resizing. Size the child window in physical pixels from WPF layout and current monitor DPI; avoid resize-event feedback loops, redundant moves and device/window recreation. Preserve aspect ratio with letterboxing, establish whether the native emulator scales its surface correctly, and handle DPI-awareness compatibility explicitly. Keep WPF controls outside the native airspace; do not depend on overlays covering the child window. Test keyboard focus, pointer coordinates, rotation, fullscreen, minimize/restore and focus loss. Preserve record/replay through a supported input adapter; prevent duplicate native and injected input. Use native audio when appropriate, with one output path and working mute controls. Preserve local authentication for controller operations still required. Stop screenshot polling and the canvas/video pipeline in native mode. Expose only frame-rate and resolution controls actually supported by the chosen renderer.

   **Fallback gate:** if native hosting cannot pass correctness and performance checks, evaluate a custom Direct3D viewport backed by a supported emulator frame-sharing/streaming interface. Vortice.Windows is a candidate .NET graphics binding, not a source of emulator GPU frames. Prove frame access, ownership and synchronization before adopting it; do not claim zero-copy without measurements. If direct GPU integration is unavailable, compare an authenticated accelerated-video path. Use bounded buffers, latest-frame presentation and independent input. Revisit RTC only with supported authorization, never by disabling security. Retain PNG only as an explicit diagnostic fallback. A full shell rewrite requires evidence that replacing the viewport cannot solve the problem.

5. **Tune runtime and background work.** Verify Windows acceleration and NVIDIA rendering instead of assuming them. Measure guest RAM/CPU/resolution changes individually. Reduce duplicate frame work, unnecessary polling and allocations. Throttle or suspend display work when hidden/minimized without stopping installed apps or losing state; resume without stale input. Gate detailed diagnostics behind an explicit mode. Change cold-boot/snapshot behavior only after validating persistent-data and recovery safety.

6. **Validate and document.** Run existing tests plus meaningful host lifetime, resize/DPI, input-coordinate and selected-transport tests. Repeat Home/game/idle benchmarks, rapid tab hovering and switching, APKM installation and device restart. Run the resizing matrix below, perform a sustained run and confirm shutdown releases owned processes/windows. Update screenshots, README limits and benchmark results only after real verification. Do not push changes unless requested.

## Resizing and native-host acceptance matrix

- Continuous edge/corner dragging between minimum, normal and maximized sizes; repeat with library/automation panels toggled.
- Fullscreen enter/exit, portrait/landscape rotation, minimize/restore and repeated Device/Setup/Settings switches.
- Windows scaling at 100%, 125%, 150% and 200%; movement between monitors with different scaling where hardware permits. Record any untested combinations.
- Verify no flicker, black/stale surface, clipping, stretching, detached window, repeated renderer restart or sustained resize-related CPU spike.
- Verify mouse/touch mapping, keyboard focus, key release on focus loss, aspect ratio and usable controls after every transition.

Fix reproduced failures in the host adapter and retest. If the emulator's cross-process window behavior remains unreliable, reject native embedding at the fallback gate rather than shipping resize hacks as the main experience.

## Acceptance targets

- No reproducible tab flicker, viewport recreation, stuck input or growing frame backlog in the tested scenarios.
- Shell input remains responsive while Android is running; shell idle CPU should be near idle.
- Target 60 fps presentation when the guest produces frames fast enough; report actual presented FPS and frame-time percentiles rather than configured caps.
- Material CPU reduction from the same-workload baseline, with separate host-shell and emulator figures. Set a defensible absolute budget after profiling; do not promise a percentage before measurement.
- Installed apps and data survive restart; memory settles rather than growing through repeated navigation and reconnects.
- Native hosting must pass the resizing matrix; native-mode display must not use the PNG gateway or browser capture chain.

Deliver the implementation, before/after measurements, test results and remaining hardware/game limitations together.

Native viewport startup/shutdown containment, dark margins, rotation and centered progress are implemented and measured in [the polish report](performance/NATIVE_VIEWPORT_POLISH.md). Remaining Phase 3 gates include full keyboard input, physical DPI and sustained frame pacing.
