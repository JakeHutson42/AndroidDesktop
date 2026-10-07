# Device profiles usage and validation

Run the rebuilt source using `dotnet run --project src/AndroidDesktop -c Release --no-build`. Historical installers do not contain Phase 6.

1. Open Setup. The header identifies the active device. Existing data becomes Default device without moving the AVD. With no configured APKs, its library remains empty.
2. Stop the active device and finish automation before profile changes. Enter a profile name, then Add profile. This saves metadata only. Highlighting the new row does not switch the running configuration.
3. Click Switch to selected profile. Configure SDK/Java/gateway paths and the desired installed image, then Save paths, Check prerequisites and Create owned device. New profiles reference shared official SDK image packages while their writable Android storage is separate. Each profile must complete normal prerequisite/license checks.
4. Set guest RAM/virtual cores before starting. New profiles default to 2048 MiB/two cores; legacy blank values preserve AVD defaults. Settings accept 512–8192 MiB and 1–8 cores. Actual host resource use includes emulator, renderer, gateway and shell overhead and must be measured. Disk growth is not quota-managed; existing setup free-space guidance still applies. Do not replace an existing profile's image to perform an upgrade; create a separate profile.
5. Start the device and import a standalone APK when one is available. Its successful selection/library belongs to this profile. Stop gracefully, select another profile and switch. No second active session is permitted. Profile removal/cloning and backups are outside this phase.

Guest RAM arguments follow [Android's emulator command-line documentation](https://developer.android.com/studio/run/emulator-commandline); `-cores` is also used in [Chromium's official emulator guide](https://chromium.googlesource.com/chromium/src/+/refs/heads/main/docs/android_emulator.md). Flag availability and actual resource behavior must be validated against the chosen stable Windows runtime. Bounds/defaults are local engineering policy, not an assertion of compatibility with every image.

## Ownership and recovery

Only the current profile can own the session. Stop must release actual owned process handles before switching; failed shutdown retains ownership and exposes the existing explicit Force stop option. Profile switching itself never forces shutdown. The per-user active-device.lock records process IDs with start times; a live previous host/root blocks another device, and corrupt ownership data blocks startup until investigated. Do not remove this record to bypass a running emulator or terminate shared adb. If the host crashed, gracefully close the recorded surviving owned emulator first and retry. Preserve an unreadable ownership record for investigation; do not treat unknown children as stopped.

Settings use atomic commits and a previous-commit .bak. Newer schema protection remains. A failed profile switch reports the error and keeps the active profile. Older desktop builds protect schema v4 instead of downgrading it. Return to this build to use profiles. Resource changes alter recording runtime identity; restore the matching settings before replaying a historical recording. Original recording files remain unchanged.

## Verification

```powershell
dotnet test AndroidDesktop.sln -c Release --no-restore
node --test tools/viewport/test/*.test.mjs
./scripts/Smoke-Host.ps1 -OutputDirectory docs/phase-6 -CheckProfiles
```

The host checks use nonpersistent profiles and create no Android devices. The synthetic performance test measures metadata only, and writes its workload report under the test output's docs/phase-6 directory. It does not measure GUI browsing, gameplay or startup distributions.

For hardware validation, first follow the Phase 0, Phase 3 and Phase 4 runbooks. Then use two disposable test profiles and your own APKs:

- Verify existing Default device saves/configuration/UUID survive migration and desktop restart. Record before/after identity and saved progress rather than counting metadata equality as Android retention.
- Create/start the two test devices sequentially; verify separate disk directories, different actual UUIDs, independent app progress/Android sign-in and profile-specific selections after close/reopen and cold boot.
- Confirm launch arguments and actual guest resources. Compare the same workload at supported budgets, measuring all processes; validate image requirements and host headroom.
- Attempt switching while recording/replaying/importing, during startup and after shutdown failure. Verify controls/direct operations cannot change identity and actual held-input release is received.
- Simulate cancellation/creation interruption, failed settings writes and crashes only on disposable fixtures. Preserve incomplete AVD files; creation must refuse overwrite. Record any surviving wrapper/grandchild processes and confirm another device is blocked. Check same-user cross-logon behavior on a suitable machine.
- Verify wrong-profile replay rejection and exact replay after returning to the matching UUID/configuration. Test all four themes, long names and mixed DPI manually.

Record each result passed, failed or unverified with exact emulator/image/Java/protocol versions and retained evidence. Existing two-hour soak and twenty lifecycle-cycle gates still apply. No backup/restore acceptance can be claimed in Phase 6.
