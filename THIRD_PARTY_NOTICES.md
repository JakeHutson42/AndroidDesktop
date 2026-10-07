# Source notices

- Reused emulator gateway / WebRTC signalling and vendored emulator protocols: Copyright The Android Open Source Project; Apache License 2.0. Original notices remain in source. See `tools/gateway/LICENSE` and `tools/viewport/LICENSE`, and source commits in `docs/phase-0/REFERENCE_NOTES.md`.
- Corvus colours/control styles, palette switching and navigation styling were adapted from the user's local Corvus reference (`Corvus.App`); no ownership or release-license claim is made here. Phase 1 re-inspected `Services/ThemeService.cs` and `MainWindow.xaml` before adapting them.
- Browser bundle includes protobufjs (BSD 3-Clause) and loglevel (MIT). Their license text is included with the viewport assets. esbuild and protobufjs-cli are build-only tools; their dependencies/versions are locked in `tools/viewport/pnpm-lock.yaml`.
- NuGet dependencies remain governed by their package licenses and are pinned in the project/lock files. Python dependencies and transitives are pinned in `tools/gateway/requirements.lock.txt`.

## Candidate distribution inventory

The candidate publish includes NuGet package metadata/license texts under `notices`, .NET 10.0.11 runtime-pack licenses/third-party notices, CPython 3.12.10's `runtime/LICENSE.txt`, Python wheel `*.dist-info` metadata/licenses and the full gateway Apache license. Browser protobufjs/loglevel license texts accompany the viewport. `inventory.json` lists packaged file sizes/SHA-256. `runtime-candidate.json` identifies the version/commit combination and its unverified hardware fields. Build-only Grpc.Tools metadata is included for provenance even though its compiler is not a runtime dependency.

Private gateway runtime wheels: aiohappyeyeballs 2.7.1, aiohttp 3.13.3, aiosignal 1.4.0, attrs 26.1.0, frozenlist 1.8.0, grpcio 1.78.0, idna 3.20, multidict 6.9.1, propcache 0.5.4, protobuf 6.33.5, typing_extensions 4.16.0, yarl 1.25.1. Their exact installed wheel metadata remains in the package. CPython archive is SHA-256 pinned; runtime dependencies are version pinned. grpcio-tools/setuptools, Node/pnpm/esbuild and the developer virtual environment are excluded from the runtime package.

Inno Setup 6.5.3 is a separate build tool; its compiler reports non-commercial use licensing. Review/obtain the applicable Inno commercial license before commercial distribution. The compiler installation is not part of the Android Desktop payload.

Corvus redistribution permission/license is not established by the supplied local reference. The full distribution license review remains open, along with hardware and clean-PC acceptance. These artifacts are for evaluation, not a certified public/commercial release. Android SDK/system images, Java and Evergreen WebView2 are installed separately through official tools/terms; no SDK/image/Java/scrcpy binaries are redistributed.
