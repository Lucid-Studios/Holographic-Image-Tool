<#
.SYNOPSIS
Runs the repo-local Phase 3 temporal failure-path smoke.

.DESCRIPTION
This smoke script verifies the committed malformed Phase 3 reference artifact
fails for an explicit temporal-contract reason rather than trust corruption or
missing files.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot "examples\\phase3-invalid-derived.hopng.json"

function Invoke-HdtStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Action,

        [int[]]$AllowedExitCodes = @(0)
    )

    Write-Host "==> $Label"
    $script:StepOutput = & $Action
    if ($LASTEXITCODE -notin $AllowedExitCodes) {
        throw "Step '$Label' failed with exit code $LASTEXITCODE."
    }
}

Invoke-HdtStep -Label "Validate malformed Phase 3 reference artifact" -AllowedExitCodes @(31) -Action {
    & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $manifestPath --json
}

$validation = $StepOutput | ConvertFrom-Json
if ($validation.isValid) {
    throw "Malformed Phase 3 reference artifact unexpectedly validated."
}

$invalidPhaseSliceError = $validation.errors | Where-Object { $_.Code -eq 31 }
if (-not $invalidPhaseSliceError) {
    throw "Malformed Phase 3 reference artifact did not report InvalidPhaseSlice."
}

Invoke-HdtStep -Label "Render malformed Phase 3 reference artifact" -AllowedExitCodes @(24) -Action {
    & (Join-Path $repoRoot "Render-HOPNGPhaseStack.ps1") --path $manifestPath --view prime --json
}

$render = $StepOutput | ConvertFrom-Json
if ($render.status -ne 1) {
    throw "Malformed Phase 3 reference artifact did not render as StructurallyIncomplete."
}

$renderInvalidPhaseSlice = $render.validationIssues | Where-Object { $_.Code -eq 31 }
if (-not $renderInvalidPhaseSlice) {
    throw "Malformed Phase 3 render output did not preserve InvalidPhaseSlice validation context."
}

exit 0
