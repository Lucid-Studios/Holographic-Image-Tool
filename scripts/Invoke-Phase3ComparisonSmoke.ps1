<#
.SYNOPSIS
Runs the repo-local Phase 3 cross-artifact comparison smoke path.

.DESCRIPTION
This smoke script creates a valid Phase 3 sample pair through the public
PowerShell wrapper surface, validates both artifacts, and compares them through
the public temporal comparison command in prime-safe and privileged modes. It
also creates a lawful divergent peer so the comparison corpus exercises both
delayed and divergent classifications under the same admitted basis.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-phase3-compare-" + [guid]::NewGuid().ToString("N"))

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
        & (Join-Path $repoRoot "New-HOPNGPhase3Sample.ps1") --output-dir $tempDir --name phase3-compare-left --json
    }

    Invoke-HdtStep -Label "Create delayed Phase 3 comparison peer" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase3PeerSample.ps1") --output-dir $tempDir --name phase3-compare-right --json
    }

    Invoke-HdtStep -Label "Create divergent Phase 3 comparison peer" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase3DivergentPeerSample.ps1") --output-dir $tempDir --name phase3-compare-divergent --json
    }

    $leftManifestPath = Join-Path $tempDir "phase3-compare-left.hopng.json"
    $rightManifestPath = Join-Path $tempDir "phase3-compare-right.hopng.json"
    $divergentManifestPath = Join-Path $tempDir "phase3-compare-divergent.hopng.json"

    Invoke-HdtStep -Label "Validate primary Phase 3 comparison sample" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $leftManifestPath --json
    }

    Invoke-HdtStep -Label "Validate delayed Phase 3 comparison peer" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $rightManifestPath --json
    }

    Invoke-HdtStep -Label "Validate divergent Phase 3 comparison peer" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $divergentManifestPath --json
    }

    Invoke-HdtStep -Label "Compare Phase 3 pair (prime)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $leftManifestPath --right $rightManifestPath --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "Delayed") {
            throw "Expected delayed comparison classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare Phase 3 pair (privileged)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $leftManifestPath --right $rightManifestPath --view privileged --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "Delayed") {
            throw "Expected delayed privileged comparison classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare Phase 3 pair (prime text)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $leftManifestPath --right $rightManifestPath --view prime
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Temporal comparison classification: Delayed",
            "Basis alignment: Aligned",
            "State rank delta: +1",
            "Classification reason: ",
            "Basis signals: ",
            "Signals: "
        ) -Context "Delayed comparison text output"
    }

    Invoke-HdtStep -Label "Compare divergent Phase 3 pair (prime)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $leftManifestPath --right $divergentManifestPath --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "Divergent") {
            throw "Expected divergent comparison classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare divergent Phase 3 pair (prime text)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGPhaseStacks.ps1") --left $leftManifestPath --right $divergentManifestPath --view prime
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Temporal comparison classification: Divergent",
            "State compatibility: Divergent",
            "State rank delta: +2",
            "Classification reason: ",
            "Final states: ",
            "Similarity score: "
        ) -Context "Divergent comparison text output"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
