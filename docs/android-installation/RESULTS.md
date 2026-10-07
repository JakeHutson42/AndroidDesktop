# Local Android installation — 6 October 2026

The user authorized download/integration and explicitly authorized SDK licence acceptance. Installed official stable Android 14 / API 34 Google Play x86_64 image revision 14, emulator 37.2.12, platform-tools 37.0.1, command-line tools 23.0, platform android-34 and build-tools 36.0.0. Build-tools 34.0.0 remain installed from the first diagnostic attempt. Verified portable Eclipse Temurin OpenJDK 21.0.12.1+1 supplies Java. Historical installers do not contain these packages or source changes.

SDK: `artifacts/android-sdk`. Java: `artifacts/android-setup/java`. Persistent AVD: `%LOCALAPPDATA%/AndroidDesktop/avd`. SettingsStore migrated settings to v4, retaining theme/window/selection metadata; runtime paths and 2048 MiB/two CPUs are saved for the active profile. Existing device disks were retained during retries. No wipe, uninstall, credential bypass or device concurrency was used.

## Passed

- Windows WHPX acceleration installed and usable.
- Actual device creation through SetupService and Android 14 boot through EmulatorSessionService: sys.boot_completed=1, SDK 34 and fingerprint recorded in acceptance.json.
- Diagnostic APK built/signed with build-tools 36 and Java 21; installed and launched on the real device.
- Native-window mode exercised actual app StartCommand and ImportFilesAsync, with successful boot/launch and zero embedded connections. Graceful shutdown receipt issued. See acceptance-standalone.json.
- Release build: zero warnings/errors. 115 .NET tests, four Python gateway tests and both Windows host smokes pass. Host smokes themselves do not connect Android.

## Failed and corrected / remaining

- SDK batch wrapper split semicolon package identifiers and returned success despite missing packages. Installer now uses the Java entry point and verifies required files.
- App JavaArguments supplied com.android.sdklib.toolsdir; official avdmanager requires com.android.sdkmanager.toolsdir. Fixed the property and added a contract assertion; real device creation then passed.
- Build-tools 34 D8 crashed on Java 21 diagnostic classes. Adding debug information did not fix it and was reverted. Stable build-tools 36 built the same APK successfully.
- Embedded RTC v2 probe returned PERMISSION_DENIED with emulator 37.2.12. This remains unresolved; authentication and service allowlists were not weakened. Gateway now reports only the RPC status, with a failure/redaction test. The first probe reported a generic HTTP 500; the retained subsequent report has the specific status.
- Enabled the existing ShowStandaloneWindow option and exposed it on Setup. In this explicit mode Start/Open APK skip RTC, Connect display is disabled, and embedded input/recording/replay stays unavailable. Use Android's emulator window for interaction.

## Use

Run `scripts/Start-LocalAndroidApp.ps1`, then **Start device** on Device. The current configuration opens Android inside the app using the embedded controller adapter. Use **Open APK** for a standalone APK; press **Stop** when finished. The launcher uses persistent settings; android.local.json is retained for diagnostics. Keep the project SDK/Java folders in place because settings reference them.

On Setup, leave **Use Android's own emulator window** off and **Use embedded controller display** on. Save setup before starting. Disabling controller display requests the provisional WebRTC transport that failed the probe. The separate-window option remains available. The diagnostic APK is diagnostic-apk/build/Phase0Diagnostic.apk; validation installed it without replacing the persistent user library.

To repeat real validation with the app closed:

```powershell
dotnet run --project tools/android-acceptance/AndroidAcceptance.csproj -c Release -- . --standalone
```

The harness configures the active profile, creates a device if necessary, boots Android, installs the diagnostic APK, stops the device and writes acceptance-standalone.json. Omit --standalone only to reproduce the embedded probe. Install-LocalAndroid.ps1 -AcceptSdkTerms reuses downloaded tools/Java and installs the listed packages; the switch requires prior agreement to Google's terms.

## Unverified

The later embedded update verifies frames and one pointer down/up. Native window quality, embedded audio, full multi-touch/keyboard/playback reception, representative games, ARM translation, actual game save retention, backups/restoration, profile isolation, twenty cycles/two-hour soak and release/signing/redistribution gates remain open. Reported arm64-v8a capability alone does not certify an ARM-only game. No Google login was performed. These results supplement historical Phase 0–7 reports without retroactively certifying them.

