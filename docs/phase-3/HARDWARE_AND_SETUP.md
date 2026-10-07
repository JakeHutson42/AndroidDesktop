# Android Desktop 0.3.0 candidate: setup and hardware acceptance

This package is an evaluation candidate. No emulator/image or final transport is certified. Windows shell and dependency tests do not establish streamed sound, gameplay, saves or performance. The private Python gateway and display assets are included; users do not need Node, npm, a development server, Python installation or a virtual environment. Android SDK/images, compatible Java and WebView2 remain official external prerequisites and are not redistributed.

## Clean Windows setup

1. On Windows 11 x64, install the candidate installer or extract the publish ZIP. Launch Android Desktop. It is self-contained with respect to .NET. Open Setup and Check prerequisites. The installed gateway/runtime paths are detected automatically, including after upgrades from developer paths.
2. Obtain the official Windows command-line tools from https://developer.android.com/studio#command-tools. Extract into `%LOCALAPPDATA%/Android/Sdk/cmdline-tools/latest` with `bin` and `lib` directly inside `latest`. Obtain a Java runtime compatible with the chosen tools (or reuse Android Studio's compatible `jbr`). Use https://developer.android.com/tools/sdkmanager and its version requirements. No Android Studio project is needed.
3. Install Microsoft's Evergreen WebView2 Runtime if missing: https://developer.microsoft.com/microsoft-edge/webview2/. Review the official installers' license terms, download sizes and disk requirements. No license is accepted by this application. Check sufficient space for unpacking, image, AVD and game data; the app's 4 GiB creation floor is not a complete storage budget.
4. Set the following paths to your real installations. API 34 Google Play/x86_64 is a test candidate, not a supported runtime promise. If the representative APK needs another stable image, explicitly select it and document the reason. Do not use prerelease channels.

```powershell
$taskSdk = "$env:LOCALAPPDATA/Android/Sdk"
$taskJava = 'C:/Program Files/Android/Android Studio/jbr'
$env:JAVA_HOME = $taskJava
& "$taskSdk/cmdline-tools/latest/bin/sdkmanager.bat" --sdk_root=$taskSdk --licenses
& "$taskSdk/cmdline-tools/latest/bin/sdkmanager.bat" --sdk_root=$taskSdk 'platform-tools' 'emulator' 'platforms;android-34' 'build-tools;34.0.0' 'system-images;android-34;google_apis_playstore;x86_64'
& "$taskSdk/emulator/emulator.exe" -accel-check
& "$taskSdk/emulator/emulator.exe" -version
```

5. If acceleration fails, enable Windows Hypervisor Platform in Windows Features and firmware virtualization, reboot, reopen Setup and recheck. This application does not change Windows features or assume a reboot succeeded. CPU acceleration does not prove GPU acceleration.
6. Enter SDK, command-line-tools and Java executable paths in Setup, Save paths, Check prerequisites, then Create owned device. Existing app-owned devices are retained. Cancelled downloads are resumed through the official SDK Manager; reopen/recheck afterwards. Partial AVD creation is reported and never overwritten. Do not remove a partial directory if it contains game data.
7. Use Open APK or drop exactly one standalone `.apk`. AAB/APKS/XAPK, split sets and separately supplied OBB are outside this release. Accepting reported ARM ABIs does not prove translation works. Compatibility and Google attestation can still prevent gameplay.

## Runtime gate: first required validation

From the source checkout, build the diagnostic APK after SDK setup:

```powershell
./scripts/Build-DiagnosticApk.ps1 -SdkRoot $taskSdk -JavaHome $taskJava
./scripts/Collect-Runtime.ps1 -SdkRoot $taskSdk -JavaExecutable "$taskJava/bin/java.exe" -SystemImage 'system-images;android-34;google_apis_playstore;x86_64' -OutputFile artifacts/hardware-runtime.json
```

Open `diagnostic-apk/build/Phase0Diagnostic.apk`, then your representative standalone game. Keep package/hash/version, save location within the game and workload in a local acceptance report. Collect diagnostic receipts without clearing shared logcat:

```powershell
& "$taskSdk/platform-tools/adb.exe" -s emulator-5580 logcat -v raw 'Phase0Diagnostic:I' '*:S' | Tee-Object -FilePath artifacts/diagnostic-log.txt
```

Perform these tests and record **passed / failed / unverified**, evidence path and runtime combination for each:

| Gate | Exact exercise and required evidence |
|---|---|
| Compatibility | Install and launch diagnostic + game; record device SDK/ABI and APK requirements. Run an ARM workload if translation is claimed. |
| Streamed synchronized sound | Mute only the emulator process in Windows Volume Mixer. Leave WebView2 audible. Diagnostic beep must still arrive; viewport mute must silence it. Externally capture diagnostic beep/flash synchronization, compare standalone, then verify game sound. A track counter alone fails to prove this. |
| Touch | Tap center and four corners, hold 5 seconds, swipe, exit rectangle while held, rapid taps; verify Android logs/coordinates and zero held contacts afterwards. Test simultaneous real touchscreen contacts; mouse clicks cannot certify multitouch. |
| Rotation | Rotate four ways and repeat corner/hold/swipe tests; a game enforcing orientation must use its actual decoded geometry. Stop if coordinates drift. |
| Focus/fullscreen/DPI | Focus away while held, switch tabs, F11/Escape and toolbar fullscreen; drag between 100/150/200% monitors and resize. Verify receipts and releases. |
| Offline | Disconnect internet after setup. Reconnect local display and verify video, streamed audio and input without hosted assets/STUN/TURN. Game online services can require internet separately. |
| Reconnect/crash | Stop only the gateway PID from this run's process evidence, click Connect display. Repeat with WebView2 failure on a disposable test session. No AVD recreation; verify all contacts released and save retained. |

