# Phase 2 local validation

This is the Phase 2 implementation handoff, not completed Android hardware acceptance. Read [the plan](../IMPLEMENTATION_PLAN.md) and [results](RESULTS.md). WebRTC remains provisional.

## Build and prepare

Follow [Phase 1 SDK / Java / licenses / gateway setup](../phase-1/RUNBOOK.md) and [Phase 0 diagnostic APK preparation](../phase-0/RUNBOOK.md). No new external dependencies were added in Phase 2.

From the project root:

```powershell
# After initial pinned dependency preparation:
dotnet build AndroidDesktop.sln -c Release --no-restore
dotnet test AndroidDesktop.sln -c Release --no-restore
Push-Location tools/viewport
node build.mjs
if ($LASTEXITCODE) { throw 'Viewport build failed' }
node --test test/*.test.mjs
if ($LASTEXITCODE) { throw 'Viewport tests failed' }
Pop-Location
# Build again after bundling to copy the updated assets:
dotnet build AndroidDesktop.sln -c Release --no-restore
./scripts/Smoke-Host.ps1 -OutputDirectory docs/phase-2
dotnet run --project src/AndroidDesktop -c Release --no-build
```

Set missing paths in Setup, save them, check prerequisites and create/retain the app-owned device. Prepare a standalone diagnostic APK and representative game APK. Use only a compatible installed stable image; record exact versions. The existing pinned source protocol has not certified any released Windows emulator's RTC v2 service.

## Normal APK workflow

- Click **Open APK**, or drop exactly one `.apk` onto the app information/drop card above the viewport. The WebView2 native viewport is not the Explorer drop target; its navigation remains restricted.
- The same importer validates picker/drop, prepares the device/display, reads metadata, checks device compatibility, installs when required and launches. Multiple files, AAB/APKS/XAPK, split manifests and separate OBB data are rejected with an explanation.
- Confirm the app name (or package-ID fallback), version and source path appear after success. Only one successful selection is retained. Replacement doesn't uninstall any other app.
- **Launch** rereads the source/checksum and confirms the expected installed package/version on the same device before skipping installation. A missing package or changed source is installed using `adb install -r`. Query/transport failures are not treated as absence. A missing source asks you to locate it with Open APK; it does not invent a checksum or silently select something else.
- **Relaunch** follows the same checks, then force-stops only the selected Android package before explicitly launching its activity. It never clears app data. Games must still perform their own saves.
- **Back**, **Home**, **Volume −/+**, mute, rotate and fullscreen are adjacent WPF controls. Device keys use atomic keypress events on the persistent input channel. Controls enable only after the actual input channel reports ready; microphone/host volume is not changed by Android volume keys. Google Play is reachable through Android Home.
- Rotate requests the virtual-device rotation. Pointer coordinates use actual decoded frame dimensions and contain-fit geometry, including games that enforce orientation. The Phase 0 requested-orientation label/settling behavior still needs diagnostic validation; it is not evidence of actual Android rotation.
- Use **Stop** or close the window for graceful asynchronous shutdown. Force stop becomes available in Diagnostics only if shutdown fails/times out, and acts only on owned host processes. Closing releases input and flushes settings even when shutdown fails. Another session's AVD/ADB server is never intentionally terminated.

Settings schema v2 migrates Phase 1 themes/bounds/runtime paths and adds `SelectedApk` metadata: source/checksum, package/name/version/requirements, device UUID, last successful installation and launch timestamps. Selection changes are committed after successful install/launch. The UUID marker lives inside the actual app-owned `.avd` directory (`android-desktop-device-id`), not the reused serial/port. Removing/recreating a whole AVD directory yields a different marker. No Android game-save contents are stored in desktop settings.

A failed install or launch leaves the previous desktop selection. If Android finishes an update before a later launch, cancellation or settings-save failure, that update may already exist on the device; the app does not roll back or delete its data. Fix the error and retry. Source files are held read-only through inspection/transfer to prevent bytes changing under their checksum.

## Exact Android acceptance sequence

These checks require installed Android tools and real APKs; none has passed in this session.

