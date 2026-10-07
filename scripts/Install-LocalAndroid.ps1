param([switch]$AcceptSdkTerms)
$ErrorActionPreference = 'Stop'
if (!$AcceptSdkTerms) { throw 'Explicit agreement to the Android SDK terms is required. Re-run with -AcceptSdkTerms after agreement.' }
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskSdk = Join-Path $taskRoot 'artifacts/android-sdk'
$taskJavaHome = (Get-ChildItem (Join-Path $taskRoot 'artifacts/android-setup/java') -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'bin/java.exe') } | Select-Object -First 1).FullName
if (!$taskJavaHome) { throw 'Verified Java archive must be extracted first.' }
$env:JAVA_HOME = $taskJavaHome
$env:ANDROID_HOME = $taskSdk
$taskJavaExe = Join-Path $taskJavaHome 'bin/java.exe'
$taskToolRoot = Join-Path $taskSdk 'cmdline-tools/latest'
$taskJars = @(Get-ChildItem (Join-Path $taskToolRoot 'lib') -Recurse -Filter '*.jar' | Sort-Object FullName | ForEach-Object FullName)
$taskManagerArgs = @("-Dcom.android.sdkmanager.toolsdir=$taskToolRoot", '-cp', ($taskJars -join [IO.Path]::PathSeparator), 'com.android.sdklib.tool.sdkmanager.SdkManagerCli', "--sdk_root=$taskSdk")
# The caller explicitly authorized acceptance; stdin answers the official manager's prompts.
1..100 | ForEach-Object { 'y' } | & $taskJavaExe @taskManagerArgs --licenses
if ($LASTEXITCODE) { throw 'SDK licence acceptance failed.' }
& $taskJavaExe @taskManagerArgs --channel=0 'platform-tools' 'emulator' 'system-images;android-34;google_apis_playstore;x86_64' 'platforms;android-34' 'build-tools;36.0.0'
if ($LASTEXITCODE) { throw 'SDK package installation failed.' }
foreach ($taskPackagePath in @('platform-tools/adb.exe', 'emulator/emulator.exe', 'system-images/android-34/google_apis_playstore/x86_64/package.xml', 'platforms/android-34/android.jar', 'build-tools/36.0.0/aapt2.exe')) {
    if (!(Test-Path (Join-Path $taskSdk $taskPackagePath))) { throw "Package installation incomplete: $taskPackagePath" }
}
& (Join-Path $taskSdk 'emulator/emulator.exe') -version
& (Join-Path $taskSdk 'emulator/emulator.exe') -accel-check
Write-Output "SDK: $taskSdk"
Write-Output "Java: $taskJavaHome"
