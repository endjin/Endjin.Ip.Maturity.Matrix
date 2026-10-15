#Requires -Version 7
<#
.SYNOPSIS
    Measures how long the Functions Host takes to serve its first badges after a cold start,
    running locally under the Azure Functions Core Tools.

.DESCRIPTION
    Publishes the Host for win-x64 (with or without ReadyToRun) to a working folder, then, for
    each iteration: starts 'func host start' in the publish folder, polls the total badge endpoint
    every 50 ms until it returns 200, requests one rule badge, and stops func and its worker.

    For each iteration it records:
      TimeToFirstBadgeMs  from starting func to the first 200 from the total badge endpoint
      FirstRequestMs      how long that successful request itself took
      RuleBadgeMs         how long the rule badge request that follows it took
      Warmup              the "Warm-up:" log line the Host wrote, if any

    It prints min and median for each measure, and returns the per-iteration results.

    The results are only indicative of the effect of ReadyToRun and the warm-up. Windows, the
    Core Tools host and this machine are not the Flex Consumption Linux host, and every request
    makes real round trips to GitHub, so network variance shows up in the numbers.

.PARAMETER ReadyToRun
    Publish with ReadyToRun (-p:PublishReadyToRun=true).

.PARAMETER DisableWarmup
    Start the Host with IMM_DISABLE_WARMUP=1, so that the StartupWarmup service is not registered.

.PARAMETER Iterations
    The number of cold starts to measure.

.PARAMETER Port
    The port for func to listen on. It must be free.

.PARAMETER Func
    The path to func.exe.

.PARAMETER WorkDir
    The folder to publish to and write func's logs to. Defaults to a folder under the temp folder.

.PARAMETER LocalSettings
    The local.settings.json to copy into the publish folder. Defaults to the Host's
    local.settings.json if it exists, otherwise local.settings.json.template.

.PARAMETER Org
    The GitHub organisation of the repository whose badges are requested.

.PARAMETER Project
    The GitHub repository whose badges are requested. It must have an imm.yaml.

.PARAMETER ProjectBranch
    The branch of the repository to read imm.yaml from.

.PARAMETER RuleId
    The id of the rule whose badge is requested.

.PARAMETER SkipPublish
    Reuse the existing publish folder instead of publishing again.

.PARAMETER OutFile
    If set, the per-iteration results are appended to this CSV file.

.EXAMPLE
    ./tools/Measure-ColdStart.ps1 -Iterations 10 -DisableWarmup
    ./tools/Measure-ColdStart.ps1 -Iterations 10 -ReadyToRun -DisableWarmup
    ./tools/Measure-ColdStart.ps1 -Iterations 10 -ReadyToRun
