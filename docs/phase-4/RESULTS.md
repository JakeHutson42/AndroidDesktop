# Phase 4 actual results — 4 October 2026

Implemented recording/archive and playback services, schema-v1 semantic input, bounded asynchronous disk queues, streaming replay with absolute deadlines, pause/resume, finite/infinite loops, deterministic bounded variations and per-run dispatch/detector evidence. Integrated controls into the existing Corvus-themed Device page. Imports/session conflicts are blocked during automation; stop/shutdown/focus/geometry/connection interruption release input through the existing adapter. No new dependency, settings schema, device, library or Phase 5 implementation was introduced.

## Passed

- Release .NET build/test: 84 tests passed, zero failures/skips. Includes recording integrity/finalization, incomplete recovery, corruption rejection, context compatibility, geometry/contact validity, queue overflow/write failure and conflicting commands, plus existing Phase 0–3 regressions.
- Node 24.19.0 packaged viewport/scheduler tests: 23 passed, zero failures/skips. Includes exact deadlines, finite/infinite loops, release-boundary pause, geometry mismatch, seeded bounds/reproducibility, bounded streaming, lateness stop, overload behavior and touch/key cancellation.
- Packaged viewport rebuilt using existing pinned dependencies.
- Windows WPF/WebView2 standard and composition isolated host smokes passed. Reports are smoke-standard.json and smoke-composition.json. WebView2 154.0.4258.53; measured shell readiness approximately 320/453 ms in this run. These smokes load the packaged viewport and check themes/bounds/setup pages; they do not exercise a connected Android device.

## Unverified / acceptance open

No Android SDK, Java or representative game was supplied; no Android installation, diagnostic APK build/run, actual received replay sequence, holds/swipes/multitouch, keyboard releases, enforced orientation, synchronized audio, saved progress, input latency or recording overhead was tested. Diagnostic source now logs received key events as well as touch events, but that Java change has not been compiled here. WebRTC remains provisional with the existing pinned experimental protocol; no emulator/image runtime combination is certified. Phase 3's soak, device cycles and clean-PC/distribution gates remain open.

Browser fake-channel tests establish emitted packets and scheduling logic, not Android reception, screen coordinates, frame timing, game behavior or detector recognition. Dispatch evidence explicitly records hardwareReceptionVerified=false. No detector outcome is claimed. Windows scheduling is not exact real-time; delays above 250 ms stop the run. Recovery-generated releases and transport zero-pressure cancellation require Android verification.

No Phase 4 publish/installer was produced; existing 0.3.0 artifacts remain historical Phase 3 candidates. Use the rebuilt Release source and RUNBOOK.md. Public distribution remains subject to the earlier license review.
