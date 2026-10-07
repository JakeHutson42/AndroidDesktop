param([Parameter(Mandatory=$true)][string]$PublishDirectory, [Parameter(Mandatory=$true)][string]$Compiler,
      [string]$OutputDirectory = 'artifacts/installer')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskPublish = [IO.Path]::GetFullPath($PublishDirectory)
& (Join-Path $PSScriptRoot 'Test-Package.ps1') -Directory $taskPublish
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputDirectory))
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
& $Compiler /Q ('/DPublishDir='+$taskPublish) ('/DOutputDir='+$taskOutput) (Join-Path $taskRoot 'packaging/AndroidDesktop.iss')
if ($LASTEXITCODE) { throw 'Inno Setup compilation failed' }
Get-ChildItem -LiteralPath $taskOutput -Filter '*.exe' | Get-FileHash -Algorithm SHA256
