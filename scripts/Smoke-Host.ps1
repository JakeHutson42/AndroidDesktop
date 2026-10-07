param([string]$OutputDirectory = 'docs/phase-0', [string]$Executable, [switch]$CheckProfiles, [switch]$CheckBackups)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskApp = if ($Executable) { [IO.Path]::GetFullPath($Executable) } else { Join-Path $taskRoot 'src/AndroidDesktop/bin/Release/net10.0-windows10.0.19041.0/AndroidDesktop.exe' }
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputDirectory))
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
foreach ($taskMode in @('standard','composition')) {
    $taskFresh = Join-Path $taskOutput ("smoke-$taskMode-"+[Guid]::NewGuid().ToString('N')+'.json')
    $taskArguments = @('--smoke-test', ('"'+$taskFresh+'"'))
    if ($taskMode -eq 'composition') { $taskArguments += '--composition' }
    $taskProcess = Start-Process -FilePath $taskApp -WorkingDirectory $taskRoot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
    if (!$taskProcess.WaitForExit(25000)) {
        Stop-Process -Id $taskProcess.Id
        throw "Owned $taskMode smoke process timed out"
    }
    if ($taskProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $taskFresh)) { throw "Smoke process failed: $taskMode / exit $($taskProcess.ExitCode)" }
    $taskReport = Get-Content -LiteralPath $taskFresh -Raw | ConvertFrom-Json
    if (!$taskReport.packagedViewportLoaded) { throw "Packaged viewport failed: $taskFresh" }
    if (!$taskReport.shellChecks.themeSwitchApplied -or !$taskReport.shellChecks.minimumBoundsRestored -or !$taskReport.shellChecks.setupCheckComplete -or $taskReport.shellChecks.pages.Count -ne 3) { throw "Desktop shell checks failed: $taskFresh" }
    if ($CheckProfiles -and (!$taskReport.shellChecks.isolatedProfileAdded -or !$taskReport.shellChecks.profileSwitched -or !$taskReport.shellChecks.defaultProfileRestored -or !$taskReport.shellChecks.conflictingProfileCommandsBlocked)) { throw "Device profile shell checks failed: $taskFresh" }
    if ($CheckBackups -and !$taskReport.shellChecks.backupRequiresGracefulShutdown) { throw "Backup shutdown guard failed: $taskFresh" }
    Move-Item -LiteralPath $taskFresh -Destination (Join-Path $taskOutput "smoke-$taskMode.json") -Force
    $taskReport | ConvertTo-Json
}
