# Backups, verification and restoration

Use the rebuilt source; historical installers do not contain Phase 7. Configure a working official emulator/profile using earlier runbooks. With no actual boot and graceful stop in this session, Backup and Restore remain disabled. Merely having a stopped-looking directory is insufficient.

1. Start the active profile normally and allow it to boot. Ensure the Android app writes its saves. Stop using the normal graceful Stop control. A failed/forced stop does not establish backup consistency; restart and complete normal shutdown before retrying.
2. In Setup, expand Device backups and restoration. Back up active device opens a directory picker. Choose an existing directory separate from AVD storage. The service creates a fresh AndroidDesktop-backup-<id> folder containing manifest.json and data. No previous backup is overwritten.
3. Verify backup selects the completed backup folder and rechecks every size/hash, the complete inventory and device index/identity. Verification does not start Android and does not demonstrate saved progress. Interrupted .backup-stage-* directories remain for inspection and are never returned as completed backups.
4. Restore backup requires the same active profile/device, a matching graceful stop and the exact recorded runtime/image/configuration. A different profile, UUID, storage home, runtime, resource/configuration setting or dependency revision is rejected. Installed SDK/image packages remain required; their contents are not bundled. This phase does not clone devices or migrate backups to a new PC.
5. Restoration stages and verifies bytes on the device volume, journals the transaction, preserves the original AVD home as <home>.original-<id>, installs the verified staging and atomically commits the saved profile selection/library. Cancellation is supported during staging; commit/recovery deliberately finishes without cancellation once journaled. Retain the original until real Android acceptance passes. Do not launch a preserved original as a second device; its index still references the canonical home.
6. If interrupted, the fixed <home>.restore.json journal blocks startup and device creation. Select that profile and use Recover interrupted restore. Prepared transactions restore the original disks and pre-restore profile metadata. Committed transactions finish journal housekeeping. Failure to save metadata keeps the journal/startup block. Ambiguous/corrupt journal states require investigation; no copy is deleted to force progress.

The UI holds exclusive per-user device ownership during verification/copy/restore/recovery and blocks conflicting operations. Use only these operations with closed device sessions; do not open the AVD in Android Studio or external tools concurrently. Source read handles deny writes/replacement while backing up. Remaining lock files/directories are rejected rather than deleted. A surviving recorded host/child blocks the ownership lease; close it gracefully and preserve unknown ownership records for investigation.

Android [snapshot compatibility depends on emulator/image/configuration](https://developer.android.com/studio/run/emulator-snapshots). This feature retains device files and uses cold boot through the existing session adapter; it does not promise that any captured Quick Boot snapshot is usable. Snapshots and backups cannot restore online server state. Backups include account/private device data; use private storage and share deliberately. This feature does not encrypt or authenticate archives, preserve ACLs/sparse allocation/empty-directory semantics, or automatically remove large staging/original copies.

## Validation

```powershell
dotnet test AndroidDesktop.sln -c Release --no-restore
node --test tools/viewport/test/*.test.mjs
./scripts/Smoke-Host.ps1 -OutputDirectory docs/phase-7 -CheckProfiles -CheckBackups
```

Fixtures verify hash/inventory checks, compatibility/refusal, originals, journaling, write failure and recovery. The retained performance workload is 16 MiB of zero-filled fixture bytes, not a real AVD; file handles are capped at 4,096, copy buffers at 64 KiB and manifests at 4 MiB. Space checks use logical byte counts plus reserve, so sparse images can require more disk space when copied. Measure actual peak memory and multi-GiB throughput before claiming performance acceptance.

On disposable real profiles only:

- First resolve the Phase 0/3/4/6 prerequisite/media/input gates. Record emulator/image/Java/protocol/GPU versions and the specific game/APK checksum.
- Create observable saved progress, exit the app inside Android, gracefully stop and back up. Verify independently and record manifest/shutdown evidence. Preserve original device files before any destructive test outside this feature.
- Restart, change progress, stop and restore the older backup. Confirm the pre-restore original copy remains, then cold boot and demonstrate actual restored progress/device UUID/selected metadata/Android sign-in behavior. Reopen desktop and repeat. Do not infer online-state rollback.
- Try wrong-device/configuration/runtime/image, corrupt files, missing dependencies, remaining locks, low space and failed/cancelled copies. Confirm rejection happens before original replacement and no reset/uninstall occurs.
- Simulate process interruption at every commit boundary only on disposable fixtures. Reopen, confirm startup/creation refusal, run recovery and verify original bytes and pre-restore metadata. Test a failed metadata save during recovery and retry. Do not use actual power-cut tests on the normal device.
- Test real descendant exit/orphan handling, cross-logon/external-tool conflicts, long paths, directory selection and all four themes/DPI scales. Measure copy/hash latency distributions, total memory, disk footprint and cancellation response on representative disk sizes.

Report each requirement passed, failed or unverified with method and evidence. Real Android save retention and hardware consistency remain unverified until these steps succeed. Preserve originals and receipts until acceptance; there is no automatic cleanup button in this phase.
