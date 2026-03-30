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
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-phase3-failure-" + [guid]::NewGuid().ToString("N"))

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

function Assert-OutputContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Output,

        [Parameter(Mandatory = $true)]
        [string[]]$ExpectedFragments,

        [Parameter(Mandatory = $true)]
        [string]$Context
    )

    foreach ($fragment in $ExpectedFragments) {
        if ($Output -notlike "*$fragment*") {
            throw "$Context did not contain expected fragment '$fragment'."
        }
    }
}

try {
    New-Item -ItemType Directory -Path $tempDir | Out-Null

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

    Invoke-HdtStep -Label "Render malformed Phase 3 reference artifact (text)" -AllowedExitCodes @(24) -Action {
        $renderText = & (Join-Path $repoRoot "Render-HOPNGPhaseStack.ps1") --path $manifestPath --view prime
        $outputText = ($renderText | Out-String).Trim()
        $outputText
        Assert-OutputContains -Output $outputText -ExpectedFragments @(
            "Temporal stack status: StructurallyIncomplete",
            "Issues: ",
            "Validation issues: ",
            "Final state: "
        ) -Context "Malformed render text output"
    }

    Invoke-HdtStep -Label "Create valid Phase 3 comparison sample for malformed comparison" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase3Sample.ps1") --output-dir $tempDir --name phase3-compare-valid --json
    }

    $validManifestPath = Join-Path $tempDir "phase3-compare-valid.hopng.json"

    Invoke-HdtStep -Label "Validate valid Phase 3 comparison sample" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $validManifestPath --json
    }

    Invoke-HdtStep -Label "Compare valid and malformed Phase 3 pair" -AllowedExitCodes @(25) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $validManifestPath --right $manifestPath --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "FlattenedOrUnsupported") {
            throw "Expected flattened or unsupported comparison classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare valid and malformed Phase 3 pair (text)" -AllowedExitCodes @(25) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $validManifestPath --right $manifestPath --view prime
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Temporal comparison classification: FlattenedOrUnsupported",
            "Basis alignment: not-comparable",
            "State compatibility: Unavailable",
            "State rank delta: ",
            "Classification reason: ",
            "Right validation issues: "
        ) -Context "Malformed comparison text output"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