## Embedded display update — later on 6 October

The user required display inside the app. Added an authenticated local EmulatorController PNG/input adapter using the installed emulator's official protocol. It renders through a canvas-backed video track inside the existing WebView2 viewport and reuses touch, keyboard, geometry, cancellation and recording/replay adapters. The SDK's authentication/allowlists remain unchanged; gateway endpoints remain loopback-only. The failed experimental RTC service was not certified or made unauthenticated.

One acknowledged frame is in flight, bounded to 8 MiB/four million pixels and at most 15 requests/sec. Readiness requires the first decoded frame; stale/oversized/mismatched frames fail closed. Input shares the authenticated socket, validates supported key/touch fields and bounds, and releases touches/held keys on disconnect. One controller socket owns input at a time. Settings/profile snapshots preserve the transport option; runtime identity records it to prevent incompatible replay. No device concurrency or image cloning was added.

Real WPF/WebView2 verification in embedded.json passes Android boot, frames/input readiness, diagnostic APK launch, a new tap received in Android and graceful shutdown. The captured viewport preview embedded.json.png was inspected and displays the diagnostic app's updated touch count. Initial short run: 55 decoded frames over about nine seconds, presentation callback p50 about 130 ms/p95 about 230 ms, with browser frame drops reported. This is a working interactive display, not gaming-performance acceptance or input-latency measurement. Audio is not carried by this adapter and its browser mute button is disabled. Native host audio and actual game performance remain unverified.

116 .NET tests, 26 browser tests and six Python gateway tests pass, including persisted controller configuration, decoded-frame acknowledgement, disconnect during decode, malformed dimensions, single-frame backpressure, rejected input, second-owner refusal and disconnect release. A shell smoke was accidentally started while the real app test held the single-instance mutex; it timed out and was stopped by its runner. Repeating after closure passed both standard/composition hosts. The final real run in embedded-final.json again passed frames/tap/shutdown; its inspected preview shows saved touch count 1 recovered after reboot and incremented to 2. That is limited diagnostic save evidence, not backup/game acceptance. The first native-window and failed RTC results above are historical; current default is embedded controller mode. Multiple active devices remain unapproved and disabled.

For explicit real embedded validation, with the app closed and controller mode saved:

```powershell
& ./src/AndroidDesktop/bin/Release/net10.0-windows10.0.19041.0/AndroidDesktop.exe --embedded-test ./docs/android-installation/embedded.json ./diagnostic-apk/build/Phase0Diagnostic.apk
```

This opt-in command boots the active device, opens the diagnostic APK, injects one tap through the browser, captures its viewport and diagnostic log, then stops and closes. Reports are local and can contain displayed Android data; use the supplied diagnostic APK only. No SDK/Java dependency was added by this display adapter. Official [controller protocol](https://android.googlesource.com/platform/external/qemu/+/89b154f521ba3ba13406d60c47c4c01da8ff219e/android/android-grpc/emulator_controller.proto).

Official sources: [SDK manager](https://developer.android.com/tools/sdkmanager), [SDK terms](https://developer.android.com/studio/terms), Google's repository2-1.xml and Google Play sys-img2-1.xml retained in artifacts/android-setup. SDK Manager installed the image; command-line archive SHA-1 and Java archive SHA-256 were independently checked before extraction.

## Audio update — 7 October 2026

The controller's earlier audio limitation is superseded by the separate authenticated PCM/AudioWorklet adapter. Real Android packets and nonzero rendered samples pass with native host sound disabled; graceful shutdown and observed app/emulator/gateway/browser process exit pass. The short real run reports zero ring drops/underruns. Physical listening, controlled performance comparison and extended gaming acceptance remain unverified. These audio runs failed the diagnostic new-touch check. See [implementation, evidence and checks](AUDIO.md); historical results above are preserved. Append --audio-test to the existing embedded-test command for the opt-in audio diagnostics. No dependencies were added; Release source was rebuilt, historical release candidates remain unchanged.
