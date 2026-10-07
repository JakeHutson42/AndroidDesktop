# Phase 4 local validation

Earlier hardware gates remain required. Start with ../phase-3/HARDWARE_AND_SETUP.md and ../phase-0/RUNBOOK.md; use an official SDK, compatible Java and an app-owned persistent AVD. Keep the exact runtime/device configuration unchanged between recording and replay. A runtime mismatch is rejected, not silently migrated.

## Build and launch

From the workspace root, using Node and .NET already installed:

```powershell
Push-Location tools/viewport
npm ci --ignore-scripts
node build.mjs
Pop-Location
dotnet restore AndroidDesktop.sln --locked-mode
dotnet test AndroidDesktop.sln -c Release --no-restore
node --test tools/viewport/test/*.test.mjs
./scripts/Smoke-Host.ps1 -OutputDirectory docs/phase-4
dotnet run --project src/AndroidDesktop -c Release --no-build
```

The Node build is a development step; existing Phase 3 installers do not contain Phase 4. Configure SDK, Java, gateway and persistent device in Setup following the existing runbooks. If RTC support/audio fails, preserve the error and evaluate the plan's native alternative before declaring acceptance. Do not clear data/recreate the AVD to make replay work.

Build the diagnostic APK with actual installed tool versions (34 is an example, not a certified runtime):

```powershell
./scripts/Build-DiagnosticApk.ps1 -SdkRoot 'C:\Android\Sdk' -JavaHome 'C:\Java\jdk-17' -BuildToolsVersion '34.0.0' -ApiLevel 34
```

Open the generated diagnostic APK using Open APK. With the actual app-owned serial shown in session evidence, capture Android reception in a separate PowerShell window:

```powershell
& 'C:\Android\Sdk\platform-tools\adb.exe' -s 'emulator-5554' logcat -v monotonic -s Phase0Diagnostic:I > docs/phase-4/android-reception.txt
```

Replace paths/serial with the selected device. Do not kill a shared ADB server. Android timestamps use uptime; host recordings use relative performance.now. Correlate received event order/positions and separately calibrate clocks or use high-speed visible-response measurement for latency. Host dispatch timestamps alone cannot establish reception or end-to-end latency.

## Record, replay and interrupt

1. In Device, open Input automation. Record a short diagnostic sequence: rapid taps, a long hold, swipe, supported simultaneous touches and a held keyboard key. Include a pointer leaving the viewport while held. Finish and inspect manifest.json/events.jsonl under `%LOCALAPPDATA%\AndroidDesktop\automation\recordings`. Confirm a complete manifest, hash/count and ordered down/move/up or cancel sequences. Compare with Android logs. Home/system keys can leave the diagnostic app and may not be observable by it.
2. Restore the same diagnostic state. Replay once with variations disabled. Compare touch/key sequence, coordinates, pressure mapping and actual Android releases. Check runs/run-*/run.json, dispatch.jsonl and result.json. A complete dispatch archive is not proof of Android reception. Prepare the same representative game state manually and repeat; preserve its normal saves.
3. Set three loops and a 1000 ms delay. Verify each loop starts at event zero, has released input at its boundary and preserves event order. The game is not reset automatically. Test infinite loops only while attended, then Stop. Confirm no late events or held pointers/keys in the Android log.
4. Request Pause during a hold/swipe. It waits until all input is released; Resume shifts subsequent deadlines without creating new touches. Use Stop for immediate interruption. WPF buttons taking browser focus do not stop playback; deactivating the desktop window does. Verify external focus loss, Escape, rotation, fullscreen/mixed-DPI movement, connection loss and closing during a hold. Geometry change stops replay instead of transforming future coordinates blindly. Confirm release/fallback receipt on Android; label failed or unobservable release explicitly.
5. Interrupt a recording, or in a disposable test recording close the host process before finalization. Reopen and Recover interrupted using its manifest. Verify the original bytes remain unchanged, the recovered copy drops only an unfinished final row, adds synthetic releases and can be reviewed/replayed. Middle corruption must fail. Test write denial/full disk on disposable output only; capture must report incomplete evidence rather than silently lose events or block gameplay.
6. Enable variations with a saved seed and bounded coordinate/path/timing values. Replay two runs from matching app state and compare transformed event parameters. Original events must remain unchanged. Record what your own detector observes in the notes field and Save observation; each run retains parameters/results. Clamping and monotonic timing can reduce the requested variation. Do not interpret variations as concealment or automatic game-state recovery.

## Performance and acceptance evidence

Use Release and the same device/game/workload/resolution. Compare recording disabled/enabled and replay separately. Record runtime/image/protocol/Java/WebView2/GPU/acceleration versions using Phase 3 evidence. Measure process-group CPU/GPU/memory, frame-time p50/p95/p99, dropped frames, buffering and visible-response latency distributions. Confirm or revise the plan's <5% recording throughput regression budget using evidence. Run long-session recording and cancellation/reconnect checks, preserving game saves. Retain Phase 3's separate two-hour stability and twenty lifecycle checks; fake-channel tests do not substitute for them.

Report each requirement passed, failed or unverified with workload, versions, logs and measurement method. Phase 4 acceptance requires actual diagnostic/game sequence, geometry, loop state and stop behavior. Recording files can contain package identity and input traces; default support export excludes raw input. Share such files deliberately.
