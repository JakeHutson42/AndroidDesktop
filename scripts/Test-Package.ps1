param([Parameter(Mandatory=$true)][string]$Directory)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath($Directory)
foreach ($taskRequired in @('AndroidDesktop.exe','coreclr.dll','hostfxr.dll','Assets/Viewport/index.html','Assets/Viewport/viewport.js','runtime/python.exe','runtime/python312._pth','runtime/LICENSE.txt','gateway/run.py','gateway/videobridge_gateway/proto/rtc_service_v2_pb2.py','runtime-candidate.json','notices/THIRD_PARTY_NOTICES.md','HARDWARE_AND_SETUP.md','RECOVERY.md')) {
    if (!(Test-Path -LiteralPath (Join-Path $taskRoot $taskRequired))) { throw "Package missing $taskRequired" }
}
foreach ($taskFile in Get-ChildItem -LiteralPath $taskRoot -Recurse -File) {
    $taskRelative = [IO.Path]::GetRelativePath($taskRoot,$taskFile.FullName)
    if ($taskRelative -match '(^|[\\/])(node_modules|\.venv|__pycache__|tests|obj|\.git|\.cache)([\\/]|$)' -or $taskFile.Extension -in @('.apk','.aab','.apks','.xapk','.pyc') -or $taskFile.Name -match '(^phase0\.local\.json$|^settings.*\.json$|^.*\.ini$|^.*\.pdb$)') { throw "Development/private file in package: $taskRelative" }
    if ($taskFile.Extension -in @('.json','.txt','.md','.py','.js','.html','._pth')) {
        $taskText = Get-Content -LiteralPath $taskFile.FullName -Raw
        if ($taskText -match '(?i)[a-z]:[\\/]Users[\\/]|ANDROID_DESKTOP_SESSION_TOKEN\s*=\s*["''][a-f0-9]{64}|grpc\.token\s*=\s*\S+') { throw "Personal path/credential suspect in $taskRelative" }
    }
}
Write-Output 'Package structure and configured private-data checks passed; no gameplay certification implied.'
