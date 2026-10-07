param([switch]$StartDevice)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskConfig = Join-Path $taskRoot 'android.local.json'
if (!(Test-Path -LiteralPath $taskConfig)) { throw 'Complete local Android installation and acceptance first.' }
$taskApp = Join-Path $taskRoot 'src/AndroidDesktop/bin/Release/net10.0-windows10.0.19041.0/AndroidDesktop.exe'
if (!(Test-Path -LiteralPath $taskApp)) { throw 'Build AndroidDesktop.sln in Release first.' }
if ($StartDevice) { Start-Process -FilePath $taskApp -ArgumentList '--start-device' -WorkingDirectory $taskRoot }
else { Start-Process -FilePath $taskApp -WorkingDirectory $taskRoot }
