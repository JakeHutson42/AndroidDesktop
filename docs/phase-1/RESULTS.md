# Phase 1 implementation and verification results

Recorded 4 October 2026. The user authorized advancing to Phase 1. Existing Phase 0 hardware acceptance remains unverified; no new transport certification is claimed.

## Implemented

- Resizable Device / Setup / Settings shell, minimum usable dimensions, adjacent WPF diagnostics, native fullscreen and retained per-monitor DPI manifest.
- Corvus Cyberpunk / Ultraviolet / Crimson / Arctic palette switching through dynamic semantic resources, shared button states and themed tab navigation. Re-inspected the local `Corvus.App/Services/ThemeService.cs` and `MainWindow.xaml` before changes.
- `DesktopSettings`, `SettingsStore` and `DesktopViewModel`: independent LocalAppData root, schema v1 with legacy-default migration and future-schema protection, serialized atomic JSON writes, unique temporary file, durable flush, previous commit backup, corrupt-primary recovery, 600 ms debounce and close-time flush. Failures appear in the shell.
- Window normal bounds/maximized state restoration, title-bar monitor visibility fallback and exclusion of fullscreen bounds from saved geometry.
- `SetupService` and `DeviceStorageService`: SDK / Java / command-line tool / gateway discovery, WebView2 availability, actual free disk, Java-to-SDK compatibility check, prepared gateway import check and emulator acceleration check. Missing downloads/licenses are guided through official tools; no invented download sizes or automatic license acceptance.
- Create/retain one application-owned persistent AVD using the installed official Java tool libraries, explicit arguments, bounded process output, timeout/cancellation and standard-input support. No force-overwrite or modification of other profiles. Four GiB free is a creation floor; selected images/game storage may need more.
- Shell shows before settings discovery/check completion. Saved setup resumes through recheck after cancellation/reboot. Setup editing and commands conflict-protect against active device operations.
- Existing Phase 0 experiment remains available. Optional runtime JSON import and standard/composition comparison are retained. No Phase 2 product importer/unchanged-APK optimization or later-phase systems added.

## Executed checks

| Check | Observed result |
|---|---|
| Release solution build | Passed, zero warnings/errors |
| .NET tests | 25 passed, zero failed/skipped (12 existing + 13 Phase 1 cases) |
| Settings tests | Serialized commits and backup; corrupt recovery; legacy migration; future-schema protection; real locked-file save failure |
| Setup tests | Image validation including ARM ABI names; rejected non-owned root invokes no tools; argument/path preservation; real standard-input process output/closure |
| Native standard WebView2 smoke | Passed: pages, live theme resource switch, minimum restored bounds, prerequisite-check completion and packaged viewport loaded |
| Native composition WebView2 smoke | Same checks passed |
| Runtime | WebView2 154.0.4258.53 |
| One shell-render callback | Standard 323.66 ms; composition 452.74 ms. These single hidden runs are not statistically valid readiness/performance acceptance measurements. |

Reports: `smoke-standard.json`, `smoke-composition.json`. Smoke uses a nonpersistent settings model and starts no emulator. It performs current prerequisite discovery; it does not install dependencies, accept licenses or create a device.

The first sandboxed host attempt failed with WebView2 COM error `0x800700AA`, recorded in `failed-sandbox-smoke.json`. That run also exposed the smoke harness's initial busy flag preventing closure; the harness was corrected. Fresh unsandboxed native Windows runs subsequently passed, including prerequisite checks. No failed run is presented as a pass.

An initial unit run rejected `arm64-v8a` because the image validator omitted hyphens. The validator was corrected and the full suite rerun successfully.

## Still requires local validation

Android SDK / compatible Java / image / target APK were not supplied. Actual SDK tool Java launching, AVD creation and license-installation workflows are implemented or guided but have not been executed against installed Android tools. Low-space device setup, cancelled/interrupted AVD creation and post-virtualization reboot recovery need a suitable machine/test environment.

Actual visible UI quality, all four palette controls, window persistence across monitor removal, mixed 100/150/200% DPI movement, keyboard focus/fullscreen interaction and setup/device conflicts need manual desktop acceptance. The native smoke covers resource changes/navigation/bounds logic, not those full interactions.

Phase 0's real game compatibility, streamed audio/synchronization, Android touch/replay receipt, orientation coordinates, offline connection, GPU behavior, latency and saved-progress checks remain open. No emulator/game was run during Phase 1. The viewport transport is still provisional. Follow [the local runbook](RUNBOOK.md) and [Phase 0 hardware steps](../phase-0/RUNBOOK.md) and record actual evidence.

Phase 1 code is delivered. Hardware/manual acceptance is not represented as complete. Phase 2 instructions are saved in `../phase-2/INSTRUCTIONS.md` and supplied in chat; Phase 2 is not implemented in this batch.
