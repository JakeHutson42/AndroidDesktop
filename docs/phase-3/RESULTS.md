# Phase 3 candidate implementation and actual results

Recorded 4 October 2026. **Phase 3 is not accepted or complete:** the inherited runtime gate remains open. Code and evaluation packaging are delivered, with unverified checks explicitly retained. No final transport or supported emulator/image combination was chosen. The plan, Phase 0–2 results, services/adapters and Corvus publishing/installer reference were read before edits.

## Implemented within Phase 3

- Kept one app-owned persistent AVD, one selected APK, existing schema v2/import/update behavior and Corvus interface. No Android data reset, uninstall workaround, snapshot/backup UI or Phase 4 features.
- Explicit three-minute boot timeout guidance; failed startup without owned handles flushes evidence. Gateway crash monitoring starts immediately. Graceful gateway shutdown failure now reports recovery instead of silently killing it; force termination requires failed graceful Stop and targets owned handles only.
- Display connect awaits actual input-channel readiness for up to 30 seconds, rather than returning at message dispatch. WebView2 process/COM failure disables interaction and recreates the control on the next connection attempt. Held-input release is requested on faults; total transport failure cannot guarantee receipt and remains a hardware gate.
- Dependency discovery tolerates inaccessible/stale SDK paths so Setup remains editable. Packaged private Python/gateway take precedence over stale developer paths after upgrades. Python runs with `-B`, avoiding runtime bytecode caches in the installation.
- Bounded asynchronous evidence/process/media sampling retained; gesture/input details now opt-in via `ANDROID_DESKTOP_VERBOSE_EVIDENCE=1`. Diagnostics sanitize sensitive JSON fields, tokens and unnecessary personal paths before disk writes; failures are surfaced and evidence marked INCOMPLETE. Browser devtools require `ANDROID_DESKTOP_DEVTOOLS=1`.
- Added opt-in Support export from Diagnostics: bounded 8 MiB evidence scan, structured versions/stages/process/media, recent sanitized errors, no complete settings/discovery/APK/device data or raw input, incomplete/truncation markers. Archive construction runs off the UI thread; cancellation/failure preserves the previous destination.
- Pinned candidate .NET SDK/runtime, NuGet locks including separate x64 publish lock, source protocol/gateway commits, CPython archive SHA-256 and versioned binary gateway dependencies. Hardware runtime fields are deliberately null/unverified. External Evergreen WebView2 updates require revalidation.
- Self-contained win-x64 publish includes local viewport assets, isolated CPython 3.12.10 with pinned wheels, generated protocols and provenance/licenses/inventory. It excludes Node/npm, development venv, test fixtures/APKs, SDK/image/Java, caches, PDBs and developer configuration. Normal users need official external Android/Java/WebView prerequisites, not a development environment.
- Inno Setup candidate installer with stable product AppId, version 0.3.0, per-user Windows 11 x64 installation, guide and shortcuts. Upgrades keep executable identity and LocalAppData root; uninstall does not declare deletion of settings/device data. App Setup performs actual prerequisite detection and guides official license/download/reboot setup. Unknown download sizes are not invented and licenses are not accepted automatically.
- Windows CI definition verifies locked preparation/restore, Release tests, publish contracts and installer compilation. Its artifact is explicitly unvalidated; it does not claim hardware checks. This workspace has no Git repository, so remote CI has not executed.
- Exact setup, media/input gate, performance/soak/cycle, persistence/upgrade/clean-PC and recovery steps in HARDWARE_AND_SETUP.md; reproducible build steps in BUILD.md.

## Checks actually run

