param([Parameter(Mandatory=$true)][string]$SdkRoot, [Parameter(Mandatory=$true)][string]$JavaExecutable,
      [Parameter(Mandatory=$true)][string]$SystemImage, [string]$OutputFile = 'artifacts/hardware-runtime.json')
$ErrorActionPreference = 'Stop'
if ($SystemImage -notmatch '^system-images;android-[0-9]+;[A-Za-z0-9_-]+;[A-Za-z0-9_-]+$') { throw 'Invalid image identifier' }
function Read-Properties([string]$Path) {
    if (!(Test-Path -LiteralPath $Path)) { return $null }
    $taskProperties = [ordered]@{}
    foreach ($taskLine in Get-Content -LiteralPath $Path) { if ($taskLine -match '^([^#=]+)=(.*)$') { $taskProperties[$Matches[1].Trim()] = $Matches[2].Trim() } }
    return $taskProperties
}
$taskEmulator = Join-Path $SdkRoot 'emulator/emulator.exe'
$taskReport = [ordered]@{ utc=[DateTime]::UtcNow.ToString('o'); status='inventory only; hardware acceptance unverified';
    os=(Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,OSArchitecture);
    cpu=(Get-CimInstance Win32_Processor | Select-Object Name,NumberOfCores,NumberOfLogicalProcessors,VirtualizationFirmwareEnabled);
    gpu=(Get-CimInstance Win32_VideoController | Select-Object Name,DriverVersion);
    emulatorProperties=(Read-Properties (Join-Path $SdkRoot 'emulator/source.properties'));
    imageIdentifier=$SystemImage; imageProperties=(Read-Properties (Join-Path $SdkRoot ($SystemImage.Replace(';','/')+'/source.properties')));
    emulatorVersion=(& $taskEmulator -version 2>&1 | Out-String);
    acceleration=(& $taskEmulator -accel-check 2>&1 | Out-String);
    javaVersion=(& $JavaExecutable -version 2>&1 | Out-String) }
New-Item -ItemType Directory -Path (Split-Path ([IO.Path]::GetFullPath($OutputFile))) -Force | Out-Null
$taskReport | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputFile -Encoding utf8
Write-Output "Inventory saved: $OutputFile; this does not certify the runtime gate."