1. Record `emulator.exe -version`, image `source.properties`, acceleration output, hardware/driver information, selected AVD path/UUID and the APK SHA-256. Use the same device for comparisons.
2. Run the diagnostic workload and capture its events/save markers:

```powershell
$taskSdk = "$env:LOCALAPPDATA/Android/Sdk" # change if you use another SDK
$taskJava = 'C:/Program Files/Android/Android Studio/jbr' # change as needed
./scripts/Build-DiagnosticApk.ps1 -SdkRoot $taskSdk -JavaHome $taskJava
& "$taskSdk/platform-tools/adb.exe" -s emulator-5580 logcat -v raw 'Phase0Diagnostic:I' '*:S' | Tee-Object -FilePath ./diagnostic-phase2-log.txt
```

Open `diagnostic-apk/build/Phase0Diagnostic.apk`. Tap/hold/swipe/multitouch, then rotate while held and verify received releases and coordinates. Verify Back, Home, Android volume changes and actual streamed audio. Follow the Phase 0 audio procedure to exclude the emulator's own host audio from the test.

3. Stop/close, reopen and click Launch. Confirm the persistent diagnostic counter remains, the AVD path/UUID stays the same and the session log reports verified installed launch without reinstalling. Confirm the selected app metadata, theme and bounds also remain. Inspect the evidence JSONL for the run's identity/version.
4. Test a cold boot: the current session already uses `-no-snapshot-load` without wiping device disks. Play/save in a representative game, close and reopen, then verify the game's actual saved progress. Use an offline save mode where available and identify server-managed state separately.
5. Build a signed standalone v2 update of the diagnostic/game test app using the same signing key and a greater version code. Open it and confirm data/counters remain. Then attempt an APK with a conflicting signing key and an older version code. Confirm clear errors, prior selection and saved data. Do not uninstall/clear to make the test pass.
6. Open a different package, then re-open the first APK and verify its existing Android data remains. No library screen should appear.
7. Rename/move the selected source APK, click Launch and verify the missing-source message. Locate it through Open APK. Replace bytes at the same source path with a different valid version and verify installation occurs rather than a stale metadata shortcut.
8. Test actual absence using a disposable diagnostic package, not the user's game: uninstall that diagnostic app deliberately through Android Settings (this deletes that diagnostic data), then click Launch and verify the app reinstalls. This is a manual test action; the desktop importer never uninstalls automatically.
9. Drop two files, a folder, each unsupported format and a known split APK; confirm explanations and unchanged selection. During startup/import, try Open/drop/Launch/rotate/Setup edits and verify conflicts are blocked. Cancel import and confirm the previous selection remains; a retry reconciles any Android install that already completed.
10. Switch pages/lose focus/leave the viewport while holding input. Verify all active pointers/keys release in the diagnostic APK. Test fullscreen and movement across 100/150/200% DPI monitors. Repeat rotation with a game that enforces portrait/landscape; check all corners against actual decoded geometry.
11. Disconnect the display and use Connect display; confirm the running AVD and game data persist. Test offline local media per Phase 0. If RTC probe/audio/input fails, preserve evidence and resolve that transport before claiming acceptance.
12. Close during an operation and confirm visible cancellation/shutdown with no UI freeze. If graceful stop fails, confirm explicit Force stop is available; do not manufacture failures by killing unrelated processes. Reopen afterwards and inspect saves.

Use [Phase 0 measurement steps](../phase-0/RUNBOOK.md) for standalone/embedded latency, buffering and frame timing. A shell smoke and fake-tool tests cannot certify these metrics. Long soaks, repeated-cycle certification, installer and fresh-PC work are Phase 3.

## Source contracts

- [Official APK Analyzer commands](https://developer.android.com/tools/apkanalyzer): version identity and resource-label lookup.
- [Android PackageManager shell source](https://android.googlesource.com/platform/frameworks/base/+/refs/heads/main/services/core/java/com/android/server/pm/PackageManagerShellCommand.java): `pm path` absence returns 1 with no package path.
- The vendored `KeyboardEvent` protocol documents `GoBack`, `GoHome`, evdev codes and atomic keypress event type. Input contract tests verify encoded transport payloads; actual device receipt is still unverified.
