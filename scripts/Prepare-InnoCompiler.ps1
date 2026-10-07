param([string]$Directory = 'artifacts/build-tools/inno')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $Directory))
if (!$taskOutput.StartsWith($taskRoot+[IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Compiler directory must be in the workspace.' }
$taskDownloads = Join-Path $taskRoot 'artifacts/downloads'
New-Item -ItemType Directory -Path $taskDownloads -Force | Out-Null
$taskSetup = Join-Path $taskDownloads 'innosetup-6.5.3.exe'
if (!(Test-Path -LiteralPath $taskSetup)) { Invoke-WebRequest 'https://github.com/jrsoftware/issrc/releases/download/is-6_5_3/innosetup-6.5.3.exe' -OutFile $taskSetup }
if ((Get-FileHash -LiteralPath $taskSetup -Algorithm SHA256).Hash -ne '9345ee029faa0b7aed0818c3d5b227699ef9a496cce79e20c19eb9d6ef2e2c2d') { throw 'Inno compiler checksum mismatch' }
# Build tool only. Commercial distribution must meet the selected Inno license terms.
$taskProcess = Start-Process -FilePath $taskSetup -ArgumentList @('/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS',('/DIR="'+$taskOutput+'"')) -WindowStyle Hidden -PassThru -Wait
if ($taskProcess.ExitCode) { throw "Compiler setup failed: $($taskProcess.ExitCode)" }
Write-Output (Join-Path $taskOutput 'ISCC.exe')
