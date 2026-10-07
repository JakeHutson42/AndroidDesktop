# Phase 1 local runbook

Run from the repository root on Windows 11 x64 with the .NET 10 SDK:

```powershell
dotnet build AndroidDesktop.sln -c Release --no-restore
dotnet test AndroidDesktop.sln -c Release --no-restore
dotnet run --project src/AndroidDesktop -c Release --no-build
```

For a fresh checkout, first prepare dependencies with `scripts/Prepare-Phase0.ps1` as documented in [Phase 0](../phase-0/RUNBOOK.md). No new NuGet dependencies were added in Phase 1.

The shell appears before prerequisite detection finishes. Open **Setup**, review the detected paths and missing items, then edit paths as necessary. **Save paths** persists them; **Check prerequisites** probes Java/SDK compatibility, gateway dependencies and emulator acceleration. Device operations disable setup editing. Stop the device before changing paths.

Settings and the stable device home are `%LOCALAPPDATA%/AndroidDesktop/settings.json` and `%LOCALAPPDATA%/AndroidDesktop/avd`. JSON writes are serialized and atomic with a previous committed `.bak`; bounds/theme updates debounce for 600 ms and flush on close. Newer schema files are protected from overwrite. Theme changes take effect immediately. The Settings page/footer reports persistence failures.

## Missing dependencies and licenses

Obtain Windows SDK command-line tools from [Google](https://developer.android.com/studio#command-tools). Extract them so `cmdline-tools/latest` contains `bin` and `lib`. Use a compatible JDK (the selected tools' required version; the check invokes the SDK tool through that Java runtime). No Android Studio project is required. For an existing Android Studio installation, its SDK Manager and compatible bundled Java may be reused.

Accept licenses yourself, and install the initial test image using official tools. Review their actual package/download sizes; the app does not estimate an unknown size or download anything automatically:

```powershell
$taskSdk = "$env:LOCALAPPDATA/Android/Sdk"
$taskJava = 'C:/Program Files/Android/Android Studio/jbr'
$env:JAVA_HOME = $taskJava
& "$taskSdk/cmdline-tools/latest/bin/sdkmanager.bat" --sdk_root=$taskSdk --licenses
& "$taskSdk/cmdline-tools/latest/bin/sdkmanager.bat" --sdk_root=$taskSdk 'platform-tools' 'emulator' 'platforms;android-34' 'build-tools;34.0.0' 'system-images;android-34;google_apis_playstore;x86_64'
& "$taskSdk/emulator/emulator.exe" -accel-check
```

[SDK Manager documentation](https://developer.android.com/tools/sdkmanager) describes installation and license handling. API 34 Google Play/x86_64 remains a test selection, not a compatibility promise or finalized runtime pin. Select a different installed stable image explicitly if the target APK requires it. No automatic prerelease updates.

If acceleration fails, enable Windows Hypervisor Platform through Windows Features and firmware virtualization as needed, reboot and recheck. CPU acceleration does not certify graphics acceleration. If WebView2 is missing, install Microsoft's [Evergreen Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).

Prepare the gateway with the existing [Phase 0 preparation steps](../phase-0/RUNBOOK.md), then enter its folder and `.venv/Scripts/python.exe` in Setup. This is still a development sidecar; release packaging is Phase 3.

## Create and start a persistent device

1. Save the configured SDK, command-line tools, Java and gateway paths.
2. Check prerequisites; resolve missing files, incompatible Java/SDK tools, gateway imports, insufficient space or unavailable acceleration.
3. Set the installed system-image package identifier. Click **Create owned device**. The service invokes the installed SDK's `AvdManagerCli` through Java with explicit arguments and `ANDROID_AVD_HOME`, and answers only the custom-hardware-profile prompt with `no`. It does not accept licenses or use `--force`. At least 4 GiB free is required before creation; this is a conservative creation floor, not a game storage guarantee.
4. If an existing complete app-owned device exists, it is retained. A partial `.avd` directory or `.ini` from an interrupted creation is not overwritten. Inspect those app-owned files before retrying; don't delete a device containing data.
5. From **Device**, start the emulator and connect display. Keep using the [Phase 0 diagnostic/game runbook](../phase-0/RUNBOOK.md) to establish actual media and input acceptance.

No reinstallation or device recreation occurs on shell resizing or app upgrade. Optional Phase 0 configuration import remains available:

```powershell
dotnet run --project src/AndroidDesktop -c Release --no-build -- ./phase0.local.json
```

Without this argument, saved desktop settings are used. Explicit import overrides runtime settings for this launch; Save paths commits them. Standard/composition mode remains in the runtime JSON for transport comparison; the application configures the matching control before its first initialization.

## Desktop acceptance steps

- Switch all four palettes in Settings. Resize the window, maximize it, restore it and close/reopen; confirm theme and normal bounds persist. Move the saved monitor away/disconnect it, then reopen and confirm the title bar is accessible. Fullscreen bounds must not replace normal bounds.
- Move between monitors at 100%, 150% and 200% scaling, resize and switch pages. Verify text, controls, focus and device coordinates. The per-monitor manifest and restoration smoke check do not certify these interactions.
- With missing SDK/Java paths, check that the shell remains responsive and reports missing items. Supply an incompatible Java runtime and verify an actionable tool failure. Correct paths and recheck.
- Cancel a running prerequisite check/device creation, then reopen and recheck. Saved paths remain. Reboot for virtualization if needed and resume. Do not mark cancelled partial device creation as ready.
- Start a device and confirm Save paths / Check prerequisites / Create device and setup fields are disabled. During setup, device Start is disabled. Cancellation does not terminate another user's AVD or shared ADB server.
- Test a real installed image/device creation, SDK licenses, low disk space and interruption only on a suitable local environment. Those scenarios were not available in this implementation session.

To repeat isolated native shell checks without creating a device or changing production settings:

```powershell
./scripts/Smoke-Host.ps1 -OutputDirectory docs/phase-1
```

This briefly starts hidden standard/composition host windows, exercises page navigation, theme resources, minimum bounds and prerequisite discovery, and loads packaged assets. Run on a normal Windows desktop; sandboxed browser initialization can fail. It does not test Android media, multi-monitor interaction, visible layout quality, APK saves or performance acceptance. See [results](RESULTS.md).
