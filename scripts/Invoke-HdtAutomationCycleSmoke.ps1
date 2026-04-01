<#
.SYNOPSIS
Runs the positive-path smoke surface for the HDT local automation lane.

.DESCRIPTION
This smoke script exercises the public parent automation cycle with bounded
stubbed repo-check helpers so the parent receipt/state conveyor can be
validated without recursively invoking the full repo-check primitive.

It verifies both:

- `candidate-ready` behavior through an `Initial` posture cycle
- `hitl-required` behavior through an `Approved` posture cycle
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-automation-smoke-" + [guid]::NewGuid().ToString("N"))

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

try {
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    $repoChecksHelper = Join-Path $tempDir "repo-checks-success.ps1"
    Write-RepoChecksHelper -Path $repoChecksHelper -ExitCode 0 -Message "Repo checks succeeded."

    $initialAuditRoot = Join-Path $tempDir "initial-audit"
    Invoke-HdtStep -Label "Run automation cycle (Initial)" -Action {
        & (Join-Path $repoRoot "scripts\Invoke-HdtAutomationCycle.ps1") `
            -DevelopmentPosture Initial `
            -ForceDigest `
            -AuditRoot $initialAuditRoot `
            -RepoChecksScriptPath $repoChecksHelper
    }

    Invoke-HdtStep -Label "Validate initial automation cycle receipts" -Action {
        $cycleState = Read-Json -Path (Join-Path $initialAuditRoot "state\local-automation-cycle.json")
        if ($cycleState.status -ne "candidate-ready") {
            throw "Expected candidate-ready status for initial automation cycle, got '$($cycleState.status)'."
        }

        if ($cycleState.stewardStage -ne "S1 WitnessSteward") {
            throw "Expected S1 WitnessSteward stage for initial automation cycle."
        }

        if ($cycleState.digestDisposition -ne "emitted") {
            throw "Expected emitted digest disposition for initial automation cycle."
        }

        if ($cycleState.workReportDisposition -ne "emitted") {
            throw "Expected emitted work report disposition for initial automation cycle."
        }

        $releaseBundle = Get-ChildItem -Path (Join-Path $initialAuditRoot "runs\release-candidates") -Directory | Select-Object -First 1
        $digestBundle = Get-ChildItem -Path (Join-Path $initialAuditRoot "runs\release-digests") -Directory | Select-Object -First 1
        $workReportBundle = Get-ChildItem -Path (Join-Path $initialAuditRoot "runs\work-reports") -Directory | Select-Object -First 1

        if ($null -eq $releaseBundle -or $null -eq $digestBundle -or $null -eq $workReportBundle) {
            throw "Initial automation cycle did not emit release, digest, and work-report bundles."
        }

        $manifest = Read-Json -Path (Join-Path $releaseBundle.FullName "build-evidence-manifest.json")
        $digest = Read-Json -Path (Join-Path $digestBundle.FullName "release-candidate-digest.json")
        $noticeRaw = Get-Content -Raw -Path (Join-Path $releaseBundle.FullName "notice.json")
        $workReportRaw = Get-Content -Raw -Path (Join-Path $workReportBundle.FullName "work-report.json")

        if ($manifest.status -ne "candidate-ready") {
            throw "Initial automation manifest did not stay candidate-ready."
        }

        if ($digest.status -ne "candidate-ready") {
            throw "Initial automation digest did not stay candidate-ready."
        }

        if ($noticeRaw -notmatch '"activeHolds"\s*:\s*\[') {
            throw "Initial automation notice did not preserve activeHolds as an array."
        }

        if ($workReportRaw -notmatch '"activeHolds"\s*:\s*\[') {
            throw "Initial automation work report did not preserve activeHolds as an array."
        }
    }

    $approvedAuditRoot = Join-Path $tempDir "approved-audit"
    Invoke-HdtStep -Label "Run automation cycle (Approved)" -Action {
        & (Join-Path $repoRoot "scripts\Invoke-HdtAutomationCycle.ps1") `
            -DevelopmentPosture Approved `
            -ForceDigest `
            -AuditRoot $approvedAuditRoot `
            -RepoChecksScriptPath $repoChecksHelper
    }

    Invoke-HdtStep -Label "Validate approved automation cycle receipts" -Action {
        $cycleState = Read-Json -Path (Join-Path $approvedAuditRoot "state\local-automation-cycle.json")
        if ($cycleState.status -ne "hitl-required") {
            throw "Expected hitl-required status for approved automation cycle, got '$($cycleState.status)'."
        }

        if ($cycleState.stewardStage -ne "S1 WitnessSteward") {
            throw "Expected S1 WitnessSteward stage for approved automation cycle."
        }

        if ($cycleState.recommendedAction -ne "review-required-before-adoption") {
            throw "Approved automation cycle did not request HITL review."
        }

        $digestBundle = Get-ChildItem -Path (Join-Path $approvedAuditRoot "runs\release-digests") -Directory | Select-Object -First 1
        if ($null -eq $digestBundle) {
            throw "Approved automation cycle did not emit a digest bundle."
        }

        $digest = Read-Json -Path (Join-Path $digestBundle.FullName "release-candidate-digest.json")
        if (-not $digest.hitlStillRequired) {
            throw "Approved automation digest did not preserve HITL-required posture."
        }

        if ($digest.note -notlike "*explicit HITL approval is still required*") {
            throw "Approved automation digest did not preserve the HITL note."
        }
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
