# Android Desktop — Implementation Plan

Prepared 4 October 2026. Working project name: Android Desktop. Product name can be chosen later.

## 1. Recommended direction

Build a Windows desktop application in C# and .NET 10, using WPF and selected Corvus interface components. Run the official Android Emulator locally as a separate process. Give the application one persistent Android Virtual Device (AVD), initially with Google Play where the target APK is compatible.

The first release opens one standalone APK through a file picker or Explorer drag-and-drop, installs and launches it, provides interactive gameplay and sound, switches device orientation, and preserves game data and user settings. An APK library, multiple concurrent devices, memory instrumentation and iOS are later stages.

Performance is an acceptance criterion from the first prototype. The Android display/control integration must pass a measured Windows proof of concept before we invest in the complete interface.

This is a researched implementation proposal, not a claim that the integrations have already been built or benchmarked. Corvus source was inspected; the Android integrations have not yet been executed on the user’s PC.

## 2. Decisions and open questions

| Item | Decision |
|---|---|
| Initial platform | Windows 11 x64, subject to checking the development PC |
| Main framework | .NET 10 / WPF, matching Corvus |
| UI design system | Corvus themes, shared XAML styles, vector controls and navigation patterns |
| Android runtime | Official Android Emulator and official system images |
| Virtualization | Windows Hypervisor Platform; verify with the emulator acceleration check |
| Device persistence | One application-owned AVD retained between launches |
| Settings | Small versioned JSON model, independent of AVD data |
| Initial display candidate | WPF-hosted WebView2 with local WebRTC display/control |
| Display decision status | Provisional; Windows support, audio, latency and recorder access must pass Phase 0 |
| Alternative | A native scrcpy-based display/control integration, evaluated if the preferred route fails |
| ADB integration | Official adb executable behind a C# service initially |
| Package library database | Defer until an APK library actually needs it |

The WebView2 candidate still delivers an installed desktop application: the frame, settings and controls are WPF; only the Android viewport uses an embedded rendering engine. It needs no browser tab, remote server or internet connection for local display after setup. Android apps and Google Play can independently require internet access.

The local-connection prototype must pass an offline test and remove dependencies on hosted demo assets or external STUN/TURN services. The application ships its viewport assets; it never loads the hosted demonstration page as its production interface.

If a completely native viewport becomes a requirement, choose the native integration after the same benchmark gate. Keep this choice isolated behind a small device-session adapter.

## 3. Stack and dependency policy

| Component | Proposed dependency | Purpose and reason |
|---|---|---|
| Desktop shell | WPF / .NET 10 | Existing Corvus expertise and interface reuse |
| ViewModels | CommunityToolkit.Mvvm | Observable properties and cancellable asynchronous commands |
| Device viewport candidate | Microsoft.Web.WebView2 | Reuse established browser media handling inside WPF |
| Emulator control/signalling | Grpc.Net.Client, Google.Protobuf, Grpc.Tools | Generate clients from the selected emulator’s protocol files; reuse connections |
| Android execution | Android SDK Emulator, platform-tools, system image | Existing Android runtime and installation tools |
| APK metadata | Official apkanalyzer tool | Read application ID, SDK requirements and package contents |
| Package architecture check | System.IO.Compression plus APK metadata | Inspect native-library ABI directories; compare against the device |
| Settings | System.Text.Json | Already available in .NET; small file needs no database |
| Bounded pipelines | System.Threading.Channels | Bound input/recording queues and separate I/O from interaction |
| Tests | xUnit and Microsoft.NET.Test.Sdk | Adapt Corvus’s existing testing setup |
| Packaging | Corvus publishing pattern and Inno Setup | Self-contained C# release and dependency/setup checks |

Use one command/observable-property implementation in the new project. Adopt the Toolkit while adapting copied ViewModels instead of maintaining both Toolkit commands and Corvus’s custom command classes.

