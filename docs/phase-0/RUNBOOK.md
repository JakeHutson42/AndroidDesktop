# Phase 0 local validation

Phase 0 is implemented as an experimental host, not a certified APK runner. The WebRTC transport remains provisional. Read [the implementation plan](../IMPLEMENTATION_PLAN.md) before extending it. Do not advance to Phase 1 until its Phase 0 gate passes.

## Prerequisites and one-time preparation

Use Windows 11 x64, .NET 10 SDK, a compatible Java runtime (JDK 17 or the Java version required by your chosen SDK command-line tools), Python 3.12 and Node with pnpm 11.19.0. The existing development host has .NET and bundled Python/Node; no Android SDK, AVD or target APK was supplied. Install Android SDK command-line tools from [Google](https://developer.android.com/studio#command-tools), platform-tools, emulator and a stable Google Play image. Accept SDK licenses yourself. The prototype does not install prerequisites or accept licenses for you.

Commands below run from the project root. Replace the two dependency paths with your real locations:

```powershell
$taskSdk = "$env:LOCALAPPDATA/Android/Sdk"
$taskJava = 'C:/Program Files/Android/Android Studio/jbr'
$env:JAVA_HOME = $taskJava
& "$taskSdk/cmdline-tools/latest/bin/sdkmanager.bat" --sdk_root=$taskSdk --licenses
& "$taskSdk/cmdline-tools/latest/bin/sdkmanager.bat" --sdk_root=$taskSdk 'platform-tools' 'emulator' 'platforms;android-34' 'build-tools;34.0.0' 'system-images;android-34;google_apis_playstore;x86_64'
& "$taskSdk/emulator/emulator.exe" -version
& "$taskSdk/emulator/emulator.exe" -accel-check
```

API 34 / x86_64 / Google Play is an initial *test image*, not a final compatibility decision. If your representative APK needs a different stable API/image, change the test image before comparing transports. Record `emulator/source.properties`, the image's `source.properties`, build fingerprint, driver versions and acceleration output. Do not obtain a prerelease emulator just to force this transport to work. If WHPX is unavailable, enable Windows Hypervisor Platform using Windows Features and enable firmware virtualization if necessary; reboot, then rerun `-accel-check`. Successful CPU virtualization does not prove GPU acceleration.

Create a new application-owned AVD, without touching your other profiles:

```powershell
$taskAvdHome = "$env:LOCALAPPDATA/AndroidDesktop/avd"
New-Item -ItemType Directory -Path $taskAvdHome -Force | Out-Null
$env:ANDROID_AVD_HOME = $taskAvdHome
'no' | & "$taskSdk/cmdline-tools/latest/bin/avdmanager.bat" create avd --name AndroidDesktop_Phase0 --package 'system-images;android-34;google_apis_playstore;x86_64' --device pixel_2
```

Do not use `--force` on an existing device. Before its first boot only, you may set `hw.lcd.width=720`, `hw.lcd.height=1280`, `hw.lcd.density=240`, `hw.ramSize=2048`, `hw.gpu.enabled=yes`, `hw.gpu.mode=auto` in `AndroidDesktop_Phase0.avd/config.ini`. Retain the same configuration for all comparisons. Never resize the AVD when resizing WPF.

Prepare the private gateway and packaged frontend (network needed at development setup, not for local playback):

```powershell
./scripts/Prepare-Phase0.ps1 -Python 'C:/path/to/python.exe' -PackageManager 'pnpm.cmd'
Copy-Item phase0.example.json phase0.local.json
```

Edit `phase0.local.json`: set SDK/command-line-tool roots, Java executable, the full path to `tools/gateway/.venv/Scripts/python.exe`, and the full path to `tools/gateway`. Keep the application-owned AVD home. JSON paths can use forward slashes. Config is a developer harness; no production settings/setup flow has been implemented.

The protocol source commit is pinned in `protocol-commit.txt`. Its experimental RTC v2 service is *not certified against any released Windows emulator*. The Python gateway probes that RPC before opening media. `UNIMPLEMENTED`, authentication failures, negotiation failures or absent audio fail the candidate. Preserve the evidence and stop at Phase 0; do not mark the transport selected or silently switch to insecure/no-token mode. A stable emulator whose discovery file only offers JWT credentials needs a separately verified credential adapter; this prototype deliberately requires `grpc.token`.

## Build the diagnostic APK and run the host

```powershell
./scripts/Build-DiagnosticApk.ps1 -SdkRoot $taskSdk -JavaHome $taskJava
dotnet test AndroidDesktop.sln -c Release --no-restore
& tools/gateway/.venv/Scripts/python.exe -m unittest discover -s tools/gateway/tests -v
Push-Location tools/viewport
pnpm.cmd test
Pop-Location
dotnet run --project src/AndroidDesktop -c Release --no-build -- ./phase0.local.json
```

In WPF: **Start device**, **Connect / reconnect display**, choose `diagnostic-apk/build/Phase0Diagnostic.apk`, then **Inspect / install / launch**. Next use the same steps with your representative standalone game. Invalid formats, split manifests, ABI/API mismatches and Android install errors stop without uninstalling or clearing data. ARM support in the ABI list is only a prerequisite; actually run an ARM-only workload before accepting translation. This prototype installs with `adb install -r` for each explicit experiment; the Phase 2 unchanged-APK optimization is deferred.

You can independently repeat the WPF/packaged-page smoke check without an SDK using `./scripts/Smoke-Host.ps1` after the Release build. It briefly starts hidden native host windows and writes readiness reports, without connecting Android or testing media. Its output is deliberately narrower than the Phase 0 gate.

If discovery fails, look under `$env:TEMP/avd/running`, `$env:LOCALAPPDATA/Temp/avd/running` or `$env:USERPROFILE/.android/avd/running`. Set `discoveryDirectory` to the correct directory. Do not paste discovery files into reports: they contain credentials. The prototype selects a matching gRPC port belonging to its newly launched session; choose unused ports and stop only its owned processes.

The headless emulator remains a separate process. Media is negotiated directly between WebView2 and the emulator; Python handles signalling. The host loads its bundled `index.html`/JS/CSS, does not expose native host objects, and restricts navigation. HTTP/WebSocket signalling binds to `127.0.0.1` and uses a random session token held in memory. No npm/Python development webserver serves the viewport at runtime. The Python virtual environment is still a development dependency, not a packaged release sidecar.

Capture Android diagnostics in a second PowerShell terminal:

```powershell
& "$taskSdk/platform-tools/adb.exe" -s emulator-5580 logcat -v raw 'Phase0Diagnostic:I' '*:S' | Tee-Object -FilePath ./diagnostic-log.txt
```

Do not clear the shared logcat buffer. Save the diagnostic log in the matching evidence directory after a run. Host measurements appear under `%LOCALAPPDATA%/AndroidDesktop/phase0-evidence/<run>/measurements.jsonl`. On completion, press **Stop** and wait for the state to become Stopped so buffered evidence flushes. Then:

```powershell
& tools/gateway/.venv/Scripts/python.exe scripts/Summarize-Evidence.py "$env:LOCALAPPDATA/AndroidDesktop/phase0-evidence/REPLACE-WITH-RUN"
```

## Required comparisons

1. Set `showStandaloneWindow=true`. Start the device, install/launch the diagnostic APK and game, and use the emulator's own window **without connecting WebView2**. Measure standalone graphics, sound, input and saves. Stop gracefully.
2. Set `showStandaloneWindow=false`, `viewportMode=standard`. Repeat with WPF/WebRTC using the same AVD/game/configuration. Record actual audio/video tracks and input channel behavior. Stop gracefully.
3. Set `viewportMode=composition`. Repeat. Compare presentation intervals, decode/buffering statistics, CPU/GPU and process-group memory. The presence of the composition control does not establish faster rendering.
4. While connected, disconnect internet access and reconnect display. Confirm video, input and sound still work. Google Play/the game may require internet independently. Assets/ICE must not contact hosted demos/STUN/TURN.
5. Run scrcpy standalone as a reference if available, using its matching client/server pair. Record `scrcpy --version`; use `scrcpy --serial=emulator-5580 --max-size=1280 --max-fps=60 --require-audio --print-fps`. Scrcpy is a reference, not an implemented embedded alternative. If WebRTC fails, document the failed requirement and evaluate that native alternative before any later phase.

## Input, geometry and recovery gate

- Test taps in all four corners and center; black bars must ignore initial touches. Check diagnostic coordinates and displayed dimensions at 100%, 150% and 200% DPI.
- Hold a mouse press for at least five seconds; drag outside the viewport and release. Repeat with focus loss, switching applications, rapid taps and a swipe. Diagnostic active pointers must return to zero.
- Use a real multitouch Windows display (or a separately validated pointer-injection harness) for simultaneous touches. A mouse test proves one pointer only. The adapter uses up to ten stable pointer slots, pressure scaled to 1–1024 on contact and zero on release. Semantic cancel maps to transport pressure-zero release; it does **not** claim Android receives `ACTION_CANCEL`.
- Press **Capture one gesture**, perform a short complete gesture, then **Replay once**. Compare the Android log's pointer IDs/order/path to `viewport.gesture` and live/replay input batches. This bounded in-memory proof is limited to ten seconds / 4096 events; it is not the Phase 4 recording product. Down/up are not silently dropped. Oversized/interrupted capture is discarded with a message.
- Replay cancellation releases active pointers/keys. Both paths use the same touch encoding. Playback schedules against absolute elapsed deadlines and logs actual dispatch times. JavaScript and Android uptime clocks are separate; this prototype does not infer end-to-end latency from their raw timestamps.
- Rotate through 0°, 90°, 180°, 270°; repeat corner taps and the diagnostic gesture. Rotation changes the emulator physical model and cancels held input. Input uses decoded frame dimensions, with requested orientation metadata after a short settling period. Verify actual Android orientation with the diagnostic log: the settling delay and requested orientation are **not** proof of synchronization. A game may enforce orientation. If input is misplaced or playback continues through an incompatible geometry, this gate fails and must be fixed.
- Move the window between monitors with different DPI, resize it continuously and enter/leave fullscreen. Confirm focus is usable and no hidden input is sent. F11 works in the WPF shell; when keyboard focus is inside the native WebView2 HWND use the adjacent Fullscreen button. Escape in the viewport releases input and exits fullscreen.
- Disconnect/reconnect display while the game is running; Android data must remain. For an unexpected transport loss, the gateway requests pointer releases using gRPC and closes RTC. Reception is still unverified until checked by the diagnostic APK. If both media and gateway fail, releases cannot be guaranteed; stop safely, reconnect and verify/reset interaction before replay.
- For a connection-loss test, terminate only the gateway PID recorded in that run's process sample. Use **Connect / reconnect display** to replace it. Do not kill unrelated emulators or a shared ADB server.
- Gracefully stop/reopen and cold boot. Wait for a diagnostic `save` record with `committed=true` before stopping; its saved-at-launch counter must survive. Test the representative game's real saved progress separately. No AVD recreation, wipe-data or package uninstall is used.

## Sound and performance gate

Check audible output, track sample counters and synchronization. A reported audio track is insufficient. In Windows Volume Mixer, mute the **emulator process** while leaving WebView2/the host audible; sound must still arrive through WebRTC. Toggle **Mute / unmute viewport**: muted WebView2 must silence streamed audio. This avoids mistaking the emulator's direct Windows speaker output for working streamed audio. The diagnostic square and beep are requested every second; compare externally captured visual/audio onset against standalone. Do not introduce a separate unsynchronized audio workaround.

Use the Release host and the same workload/settings for each run. The prototype samples the shell and descendants (including emulator/QEMU, Python and WebView2) at 1 Hz. It reports working set, private bytes and CPU as a percentage of total machine capacity. Browser statistics include decoded/dropped frames, decode time, jitter-buffer counters, packets lost and audio samples. Presentation callbacks produce windowed p50/p95/p99 intervals; they are **not** Android frame times or input-to-visible latency. The summary uses deltas for cumulative decode/jitter counters. Missing counters remain absent, not zero.

For GPU and Android frame timing, collect these alongside host evidence:

```powershell
Get-CimInstance Win32_Processor | Select-Object Name,NumberOfCores,NumberOfLogicalProcessors,VirtualizationFirmwareEnabled
Get-CimInstance Win32_VideoController | Select-Object Name,DriverVersion
& "$taskSdk/platform-tools/adb.exe" -s emulator-5580 shell dumpsys gfxinfo com.androiddesktop.diagnostic framestats | Set-Content diagnostic-framestats.txt
& "$taskSdk/platform-tools/adb.exe" -s emulator-5580 shell dumpsys gfxinfo YOUR.GAME.PACKAGE framestats | Set-Content game-framestats.txt
```

`gfxinfo` does not capture every game's native rendering pipeline; use Android profiling appropriate to that game if counters are unavailable. In Visual Studio use **Debug → Performance Profiler → CPU Usage / GPU Usage / Memory Usage**, targeting the relevant processes; also inspect the emulator and WebView2 in Windows GPU performance tools. Record the selected physical GPU and driver. WMI `AdapterRAM` can truncate high-VRAM cards, so it is not authoritative GPU capacity.

Measure input-to-visible response externally (for example, a 240 FPS camera filming the physical input action and diagnostic colour response), for at least 100 taps in both standalone and embedded conditions. Record trigger frame, response frame and camera FPS; latency is `(responseFrame-triggerFrame)/cameraFps*1000`. Report p50/p95/p99 and uncertainty (one 240 FPS frame is 4.17 ms). Comparing p95 end-to-end values estimates the added transport cost; it does not measure a paired p95 added latency by itself. Define the matched comparison before accepting the plan's additional-latency budget. Do not subtract uncalibrated host/Android timestamps or claim FPS proves responsiveness.

The initial budgets remain 60 FPS where standalone reaches it, p95 additional latency aiming at ≤50 ms, and aspirational sub-100 ms end-to-end response. Record distributions and dropped frames, not only averages. Long soaks, twenty lifetime cycles and recording-overhead validation belong to their later release/recording phases; no result for those is claimed here.

Fill in `RESULTS.md` with actual evidence paths, versions, workload and failures. Phase 0 remains open until compatibility, audio/sync, input/replay, orientation, focus/DPI and performance gates have all been demonstrated on Windows.

## Current UI handoff

Phase 2 replaces the prototype's Choose APK / Install buttons with Open APK and the Explorer drop card, and automatically prepares/connects the device on import. Use `../phase-2/RUNBOOK.md` for current workflow steps. The measurement and hardware acceptance procedures above still apply; prior recorded outcomes remain historical rather than new acceptance claims.
