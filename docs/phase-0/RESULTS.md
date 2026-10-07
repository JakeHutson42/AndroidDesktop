# Phase 0 validation record

Prepared 4 October 2026. **Phase 0 exit gate remains OPEN. No final display transport or supported emulator/image combination has been selected.**

## Actually executed

| Check | Result | Evidence / limits |
|---|---|---|
| Read supplied plan and inspect Corvus source before changes | Completed | `REFERENCE_NOTES.md`; canonical plan moved to `docs/IMPLEMENTATION_PLAN.md` |
| Windows reference hardware inventory | Completed | `reference-pc.json`: Windows 11 Home x64, Ryzen 7 9800X3D, RTX 5080 + AMD integrated GPU, approximately 32 GiB RAM; firmware virtualization reported enabled |
| .NET 10 Release compilation | Passed | Both WPF and xUnit projects compile; no warnings/errors in final build |
| .NET compatibility/process/evidence tests | 12 passed | ABI/API checks, signature/downgrade/split install errors without uninstall, safe argument boundaries, concurrent bounded drains, owned-tool timeout cleanup, app-owned AVD policy and process measurement flushing |
| Python gateway contract tests | 3 passed | Actual Windows loopback HTTP/WebSocket and gRPC traffic to a **fake emulator**. Checks authorization/origin, empty external ICE configuration, RTC probe and JSEP forwarding/teardown |
| JavaScript geometry and packaged-input tests | 8 passed | Pure geometry/protobuf tests plus packaged CSP-safe bundle running against **fake browser/RTC interfaces**. Stable multitouch slots, out-of-viewport release, focus-loss release, diagnostic capture/replay and cancellation/geometry mismatch |
| Protocol client generation | Passed | Python generated from pinned AOSP files; protoc warns about upstream unused `empty.proto` import |
| Local frontend bundle generation | Passed | Static protobuf serialization; no runtime evaluation permission added to CSP |
| PowerShell script syntax and Python compile check | Passed | Preparation/APK-build scripts parse; gateway/summary Python compiles. This does not establish APK compilation |
| Standard WPF/WebView2 host smoke | Passed | `smoke-standard.json`: real native host rendered and packaged page posted readiness; WebView2 runtime 154.0.4258.53; no Android connection or media test |
| Composition WPF/WebView2 host smoke | Passed | `smoke-composition.json`: same scope; required Windows SDK projections included |

The stored smoke reports include single-run shell render callback times. They are **not** gameplay benchmarks, mixed-DPI validation, user-usability tests or proof that the media transport works. Early smoke attempts exposed missing SDK projections, a close-handler re-entry bug and a protobuf/CSP incompatibility; those were fixed and the above smoke tests rerun successfully. Input fixtures subsequently exposed a first-event capture bug; it was fixed and all eight input tests passed.

## Not executed / not demonstrated

| Phase 0 gate | Status and reason |
|---|---|
| Stable Windows emulator runtime + required RTC service | Unverified. Android SDK/emulator is absent from the standard location and PATH; no alternate SDK path or AVD was supplied. Pinned source protocols do not establish released runtime support |
| WHPX and emulator graphics acceleration | Unverified. Firmware virtualization inventory is not `emulator -accel-check` or GPU validation |
| Representative APK compatibility / ARM translation / Google Play behavior | Unverified. No target game APK was supplied and no Android device was started |
| Diagnostic APK compilation / install / launch | Unverified. Source/build script supplied; Java/Android SDK prerequisites unavailable |
| Video decode/rendering under gameplay | Unverified. Host asset readiness does not establish an incoming video track or performance |
| Streamed audio playback and synchronization | Unverified. No audio path was run or heard; track presence alone will not count as a pass |
| Android receipt of touch/holds/swipes/multitouch | Unverified. Encoder/fake-transport tests are not Android event-reception evidence |
| Gesture replay equivalence in Android | Unverified. Needs matching diagnostic APK log and host event evidence |
| Actual rotation, coordinate mapping, focus, fullscreen and mixed-DPI behavior | Unverified. Rotation uses physical-model request and decoded-frame geometry, with a provisional settling delay; must pass the diagnostic tests |
| Offline media/reconnect test | Unverified. Assets are local and ICE servers are empty by code/contract tests; offline Android media has not run |
| Input-to-visible latency, dropped-frame/frame-time budgets, decode/buffering behavior and GPU cost | Unverified. No standalone/embedded gameplay baseline; use external visible-response measurement plus profiling |
| Persistent game saves and reconnect/lifetime integrity | Unverified. App avoids wipes/uninstalls and owns one persistent AVD directory, but no saved-progress reopening test has run |
| Native scrcpy integration / comparison | Not implemented or benchmarked. Remains the planned alternative/reference |

Exact local steps and measurement protocol are in [RUNBOOK.md](RUNBOOK.md). Populate these fields after running them:

- Target APK/package/checksum: **not supplied**
- Emulator release/build: **not selected**
- System image API/ABI/Google variant/revision: **not selected**
- GPU / acceleration output: **not measured**
- Standalone evidence path/workload duration: **not run**
- Standard/composition embedded evidence paths: **not run**
- Diagnostic received-event log: **not run**
- Sound/sync evidence: **not run**
- Latency distributions / frame-time distributions / process-group resources: **not measured**
- Failed requirements and transport decision: **pending Windows emulator validation**

## Scope boundary

Only Phase 0 experiment code is present. Resizable WPF hosting and copied Corvus resource dictionaries are the minimum prototype scaffolding. No production setup/settings/import flow, APK library, installer, backups/snapshots, persistent recorder, loops, variation testing or instrumentation were built. Long soaks, twenty start/stop cycles and recording-overhead acceptance remain in their later planned phases.
