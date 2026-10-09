param(
    [ValidateSet('native','standalone','controller')][string[]]$Modes = @('native','standalone','controller'),
    [ValidateRange(10,600)][int]$Seconds = 20,
    [string]$OutputDirectory = 'docs/performance',
    [ValidateSet("home","game","diagnostic")][string]$Workload = "home",
    [switch]$GrpcShutdown,
    [string]$DiagnosticApk
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputDirectory))
if (!$taskOutput.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Reports must remain inside the workspace.' }
if (Get-Process AndroidDesktop -ErrorAction SilentlyContinue) { throw 'Close Android Desktop normally before testing.' }
$taskExe = Join-Path $taskRoot 'src/AndroidDesktop/bin/Release/net10.0-windows10.0.19041.0/AndroidDesktop.exe'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
foreach ($taskMode in $Modes) {
    $taskReport = Join-Path $taskOutput "$taskMode-phase-2.json"
    $taskArguments = '--render-test ' + $taskMode + ' "' + $taskReport + '" ' + $Seconds
    if ($DiagnosticApk) {
        $taskApk = [IO.Path]::GetFullPath($DiagnosticApk)
        if (!(Test-Path -LiteralPath $taskApk -PathType Leaf)) { throw 'Build the repository diagnostic APK first.' }
        $taskArguments += ' "' + $taskApk + '"'
    }
    if (!$DiagnosticApk) { $taskArguments += " -" }
    $taskArguments += " " + $Workload
    if ($GrpcShutdown) { $taskArguments += " --grpc-shutdown" }
    $taskProcess = Start-Process -FilePath $taskExe -WorkingDirectory $taskRoot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
    $taskDeadline = [DateTime]::UtcNow.AddMinutes(15)
    while (!$taskProcess.WaitForExit(1000)) {
        if ([DateTime]::UtcNow -gt $taskDeadline) {
            $null = $taskProcess.CloseMainWindow()
            throw "Owned $taskMode test timed out; normal close requested. Inspect the app before retrying."
        }
    }
    if ($taskProcess.ExitCode -ne 0) { throw "$taskMode test exited with $($taskProcess.ExitCode)." }
    $taskResult = Get-Content -LiteralPath $taskReport -Raw | ConvertFrom-Json
    if (!$taskResult.ready -or $taskResult.error -or !$taskResult.gracefulStop -or $taskResult.ownedProcessHandlesAfterStop) { throw "$taskMode lifecycle failed; inspect $taskReport." }
    if ($taskMode -eq 'native' -and (!$taskResult.retainedDuringResizeAndNavigation -or !$taskResult.nativeDetached -or !$taskResult.nativeFocus)) { throw "Native host checks failed; inspect $taskReport." }
    if ($taskMode -eq 'native') {
        if (!$taskResult.hiddenBeforeNativeAttach -or $taskResult.visibleDetachedEvents -ne 0 -or !$taskResult.darkMargins -or $taskResult.startupProgressAtReady -ne 100 -or !$taskResult.rotationCommandEnabled -or $taskResult.rotations.Count -ne 4) { throw "Native containment/loading checks failed; inspect $taskReport." }
        foreach ($taskRotation in $taskResult.rotations) {
            $taskOrientation = [int]($taskRotation.requestedDegrees / 90)
            if (!$taskRotation.attached -or !$taskRotation.geometryMatches -or $taskRotation.orientation -notmatch "orientation=$taskOrientation,") { throw "Native rotation failed; inspect $taskReport." }
        }
    }
    if ($taskMode -eq 'native' -and $DiagnosticApk -and (!$taskResult.nativeInputTestRan -or !$taskResult.nativeTouch -or !$taskResult.nativeKey)) { throw "Native input receipt failed; inspect $taskReport." }
    if ($taskResult.saveReopen -and !$taskResult.saveReopen.passed) { throw "Diagnostic private data changed across restart; inspect $taskReport." }
    Write-Output "$taskMode passed: $taskReport"
}