Reuse Corvus’s design system rather than adding Material Design, MahApps or another overlapping visual framework. Add a dependency only when it replaces meaningful work. ADB can later use AdvancedSharpAdbClient if repeated process management becomes a problem; it is not needed to establish the first milestone.

Pin tested stable dependency versions and the emulator/protocol combination. Do not automatically install prerelease packages. Review update changes and rerun compatibility checks before moving the supported runtime forward.

## 4. Corvus reuse boundaries

Create a separate project. Adapt the following source groups:

| Source area | Reuse approach |
|---|---|
| Themes and Controls.xaml | Preserve semantic resources and consistent control states |
| MainWindow.xaml | Reuse layout/navigation styling; replace page content and status bindings |
| ThemeService | Reuse palette switching and persistence pattern |
| Icon controls | Extend the existing vector icon approach for Android controls |
| AppStateStore | Adapt to the new schema/root; report save errors and serialize writes |
| Explorer drop handling | Restrict initial intake to one APK and share logic with the file picker |
| Startup and logging | Preserve useful error handling; replace artificial splash delays with actual progress |
| Installer/publish scripts | Adapt product identity and dependency discovery |

Replace Corvus’s fixed window dimensions with a resizable layout, minimum usable size, persistent bounds and proper mixed-DPI handling. Remove audio-desk services, starter sounds, achievements and unrelated global hooks from the new shell.

Corvus’s hotkey handling may inform keyboard shortcuts. It must not become an OS-wide mouse recorder for Android gestures.

## 5. Device display: prove this first

### Preferred candidate

Use one WebView2 viewport containing packaged local display assets. Start from the Android emulator project’s existing WebRTC frontend/gateway examples. Limit the web component to media display, touch capture and device input; keep the product interface in WPF.

The available example gateway is Python. Reuse it for the prototype, first checking that the required RTC services exist in the chosen Windows emulator build. Its documented setup primarily shows Linux/macOS and shell scripts, so Windows packaging is an explicit unresolved dependency.

For the release, choose between a packaged, tested gateway sidecar and a small C# signalling adapter using the same protocol. Prefer reuse if the sidecar is dependable. Only port signalling after the end-to-end path works; keep media out of the C# UI process and avoid implementing codecs or a new WebRTC stack.

Audio is a release requirement. The available streaming documentation establishes video/control, not a guarantee of the complete audio path on Windows. Verify audio capture/playback and synchronization explicitly. A viewport that needs a parallel unsynchronized audio workaround does not pass the gate.

Benchmark the normal WebView2 control and, where overlays are required, WebView2CompositionControl. Microsoft documents the normal control’s WPF layering limitation. Prefer an isolated device rectangle and adjacent WPF toolbars so the UI does not depend on expensive overlays. Do not assume composition is faster.

### Native alternative

Use scrcpy as a performance/reference tool and investigate a native integration if necessary. Its existing client provides video, audio and control. Simply attaching its external window to WPF is not a proven production solution: verify DPI, focus, resizing, input recording access, lifetime and WPF layering.

If using scrcpy’s transport directly or modifying its client, pin client/server versions together: its protocol is explicitly internal and can change. A custom native renderer adds maintenance and packaging work, so select it for measured benefits or a native-only requirement, not speculation.

### Phase 0 exit gate

Demonstrate on Windows, using one target game and a small diagnostic APK:

- Install, launch, graphics and sound operate correctly.
- Touch down/up, holds, swipe paths and required simultaneous touches reach Android correctly.
- Portrait/landscape transitions update geometry without misplaced input.
- Keyboard focus, fullscreen and mixed-DPI resizing behave correctly.
- The live input path exposes events for future recording, and the chosen replay path can inject equivalent events.
- Video decoding/rendering behavior and media buffering are measured.
- The application can recover a failed connection without discarding Android data.

Only then select and document the final viewport transport. If neither candidate meets these requirements, report the failed requirement and revise the integration before advancing.

## 6. Simple architecture

Keep Android-specific operations out of Views and ViewModels. Use a small composition root with constructor injection; a large hosting framework is unnecessary at this stage.

