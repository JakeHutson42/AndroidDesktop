# Phase 1 — shell lifetime and baseline

Completed 8 October 2026. No rendering-transport replacement or FPS increase is included in this phase.

## Changes

- Shell tabs now keep their content mounted through a persistent content host. Switching Device/Setup/Settings changes visibility instead of detaching and reattaching the viewport. The host is designed for the shell's static TabItem pages.
- Page navigation reacts only to selection events originating from the shell TabControl. APK library and profile selections no longer trigger the shell's input-release handler through routed events.
- The native WebView smoke exercises 20 cycles through all three tabs and fails if the viewport unloads. The regular theme verification requires this check.
- `scripts/Summarize-Performance.ps1` produces reusable JSON summaries from existing evidence, aggregating same-name processes before calculating CPU and memory statistics. Browser image bytes and frame delivery are recorded separately from presentation timing. Browser profiles and screenshot fixtures are excluded from Git.

## Baseline evidence

`png-baseline.json` summarizes the previous Tap Titans 2 import/first-run test, not a controlled new benchmark. Its 59-second media window includes installation and launch.

| Measurement | Historical result |
| --- | --- |
| Frame size | 1080 × 1920 |
| Received decoded frames | 9.37/sec |
| Received image traffic | 9.64 MiB/sec |
| Median / p95 presentation callback interval | 120.2 / 145 ms |
| Emulator CPU, mean / peak of machine capacity | 24.67% / 48.60% |
| Python CPU, mean | 1.51% |
| WebView2 processes CPU, mean combined | 1.37% |
| WPF app CPU, mean | 0.62% |

Most measured CPU is inside the emulator process. PNG production occurs there, but guest startup, installation, game execution and the actual graphics backend are confounders. These results cannot establish how much CPU native presentation will save. Received frames and video callbacks are not validated end-to-end input latency. Actual GPU selection remains unverified.

## Verification

Release build: zero warnings/errors. All 128 .NET tests passed. The standard native WebView shell smoke passed, including all 60 page transitions without a viewport unload, adaptive sizes, theme switching, profile isolation and backup command guards. `shell-phase-1.json` records the result. Existing screenshots were visually checked for setup/layout correctness in earlier validation; this run supplies fresh screenshot fixtures for review.

The reported mouse-hover flicker has not been reproduced and conclusively eliminated. This phase fixes confirmed content lifetime and routed-event behavior; it does not claim a general flicker or CPU fix. Composition mode and physical mixed-DPI monitors were not validated in this phase. Real Android transport performance is unchanged.

## Next phase

Build an opt-in native-window viewport prototype behind the existing viewport abstraction. Keep the current renderer as an explicit fallback. Verify owned-window identification, GPU/iGPU backend, DPI compatibility, aspect-ratio resizing, focus, native input, optional audio and teardown. Compare native standalone, embedded native and existing controller modes under the same workloads. Test continuous resizing and tab transitions before making native hosting the default. Preserve existing installation and device data; no wipe/reset or authorization bypass. See `../PERFORMANCE_PLAN.md` for the fallback gate.
