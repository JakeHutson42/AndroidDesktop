# Phase 2 implementation and verification results

Recorded 4 October 2026. Phase 2 code is delivered; **Android hardware acceptance remains open**. No final viewport transport or supported emulator/image combination is certified.

## Implemented

- A clear Open APK action and dedicated Explorer drop card share `ApkImportService`. Exactly one existing standalone `.apk` is accepted; unsupported formats/multiple files, absent manifests and split manifests fail with explanations.
- Official APK Analyzer commands read package identity, version code/name, minimum SDK and app label where resolvable; ZIP native-library directories and SHA-256 are read locally. Missing resource labels fall back to package identity. Version-code major metadata is supported. Device SDK/ABI comparison retains reported ARM support without claiming translated gameplay works.
- Schema v2 adds one selected APK's source/checksum/package/name/version/device identity and install/launch timestamps. Phase 1 themes, bounds, runtime paths and AVD home migrate non-destructively. Atomic selection commits happen only after successful install/launch; settings writes remain serialized, including against debounced resize/theme saves.
- An unchanged APK skips reinstall only after checksum/package/version and device-UUID agreement plus actual selected-serial Android package/path/version checks. Missing package or changed source installs with `adb install -r`; transport/query errors abort rather than inventing absence. Missing source files produce a locate-again message.
- Selection replacement preserves previous packages' device data. Install/signature/downgrade/split/storage/ABI/API failures never trigger uninstall, clear, downgrade flags or AVD recreation. If an update succeeds before a later launch/persistence failure, it may already exist on Android; desktop selection remains previous, and retry reconciles it without destructive rollback.
- Source bytes are protected against concurrent writes/replacement through metadata inspection and adb transfer. A persistent UUID is created inside the app-owned AVD directory, independently of emulator ports, and reused across cold boots.
- Launch/relaunch, Back, Home, volume up/down, mute, rotate and fullscreen controls are present. Relaunch force-stops only the selected package, preserving data. Back/Home/volume use atomic events over the persistent RTC input channel, not per-gesture adb processes. Input controls depend on actual channel readiness.
- Actual preparation/connection/inspection/install/launch stages remain asynchronous/cancellable. Busy/closing operations disable conflicting actions. Input is released before import, rotation, page changes, focus loss, cancellation and shutdown. Coordinates continue following actual decoded geometry; requested-orientation labeling/settling remains provisional pending real diagnostic validation.
- Close visibly attempts graceful shutdown and flushes settings. Explicit Force stop is enabled only after failed/timed-out graceful shutdown; only owned processes are targeted. No installer, library, backup/checkpoint UI or Phase 4 recording system added.

## Checks actually executed

| Check | Result / limits |
|---|---|
| Read plan, Phase 0/1 results and existing session/installer/settings/viewport code before editing | Completed |
| Final .NET 10 Release build | Passed, zero warnings/errors |
| .NET suite | 56 passed, zero failed/skipped; 25 previous cases plus 31 Phase 2 cases |
| Shared importer / selection contract tests | Format/count/missing-source validation; unchanged launch; absent/wrong-version install; changed source/different device; failed compatibility/install/launch/persistence; source lock; cancellation; replacement/relaunch scope |
| Official-tool command/parser tests | Fake SDK/adb outputs verify metadata/label/hash/ABI extraction, split rejection, user-0 installed queries, missing-package exit 1, transport failures and explicit launch/relaunch/install arguments. These are not actual Android tool runs. |
| Migration and command-protection tests | Phase 1 settings retained through v2 migration; busy imports/launches disabled and direct busy drop cannot mutate selection; force termination unavailable before graceful failure |
| Packaged browser build | Passed using pinned existing dependencies; initial sandboxed esbuild directory resolution failed, then a fresh unsandboxed build succeeded |
| JavaScript tests | 10 passed: existing input/geometry/capture tests plus toolbar key encoding/whitelist/pointer release and actual channel-readiness transitions. Fake browser/RTC fixtures, not Android receipts. |
| Standard WPF/WebView2 smoke | Passed: native shell, page navigation/themes/minimum bounds/prerequisites and packaged page loaded |
| Composition WPF/WebView2 smoke | Passed with the same scope |
| WebView2 runtime | 154.0.4258.53 |
| Latest single hidden shell render callback | Standard 334.03 ms; composition 448.16 ms. Not a benchmark or acceptance distribution. |

The first expanded .NET run had an assertion comparing array references after JSON deserialization. It was corrected to verify the exact committed settings file and reloaded package identity; the full suite then passed. Android source inspection established that `pm path` can return exit 1 without output for absence; that case has an explicit passing test.

The reports `smoke-standard.json` and `smoke-composition.json` explicitly state `androidConnected=false` and `mediaVerified=false`. No smoke changed production settings or created an AVD. The inherited Python gateway was not changed in this phase; its earlier fake-server contract results are recorded in Phase 0.

## Not executed

A fresh check found no emulator at the standard LocalAppData SDK location, no SDK environment-variable path and no adb/Java on PATH. No alternate SDK/Java path or representative game APK was supplied. No Android emulator, diagnostic APK or game was started during Phase 2.

Unverified: real APK Analyzer and device-version output compatibility, AVD UUID creation/reuse on a running device, actual Explorer drag/drop usability, saved progress on close/reopen/cold boot, compatible and rejected updates, package replacement data retention, actual Android toolbar keys, streamed sound/synchronization, touch/replay receipt, rotation/mixed DPI, offline media, recovery and gameplay latency/performance.

The source and smoke checks do not close Phase 0's media/input/runtime gate or Phase 2's save/reopen exit criteria. Follow [RUNBOOK.md](RUNBOOK.md) for exact local steps and retain evidence. Resolve any actual failed core transport requirement before claiming acceptance.

Phase 3 is documented in `../phase-3/INSTRUCTIONS.md` for the next batch. Its release packaging/recovery/performance work has not begun.