The RTC v2 protocol comes from an experimental emulator branch. `UNIMPLEMENTED`, JWT-only discovery/authentication failure, missing audio or failed input are gate failures. Never disable local authorization to work around them. Preserve error evidence, do not select WebRTC. Evaluate a stable matching scrcpy client/server pair using `scrcpy --serial=emulator-5580 --max-size=1280 --max-fps=60 --require-audio --print-fps`; record version, sound, latency, multitouch, focus, DPI and recording-access findings. This candidate does not contain an embedded native scrcpy adapter. Native integration needs its own proven gate before distribution.

## Persistence, recovery and upgrades

Use a disposable game account where appropriate. Save meaningful real progress and wait for its commit indication. Close gracefully, reopen same package, then repeat cold boot. Record `%LOCALAPPDATA%/AndroidDesktop/avd/.../android-desktop-device-id` identity; the `.ini`/device disk location must remain stable. This prototype always uses `-no-snapshot-load`, so invalid Quick Boot is bypassed without deleting snapshots or disks. Test an invalid existing Quick Boot only on a dedicated validation AVD; do not corrupt production device files.

Install a compatible signed newer APK and verify the game's saved progress. Reject a signature-conflicting APK and a lower version, verify previous selection and game progress remain. Select a different APK, then return to the original and verify data. Never uninstall or clear data as a test shortcut. For a desktop upgrade, record theme, bounds, selection, device UUID and game save; install over the same AppId/directory, reopen and confirm all remain. Developer paths must switch to the packaged private sidecar. Uninstall the desktop shell and reinstall: app-owned LocalAppData/AVD must remain.

## Performance and stability

Use Release builds and the same AVD/image/game settings for all comparisons. For standalone, stop the host first; run the official emulator with the same AVD home and ports, `-gpu auto -no-snapshot-load`, using its native window and no gateway. Install/launch only the same APK using selected-serial adb. Stop it with `adb -s emulator-5580 emu kill` before the embedded run. Do not run both against the same disk concurrently.

```powershell
$env:ANDROID_AVD_HOME = "$env:LOCALAPPDATA/AndroidDesktop/avd"
& "$taskSdk/emulator/emulator.exe" -avd AndroidDesktop_Phase0 -port 5580 -gpu auto -no-snapshot-load
# In another terminal, after boot:
& "$taskSdk/platform-tools/adb.exe" -s emulator-5580 shell dumpsys gfxinfo YOUR.GAME.PACKAGE framestats | Set-Content artifacts/game-framestats.txt
# End standalone before opening the host:
& "$taskSdk/platform-tools/adb.exe" -s emulator-5580 emu kill
```

Profile standalone and embedded for matched 10-minute warmed-up workloads. Use Windows/Visual Studio CPU, GPU and memory profiling for the emulator/QEMU, WebView2, shell and gateway; count the whole process group. Report p50/p95/p99 frame time, dropped frames, decode/buffering counters, CPU/GPU and private/working-set memory. Browser presentation intervals are not Android frame times; gfxinfo may omit native game rendering. Export support after stopping to capture flushed media/process counters; local raw measurements are under `%LOCALAPPDATA%/AndroidDesktop/phase0-evidence`.

Measure at least 100 physical-input-to-visible-response trials per condition with an external high-speed camera, noting camera FPS and trigger/response frames. Compute distributions and sampling uncertainty; do not subtract uncalibrated host/Android clocks. Pair equivalent trials to assess additional latency. Budgets remain **unconfirmed**: usable shell within 2 s; comparable 60 FPS where standalone achieves it; p95 added latency aiming at <=50 ms; aspirational end-to-end <100 ms. Revise only with reported evidence.

Run two hours of actual gameplay with bounded diagnostics enabled after warm-up. Log saves, reconnects, errors and process-group memory at one-minute intervals; compare steady-state trend, not only final peak. No Phase 4 recorder is required. Then perform twenty real Start/Open/Stop cycles, waiting for Stopped every time. After each, check recorded owned PIDs have exited and connection/process counts do not accumulate. Reopen the save on cycles 1, 10 and 20. Twenty shell-only smoke launches do not count. Capture every failure and recovery, not just successful cycles.

## Clean-PC matrix and release hold

Test on a fresh physical Windows 11 x64 PC as well as an upgrade installation: existing SDK; missing SDK/Java/WebView2; unavailable acceleration; low storage; interrupted official downloads/setup; image revision mismatch. Confirm usable shell, actionable guidance, cancellation/reboot resume and retained data. A VM with insufficient GPU/nested acceleration cannot certify gameplay. Use offline installation of the shell ZIP/installer to establish that private gateway dependencies are present; first-time official Android/WebView setup may require network.

Fill runtime-candidate.json's hardware fields only from actual measurements, attach the completed matrix, address failures, resolve Corvus redistribution authorization and full third-party license review, then explicitly choose the transport and supported runtime. Until then, publish/installer artifacts remain unvalidated candidates. Check signatures/installer trust prompts separately; these candidate artifacts are unsigned.
