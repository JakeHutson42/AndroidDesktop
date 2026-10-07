# Candidate build and verification

From this Windows checkout with the pinned .NET SDK, Node/pnpm and development Python dependencies:

```powershell
./scripts/Prepare-Phase0.ps1 -Python 'C:/path/to/python.exe' -PackageManager 'pnpm.cmd'
dotnet restore AndroidDesktop.sln --locked-mode
dotnet test AndroidDesktop.sln -c Release --no-restore
node --test tools/viewport/test/*.test.mjs
./tools/gateway/.venv/Scripts/python.exe -B -m unittest discover -s tools/gateway/tests -v
./scripts/Publish-Candidate.ps1 -Python 'C:/path/to/python.exe' -OutputDirectory artifacts/new-empty-candidate
./scripts/Prepare-InnoCompiler.ps1
./scripts/Build-Installer.ps1 -PublishDirectory artifacts/new-empty-candidate -Compiler artifacts/build-tools/inno/ISCC.exe
./scripts/Smoke-Host.ps1 -Executable artifacts/new-empty-candidate/AndroidDesktop.exe -OutputDirectory docs/phase-3
./scripts/Test-Installer.ps1 -Installer artifacts/installer/AndroidDesktop-0.3.0-win-x64-candidate.exe
```

Build tools are development-only. The publish output contains the self-contained x64 desktop runtime, packaged browser assets, isolated CPython and pinned binary gateway dependencies. It never copies a venv, repository paths, Node modules, SDK/image/Java, APKs, caches or developer configuration. Use a new empty output directory each time; stale files cause a failure. Downloaded private Python and Inno compiler are SHA-256 verified. Two committed NuGet locks cover ordinary builds and x64 publish; .NET runtime 10.0.11 is explicitly pinned. Runtime package licenses/third-party notices and NuGet/Python metadata accompany the payload; inventory SHA-256 covers all packaged files except the inventory itself.

Test-Package checks required content and known private/development patterns. It is a targeted contract check, not proof that all credentials or licensing concerns have been resolved. Review the complete inventory before distribution. Inno 6.5.3 compiler license and Corvus redistribution authorization require review before public/commercial release.

Test-Installer refuses to run when the product is already installed. It installs only into a unique workspace test directory, skips desktop-shortcut selection/app launch, executes isolated nonpersistent shell smokes, reinstalls the same version, uninstalls and checks existing settings fingerprints. Temporary Start Menu entries are created and removed by the installer/uninstaller. It does not create a device, mutate production settings or establish Android upgrade/save retention. Use a fresh physical Windows PC for the complete matrix in HARDWARE_AND_SETUP.md.

To run the fake-server gateway contract against the **packaged** runtime:

```powershell
$env:ANDROID_DESKTOP_TEST_PYTHON = (Resolve-Path artifacts/new-empty-candidate/runtime/python.exe).Path
$env:ANDROID_DESKTOP_TEST_GATEWAY = (Resolve-Path artifacts/new-empty-candidate/gateway/run.py).Path
./tools/gateway/.venv/Scripts/python.exe -B -m unittest discover -s tools/gateway/tests -v
Remove-Item Env:ANDROID_DESKTOP_TEST_PYTHON, Env:ANDROID_DESKTOP_TEST_GATEWAY
```

The fixture server runs in development Python; the gateway child uses only packaged Python/dependencies. These tests check loopback authorization, origin restrictions, no external ICE, signalling/probe/release and graceful teardown. They do not supply actual video/audio/Android.

Normal diagnostics are bounded, sampled at 1 Hz and asynchronously written. Raw gesture evidence is opt-in using `ANDROID_DESKTOP_VERBOSE_EVIDENCE=1` before starting the host; it is still the bounded Phase 0 diagnostic proof, not Phase 4 recording. Browser developer tools are disabled unless `ANDROID_DESKTOP_DEVTOOLS=1`. Support exports omit raw input, APK/settings/discovery data, redact structured sensitive fields and personal paths, mark incomplete/truncated evidence and preserve a previous destination on failure/cancellation.

The Windows GitHub Actions workflow prepares locked build dependencies, runs tests, publishes/contracts and compiles the installer. It has not run on GitHub until this workspace is placed in a repository and pushed. It deliberately does not certify emulator hardware, media, game saves, performance, soak or clean-PC acceptance.
