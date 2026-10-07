# APK library validation

Build/run the source with `dotnet test AndroidDesktop.sln -c Release --no-restore` and `dotnet run --project src/AndroidDesktop -c Release --no-build`. Historical 0.3.0 installers do not contain this library. The existing Phase 3 build guide describes candidate publishing if a new evaluation installer is needed.

The library starts empty when no APK has been imported. Expand APK library on Device. Use Open APK or drop one standalone APK on the existing card; a successful install and launch adds its entry. Source files are referenced rather than copied. Keep them available. Selecting a row does not change the running or selected Android app until Launch library app succeeds.

Use Locate APK / update on a selected row when its source moves or to select a compatible version. Its package identity must match the row. A different package must use Open APK. The actual active device UUID determines the entry being committed; a row from another device never establishes installation on this device. Version history and removal actions are outside this scope. No desktop library operation removes an APK source, uninstalls a package, clears Android data or recreates a device.

Settings schema v3 adds ApkLibrary to the existing JSON. Schema-v2 selected metadata migrates to a library entry at load, and is persisted through the normal atomic settings save with previous-commit backup. Theme, bounds, runtime paths, selected metadata and device root remain. Newer schemas are read-only. Older application versions protect schema v3 rather than downgrading it; return to the current build to use the library.

## Hardware checks when APKs become available

First execute the inherited Phase 0/3/4 hardware steps. Use your own representative standalone APKs and compatible signed updates; preserve original settings, sources, recordings and Android data. Do not reset the AVD to make a check pass.

1. Import two different packages, save progress in each, switch using library launch and close/reopen. Confirm both entries and actual Android progress remain. Unchanged installed sources should launch without reinstall, following actual package/version verification.
2. Move an APK source. Launch should explain the missing file while keeping metadata. Locate the same package, then verify successful launch commits the new path. Locating a different package must reject before installation and retain the row.
3. Update with a compatible signed APK. Verify progress and the other entry survive. Try incompatible signatures/downgrades/split formats and retain errors; the previous row/selection must survive rejection without uninstall or clear.
4. Test failure after Android installation but before launch/settings commit on disposable fixtures. Desktop metadata remains previous; Android may already contain the update. Retry must reconcile actual state without destructive rollback.
5. During recording/replay, setup, import or shutdown, library launch/locate must be disabled. Verify actual input releases and no conflicting device operation.
6. Inspect rows/themes at minimum window size and multiple DPI scales, with long paths and a representative entry count. The list has bounded visible height and scrolls. Record actual usability and browsing timing; source-level tests do not prove manual usability.

The synthetic 1,000-entry metadata check measures save, load and 1,000 identity lookups together; it is not a UI or gameplay benchmark. Run it through the .NET suite and retain the report from the test output's docs/phase-5 directory. Report workload and machine/runtime with any performance claim. Hardware acceptance still requires real Android observations.
