# Real APK test — 7 October 2026

Input: `uptodown-com.gamehivecorp.taptitans2.apk` from the user's Pictures folder.

The manifest identifies **Uptodown App Store 7.40**, package `com.uptodown`, not Tap Titans 2. SHA256: `A753C6ADC1338F0318F145B27C08777A207337945D9696015363FF11C119FF07`.

The current Release app booted its persistent Android device, connected the controller viewport, inspected the APK, installed it through its normal import service, and launched it. Package Manager confirmed installation; the process remained running. Repeated imports also exercised the existing installed-app path. The test retained the installed application and existing device data but did not replace the user's saved APK library selection. Android's own screenshot showed Uptodown's all-files-access permission page; permission was not granted. The test returned Android to Home and gracefully stopped the owned session.

**Visual acceptance failed:** both the WebView preview and a frame sampled directly from its video were black, including after returning to Android Home. Android's screenshot was visibly rendered. The viewport reported connected input and received frames, which therefore do not establish successful visual display. This is an unresolved display-path issue; gameplay and visible interactive input are not verified.

Evidence: `artifacts/real-apk-display-test.json`, `.activity.txt`, `.android.png`, `.frame.png`, `.home.png`, and `.png` alongside the report. A generic `--apk-test` option was added to the explicit embedded test harness to record package/process/foreground evidence and screenshots. No game download or Uptodown permission grant was performed.

## Display repair — 8 October 2026

Normal startup now migrates older RTC settings to the authenticated controller transport. Controller video uses muted autoplay (audio remains a separate opt-in stream), correcting the black decoded video. A new real-device run accepted the authenticated controller API, connected input and produced a visibly rendered 1080×1920 Android home frame: `artifacts/display-fix-frames.json.startup.frame.png`. The harness subsequently reported failure because the user's original APK is no longer at its Pictures path; installation was not repeated. The terms checkbox no longer depends on device process ownership and is disabled only during setup. Build is clean, 123 .NET tests and 30 viewport tests pass.