| Module/service | Responsibility |
|---|---|
| EmulatorSessionService | Serializes startup, attachment, shutdown and recovery |
| AndroidToolService | Runs supported SDK tools asynchronously with timeouts |
| ApkInstallService | Validates, identifies, installs and launches the selected package |
| DeviceViewport adapter | Display/control transport and geometry updates |
| DeviceInput adapter | Common semantic input model for live interaction and replay |
| SettingsStore | Versioned settings and metadata, with observable save failures |
| DeviceStorageService | Owns persistent AVD location and later backup/checkpoints |
| RecordingService, later | Captures input events through a bounded archival pipeline |
| PlaybackService, later | Schedules playback, cancellation, loops and test variations |

Use one session with explicit states: Not configured, Stopped, Starting, Ready, Installing, Running, Stopping and Faulted. Disable conflicting actions and provide an actionable recovery path. Display names are user-facing; internal state transitions are serialized.

Launch tools without shell command construction. Use ProcessStartInfo.ArgumentList, drain output/error concurrently, specify the device serial for each command, and cancel/time out operations. Track only processes owned by this app. Do not terminate unrelated devices or a shared ADB server.

Serve embedded assets/control endpoints only locally, with a per-session authorization boundary and restricted navigation. The viewport must not expose arbitrary native host operations to external pages. These controls belong behind the interface, not in a routine user checklist.

## 7. User experience

### First launch

1. Show the familiar themed desktop shell immediately.
2. Detect an existing usable Android SDK and WebView2 runtime where applicable.
3. Explain only missing prerequisites, available storage and actual download size.
4. Guide setup using official tools and required license acceptance; no requirement to create an Android Studio project.
5. Check acceleration and create a compatible persistent device.
6. Show real stages: preparing Android, starting device, connecting display and ready.

Use an existing SDK when sensible but create an AVD owned by this app. Do not alter the user’s other emulator profiles. Initial setup may require a reboot for virtualization; it must be resumable.

Check the Java runtime requirements of the selected SDK command-line tools, including APK Analyzer/device creation tools. Reuse a compatible runtime or provide a supported private dependency setup; do not assume .NET publishing supplies Java or Android tools.

### Normal use

The home screen has a clear Open APK action and a drop target. After import, show the app name where available, device state and a compact toolbar: launch/relaunch, rotate, Back, Home, volume and fullscreen. Google Play remains accessible through the Android home screen.

Opening an already-installed unchanged APK launches it rather than reinstalling it. A new compatible version updates the package while preserving its data. Signature conflicts, downgrades and missing split packages produce clear errors; never silently uninstall to solve them.

Allow replacing the currently selected APK; each package’s Android data remains in the persistent device unless explicitly removed. The application manages one selected APK initially, not a browseable library of APKs.

Closing begins a visible, asynchronous graceful shutdown. Forced termination is a recovery option after a timeout, not the normal save mechanism. Rotation changes the virtual device and the displayed coordinates; a game can still enforce its own orientation.

## 8. APK and device compatibility

The initial importer supports one standalone .apk. AAB, APKS, XAPK, split-APK sets and separately supplied OBB expansion data are outside the first release. Explain this when a file cannot be installed; Play Store downloads may manage their own additional data.

Read the package identifier, minimum SDK and native ABIs before installation. Compare the ABI list with the device’s reported supported ABIs; do not reject an APK merely for containing ARM libraries if that exact device demonstrably supports them. Test ARM translation rather than assume it.

Choose the system-image version after testing a representative APK. Use a supported, stable image with Google Play when compatible. Do not automatically choose the newest API level or promise every APK will run. Emulator checks, graphics requirements and Google service/device-attestation behavior can prevent some applications from working even when installation succeeds.

Start with modest resolution and resource settings, benchmark them, and expose quality options later. A provisional 720-class viewport and 60 FPS target is a starting experiment, not a certified device requirement. Preserve the AVD configuration between launches rather than rebuilding it whenever the window is resized.

