param([string]$Directory = 'docs/performance')
$ErrorActionPreference = 'Stop'
foreach ($taskMode in @('native','standalone','controller')) {
    $taskReport = Get-Content -LiteralPath (Join-Path $Directory "$taskMode-phase-2.json") -Raw | ConvertFrom-Json
    $taskSamples = @($taskReport.samples | Where-Object { $_.processes.Count -gt 0 })
    foreach ($taskName in @($taskSamples.processes.name | Sort-Object -Unique)) {
        $taskTotals = @($taskSamples | ForEach-Object { ($_.processes | Where-Object name -eq $taskName | Measure-Object cpuPercentOfMachine -Sum).Sum })
        [pscustomobject]@{Mode=$taskMode; Process=$taskName; MeanCpuPercent=[Math]::Round(($taskTotals | Measure-Object -Average).Average,2); PeakCpuPercent=[Math]::Round(($taskTotals | Measure-Object -Maximum).Maximum,2)}
    }
}
