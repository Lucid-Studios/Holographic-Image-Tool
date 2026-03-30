<#
.SYNOPSIS
Runs the repo-local Phase 3 comparison failure smoke path.

.DESCRIPTION
This smoke script creates a valid Phase 3 reference sample and a lawful
incompatible-basis sample through the public PowerShell wrappers, validates
both artifacts, and proves that cross-artifact comparison fails cleanly with
an `Incompatible` result.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-phase3-compare-fail-" + [guid]::NewGuid().ToString("N"))

function Invoke-HdtStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Action,

        [int[]]$AllowedExitCodes = @(0)
    )

    Write-Host "==> $Label"
    & $Action
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

    Invoke-HdtStep -Label "Create primary Phase 3 comparison sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase3Sample.ps1") --output-dir $tempDir --name phase3-compare-ok --json
    }

    Invoke-HdtStep -Label "Create incompatible-basis Phase 3 sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase3IncompatibleBasisSample.ps1") --output-dir $tempDir --name phase3-compare-incompatible --json
    }

    $leftManifestPath = Join-Path $tempDir "phase3-compare-ok.hopng.json"
    $rightManifestPath = Join-Path $tempDir "phase3-compare-incompatible.hopng.json"

    Invoke-HdtStep -Label "Validate primary Phase 3 comparison sample" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $leftManifestPath --json
    }

    Invoke-HdtStep -Label "Validate incompatible-basis Phase 3 sample" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $rightManifestPath --json
    }

    Invoke-HdtStep -Label "Compare incompatible Phase 3 pair" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $leftManifestPath --right $rightManifestPath --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "Incompatible") {
            throw "Expected incompatible comparison classification, got '$($comparisonJson.classification)'."
        }
    } -AllowedExitCodes @(24)

    Invoke-HdtStep -Label "Compare incompatible Phase 3 pair (text)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $leftManifestPath --right $rightManifestPath --view prime
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Temporal comparison classification: Incompatible",
            "Basis alignment: Incompatible",
            "State rank delta: 0",
            "Classification reason: ",
            "Primary horizons: left ",
            "Final states: "
        ) -Context "Incompatible comparison text output"
    } -AllowedExitCodes @(24)
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
