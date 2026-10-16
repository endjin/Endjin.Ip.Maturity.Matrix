#Requires -Version 7
<#
.SYNOPSIS
    Checks that the assemblies in a publish folder were compiled with ReadyToRun.

.DESCRIPTION
    A ReadyToRun assembly has a managed native header in its CLI header, which holds the
    precompiled code. This script reads each .dll in the publish folder with PEReader and lists
    every managed assembly, its size, and whether it is ReadyToRun. It exits with code 1 if any
    assembly that matches -RequiredPattern (by default, this repository's own Endjin.* assemblies)
    is not ReadyToRun, or if none match.

.PARAMETER PublishDir
    The publish folder, for example _packages/Endjin.Ip.Maturity.Model.Host/linux-x64.

.PARAMETER RequiredPattern
    A wildcard pattern for the file names of the assemblies that must be ReadyToRun.

.EXAMPLE
    ./tools/Test-ReadyToRun.ps1 -PublishDir _packages/Endjin.Ip.Maturity.Model.Host/linux-x64
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PublishDir,

    [string] $RequiredPattern = "Endjin.*.dll"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PublishDir -PathType Container)) {
    Write-Error "Publish folder '$PublishDir' does not exist."
    exit 1
}

$PublishDir = (Resolve-Path -LiteralPath $PublishDir).Path

$results = foreach ($file in Get-ChildItem -LiteralPath $PublishDir -Filter *.dll -File) {
    $stream = [System.IO.File]::OpenRead($file.FullName)
    try {
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            $corHeader = $reader.PEHeaders.CorHeader
            if ($null -eq $corHeader) {
                # A native library, not a .NET assembly.
                continue
            }

            [pscustomobject]@{
                Assembly    = $file.Name
                SizeKB      = [math]::Round($file.Length / 1KB, 1)
                ReadyToRun  = $corHeader.ManagedNativeHeaderDirectory.Size -gt 0
                Required    = $file.Name -like $RequiredPattern
            }
        }
        finally {
            $reader.Dispose()
        }
    }
    catch [System.BadImageFormatException] {
        # Not a PE file.
    }
    finally {
        $stream.Dispose()
    }
}

$results | Sort-Object -Property @{ Expression = "Required"; Descending = $true }, Assembly |
    Format-Table -AutoSize | Out-String -Width 200 | Write-Host

$allFiles = Get-ChildItem -LiteralPath $PublishDir -Recurse -File -Force
$totalMB = [math]::Round(($allFiles | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
$readyToRunCount = @($results | Where-Object ReadyToRun).Count
Write-Host "Publish folder: $PublishDir"
Write-Host "Total size: $totalMB MB in $($allFiles.Count) files; $readyToRunCount of $(@($results).Count) managed assemblies are ReadyToRun."

$required = @($results | Where-Object Required)
if ($required.Count -eq 0) {
    Write-Error "No assemblies matching '$RequiredPattern' were found in '$PublishDir'." -ErrorAction Continue
    exit 1
}

$missing = @($required | Where-Object { -not $_.ReadyToRun })
if ($missing.Count -gt 0) {
    Write-Error "These assemblies are not ReadyToRun: $($missing.Assembly -join ', ')" -ErrorAction Continue
    exit 1
}

Write-Host "All $($required.Count) assemblies matching '$RequiredPattern' are ReadyToRun."
exit 0
