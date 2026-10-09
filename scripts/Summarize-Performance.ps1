param(
    [Parameter(Mandatory=$true)][string]$EvidenceFile,
    [Parameter(Mandatory=$true)][string]$OutputFile,
    [string]$Scenario = 'Existing evidence; workload not controlled'
)
$ErrorActionPreference = 'Stop'
$taskRows = @(Get-Content -LiteralPath $EvidenceFile | ForEach-Object { try { $_ | ConvertFrom-Json } catch { } })
$taskMedia = @($taskRows | Where-Object { $_.kind -eq 'viewport.media' })
if ($taskMedia.Count -lt 2) { throw 'At least two media samples are required.' }
$taskStart = [DateTimeOffset]::Parse($taskMedia[0].utc)
$taskEnd = [DateTimeOffset]::Parse($taskMedia[-1].utc)
$taskCpuRows = @($taskRows | Where-Object { $_.kind -eq 'processes' -and [DateTimeOffset]::Parse($_.utc) -ge $taskStart -and [DateTimeOffset]::Parse($_.utc) -le $taskEnd })
$taskCpu = @($taskCpuRows | ForEach-Object { $_.data } | Group-Object name | ForEach-Object {
    # Sum same-name processes within each sample before averaging (WebView2 has several).
    $taskName = $_.Name
    $taskTotals = @($taskCpuRows | ForEach-Object {
        $taskGroup = @($_.data | Where-Object name -eq $taskName)
        [pscustomobject]@{ Cpu = ($taskGroup | Measure-Object cpuPercentOfMachine -Sum).Sum; Memory = ($taskGroup | Measure-Object WorkingSet64 -Sum).Sum }
    })
    [pscustomobject]@{ process = $taskName; meanCpuPercentOfMachine = [Math]::Round(($taskTotals | Measure-Object Cpu -Average).Average, 2);
        peakCpuPercentOfMachine = [Math]::Round(($taskTotals | Measure-Object Cpu -Maximum).Maximum, 2);
        peakWorkingSetMiB = [Math]::Round(($taskTotals | Measure-Object Memory -Maximum).Maximum / 1MB, 1) }
})
$taskFirst = $taskMedia[0].data.tracks | Where-Object kind -eq 'video' | Select-Object -First 1
$taskLast = $taskMedia[-1].data.tracks | Where-Object kind -eq 'video' | Select-Object -First 1
$taskSeconds = ($taskEnd - $taskStart).TotalSeconds
$taskReport = [ordered]@{
    scenario = $Scenario; source = [IO.Path]::GetFileName([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($EvidenceFile)))
    seconds = [Math]::Round($taskSeconds, 2); transport = $taskMedia[-1].data.displayTransport
    geometry = $taskMedia[-1].data.geometry
    receivedDecodedFramesPerSecond = [Math]::Round(($taskLast.framesDecoded - $taskFirst.framesDecoded) / $taskSeconds, 2)
    receivedMiBPerSecond = [Math]::Round(($taskLast.bytesReceived - $taskFirst.bytesReceived) / $taskSeconds / 1MB, 2)
    presentationIntervalMs = $taskMedia[-1].data.presentationIntervalMs
    processes = $taskCpu | Sort-Object meanCpuPercentOfMachine -Descending
    limitations = @('Historic evidence, not a controlled benchmark.', 'Frame delivery is not verified presentation or input latency.', 'Working sets include shared pages and are not unique physical RAM.', 'GPU backend and GPU engine use require separate verification.', 'CPU values are normalized to total machine capacity by the evidence sampler.')
}
$taskReport | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputFile
$taskReport | ConvertTo-Json -Depth 8
