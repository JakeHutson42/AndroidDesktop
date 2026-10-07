# Source inspection and transport status

Before implementation, the supplied plan was read from the root (the requested `docs/IMPLEMENTATION_PLAN.md` did not yet exist). The complete canonical copy is now under `docs/`.

Corvus reference checkout inspected: `C:/Users/jakeh/source/repos/Corvus`.

- `Corvus.App/MainWindow.xaml` and `.xaml.cs`: themed layout, native lifetime, constructor-injected ViewModel. The prototype retains native resizable WPF chrome and adjacent controls, without copying soundboard pages or hotkey hooks.
- `Corvus.App/App.xaml` and `.xaml.cs`: resource dictionaries and composition. The prototype has a small constructor-based composition root, without Corvus audio/hosting services.
- `Corvus.App/Themes/Colors.xaml`, `Controls.xaml`, `Services/ThemeService.cs`: semantic palette and shared input/button states. Colours and shared controls were copied; live theme switching/persistence remains Phase 1.
- `Corvus.Infrastructure/Storage/AppSettingsStorage.cs`: the current reference actually uses SQLite, unlike the earlier proposal's AppStateStore description. No database or product settings persistence was imported into Phase 0.
- `Corvus.App/Corvus.App.csproj`: .NET 10/WPF and CommunityToolkit.Mvvm match the scaffold stack.

## Reused official streaming code

[Google emulator streaming repository](https://github.com/google/android-emulator-container-scripts) was fetched and inspected at commit `f3f20a06bac488cb7505ec95c8f6f936c0f8a782`.

- Python: `gateway/src/videobridge_gateway/gateway_server.py` → `tools/gateway/videobridge_gateway/upstream_gateway.py`.
- Browser signalling: `js/src/components/emulator/net/ws_jsep_protocol_driver.ts` and `logger.ts` → `tools/viewport/upstream/`.
- Inspected gateway setup/demo/protocol, browser input/viewport source and controller schema.
- Local changes: WebSocket session-token subprotocol, no external STUN/TURN, bounded unary deadlines, pointer release/RTC teardown, and removal of the driver's protobuf sender in favour of one shared semantic input adapter. The wrapper restricts binding/origin, exposes a reduced route set and probes RTC before opening the display.
- The upstream standalone launcher remains in the vendored file for provenance but is not the application entry point. Always run `tools/gateway/run.py`, never the upstream standalone server. The wrapper binds only loopback.
- Protobuf serialization is generated statically at build time; the browser's content security policy does not allow runtime string evaluation. Media is handled by Chromium and the emulator, without managed screenshot rendering or C# codecs.

Official protocol files are from [AOSP aemu](https://android.googlesource.com/platform/hardware/google/aemu/+/edd3526160771a72bb964317bf420be44208ab26/protos/) commit `edd3526160771a72bb964317bf420be44208ab26` (`emu-main-next` at inspection). They define experimental RTC v2. They are pinned *source inputs*, **not a supported stable emulator release combination**. Python clients are generated using the matching files. No C# signalling port has been attempted. There is no guarantee a stable Windows emulator implements this RTC service; a failed probe is a failed candidate check.

## Decision

**No final transport selected.** WebRTC/WebView2 is a provisional prototype. The real emulator/image pair, target APK, audio/sync, geometry, latency, decoded frames and resource budgets remain unverified. Scrcpy/native embedding has not been implemented or benchmarked. Follow the runbook to obtain evidence before choosing the transport or advancing a phase.

No hosted demo, React/Vite runtime server or external ICE service is required by this frontend. Python sidecar packaging, .NET self-contained distribution, installer, production setup/import UI and third-party release audit remain their later planned phases.
