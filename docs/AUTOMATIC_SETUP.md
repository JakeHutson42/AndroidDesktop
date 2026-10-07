# Automatic Android preparation

The application discovers installed dependencies from saved settings, the packaged application, standard Android/Java installation locations, and the project ancestors of its executable. Launching from Visual Studio no longer requires the working directory to be the repository root.

When a complete runtime is present, normal startup automatically retains or creates the application-owned Android device, opens the Device page, boots Android and connects its viewport. Missing runtimes open Setup. Successful preparation returns to Device and boots Android after the setup busy state is released. Opening an APK also ensures preparation completes before starting and importing it. Existing devices are retained; no force-create or wipe flags are used. Smoke and explicit embedded tests keep their own startup sequence.

For missing Android components, the Setup page presents a license link, acceptance checkbox and Prepare Android action. It installs private checksum-verified Temurin Java 21 and Android command-line tools 23, then invokes Google's SDK Manager for the stable emulator, platform tools, Android system image and APK build tools. Downloads require an internet connection; Android preparation requires 12 GB of free disk space. Installed components are discovered on restart and SDK Manager resumes missing packages. Cancellation terminates owned tool processes and retains completed components and device data.

The packaged application includes its private Python runtime and gateway. A development checkout needs its prepared gateway virtual environment. An incomplete package reports a repair message rather than requesting Python paths from ordinary users. Hardware virtualization and Windows Hypervisor Platform still need to be enabled on the computer; the app does not change firmware or Windows features automatically.

Advanced paths, diagnostics, resource settings, profiles and backups remain available under a collapsed Advanced section. The default embedded transport is the controller display.

Validation: application builds without warnings, 123 automated tests pass, and the standard WebView2 shell smoke passes. Fresh multi-GB downloading and actual APK playback have not been rerun for this change.
