# Phase 7 actual results — 6 October 2026

Implemented only same-profile/device backups, verification, staged restoration and interrupted-restore recovery. Reviewed Phase 0–6 results, inherited hardware gates and the profile/storage/session/lease/settings adapters. The transport and release gates remain open.

## Implemented

- Themed Setup expander with Back up active device, Verify backup, Restore backup, Recover interrupted restore and cancellable copying/verification/staging. Operations run on a worker and conflict with setup, device actions, profile switching and automation.
- Backup/restore require a matching in-memory graceful-stop receipt from an actual successfully booted device. Unknown shutdown, initial app startup, changed profile/setup and Force stop do not supply a receipt. The stop path checks recorded live emulator/gateway descendants after exit. A separate exclusive per-user device lease spans each operation; prior recorded live owners block it.
- Compatibility includes actual device UUID, profile/AVD identity, original storage home, runtime identity/configuration, emulator executable SHA-256 and installed image package.xml SHA-256. Installed SDK/image dependencies remain external. Missing compatibility evidence disables backup without preventing ordinary gameplay. This is strict same-device restoration, not cloning or cross-runtime migration.
- New uniquely named staging/completed backup directories. Source files are held read-only with Windows sharing that denies writers/replacement throughout copying and verification. Bounded inventory of 4,096 files/8,192 directories, 64 KiB streaming copies, space checks with 64 MiB reserve, cancellation and progress. Symlinks/junctions, lock artifacts, overlap and invalid relative paths are rejected. No existing backup is overwritten or deleted.
- Versioned schema-v1 manifest with profile metadata, identity, shutdown/runtime evidence, file sizes and SHA-256 hashes. Completion follows independent staged verification and atomic directory rename. The manifest explicitly labels Android consistency/saved-progress acceptance unverified. Checksum verification detects accidental corruption; it is not a signed/authenticated backup format or an encryption feature.
- Restoration validates the complete inventory, checksums, AVD index/UUID and strict compatibility before staging. Staged bytes are verified again before a durable transaction journal. Original AVD home is renamed to a unique .original-* sibling, verified staging becomes the active home, and profile metadata commits through the existing atomic settings store.
- Cancellation applies before journaled commit. Once journaled, the transaction completes or remains recoverable without cancellation. A prepared journal recovers original disks and pre-restore metadata; committed journals finish housekeeping. Recovery retains displaced/staged copies and archived journal receipts. Failed metadata persistence leaves startup blocked until recovery succeeds. Startup and device creation refuse pending restore journals.
- Original sources, recordings, other profiles and historical candidate artifacts remain preserved. Settings stay at schema v4; no new dependency or database. Backup manifests/journals are separate schemas. No live snapshots/checkpoints, concurrent devices, automatic data cleanup, instrumentation, remote access or iOS work.

## Passed

| Check | Actual result / limits |
|---|---|
| Release .NET build/test | 115 passed, zero failed/skipped; successful build with zero warnings/errors |
| New backup/recovery tests | Verified byte roundtrip/original preservation; interruptions at prepared, original-preserved, device-installed and metadata-committed boundaries; failed metadata commit/retry; incompatible runtime/device; corruption/extra files/future schema/traversal; low space/locked source/overlap; cancellation before and during staging; lock artifacts/junction rejection; corrupt/pending journal retention; absent graceful receipt refusal |
| Existing regression suite | All previous profile, lease, migration, importer, recording and replay tests pass |
| Browser contracts | 23 passed, zero failed/skipped; fake browser/RTC fixtures, no Android receipts |
| Standard/composition host smokes | Passed packaged viewport, themes/navigation/bounds, profile switching/conflict guards and unavailable backup/restore without graceful receipt. WebView2 154.0.4258.53; reports retain single shell callback timings only |
| Synthetic copy/verification | 16 MiB zero-filled disposable fixture; copy plus two verifications: 126.898 ms in retained run. 64 KiB copy buffer, 4,096 file-handle ceiling, .NET 10.0.11; generous 30-second regression bound. Not real AVD throughput or a measured peak-memory distribution |
| Inventory/documentation | No new dependencies; source inventory and runbook supplied; historical artifacts preserved |

Reports: smoke-standard.json, smoke-composition.json and backup-performance.json. Native smokes used nonpersistent profiles and did not back up, restore or start an Android device. Both report androidConnected=false and mediaVerified=false.

## Failed attempts and corrections

Four first-run recovery assertions compared array references after journal deserialization. Changed those assertions to compare the exact serialized metadata structure; recovery bytes already matched. The subsequent full suite passed.

A symlink fixture could not be created because the ordinary Windows process lacks symbolic-link privilege. Replaced the fixture with a real directory junction requiring no elevation; rejection and source preservation then passed. No final automated failure remains.

## Unverified

No Android SDK/emulator/Java/target APK was supplied or run. Real emulator shutdown semantics and application save flushing, runtime/image metadata compatibility, multi-GiB/sparse/QCOW disk behavior, actual saved progress after restore/cold boot, Google sign-in state, manual folder selection/long paths/DPI usability, cross-logon/external SDK use, power-loss durability and peak memory remain unverified. Artificial checkpoint exceptions model process interruption, not hardware power failure.

Known ownership checks cover recorded roots and descendants observed before shutdown; a child spawned or missed outside that observation, noncooperating external writers and filesystem races need real validation. The source-sharing locks and remaining-lock refusal add checks but do not certify Android application consistency. File contents/identities are archived; filesystem ACLs, sparse allocation, empty directories and online server state are not preserved as restore guarantees. Backup contents may include Google/Android credentials and private data, and are not encrypted by this feature.

All inherited gameplay/audio/input/replay/performance/save gates, two-hour soak, twenty device cycles, clean-PC upgrade, distribution rights and signing remain open. WebRTC/protocol remains provisional. Existing 0.3.0 installers are historical Phase 3 candidates; no Phase 7 publish/installer was produced. Run from rebuilt source. Do not represent these fixture results as certified Android backup acceptance.

Next scoped handoff: execute the real shutdown/backup/restore/save tests in RUNBOOK.md on disposable devices with suitable dependencies/APKs, retaining originals. Close those and the inherited hardware gates before selecting another expansion.
