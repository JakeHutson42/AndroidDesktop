param(
    [Parameter(Mandatory=$true)][string]$Python,
    [string]$PackageManager = 'pnpm.cmd'
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $taskRoot
try {
    & $Python -m venv tools/gateway/.venv
    if ($LASTEXITCODE) { throw 'Python venv creation failed' }
    $taskPython = Join-Path $taskRoot 'tools/gateway/.venv/Scripts/python.exe'
    & $taskPython -m pip install -r tools/gateway/requirements.lock.txt
    if ($LASTEXITCODE) { throw 'Gateway dependency installation failed' }
    & $taskPython tools/gateway/generate_protos.py
    if ($LASTEXITCODE) { throw 'Protocol generation failed' }
    Push-Location tools/viewport
    try {
        & $PackageManager install --frozen-lockfile --ignore-scripts
        if ($LASTEXITCODE) { throw 'Viewport dependency installation failed' }
        & $PackageManager run build
        if ($LASTEXITCODE) { throw 'Viewport build failed' }
    } finally { Pop-Location }
    dotnet restore AndroidDesktop.sln --locked-mode
    if ($LASTEXITCODE) { throw 'NuGet restore failed' }
    dotnet build AndroidDesktop.sln -c Release --no-restore
    if ($LASTEXITCODE) { throw 'Release build failed' }
} finally { Pop-Location }
