param(
    [Parameter(Mandatory=$true)][string]$SdkRoot,
    [Parameter(Mandatory=$true)][string]$JavaHome,
    [string]$BuildToolsVersion = '34.0.0',
    [int]$ApiLevel = 34
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskBuild = Join-Path $taskRoot 'diagnostic-apk/build'
$taskTools = Join-Path $SdkRoot "build-tools/$BuildToolsVersion"
$taskJar = Join-Path $SdkRoot "platforms/android-$ApiLevel/android.jar"
function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE) { throw "$Executable failed with exit code $LASTEXITCODE" }
}
foreach ($taskFile in @($taskJar, (Join-Path $taskTools 'aapt2.exe'), (Join-Path $JavaHome 'bin/javac.exe'))) {
    if (!(Test-Path -LiteralPath $taskFile)) { throw "Missing prerequisite: $taskFile" }
}
New-Item -ItemType Directory -Path $taskBuild,(Join-Path $taskBuild 'classes'),(Join-Path $taskBuild 'dex') -Force | Out-Null
$taskSources = @(Get-ChildItem (Join-Path $taskRoot 'diagnostic-apk/src') -Recurse -Filter '*.java' | ForEach-Object FullName)
Invoke-Checked (Join-Path $JavaHome 'bin/javac.exe') (@('-encoding','UTF-8','-source','8','-target','8','-classpath',$taskJar,'-d',(Join-Path $taskBuild 'classes')) + $taskSources)
Invoke-Checked (Join-Path $JavaHome 'bin/jar.exe') @('cf',(Join-Path $taskBuild 'classes.jar'),'-C',(Join-Path $taskBuild 'classes'),'.')
Invoke-Checked (Join-Path $JavaHome 'bin/java.exe') @('-cp',(Join-Path $taskTools 'lib/d8.jar'),'com.android.tools.r8.D8','--min-api','26','--lib',$taskJar,'--output',(Join-Path $taskBuild 'dex'),(Join-Path $taskBuild 'classes.jar'))
Invoke-Checked (Join-Path $taskTools 'aapt2.exe') @('link','-o',(Join-Path $taskBuild 'unsigned.apk'),'--manifest',(Join-Path $taskRoot 'diagnostic-apk/AndroidManifest.xml'),'-I',$taskJar)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskZip = [IO.Compression.ZipFile]::Open((Join-Path $taskBuild 'unsigned.apk'),[IO.Compression.ZipArchiveMode]::Update)
try { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskZip,(Join-Path $taskBuild 'dex/classes.dex'),'classes.dex') | Out-Null } finally { $taskZip.Dispose() }
Invoke-Checked (Join-Path $taskTools 'zipalign.exe') @('-f','4',(Join-Path $taskBuild 'unsigned.apk'),(Join-Path $taskBuild 'aligned.apk'))
$taskKey = Join-Path $taskBuild 'diagnostic.keystore'
if (!(Test-Path -LiteralPath $taskKey)) {
    Invoke-Checked (Join-Path $JavaHome 'bin/keytool.exe') @('-genkeypair','-keystore',$taskKey,'-storepass','android','-keypass','android','-alias','diagnostic','-dname','CN=Phase0 Diagnostic','-keyalg','RSA','-validity','3650','-noprompt')
}
Invoke-Checked (Join-Path $JavaHome 'bin/java.exe') @('-jar',(Join-Path $taskTools 'lib/apksigner.jar'),'sign','--ks',$taskKey,'--ks-pass','pass:android','--out',(Join-Path $taskBuild 'Phase0Diagnostic.apk'),(Join-Path $taskBuild 'aligned.apk'))
Invoke-Checked (Join-Path $JavaHome 'bin/java.exe') @('-jar',(Join-Path $taskTools 'lib/apksigner.jar'),'verify',(Join-Path $taskBuild 'Phase0Diagnostic.apk'))
Write-Output "Diagnostic APK: $(Join-Path $taskBuild 'Phase0Diagnostic.apk')"