#>
[CmdletBinding()]
param(
    [switch] $ReadyToRun,
    [switch] $DisableWarmup,
    [ValidateRange(1, 1000)]
    [int] $Iterations = 5,
    [int] $Port = 7071,
    [string] $Func = "C:\Program Files\Microsoft\Azure Functions Core Tools\func.exe",
    [string] $WorkDir = (Join-Path ([IO.Path]::GetTempPath()) "imm-coldstart"),
    [string] $LocalSettings,
    [string] $Org = "endjin",
    [string] $Project = "dotnet-adr",
    [string] $ProjectBranch = "main",
    [string] $RuleId = "f026d5a2-ce1a-4e04-af15-5a35792b164b",
    [switch] $SkipPublish,
    [string] $OutFile
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$hostProjectDir = Join-Path $repoRoot "Solutions/Endjin.Ip.Maturity.Model.Host"
$configuration = "$($ReadyToRun ? 'R2R' : 'JIT')/$($DisableWarmup ? 'no-warmup' : 'warmup')"

# func is often not on the PATH of a shell that was started before the Core Tools were installed.
$env:PATH = [Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [Environment]::GetEnvironmentVariable("PATH", "User")
if (-not (Test-Path -LiteralPath $Func)) {
    $command = Get-Command func -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "Azure Functions Core Tools not found at '$Func' or on the PATH."
    }
    $Func = $command.Source
}

Write-Host "Configuration: $configuration; $Iterations iteration(s); func $(& $Func --version) at $Func"

function Test-PortListening([int] $port) {
    return $null -ne (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
}

function Wait-PortFree([int] $port) {
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    while (Test-PortListening $port) {
        if ([DateTime]::UtcNow -gt $deadline) {
            throw "Port $port is still in use."
        }
        Start-Sleep -Milliseconds 100
    }
}

function Get-Median([double[]] $values) {
    $sorted = $values | Sort-Object
    $count = $sorted.Count
    if ($count % 2 -eq 1) {
        return $sorted[($count - 1) / 2]
    }
    return ($sorted[$count / 2 - 1] + $sorted[$count / 2]) / 2
}

if (Test-PortListening $Port) {
    throw "Port $Port is already in use. Stop whatever is listening on it, or choose another port with -Port."
}

# Publish
$publishDir = Join-Path $WorkDir ($ReadyToRun ? "r2r" : "jit")
if (-not $SkipPublish) {
    if (Test-Path -LiteralPath $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }

    Write-Host "Publishing to $publishDir"
    $publishOutput = & dotnet publish $hostProjectDir -c Release -r win-x64 --no-self-contained "-p:PublishReadyToRun=$($ReadyToRun.IsPresent.ToString().ToLowerInvariant())" -o $publishDir --nologo 2>&1
    if ($LASTEXITCODE -ne 0) {
        $publishOutput | Write-Host
        throw "dotnet publish failed."
    }
}

$readyToRunAssemblies = foreach ($name in "Endjin.Badger.dll", "Endjin.Imm.dll", "Endjin.Ip.Maturity.Model.Host.dll") {
    $reader = [System.Reflection.PortableExecutable.PEReader]::new([IO.File]::OpenRead((Join-Path $publishDir $name)))
    try {
        $reader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size -gt 0
    }
    finally {
        $reader.Dispose()
    }
}
$expected = $ReadyToRun.IsPresent
if (@($readyToRunAssemblies | Where-Object { $_ -ne $expected }).Count -gt 0) {
    throw "The publish folder's ReadyToRun state does not match -ReadyToRun:$($ReadyToRun.IsPresent)."
}
$publishMB = [math]::Round((Get-ChildItem -LiteralPath $publishDir -Recurse -File -Force | Measure-Object Length -Sum).Sum / 1MB, 2)
Write-Host "Publish folder: $publishMB MB; Endjin assemblies ReadyToRun: $($ReadyToRun.IsPresent)"

if (-not $LocalSettings) {
    $LocalSettings = Join-Path $hostProjectDir "local.settings.json"
    if (-not (Test-Path -LiteralPath $LocalSettings)) {
        $LocalSettings = Join-Path $hostProjectDir "local.settings.json.template"
    }
}
Copy-Item -LiteralPath $LocalSettings -Destination (Join-Path $publishDir "local.settings.json") -Force

$baseUrl = "http://localhost:$Port/api/imm/github/$Org/$Project"
$totalUrl = "$baseUrl/total?projectBranch=$ProjectBranch"
$ruleUrl = "$baseUrl/rule/$RuleId`?projectBranch=$ProjectBranch"

$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(30)

$previousDisableWarmup = $env:IMM_DISABLE_WARMUP
$results = [System.Collections.Generic.List[object]]::new()
try {
    if ($DisableWarmup) {
        $env:IMM_DISABLE_WARMUP = "1"
    }
    else {
        Remove-Item Env:IMM_DISABLE_WARMUP -ErrorAction SilentlyContinue
    }

    for ($i = 1; $i -le $Iterations; $i++) {
        $logFile = Join-Path $WorkDir "func-$($configuration.Replace('/', '-'))-$i.log"
        $errFile = "$logFile.err"
        $process = $null
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $process = Start-Process -FilePath $Func -ArgumentList "host", "start", "--port", $Port, "--no-build" `
                -WorkingDirectory $publishDir -NoNewWindow -PassThru `
                -RedirectStandardOutput $logFile -RedirectStandardError $errFile

            $firstRequestMs = $null
            $deadline = [TimeSpan]::FromSeconds(120)
            while ($null -eq $firstRequestMs) {
                if ($stopwatch.Elapsed -gt $deadline) {
                    throw "No 200 from $totalUrl within $($deadline.TotalSeconds) s. See $logFile."
                }
                if ($process.HasExited) {
                    throw "func exited with code $($process.ExitCode). See $logFile."
                }

                $requestTimer = [System.Diagnostics.Stopwatch]::StartNew()
                try {
                    $cts = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(2))
                    $response = $client.GetAsync($totalUrl, $cts.Token).GetAwaiter().GetResult()
                    if ($response.StatusCode -eq [System.Net.HttpStatusCode]::OK) {
                        $null = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                        $firstRequestMs = $requestTimer.Elapsed.TotalMilliseconds
                    }
                    $response.Dispose()
                }
                catch {
                    # Not listening yet, or timed out: poll again.
                }

                if ($null -eq $firstRequestMs) {
                    Start-Sleep -Milliseconds 50
                }
            }
            $timeToFirstBadgeMs = $stopwatch.Elapsed.TotalMilliseconds

            $requestTimer = [System.Diagnostics.Stopwatch]::StartNew()
            $response = $client.GetAsync($ruleUrl).GetAwaiter().GetResult()
            $null = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $ruleBadgeMs = $requestTimer.Elapsed.TotalMilliseconds
            if ($response.StatusCode -ne [System.Net.HttpStatusCode]::OK) {
                throw "Rule badge request returned $([int]$response.StatusCode). See $logFile."
            }
            $response.Dispose()

            # Give the warm-up a moment to log, if it has not finished yet.
            $warmupLine = $null
            $logDeadline = [DateTime]::UtcNow.AddSeconds($DisableWarmup ? 0 : 10)
            do {
                $warmupLine = Select-String -LiteralPath $logFile -Pattern "Warm-up:.*" -ErrorAction SilentlyContinue |
                    Select-Object -First 1 | ForEach-Object { $_.Matches[0].Value.Trim() }
                if ($warmupLine -or [DateTime]::UtcNow -gt $logDeadline) {
                    break
                }
                Start-Sleep -Milliseconds 100
            } while ($true)

            $result = [pscustomobject]@{
                Configuration      = $configuration
                Iteration          = $i
                TimeToFirstBadgeMs = [math]::Round($timeToFirstBadgeMs)
                FirstRequestMs     = [math]::Round($firstRequestMs)
                RuleBadgeMs        = [math]::Round($ruleBadgeMs)
                Warmup             = $warmupLine
            }
            $results.Add($result)
            Write-Host ("  #{0}: first badge after {1} ms (request {2} ms), rule badge {3} ms{4}" -f `
                $i, $result.TimeToFirstBadgeMs, $result.FirstRequestMs, $result.RuleBadgeMs, ($warmupLine ? "; $warmupLine" : ""))
        }
        finally {
            if ($process -and -not $process.HasExited) {
                # func starts the .NET worker as a child process, so stop the whole tree.
                $process.Kill($true)
                $process.WaitForExit(30000) | Out-Null
            }
            Wait-PortFree $Port
        }
    }
}
finally {
    $client.Dispose()
    if ($null -eq $previousDisableWarmup) {
        Remove-Item Env:IMM_DISABLE_WARMUP -ErrorAction SilentlyContinue
    }
    else {
        $env:IMM_DISABLE_WARMUP = $previousDisableWarmup
    }
}

if ($OutFile) {
    $results | Export-Csv -LiteralPath $OutFile -Append -NoTypeInformation
}

Write-Host ""
Write-Host "Summary for $configuration over $($results.Count) cold start(s) (ms):"
foreach ($measure in "TimeToFirstBadgeMs", "FirstRequestMs", "RuleBadgeMs") {
    $values = [double[]]($results | ForEach-Object $measure)
    Write-Host ("  {0,-20} min {1,6}  median {2,8}  max {3,6}" -f $measure, ($values | Measure-Object -Minimum).Minimum, (Get-Median $values), ($values | Measure-Object -Maximum).Maximum)
}
Write-Host ""
Write-Host "Caveats: measured on Windows under the Core Tools, not on the Flex Consumption Linux host; each request makes real round trips to GitHub."

$results