## 9. Saving and recovery

| Data | Persistence approach |
|---|---|
| Desktop settings | Versioned JSON under the new app’s LocalAppData root |
| Selected APK metadata | Package ID, source/checksum, last successful install and device ID |
| Normal game progress | Android application data within the persistent AVD |
| Android preferences/Google sign-in | Device storage; authentication stays inside Android |
| Resume-running-state | Optional Quick Boot when supported |
| Explicit checkpoints | Later snapshot UI, separate from ordinary game saves |
| Device backup | Later consistent backup while the device is fully stopped |

Use a stable device-data location independent of the executable/release-version folder. Upgrade the desktop app without creating an empty new device. Migrate settings non-destructively. Serialize settings saves; write a unique temporary file, replace the committed file and retain a last-known-good copy. Debounce frequent slider/resize updates and flush important settings on shutdown.

Quick Boot and snapshots are conveniences, not the sole data-protection strategy. Their validity depends on emulator/image/configuration compatibility. Fall back to a cold boot while retaining device disk data. Snapshots cannot restore online server state. Do not copy a running virtual disk as a supposedly consistent backup.

Do not write Android game saves from our desktop settings file. We preserve the environment in which the game writes its normal saves and verify the target game’s behavior.

## 10. Performance design and measurement

Separate the control path, media path and archival path. UI changes, diagnostics and recording must not block video/input handling.

| Area | Implementation rule |
|---|---|
| Video | Existing media pipeline; avoid screenshots/base64 or managed bitmap copies as the gameplay renderer |
| Graphics | Verify virtualization and graphics acceleration independently |
| Input | Persistent connection; no new adb process for each tap or gesture sample |
| UI | Asynchronous cancellable commands; avoid synchronous waits and blocking SDK calls |
| Recording | Bounded queue, batch disk writes, low allocation and explicit overload handling |
| Frame buffers | Prefer current frames over a growing queue of stale images; respect codec dependencies |
| gRPC | Reuse channels; dispose streams and sessions deterministically |
| Diagnostics | Sample counters at a low bounded rate; verbose traces opt-in |
| Startup | Load the shell first; initialize the viewport once and reuse it while the session is active |
| Minimized window | Reduce preview cost when possible; do not silently pause the running Android game |
| Memory | Bound caches and buffers; inspect the entire process group, not only the C# shell |

### Proposed acceptance targets

These are initial engineering budgets. Phase 0 records the reference PC, runtime versions and workload, then confirms or adjusts them explicitly.

| Metric | Initial target / measurement |
|---|---|
| Shell readiness | Usable shell within 2 seconds on the reference SSD PC, excluding downloads/device boot |
| Gameplay rendering | Aim for 60 FPS where the same game/device runs at 60 FPS standalone; report frame-time percentiles and dropped frames |
| Added interaction latency | Aim for p95 additional input-to-visible-response latency of no more than 50 ms versus standalone |
| End-to-end latency | Measure separately; an initial sub-100 ms target is aspirational, workload dependent |
| UI responsiveness | SDK operations do not freeze navigation, resizing or cancellation |
| Recording overhead | Aim for under 5% throughput regression against the same session without recording |
| Long-session stability | Two-hour gameplay/recording soak shows no sustained memory growth after warm-up |
| Repeated lifetime | Twenty start/stop cycles leave no owned orphan processes or accumulating connections |

Use a diagnostic APK that records received touch events and timestamps, plus a visible-response measurement for end-to-end latency. Compare standalone emulator, selected embedded transport and recording enabled. Measure CPU, GPU, memory and frame timing for all relevant child processes. Do not infer performance from average FPS alone.

Use Release builds and Visual Studio profiling; introduce dotnet-trace or a microbenchmark tool only for a diagnosed CPU/allocation bottleneck. Profile before writing custom codecs, unsafe buffer management or elaborate caching.

## 11. Recording and replay — designed now, delivered later

