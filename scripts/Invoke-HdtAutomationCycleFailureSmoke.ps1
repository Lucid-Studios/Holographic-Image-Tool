<#
.SYNOPSIS
Runs the negative-path smoke surface for the HDT local automation lane.

.DESCRIPTION
This smoke script verifies that the parent automation cycle classifies blocked
states deterministically when repo checks fail or when the requested audit root
cannot be used directly.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-automation-failure-smoke-" + [guid]::NewGuid().ToString("N"))

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

    return (Get-Content -Path $Path -Raw | ConvertFrom-Json)
}

function Get-FallbackRootFromOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Output
    )

    $pattern = 'Audit root fallback:\s*(?<path>.+)$'
    $match = [regex]::Match($Output, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $match.Success) {
        throw "Automation output did not expose an audit root fallback path."
    }

    return $match.Groups["path"].Value.Trim()
}

try {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    $invalidAuditRootOutput = $null

    $failingRepoChecksHelper = Join-Path $tempDir "repo-checks-failure.ps1"
    Write-RepoChecksHelper -Path $failingRepoChecksHelper -ExitCode 42 -Message "Repo checks failed intentionally."

    $failureAuditRoot = Join-Path $tempDir "failure-audit"
    Invoke-HdtStep -Label "Run blocked automation cycle from failing repo checks" -AllowedExitCodes @(1) -Action {
        & (Join-Path $repoRoot "scripts\Invoke-HdtAutomationCycle.ps1") `
            -DevelopmentPosture Closing `
            -ForceDigest `
            -AuditRoot $failureAuditRoot `
            -RepoChecksScriptPath $failingRepoChecksHelper
    }

    Invoke-HdtStep -Label "Validate blocked receipts from failing repo checks" -Action {
        $cycleState = Read-Json -Path (Join-Path $failureAuditRoot "state\local-automation-cycle.json")
        if ($cycleState.status -ne "blocked") {
            throw "Expected blocked status for failing repo checks, got '$($cycleState.status)'."
        }

        if ($cycleState.repoChecksExitCode -ne 42) {
            throw "Expected repo checks exit code 42 in blocked cycle state."
        }

        $releaseBundle = Get-ChildItem -Path (Join-Path $failureAuditRoot "runs\release-candidates") -Directory | Select-Object -First 1
        if ($null -eq $releaseBundle) {
            throw "Blocked automation cycle did not emit a release-candidate bundle."
        }

        $manifest = Read-Json -Path (Join-Path $releaseBundle.FullName "build-evidence-manifest.json")
        if ($manifest.status -ne "blocked") {
            throw "Blocked automation manifest did not stay blocked."
        }
    }

    $invalidAuditRoot = Join-Path $tempDir "invalid-audit-root.txt"
    Set-Content -Path $invalidAuditRoot -Value "not a directory"
    $successRepoChecksHelper = Join-Path $tempDir "repo-checks-success.ps1"
    Write-RepoChecksHelper -Path $successRepoChecksHelper -ExitCode 0 -Message "Repo checks succeeded."

    Invoke-HdtStep -Label "Run blocked automation cycle from invalid audit root" -AllowedExitCodes @(1) -Action {
        $run = & powershell `
            -NoProfile `
            -ExecutionPolicy Bypass `
            -File (Join-Path $repoRoot "scripts\Invoke-HdtAutomationCycle.ps1") `
            -DevelopmentPosture Initial `
            -ForceDigest `
            -AuditRoot $invalidAuditRoot `
            -RepoChecksScriptPath $successRepoChecksHelper 2>&1
        $script:invalidAuditRootOutput = ($run | Out-String).Trim()
        $script:invalidAuditRootOutput
    }

    Invoke-HdtStep -Label "Validate blocked receipts from invalid audit root" -Action {
        if ([string]::IsNullOrWhiteSpace($script:invalidAuditRootOutput)) {
            throw "Invalid audit root run did not preserve parent automation output."
        }

        $fallbackRoot = Get-FallbackRootFromOutput -Output $script:invalidAuditRootOutput
        $cycleState = Read-Json -Path (Join-Path $fallbackRoot "state\local-automation-cycle.json")
        if ($cycleState.status -ne "blocked") {
            throw "Invalid audit root fallback cycle did not stay blocked."
        }

        if (-not $cycleState.usedAuditRootFallback) {
            throw "Invalid audit root fallback cycle did not record fallback usage."
        }

        if ($cycleState.failureReasons.Count -lt 1) {
            throw "Invalid audit root fallback cycle did not preserve failure reasons."
        }
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
