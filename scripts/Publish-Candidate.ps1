param(
    [Parameter(Mandatory=$true)][string]$Python,
    [string]$OutputDirectory = 'artifacts/publish',
    [string]$PythonArchive = 'artifacts/downloads/python-3.12.10-embed-amd64.zip'
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputDirectory))
if (!$taskOutput.StartsWith($taskRoot+[IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Publish directory must be inside this workspace.' }
if (Test-Path -LiteralPath $taskOutput) { if (@(Get-ChildItem -LiteralPath $taskOutput -Force).Count -gt 0) { throw 'Use a new empty output directory to avoid stale package files.' } }
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskPin = Get-Content (Join-Path $taskRoot 'packaging/runtime-candidate.json') -Raw | ConvertFrom-Json
$taskArchive = [IO.Path]::GetFullPath((Join-Path $taskRoot $PythonArchive))
if (!(Test-Path -LiteralPath $taskArchive)) {
    New-Item -ItemType Directory -Path (Split-Path $taskArchive) -Force | Out-Null
    Invoke-WebRequest $taskPin.pythonDownload -OutFile $taskArchive
}
if ((Get-FileHash $taskArchive -Algorithm SHA256).Hash -ne $taskPin.pythonSha256) { throw 'Official Python archive checksum mismatch.' }
& dotnet restore (Join-Path $taskRoot 'AndroidDesktop.sln') --locked-mode
if ($LASTEXITCODE) { throw 'Locked restore failed' }
# A separate committed RID lock preserves the platform-neutral application/test locks.
$taskRidLock = Join-Path $taskRoot 'packaging/packages.win-x64.lock.json'
& dotnet publish (Join-Path $taskRoot 'src/AndroidDesktop/AndroidDesktop.csproj') -c Release -r win-x64 --self-contained true -p:RestoreLockedMode=true ('-p:NuGetLockFilePath='+$taskRidLock) -p:DebugType=None -p:DebugSymbols=false -o $taskOutput
if ($LASTEXITCODE) { throw 'Self-contained publish failed' }
$taskRuntime = Join-Path $taskOutput 'runtime'
Expand-Archive -LiteralPath $taskArchive -DestinationPath $taskRuntime
# Isolated Python: no registry, global installation, user packages or environment Python path.
@('python312.zip','.', 'site-packages','../gateway') | Set-Content -LiteralPath (Join-Path $taskRuntime 'python312._pth') -Encoding ascii
$taskDependencies = Get-Content (Join-Path $taskRoot 'tools/gateway/requirements.lock.txt') | Where-Object { $_ -notmatch '^(grpcio-tools|setuptools)==' }
$taskRuntimeLock = Join-Path $taskOutput 'gateway-requirements.lock.txt'
$taskDependencies | Set-Content -LiteralPath $taskRuntimeLock -Encoding ascii
& $Python -m pip install --disable-pip-version-check --only-binary=:all: --no-compile --no-deps --target (Join-Path $taskRuntime 'site-packages') -r $taskRuntimeLock
if ($LASTEXITCODE) { throw 'Private gateway dependencies failed' }
$taskGateway = Join-Path $taskOutput 'gateway'
New-Item -ItemType Directory -Path $taskGateway -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot 'tools/gateway/run.py') -Destination $taskGateway
Copy-Item -LiteralPath (Join-Path $taskRoot 'tools/gateway/LICENSE') -Destination $taskGateway
$taskSource = Join-Path $taskRoot 'tools/gateway/videobridge_gateway'
foreach ($taskFile in Get-ChildItem -LiteralPath $taskSource -Recurse -File | Where-Object { $_.Extension -eq '.py' -and $_.FullName -notmatch '[\\/]__pycache__[\\/]' }) {
    $taskRelative = [IO.Path]::GetRelativePath((Split-Path $taskSource), $taskFile.FullName)
    $taskDestination = Join-Path $taskGateway $taskRelative
    New-Item -ItemType Directory -Path (Split-Path $taskDestination) -Force | Out-Null
    Copy-Item -LiteralPath $taskFile.FullName -Destination $taskDestination
}
$taskNotices = Join-Path $taskOutput 'notices'
New-Item -ItemType Directory -Path $taskNotices -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot 'THIRD_PARTY_NOTICES.md') -Destination $taskNotices
Copy-Item -Path (Join-Path $taskRoot 'packaging/licenses/*') -Destination $taskNotices
Copy-Item -LiteralPath (Join-Path $taskRoot 'packaging/runtime-candidate.json') -Destination $taskOutput
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/phase-3/HARDWARE_AND_SETUP.md') -Destination $taskOutput
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/phase-3/RECOVERY.md') -Destination $taskOutput
# NuGet license provenance from the exact restored packages; do not ship their build caches.
$taskLicenseInventory = @()
$taskAssets = Get-Content (Join-Path $taskRoot 'src/AndroidDesktop/obj/project.assets.json') -Raw | ConvertFrom-Json
$taskPackageRoot = ($taskAssets.packageFolders.PSObject.Properties | Select-Object -First 1).Name
foreach ($taskLibrary in $taskAssets.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' }) {
    $taskPackagePath = Join-Path $taskPackageRoot $taskLibrary.Value.path
    $taskNuspec = Get-ChildItem -LiteralPath $taskPackagePath -Filter '*.nuspec' | Select-Object -First 1
    [xml]$taskSpec = Get-Content -LiteralPath $taskNuspec.FullName -Raw
    $taskLicenseInventory += [ordered]@{ package=$taskLibrary.Name; license=[string]$taskSpec.package.metadata.license.InnerText; licenseUrl=[string]$taskSpec.package.metadata.licenseUrl; projectUrl=[string]$taskSpec.package.metadata.projectUrl }
    foreach ($taskLicense in Get-ChildItem -LiteralPath $taskPackagePath -Recurse -File | Where-Object { $_.Name -match '^(LICENSE|NOTICE|THIRD.?PARTY)' -or $_.Extension -eq '.nuspec' }) {
        $taskLicenseDir = Join-Path $taskNotices ($taskLibrary.Name.Replace('/','-'))
        New-Item -ItemType Directory -Path $taskLicenseDir -Force | Out-Null
        Copy-Item -LiteralPath $taskLicense.FullName -Destination (Join-Path $taskLicenseDir $taskLicense.Name) -Force
    }
}
# SDK runtime packs are downloadDependencies rather than ordinary lock entries.
$taskFramework = ($taskAssets.project.frameworks.PSObject.Properties | Select-Object -First 1).Value
foreach ($taskDownload in $taskFramework.downloadDependencies | Where-Object { $_.name -match '^Microsoft\.(NETCore|WindowsDesktop)\.App\.Runtime\.win-x64$' }) {
    $taskVersion = $taskDownload.version.Trim('[',']').Split(',')[0].Trim()
    $taskRuntimePackage = Join-Path $taskPackageRoot ($taskDownload.name.ToLowerInvariant()+'/'+$taskVersion)
    $taskLicenseDir = Join-Path $taskNotices ($taskDownload.name+'-'+$taskVersion)
    New-Item -ItemType Directory -Path $taskLicenseDir -Force | Out-Null
    foreach ($taskLicense in Get-ChildItem -LiteralPath $taskRuntimePackage -File | Where-Object { $_.Name -match '^(LICENSE|THIRD.?PARTY)' -or $_.Extension -eq '.nuspec' -or $_.Name -like '*.nupkg.sha512' }) {
        Copy-Item -LiteralPath $taskLicense.FullName -Destination $taskLicenseDir
    }
    $taskLicenseInventory += [ordered]@{ package=($taskDownload.name+'/'+$taskVersion); license='MIT; see runtime third-party notices'; licenseUrl='https://github.com/dotnet/runtime/blob/main/LICENSE.TXT'; projectUrl='https://dotnet.microsoft.com/' }
}
$taskLicenseInventory | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $taskNotices 'nuget-inventory.json') -Encoding utf8
& (Join-Path $taskRuntime 'python.exe') -B (Join-Path $taskGateway 'run.py') --self-test | Set-Content (Join-Path $taskOutput 'gateway-self-test.json') -Encoding utf8
if ($LASTEXITCODE) { throw 'Isolated packaged gateway import check failed' }
& (Join-Path $PSScriptRoot 'Test-Package.ps1') -Directory $taskOutput
$taskInventory = Get-ChildItem -LiteralPath $taskOutput -Recurse -File | ForEach-Object {
    [ordered]@{ path=[IO.Path]::GetRelativePath($taskOutput,$_.FullName); bytes=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$taskInventory | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $taskOutput 'inventory.json') -Encoding utf8
Compress-Archive -Path (Join-Path $taskOutput '*') -DestinationPath ($taskOutput+'.zip')
Write-Output "Candidate package: $taskOutput.zip (hardware/distribution acceptance unverified)"