Build the semantic input model during Phase 0. It includes relative monotonic timestamp, event sequence, pointer ID, down/move/up/cancel phase, device coordinates, normalized coordinates, dimensions and orientation. Include explicit geometry changes. Specify what pressure/multi-touch information the selected transport actually supports.

Both live interaction and playback use the same device-input semantics. The capture path copies events to the recorder without synchronous disk writes or extra remote round trips. Keep the direct gameplay path responsive. Batch archival transfer if the viewport is embedded, and calibrate timestamps if clocks cross process boundaries.

Persist a versioned recording with target package, device configuration and geometry metadata. Prefer appendable event storage with crash recovery, then finalize the manifest. If recording cannot keep up, stop recording with a clear message instead of silently producing an incomplete trace. Never drop down/up/cancel events.

Replay uses one cancellable scheduling worker and absolute elapsed-time deadlines so late events do not accumulate timing drift. Record actual dispatch timing. Do not claim exact real-time precision on Windows. Cancellation releases every active pointer/key. Interruption, disconnection or a geometry mismatch stops safely rather than continuing blind.

Deliver exact replay first, then finite/infinite loops and pause/resume. State changes matter: repeating a recording does not automatically reset a game, and fixed coordinates can fail if loading times change. Later checkpoints or screen-state conditions improve reliability.

For the user’s own detection testing, add bounded, seeded coordinate/timing/path variations, preserve the original recording and save each run’s parameters/results. Measure what the user’s detector recognizes. Do not market variations as undetectable automation or promise to conceal the emulator or instrumentation. Memory testing later uses a separate instrumented device/build profile because Google Play images restrict elevated privileges.

## 12. Implementation phases and exit criteria

| Phase | Deliverable | Exit criteria |
|---|---|---|
| 0 — Runtime and viewport proof | Target APK running inside a minimal WPF host; chosen media/input adapter | Compatibility, audio, latency, orientation and record/replay input access demonstrated on Windows |
| 1 — Desktop foundation | Corvus-based resizable shell, themes, settings and setup flow | New identity/data root; real progress; responsive commands; usable DPI behavior |
| 2 — First usable APK runner | Open/drop APK, install/launch, device controls and persistent saves | Save/reopen test passes; install errors clear; no implicit data deletion |
| 3 — Release readiness | Recovery, diagnostics, installer and performance validation | Fresh-PC install, repeated session cycles and two-hour soak pass |
| 4 — Input automation | Record, replay, cancellation, loops, then reproducible test variation | Diagnostic APK verifies event sequence, geometry, loop state and stop behavior |
| 5 — Expansion | APK library, profiles/backups, instrumentation, later remote access/iOS evaluation | Separate scoped plans using measured needs |
| 6 — Device profiles | Isolated device identities/storage/configuration; one active device | Non-destructive migration and safe switching; actual device isolation/retention requires hardware evidence |
| 7 — Backups and restoration | Verified copies after graceful stop; journaled same-device restoration preserving originals | Source/recovery checks pass; real Android disk consistency/saved-progress acceptance remains open |

Phases 0–3 constitute the first release. Capture architecture belongs in Phase 0; a recording editor or detection-testing dashboard does not. The initial technical proof is normally days rather than a polished-product effort, but give a delivery estimate only after the viewport and APK compatibility gates pass. The complete first release is a multi-week project, subject to those integration results.

## 13. Verification and release checklist

- Unit tests: settings migration/write failure; session transitions; conflicting command protection; coordinate/rotation transforms; later replay scheduling and cancellation.
- Android integration tests: standalone APK install, installed-app launch, compatible update retains data, incompatible update does not delete data, connection loss and clean restart.
- Saved-progress tests: close/reopen; cold boot; invalid Quick Boot recovery; desktop app upgrade retains the same AVD.
- Interaction tests: pointer exits viewport while held, focus loss, rapid taps, long hold, swipe, supported simultaneous touches, rotation mid-session and mixed-DPI monitor movement.
- Performance tests: standalone vs embedded vs recording, frame-time distribution, input response, total memory and process cleanup.
- Setup tests: existing SDK; missing SDK; insufficient disk; cancelled/interrupted downloads; missing acceleration; missing WebView2 if used; image/version mismatch.
- Packaging: self-contained Windows application; packaged viewport assets/sidecars; clear dependency setup; third-party notices; no runtime development server or npm install required.
- CI: Windows runner builds/tests/package contract; hardware-accelerated Android gameplay validated on a suitable Windows machine rather than assumed from unit-test success.