| Check | Actual result and scope |
|---|---|
| Final locked .NET restore / Release builds | Passed; final builds have zero warnings/errors |
| Final .NET suite | 65 passed, zero failed/skipped; includes missing-tool recovery, redaction, valid structured telemetry, cancellation/previous-export preservation and prior importer/settings/command tests |
| Browser input contracts | 10 passed; fixture paths corrected to resolve relative to the test files so root/CI execution works. Fake DOM/RTC, not Android touch receipts |
| Private runtime self-test | Passed with isolated CPython 3.12.10, aiohttp 3.13.3, grpcio 1.78.0, protobuf 6.33.5 and generated protocol imports; no emulator/media |
| Final packaged gateway fake-server tests | 3 passed, actually launching `artifacts/publish-final/runtime/python.exe` and packaged gateway; authorization/origins, authenticated signalling, empty external ICE, RTC probe and teardown. Fake gRPC endpoints, no media |
| Final publish structure/private-data contract | Passed required runtime/assets/licenses and targeted exclusion/path/credential checks; not an exhaustive binary credential/license audit |
| Final standard WPF/WebView2 publish smoke | Passed packaged page, themes/pages/minimum bounds/prerequisite checks; shell callback 337.44 ms |
| Final composition WPF/WebView2 publish smoke | Passed same scope; shell callback 470.81 ms |
| Installed executable smokes | Standard 339.45 ms; composition 442.75 ms. These single callbacks are not shell-readiness distributions |
| Inno compiler | Stable 6.5.3, checksum verified; final installer compilation passed |
| Isolated installer lifecycle | Passed installation, package contract, both host smokes, same-version reinstallation, uninstall of program files and removal of product registration on the existing development PC |
| Existing settings during installer smoke | Zero settings files observed. Equality before/after is vacuous and does not establish real settings/AVD/save upgrade retention |
| Runtime/game gate, performance, soak, cycles | **Unverified — no emulator, diagnostic APK or game was run** |
| Fresh-PC matrix / historical desktop upgrade | **Unverified** |
| CI execution | **Unverified**; workflow supplied, not pushed/run |

Reports: smoke-standard.json, smoke-composition.json, installed-smoke/, installer-smoke.json and artifact-hashes.json. Every browser smoke states `androidConnected=false`, `mediaVerified=false`. They did not create an AVD or persist selected APK/settings.

Initial build/test issues were resolved: incompatible NuGet lock settings led to a separate RID lock; private Python import self-test initially generated bytecode caches, fixed with `-B`; a broad runtime property affected the Windows SDK reference, replaced by framework-specific runtime pins; existing JS tests assumed the viewport working directory, fixed with file-relative fixtures. A sandboxed restore could not access NuGet configuration and succeeded with the authorized normal environment.

The first isolated installer attempt returned exit 5 with a long generated destination. A shorter destination installed successfully; the test script now uses a short unique suffix and writes an installer log. The original failure had no retained log, so path length is suspected, not proven. Long custom destinations remain unverified. A temporary diagnostic installation was removed with its own uninstaller. No Android devices/data were involved.

## Runtime and release hold

Observed Windows/reference hardware is inherited from phase-0/reference-pc.json; WebView2 154.0.4258.53 was observed in the current native smokes. Candidate runtime/protocol/sidecar identifiers are in packaging/runtime-candidate.json and the packaged inventory. .NET self-contained runtime is 10.0.11; private Python is 3.12.10; protocols remain from experimental `emu-main-next`. No emulator, API/ABI/Google variant/image revision, compatible Java, WHPX result or measured GPU rendering combination is pinned as supported.

A fresh final check found no emulator in the standard LocalAppData SDK path and no Java/adb on PATH or SDK environment variables. No alternate dependency paths or representative game APK were supplied after requesting them. Therefore diagnostic APK building, actual compatibility, streamed audio/synchronization, touch/holds/swipes/multitouch, rotation, focus/fullscreen/mixed DPI, offline media, reconnect/held releases and translation remain unverified. There is no observed WebRTC hardware failure to justify declaring a native alternative selected. The guide defines the failure-to-scrcpy evaluation path; no embedded native replacement was built.

Also unverified: cold boot/invalid Quick Boot game saves, signed update/rejected update/replacement retention, historical desktop upgrade/AVD identity retention, real cancellation/shutdown recovery, Release standalone-vs-embedded frame-time/latency/dropped-frame/buffering/CPU/GPU/memory distributions, two-hour gameplay soak and twenty **device** cycles. Budgets are unchanged and unconfirmed. No shell smoke is counted as a gameplay cycle or soak.

Corvus license/redistribution authorization is not established by the local reference. Runtime/NuGet/Python/browser/gateway notices are packaged, but full distribution-license review remains open. Inno compiler reports non-commercial licensing; applicable commercial rights must be reviewed before commercial distribution. Candidate artifacts are unsigned. They must not be represented as a certified public release.

## Delivered evaluation artifacts

- `artifacts/publish-final.zip` — self-contained Windows x64 candidate.
- `artifacts/installer/AndroidDesktop-0.3.0-win-x64-candidate.exe` — unsigned per-user installer.
- `artifacts/publish-final/inventory.json` and `notices/` — file hashes and dependency provenance/licenses.
- `artifact-hashes.json` — final ZIP/installer SHA-256.

See HARDWARE_AND_SETUP.md for exact local checks, RECOVERY.md for non-destructive recovery and BUILD.md for candidate rebuild/CI contracts. Acceptance can be closed only after actual recorded successes and any failed core requirements are resolved.
