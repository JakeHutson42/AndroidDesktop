# Phase 5 scope proposal — 5 October 2026

Status: APK-library scope agreed by the user on 5 October 2026 and implemented. The user has no APKs currently; no measured multi-APK workload is claimed. This document retains the agreed proposal; actual results and implementation limits are in RESULTS.md.

## Evidence and hardware acceptance

Read the canonical implementation plan, Phase 0–4 results, Phase 4 runbook, runtime inventory, shared APK importer, settings store and desktop selection commit flow.

The existing results establish source-level and shell/package checks. They do not demonstrate a connected Android runtime, game compatibility, media, received input, saved progress or gameplay performance. The existing protocol/transport remains provisional. No measured bottleneck or demonstrated demand selects one expansion yet.

A fresh local discovery check found no adb, emulator or Java on PATH, no ANDROID_HOME, ANDROID_SDK_ROOT or JAVA_HOME environment variables, and no SDK at `%LOCALAPPDATA%/Android/Sdk` or `C:/Android/Sdk`. This is limited discovery, not proof that dependencies do not exist elsewhere. .NET and Node are available. No representative APK path was provided in this request.

| Acceptance gate | Status | Evidence needed to close |
|---|---|---|
| Emulator/image/Java/protocol combination and WHPX/GPU | Unverified | Supplied dependency paths; actual versions, acceleration output and successful startup |
| Game installation, compatibility, Google Play and translation | Unverified | Representative standalone APK and actual device/game observations |
| Video, sound/synchronization and offline reconnect | Unverified | Connected media tests and retained observations |
| Diagnostic APK build, touch/key/replay receipt and geometry | Unverified | Actual build/install plus correlated Android receipt logs, rotation/focus/DPI tests |
| Saves, updates/rejection, cold boot and desktop upgrade | Unverified | Persistent progress/data before and after each non-destructive operation |
| Latency/frame times/resources/recording overhead | Unverified | Comparable standalone/embedded/recording distributions using the same workload |
| Two-hour soak and twenty device lifecycle cycles | Unverified | Recorded device runs and process cleanup evidence |
| Fresh-PC installation, distribution rights and signing | Unverified/open | Clean-PC checks, license review and applicable signing/release work |

Use the existing Phase 0 runbook, Phase 3 HARDWARE_AND_SETUP.md and Phase 4 runbook to execute these checks. Preserve settings, original recordings and the existing AVD. Shell callbacks and fake-channel tests cannot close these gates. Do not select a different transport without observed failures or measured justification.

## Proposed single expansion: APK library

This is a candidate based on the existing importer architecture, not a conclusion from measured user demand. The user should identify the recurring need, such as repeatedly locating and switching among multiple APKs, and agree to this scope before code changes.

- Browse previously successfully imported standalone APKs using the existing Corvus resources. Keep one active device and one selected app.
- Reuse the shared importer for launch/update. Retain source path, package identity, checksum/version, device UUID and successful install/launch timestamps. Revalidate the actual package and source before skipping installation.
- Define one current library entry per device UUID/package identity; a successful update replaces that entry's source/version metadata. Do not store APK copies or a version archive in this scope.
- Migrate schema-v2 selected metadata into a library entry while retaining the selected app, theme, window bounds, runtime paths and AVD location. Keep atomic serialized writes, previous-commit recovery and future-schema protection. JSON remains sufficient for a small list; add no database without measured browsing/query requirements.
- Missing sources require locating a replacement through the same inspection/import path. Failed inspection, signature conflict, downgrade, installation, launch or metadata persistence must retain previous desktop metadata and Android data. An Android update that succeeds before a subsequent failure cannot be safely rolled back automatically; retry must reconcile actual device state.
- Any removal action affects desktop library metadata only, with clear wording; it must never uninstall, clear data or remove source files. Specify selected-entry behavior before implementing removal.
- Disable library mutations/launch while conflicting import, automation, setup or shutdown operations run. Continue to use existing input release and session adapters.

Excluded: additional APK formats, concurrent devices, profiles, backup/checkpoint UI, instrumentation, remote endpoints and iOS work.

## Validation after agreement

Add meaningful tests for legacy migration with exact metadata retention, corrupt-primary recovery, future-schema protection, failed write/commit retention, update/rejected update and per-device identity. Verify library switching passes the matching previous entry to the unchanged-install check rather than using another package's selection. Cover missing-source relocation and conflicting commands.

Measure load/save/browse behavior on representative library sizes and document workload, timings and limits; do not infer gameplay performance from metadata tests. Run existing .NET and browser regressions and applicable host/package checks after implementation. Execute real Android switching/update/save tests when prerequisites are available, otherwise mark them unverified.

Update documentation and dependency inventory to match the implemented scope. Rebuild release candidates only when applicable, retaining old artifacts and clearly identifying which source/features each contains. Existing 0.3.0 candidates remain historical Phase 3 artifacts.