Windows hardware validation remains necessary. A Linux-only development workspace can produce supporting code and plan artifacts, but it cannot certify WPF rendering, WHPX or this game’s real performance.

## 14. First implementation batch

1. Create a separate Windows project and bring across only Corvus’s useful visual resources.
2. Record the reference PC hardware and select one representative standalone APK.
3. Verify the APK runs directly in the official emulator, including saved progress and sound.
4. Build the small WPF viewport prototype; validate WebRTC on Windows and compare against the standalone baseline.
5. Prove one live gesture and its replay reach the diagnostic APK through the proposed input model.
6. Document the transport decision and supported emulator/image versions.
7. Build the real setup/import/session interface after the integration gate passes.

## 15. Source references

Official/source repositories consulted on 4 October 2026. Recommendations, budgets and sequencing above are engineering judgments; source documentation establishes the available components, not the finished integration’s performance.

- Corvus repository — inspected project, shell, persistence, theme, drag/drop, tests and publishing files.
- Android hardware acceleration.
- Android virtual devices and system images.
- Android snapshots.
- Android Debug Bridge.
- APK Analyzer command-line tool.
- Android emulator streaming demo.
- Android emulator gateway protocol.
- scrcpy development/protocol documentation.
- WPF and Win32 integration limitations.
- WebView2 in WPF.
- WebView2 distribution.
- WebView2 performance guidance.
- WebView2 host security.
- .NET MVVM Toolkit.
- .NET gRPC client.

---

## Historical scaffold status — before Phase 0 implementation

The sections above preserve the supplied implementation proposal. Statements about prior research refer to that proposal; source repositories were not re-inspected during scaffolding.

Only project scaffolding is authorized for this batch. Phase 0 has not started. Use this file as the reference for future work and respect every phase gate.

- Separate C# / .NET 10 WPF solution and xUnit test project created.
- Stable NuGet dependencies pinned in the project files; scaffold build validation does not establish Android integration compatibility.
- Reserved folders map to the planned Views, ViewModels, models, services, viewport/input adapters, visual resources, local viewport assets and protocols.
- Corvus source is absent from this workspace. Visual adaptation remains pending; no substitute design system has been added.
- No emulator/system image/protocol combination has been selected, downloaded or certified. No AVD, APK installation, WebRTC gateway, input implementation or performance measurement has been created.
- Packaging folders are reserved only; installer/publishing implementation remains in its planned phase.


## Phase 0 implementation handoff

Phase 0 prototype code has now been added. See [Phase 0 source notes](phase-0/REFERENCE_NOTES.md), [local runbook](phase-0/RUNBOOK.md) and [validation results](phase-0/RESULTS.md). The historical scaffold status above describes the preceding batch. No later phase has been implemented, no final display transport has been selected, and Android hardware acceptance remains open. Follow this plan and those evidence records before advancing.


## Phase 1 desktop foundation handoff

Phase 1 was authorized by the user on 4 October 2026. The resizable shell, Corvus theme switching, versioned JSON settings, setup guidance/checks and application-owned device creation flow are implemented. See [instructions](phase-1/INSTRUCTIONS.md), [local runbook](phase-1/RUNBOOK.md) and [actual results](phase-1/RESULTS.md). Phase 2 was subsequently implemented as recorded below. Advancing at the user's request does not replace the still-missing Phase 0 Android hardware evidence or select a final transport.

## Phase 2 APK runner handoff

