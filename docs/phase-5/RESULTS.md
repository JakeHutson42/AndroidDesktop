# Phase 5 actual results — 5 October 2026

The user agreed to the proposed APK library and reported having no APKs currently. Implemented only that expansion. The agreement establishes scope; no observed gameplay bottleneck or repeated multi-APK workload is claimed. Hardware acceptance remains open.

## Implemented

- Device-page library using existing Corvus resources, scrolling entries and shared importer/session adapters. Successful imports retain one current entry per device UUID/package; switching uses that package's matching entry for actual installed verification, rather than the previously selected different app.
- Schema v3 JSON metadata with non-destructive selected-entry migration, atomic serialized commits, prior settings backup/recovery and newer-schema protection. Theme, bounds, runtime/device paths and previous successful timestamps survive migration. No database, source copies or Android data deletion.
- Launch library app and Locate APK / update. Relocation/update requires matching package identity before compatibility/install; missing files explain recovery. Failed install/launch/persistence retains previous desktop metadata. An already-successful Android installation is not destructively rolled back after a later failure.
- Library actions conflict-protected during setup/import/automation/shutdown. One active device remains. No removal/version-history UI, profiles, backups, instrumentation, remote exposure or iOS was introduced.

## Passed

| Check | Actual result / limits |
|---|---|
| Release .NET build and suite | 89 passed, zero failed/skipped; no build warnings/errors reported |
| New library tests | v2 metadata/backup migration and disposable disk-byte preservation; package/device-isolated update; corrupt library recovery and locked-file write retention; matching-entry switching without reinstall; wrong-package relocation rejection; 1,000-entry roundtrip/lookup |
| Existing regression tests | importer failures/cancellation/source locking, settings recovery/future schemas and prior recording/recovery tests pass; command protection extended to library launch/locate |
| Browser input/scheduler contracts | 23 passed, zero failed/skipped; fixture tests, no Android receipts |
| Standard and composition Windows host smokes | Passed outside sandbox: packaged viewport, navigation, themes, bounds and setup check. WebView2 154.0.4258.53; approximately 333/449 ms single callbacks on final successful runs, not gameplay benchmarks |
| Synthetic metadata performance | 1,000 entries; save/load plus 1,000 lookups measured 28.3562 ms in the retained run; JSON 423,808 bytes; .NET 10.0.11. Generous 15-second regression ceiling passed. Single local synthetic run, not UI/gameplay acceptance |
| Dependency inventory | No dependencies added; source inventory in dependency-inventory.json |

Reports: smoke-standard.json, smoke-composition.json and library-performance.json. Both host reports state androidConnected=false and mediaVerified=false. The smokes use nonpersistent settings and do not create an AVD.

## Failed attempts and corrections

The first new migration test used a temporary AVD home that intentionally violates the production app-owned path validator; loading fell back to defaults and the assertion failed. Corrected the fixture to retain the valid configured root while checking an independent disposable disk fixture. The complete suite then passed. This test does not establish real AVD retention.

The first sandboxed standard smoke failed with WebView2 COM error 0x800700AA (resource in use). Its original report is retained as smoke-standard-009ba7ff6f8c4b09bac7578b36c4497a.json. Standard/composition checks passed in the authorized normal Windows environment. No unresolved automated test failure is reported.

## Unverified and release limits

No actual APK, emulator or diagnostic app was run. Actual migration of existing production selections, Explorer/library manual usability, mixed DPI, signed updates/rejections, installed package switching, game-save retention and relocation on Android remain unverified. Synthetic file preservation is not real Android save evidence.

All inherited runtime/media/input/geometry/save/performance gates remain open, including actual replay reception, audio synchronization, latency/frame-time/resource distributions, recording overhead, two-hour soak, twenty device cycles and clean-PC/upgrade validation. WebRTC remains provisional; no supported emulator/image/Java combination is selected. Distribution-license and signing review remains open.

No Phase 5 publish/installer was produced. Existing 0.3.0 artifacts remain preserved historical Phase 3 candidates; use the rebuilt Release source for Phase 4/5. Existing packaging/runtime-candidate.json continues to describe those historical artifacts. No public release acceptance is claimed.

Follow RUNBOOK.md when suitable APKs/dependencies become available. Next work should close hardware acceptance using retained real evidence before selecting another expansion.

