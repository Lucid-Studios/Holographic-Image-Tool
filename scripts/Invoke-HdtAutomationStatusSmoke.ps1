<#
.SYNOPSIS
Runs a wrapper-backed smoke check for the HDT automation status surface.

.DESCRIPTION
Ensures the public status wrapper can read the live automation state and emit
both text and JSON views without requiring operators to open raw `.audit`
files directly.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

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

Invoke-HdtStep -Label "Show automation summary (text)" -Action {
    $textOutput = & (Join-Path $repoRoot "Show-HDTAutomationStatus.ps1") -View all
    $rendered = ($textOutput | Out-String)
    if ($rendered -notlike "*HDT Automation Summary*") {
        throw "Automation status text output did not contain the summary header."
    }

    if ($rendered -notlike "*HDT Automation Orchestration*") {
        throw "Automation status text output did not contain the orchestration header."
    }

    if ($rendered -notlike "*Current observed git worktree state:*") {
        throw "Automation status text output did not expose the live observed git worktree state."
    }
}

Invoke-HdtStep -Label "Show automation summary (json)" -Action {
    $jsonOutput = & (Join-Path $repoRoot "Show-HDTAutomationStatus.ps1") -View all -Json
    $payload = ($jsonOutput | Out-String | ConvertFrom-Json)

    if ($payload.summary.status -ne "candidate-ready" -and $payload.summary.status -ne "hitl-required") {
        throw "Automation status JSON did not return an admitted live cycle posture."
    }

    if (-not $payload.tasking.tasks) {
        throw "Automation status JSON did not include tasking surfaces."
    }

    if ($null -eq $payload.orchestration.publishReady) {
        throw "Automation status JSON did not include orchestration posture."
    }

    if ($null -eq $payload.currentObservation) {
        throw "Automation status JSON did not include the live current-observation overlay."
    }
}

exit 0