Phase 2 was authorized and implemented on 4 October 2026. The shared Open/drop importer, schema v2 selected-app metadata, verified unchanged launch, non-destructive updates/replacement and device toolbar are present. See [local validation](phase-2/RUNBOOK.md) and [actual results](phase-2/RESULTS.md). Real Android saves/media/input and other hardware exit criteria remain unverified; no final transport is selected. Phase 3 was subsequently implemented as recorded below.

## Phase 3 readiness handoff

Recovery, bounded support diagnostics, candidate publishing/installer and Windows CI code are implemented. See [actual results](phase-3/RESULTS.md). The runtime gate, clean-PC acceptance, Android saves, two-hour soak, twenty device cycles and distribution license review remain open. Historical candidate artifacts are Phase 3 builds, not Phase 4 builds.

## Phase 4 input automation handoff

Phase 4 code is implemented; see [actual results](phase-4/RESULTS.md) and [local validation steps](phase-4/RUNBOOK.md). Corvus styling and the single device/selected APK scope are retained. No recording editor, APK library, instrumentation or Phase 5 feature was added. Android acceptance remains unverified, and the WebRTC transport remains provisional.

Recordings use schema-v1 manifest.json plus appendable events.jsonl, under the stable application data root. The live viewport copies semantic events into 50 ms batches without awaiting disk writes. The host bounds its queue to 32 batches of at most 256 events, rejects overload explicitly, and finalizes a SHA-256 and event count. Recovery creates a separate valid-prefix copy and synthetic releases, preserving the original. It rejects middle corruption; only an unfinished final row is discarded.

Replay verifies package/version/checksum, actual device UUID, configuration/runtime identity, protocol and initial geometry. A single browser worker streams batches of at most 128 events and uses absolute monotonic deadlines through the existing persistent input connection. Actual dispatch receipts are archived separately; they do not establish Android reception or latency. Late scheduling over 250 ms stops playback rather than bursting stale input. This is a safety threshold, not a measured performance budget. Geometry changes stop safely; replay does not rotate the device or guess coordinates automatically.

Pause takes effect at the next boundary where every pointer/key is released. Resume shifts deadlines by the paused duration. Finite loops (1–10000) and cancellable infinite loops do not reset game state. Cancellation, deactivation, geometry mismatch, connection loss and shutdown attempt to release all held input; disconnected release still needs diagnostic APK confirmation.

Optional seeded variations preserve the original: coordinate translation up to 32 px, sinusoidal path variation up to 16 px and timestamp jitter up to 100 ms, clamped to geometry and monotonic event ordering. The deterministic mulberry32-v1 seed continues across loops. Per-run parameters, dispatches, failures and optional user detector observations are saved. No detector result or concealment guarantee is inferred. Touch transport supports ten slots and quantized pressure; cancel encodes zero-pressure release, not guaranteed Android ACTION_CANCEL. Android and browser clocks remain separate and require external calibration for cross-process latency measurement.

Phase 5 is deferred until explicitly authorized. Scope it around measured needs and the outstanding hardware gates; the prepared steps are in [Phase 5 instructions](phase-5/INSTRUCTIONS.md).

## Phase 5 assessment handoff

The Phase 5 steps were requested on 5 October 2026. The plan and Phase 0–4 records were reviewed, and limited dependency discovery still found no Android tools/Java at the checked locations. Hardware acceptance remains open. The user agreed to the [APK-library scope](phase-5/SCOPE.md), implemented with schema-v3 JSON migration, retained device/package entries, library launch and matching-package relocation/update through the shared importer. The user has no APKs currently; hardware checks remain unverified. See [actual results](phase-5/RESULTS.md) and [usage/validation](phase-5/RUNBOOK.md). No dependencies were added and historical candidate artifacts remain unchanged.

## Phase 6 device profiles handoff

Device profiles were authorized and implemented on 6 October 2026. Settings schema v4 migrates the existing device into Default device without moving its disks or changing legacy resource settings. New profiles have isolated AVD homes/names, per-profile runtime/image/resource settings and selected app/library snapshots. Explicit switching requires a stopped session and no active automation. A per-user ownership lease keeps one active device; no concurrency or backup feature is introduced. Resource overrides use validated guest RAM/CPU settings, not whole-process resource quotas.

