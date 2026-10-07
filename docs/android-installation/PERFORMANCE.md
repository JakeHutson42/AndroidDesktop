# Performance and resource cleanup

User priority: efficient embedded display, audio deferred, resource release on device shutdown. One active device remains enforced; originals and Android data are preserved.

This is the earlier performance pass. Audio was subsequently authorized and implemented; see [current audio implementation and evidence](AUDIO.md).

Changes: frame pacing subtracts work already performed from the 15 fps interval, preserving one acknowledged frame in flight and existing byte/pixel bounds. Disconnect releases media tracks, canvas backing storage, video source and socket handlers. Remote socket closure follows the same cleanup. Disconnected WebView controls are disposed and recreated lazily; the host waits for BrowserProcessExited, reporting a ten-second timeout while allowing emulator shutdown to proceed. Gateway shutdown explicitly closes controller sockets before application/channel cleanup. Dependencies are unchanged; the inventory in this directory remains applicable. Historical packaged releases are unchanged; Release source build was rebuilt.

Passed: 116 .NET tests; 27 browser/input tests including remote-close cleanup and disconnect during decode; 6 gateway contract tests. Release build passes. Standard/composition smoke results are in performance-smoke.

Failed attempt: the first Release rebuild encountered an existing app locking its executable; a subsequent rebuild succeeded. The real-device performance harness did not produce a report before graceful closure was requested. This is not a measured speedup or a passed Android acceptance run.

Unverified: resulting real Android frame rate, CPU/memory/bandwidth and input latency; repeated reconnect/stop soak; zero remaining owned emulator/gateway/browser descendants after each real shutdown; dedicated ownership and release of the currently shared ADB server. Shared ADB must not be terminated indiscriminately. The existing real Android diagnostic evidence remains historical, predating these changes. PNG encoding/decoding and canvas-to-video conversion remain potential bottlenecks requiring measurement before choosing a different transport. No audio or gaming performance claim is made.
