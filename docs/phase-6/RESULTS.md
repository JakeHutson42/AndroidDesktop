# Phase 6 actual results — 6 October 2026

The user authorized the proposed next phase: isolated device profiles with one active device. Phase 7 backups/restoration remain planned only. Reviewed the canonical plan, Phase 0–5 results, existing storage/setup/session/import/settings/recording/evidence services and viewport adapters before implementing the expansion.

## Implemented

- Setup-page Add profile and explicit Switch to selected profile, reusing Corvus styles. The header identifies the actual active profile independently of the highlighted candidate. Adding a profile saves configuration only; Create owned device remains a separate explicit operation.
- Settings schema v4. The current device becomes Default device, keeping its original AVD home/name, configuration, image, selected APK, library, theme and bounds. No disk moves, copying, wipes, package removal or recording deletion. Profile snapshots retain independent setup, resources, selected app and library. The top-level runtime/library remains the active snapshot for existing adapters.
- New profiles have stable GUID identities, unique generated AVD names and `%LOCALAPPDATA%/AndroidDesktop/profiles/<id>/avd` homes. Actual device UUID markers remain independent of emulator serials and are assigned by the existing storage service when the device starts. Path escape, cross-profile roots, wrong AVD names and storage junctions/symlinks are rejected.
- New profiles request 2048 MiB guest RAM and two virtual cores; validated settings allow 512–8192 MiB and 1–8 cores. Blank values preserve the legacy AVD defaults. Emulator launch receives explicit resource arguments only when set. These are guest settings, not hard limits on total host-process memory, CPU utilization, GPU memory or disk growth. Guest defaults/limits are engineering choices pending runtime measurement.
- Switching requires no owned process handles and no active import/setup/recording/playback operation. Saved metadata commits before configuring the session. Failed metadata writes retain the current profile. Switching releases input, clears readiness/runtime identity and restores the target profile's selection/library. An existing device's image is retained; use another profile for a different image.
- One desktop instance per Windows logon session, plus an exclusive per-user device lease. The lease records host and emulator/gateway root PIDs with start times, rejects live prior owners and PID reuse mistakes, and is retained through failed shutdown. Clean shutdown clears it. Corrupt records fail closed; no unrelated processes or shared adb server are killed. Real emulator crash/child-process behavior remains unverified, including an unrecorded grandchild or a crash before ownership metadata flush.
- Existing recording files remain at their stable root. Replay still requires actual device UUID/configuration matching. Explicit resource overrides are now included in runtime identity so a changed resource configuration cannot silently reuse a matching recording context.
- Setup saves now persist before changing runtime configuration. No profile deletion, cloning, concurrent-device execution, backup/checkpoint, memory instrumentation, remote access or iOS implementation.

## Passed checks

| Check | Actual result and scope |
|---|---|
| Release .NET build/test | 100 passed, zero failed/skipped; no compiler warnings/errors reported |
| New profile/lease tests | Legacy migration/exact backup retention; per-profile selection/library/configuration roundtrip; distinct IDs/paths despite shared serial; invalid identities/names/path/resource limits; resource arguments; unknown/duplicate/mismatched profiles; corrupt-primary recovery; failed-write active-profile retention; exclusive lease/live-owner refusal/PID-start matching/corrupt record preservation |
| Existing regression suite | Import/update rejection, source locking, cancellation, settings future-schema protection and all earlier automation/recovery tests pass |
| Browser input/scheduler tests | 23 passed, zero failed/skipped; fake browser/RTC fixtures, no Android receipt |
| Standard native host smoke | Passed profile addition, switching, default restoration, playback conflict guards, packaged viewport, themes/navigation/bounds/setup checks; single shell callback 378.60 ms |
| Composition native host smoke | Same checks passed; single shell callback 484.21 ms |
| Synthetic metadata workload | 32 profiles, 3,101 total library entries; save/load and 32 metadata switches: 585.1893 ms; JSON 1,582,049 bytes; .NET 10.0.11. Single local fixture run with a generous 15-second regression ceiling, not a UI/gameplay performance distribution |
| Dependency inventory | No new dependency; see dependency-inventory.json |

Reports: smoke-standard.json, smoke-composition.json and profile-performance.json. Native smokes used nonpersistent metadata and did not create an AVD. WebView2 observed: 154.0.4258.53. Both reports state androidConnected=false and mediaVerified=false.

## Failed checks

No executed final automated check failed. No new Android test was attempted; absent prerequisites are not a failed transport result. Previous phases' historical failures remain recorded in their original results.

## Unverified and release limits

A fresh limited dependency check found no adb/emulator/Java on PATH and no SDK at the two previously checked locations. The user last reported no APKs. No Android image, diagnostic APK, game or actual profile AVD was run.

Real legacy AVD retention, multiple profile device creation, independent Google sign-in/data/saves, resource flag behavior, actual graceful/failed shutdown and crash recovery, library updates/rejections on Android, cross-logon ownership, junction rejection under real SDK paths and manual profile UI/DPI usability remain unverified. Tests validate metadata and synthetic ownership records rather than production Android disks or actual orphaned emulator descendants.

Inherited media/input/save/performance/replay gates, offline media, audio synchronization, latency/frame-time/resource distributions, recording overhead, two-hour soak, twenty device cycles, clean-PC installation/upgrade, licensing and signing remain open. WebRTC and the experimental protocol remain provisional; no supported emulator/image/Java combination is selected.

Use the rebuilt Release source. No Phase 6 installer/publish candidate was produced; preserved 0.3.0 artifacts are historical Phase 3 candidates and do not include Phases 4–6. No public-release acceptance is claimed. Follow RUNBOOK.md for actual hardware acceptance. Phase 7 steps are saved separately and supplied in chat; no backup operations are implemented.