See [actual Phase 6 results](phase-6/RESULTS.md), [usage and hardware steps](phase-6/RUNBOOK.md) and [source dependency inventory](phase-6/dependency-inventory.json). Source/host checks pass, while actual Android profile creation, isolation, save retention and all inherited hardware/release gates remain unverified. Existing 0.3.0 release artifacts remain historical Phase 3 candidates. Phase 7 was subsequently implemented as recorded below; its [authorized steps](phase-7/INSTRUCTIONS.md) retain real Android acceptance requirements.

## Phase 7 backup/restoration handoff

Phase 7 was authorized and implemented on 6 October 2026. Themed explicit backup, verification, same-device restoration and interrupted recovery use the existing profile/session/ownership/settings adapters. Backup/restore require a matching graceful-stop receipt and exclusive ownership; manifests record byte integrity, identity and strict dependency/runtime compatibility. Restore stages/verifies files, preserves the original home and journals disk/metadata commits; pending transactions block startup and creation until recovery succeeds. Settings remain schema v4, with separate schema-v1 backup/journal files. No dependency, live checkpoint, clone or concurrency feature was added.

See [actual results](phase-7/RESULTS.md), [usage/recovery and real validation steps](phase-7/RUNBOOK.md) and [source inventory](phase-7/dependency-inventory.json). Automated fixture/native-host checks pass; real Android shutdown/save/disk consistency and inherited hardware/release gates remain unverified. Originals, recordings and historical artifacts remain preserved. Use rebuilt source; existing 0.3.0 artifacts do not contain Phases 4–7. The next scoped work is real hardware acceptance, not another unselected expansion.


## Local Android installation acceptance — 6 October 2026

The user authorized official Android image installation and SDK licence acceptance. Android 14 Google Play, stable emulator and Java are now installed/configured. Real boot, diagnostic APK launch and graceful shutdown pass, including the app Start/Open APK commands in explicit native-window mode. The embedded RTC v2 probe fails with PERMISSION_DENIED; authentication remains enforced. See [actual installation results](android-installation/RESULTS.md). Other hardware, gameplay, backup/save and release gates remain open; no new expansion was selected.

### Embedded display correction

The user required Android inside the application. The supported authenticated EmulatorController frame/input adapter now renders within WebView2, preserving the existing input/geometry and recording/replay paths. Real frames, a diagnostic tap and graceful shutdown pass. Frame transport is capped at 15 fps and has no browser audio; game performance and inherited acceptance gates remain open. Native-window mode is optional; multiple active devices remain disabled. See the embedded update in [installation results](android-installation/RESULTS.md).

### Performance and resource cleanup priority

Audio is deferred by the user. Frame pacing now includes encoding/transport/drawing time in the existing 15 fps budget rather than adding a full interval afterwards; the single acknowledged frame limit is retained. Disconnect clears canvas/video/track/socket references and disposes the WebView. The host waits up to ten seconds for BrowserProcessExited and reports an unconfirmed release without preventing Android shutdown. Gateway shutdown closes active controller sockets before cleanup. No dependencies were added. See [current checks and remaining acceptance](android-installation/PERFORMANCE.md).

### Embedded audio — 7 October 2026

Audio was subsequently authorized. The embedded controller supports an independent authenticated 48 kHz stereo PCM stream, enabled on demand by Mute / unmute. A fixed 100 ms AudioWorklet ring drops stale samples instead of growing; one packet awaits renderer receipt, with a 500 ms stalled-consumer cutoff. Native host playback is disabled for embedded controller sessions; microphone capture/injection is not introduced. Stop, reconnect, remote closure and renderer faults release the audio stream, worklet and context. No new dependency was added. See [audio implementation and actual validation](android-installation/AUDIO.md); physical listening, representative gameplay and extended performance acceptance remain explicit gates.
