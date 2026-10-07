param([string]$OutputDirectory = 'docs/design/validation')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputDirectory))
if (!$taskOutput.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Theme evidence must stay inside this workspace.' }
$taskExe = Join-Path $taskRoot 'src/AndroidDesktop/bin/Release/net10.0-windows10.0.19041.0/AndroidDesktop.exe'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
foreach ($taskMode in @('standard','composition')) {
    $taskReportPath = Join-Path $taskOutput "theme-$taskMode.json"
    $taskArguments = @('--smoke-test', ('"' + $taskReportPath + '"'), '--theme-preview')
    if ($taskMode -eq 'composition') { $taskArguments += '--composition' }
    $taskProcess = Start-Process -FilePath $taskExe -WorkingDirectory $taskRoot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
    if (!$taskProcess.WaitForExit(35000)) { Stop-Process -Id $taskProcess.Id; throw "Owned $taskMode theme smoke timed out." }
    if ($taskProcess.ExitCode -ne 0) { throw "Theme smoke exited with $($taskProcess.ExitCode)." }
    $taskReport = Get-Content -LiteralPath $taskReportPath -Raw | ConvertFrom-Json
    if (!$taskReport.packagedViewportLoaded -or !$taskReport.shellChecks.adaptiveLayoutVerified -or !$taskReport.shellChecks.themeSwitchApplied -or !$taskReport.shellChecks.conflictingProfileCommandsBlocked -or !$taskReport.shellChecks.backupRequiresGracefulShutdown) { throw "Theme checks failed: $taskReportPath" }
    $taskReport | ConvertTo-Json -Depth 4
}
