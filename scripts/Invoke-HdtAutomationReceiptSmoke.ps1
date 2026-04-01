<#
.SYNOPSIS
Runs a wrapper-backed smoke check for the HDT automation receipt surface.

.DESCRIPTION
Ensures the public receipt wrapper can read the latest emitted release-candidate
bundle and digest from a bounded automation cycle without operators opening raw
`.audit` receipt files directly.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-automation-receipt-smoke-" + [guid]::NewGuid().ToString("N"))

function Invoke-HdtStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Action,

        [int[]]$AllowedExitCodes = @(0)
    )

    Write-Host "==> $Label"
    $global:LASTEXITCODE = 0
    & $Action
    $stepExitCode = $global:LASTEXITCODE
    if ($stepExitCode -notin $AllowedExitCodes) {
        throw "Step '$Label' failed with exit code $stepExitCode."
    }
}

function Write-RepoChecksHelper {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [int]$ExitCode,

        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    @"
param([string]`$DevelopmentPosture)
Write-Output '$Message'
Write-Output "Development posture: `$DevelopmentPosture"
exit $ExitCode
"@ | Set-Content -Path $Path -Encoding UTF8
}

function Read-Json {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json)
}

try {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    $auditRoot = Join-Path $tempDir "audit"
    $repoChecksHelper = Join-Path $tempDir "repo-checks-success.ps1"
    Write-RepoChecksHelper -Path $repoChecksHelper -ExitCode 0 -Message "Repo checks succeeded."

    Invoke-HdtStep -Label "Run automation cycle for receipt smoke" -Action {
        & (Join-Path $repoRoot "scripts\Invoke-HdtAutomationCycle.ps1") `
            -DevelopmentPosture Closing `
            -ForceDigest `
            -AuditRoot $auditRoot `
            -RepoChecksScriptPath $repoChecksHelper
    }

    $cycleState = Read-Json -Path (Join-Path $auditRoot "state\local-automation-cycle.json")

    Invoke-HdtStep -Label "Show automation receipt (text)" -Action {
        $textOutput = & (Join-Path $repoRoot "Show-HDTAutomationReceipt.ps1") `
            -View all `
            -AuditRoot $auditRoot
        $rendered = ($textOutput | Out-String)

        if ($rendered -notlike "*HDT Automation Bundle*") {
            throw "Automation receipt text output did not contain the bundle header."
        }

        if ($rendered -notlike "*HDT Automation Digest*") {
            throw "Automation receipt text output did not contain the digest header."
        }

        if ($rendered -notlike "*$($cycleState.lastBundleId)*") {
            throw "Automation receipt text output did not expose the latest bundle id."
        }
    }

    Invoke-HdtStep -Label "Show automation receipt (json)" -Action {
        $jsonOutput = & (Join-Path $repoRoot "Show-HDTAutomationReceipt.ps1") `
            -View all `
            -Json `
            -AuditRoot $auditRoot
        $payload = ($jsonOutput | Out-String | ConvertFrom-Json)

        if ($payload.bundle.manifest.status -ne "candidate-ready") {
            throw "Automation receipt JSON did not expose the bundle status."
        }

        if ($payload.digest.receipt.status -ne "candidate-ready") {
            throw "Automation receipt JSON did not expose the digest status."
        }
    }

    Invoke-HdtStep -Label "Show automation receipt by explicit bundle id" -Action {
        $jsonOutput = & (Join-Path $repoRoot "Show-HDTAutomationReceipt.ps1") `
            -View bundle `
            -Json `
            -AuditRoot $auditRoot `
            -BundleId $cycleState.lastBundleId
        $payload = ($jsonOutput | Out-String | ConvertFrom-Json)

        if ($payload.bundle.manifest.bundleId -ne $cycleState.lastBundleId) {
            throw "Automation receipt wrapper did not respect the explicit bundle id."
        }
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
