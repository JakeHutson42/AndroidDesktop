param([Parameter(Mandatory=$true)][string]$Installer, [string]$OutputFile = 'docs/phase-3/installer-smoke.json')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskInstall = Join-Path $taskRoot ('artifacts/i-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
$taskAppKey = '{B6CDA357-735F-4AA8-A37B-4F19A81E9C54}_is1'
foreach ($taskRegistryRoot in @('HKCU:/Software/Microsoft/Windows/CurrentVersion/Uninstall','HKLM:/Software/Microsoft/Windows/CurrentVersion/Uninstall','HKLM:/Software/WOW6432Node/Microsoft/Windows/CurrentVersion/Uninstall')) {
    if (Test-Path -LiteralPath ($taskRegistryRoot+'/'+$taskAppKey)) { throw 'Android Desktop is already installed. Use a clean validation PC; this test will not replace its registration.' }
}
if (!$taskInstall.StartsWith($taskRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe installer validation directory' }
function Run-OwnedInstaller([string]$File, [string[]]$Arguments) {
    $taskProcess = Start-Process -FilePath $File -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    if (!$taskProcess.WaitForExit(60000)) { throw 'Installer still running after one minute; inspect the owned process before continuing.' }
    if ($taskProcess.ExitCode -ne 0) { throw "Installer operation failed: $($taskProcess.ExitCode)" }
}
$taskDataRoot = Join-Path $env:LOCALAPPDATA 'AndroidDesktop'
function Settings-Fingerprints {
    $taskHashes = @{}
    foreach ($taskName in @('settings.json','settings.json.bak')) {
        $taskPath = Join-Path $taskDataRoot $taskName
        if (Test-Path -LiteralPath $taskPath) { $taskHashes[$taskName] = (Get-FileHash -LiteralPath $taskPath).Hash }
    }
    return $taskHashes
}
$taskBefore = Settings-Fingerprints
$taskLog = Join-Path $taskRoot 'artifacts/installer-smoke.log'
$taskArgs = @('/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS','/TASKS=',('/DIR="'+$taskInstall+'"'),('/LOG="'+$taskLog+'"'))
Run-OwnedInstaller ([IO.Path]::GetFullPath($Installer)) $taskArgs
try {
    if (!(Test-Path -LiteralPath (Join-Path $taskInstall 'AndroidDesktop.exe'))) { throw 'Installed executable missing' }
    & (Join-Path $PSScriptRoot 'Test-Package.ps1') -Directory $taskInstall
    & (Join-Path $PSScriptRoot 'Smoke-Host.ps1') -Executable (Join-Path $taskInstall 'AndroidDesktop.exe') -OutputDirectory 'docs/phase-3/installed-smoke'
    # Same-version reinstallation validates replacement mechanics; it does not prove a historical Android-data upgrade.
    Run-OwnedInstaller ([IO.Path]::GetFullPath($Installer)) $taskArgs
    Run-OwnedInstaller (Join-Path $taskInstall 'unins000.exe') @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART')
    if (Test-Path -LiteralPath (Join-Path $taskInstall 'AndroidDesktop.exe')) { throw 'Uninstall left the desktop executable' }
    $taskAfter = Settings-Fingerprints
    if (($taskBefore | ConvertTo-Json -Compress) -ne ($taskAfter | ConvertTo-Json -Compress)) { throw 'Existing desktop settings changed during isolated installation smoke' }
    $taskReport = [ordered]@{ install='passed on existing development Windows PC'; packagedHost='standard and composition smoke passed';
        sameVersionReinstall='passed'; uninstallProgramFiles='passed'; existingSettingsObserved=$taskBefore.Count;
        existingSettingsUnchanged=$true; freshPc='unverified'; historicalVersionUpgrade='unverified'; androidDataAndSaves='unverified'; hardwareMediaAndPerformance='unverified' }
    $taskReport | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot $OutputFile) -Encoding utf8
    $taskReport | ConvertTo-Json
} catch {
    Write-Warning "Installer validation failed. Installed test program may remain at $taskInstall. Close it and run its own uninstaller; no user data is deleted by this script."
    throw
}
