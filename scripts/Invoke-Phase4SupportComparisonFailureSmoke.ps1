<#
.SYNOPSIS
Runs the repo-local Phase 4 support-comparison negative-path smoke.

.DESCRIPTION
This smoke script verifies that the public engram-support comparison surface
rejects incompatible support types and counterfeit or unsupported support
artifacts deterministically.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-phase4-compare-failure-" + [guid]::NewGuid().ToString("N"))

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

    Invoke-HdtStep -Label "Create lawful Phase 4 perspectival support sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4PerspectivalSample.ps1") --output-dir $tempDir --name phase4-compare-lawful --json
    }

    Invoke-HdtStep -Label "Create invalid Phase 4 perspectival support sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4InvalidPerspectivalSample.ps1") --output-dir $tempDir --name phase4-compare-invalid --json
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 participatory support sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4ParticipatorySample.ps1") --output-dir $tempDir --name phase4-compare-participatory --json
    }

    $lawfulPerspectival = Join-Path $tempDir "phase4-compare-lawful.hopng.json"
    $invalidPerspectival = Join-Path $tempDir "phase4-compare-invalid.hopng.json"
    $lawfulParticipatory = Join-Path $tempDir "phase4-compare-participatory.hopng.json"

    Invoke-HdtStep -Label "Compare lawful vs invalid Phase 4 support pair" -AllowedExitCodes @(25) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $lawfulPerspectival --right $invalidPerspectival --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "CounterfeitOrUnsupported") {
            throw "Expected counterfeit or unsupported comparison classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare lawful vs invalid Phase 4 support pair (text)" -AllowedExitCodes @(25) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $lawfulPerspectival --right $invalidPerspectival
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Engram support comparison classification: CounterfeitOrUnsupported",
            "Counterfeit pressure: detected",
            "Classification reason: At least one artifact fails Phase 4 support validation"
        ) -Context "Counterfeit comparison text output"
    }

    Invoke-HdtStep -Label "Compare incompatible lawful Phase 4 support types" -AllowedExitCodes @(24) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $lawfulPerspectival --right $lawfulParticipatory --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "IncompatibleSupportType") {
            throw "Expected incompatible support type classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare incompatible lawful Phase 4 support types (text)" -AllowedExitCodes @(24) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $lawfulPerspectival --right $lawfulParticipatory
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Engram support comparison classification: IncompatibleSupportType",
            "Support type compatibility: Incompatible",
            "Classification reason: Support comparison requires artifacts of the same Phase 4 support type."
        ) -Context "Incompatible support comparison text output"
    }

    Invoke-HdtStep -Label "Compare committed lawful vs committed invalid Phase 4 support corpus" -AllowedExitCodes @(25) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") `
            --left (Join-Path $repoRoot "examples\\phase4-perspectival-sample.hopng.json") `
            --right (Join-Path $repoRoot "examples\\phase4-invalid-perspectival.hopng.json") `
            --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "CounterfeitOrUnsupported") {
            throw "Committed counterfeit comparison did not stay counterfeit or unsupported."
        }
    }

    Invoke-HdtStep -Label "Compare Phase 3 vs Phase 4 support surfaces" -AllowedExitCodes @(25) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") `
            --left (Join-Path $repoRoot "examples\\phase3-sample.hopng.json") `
            --right (Join-Path $repoRoot "examples\\phase4-perspectival-sample.hopng.json") `
            --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "FlattenedOrUnsupported") {
            throw "Expected flattened or unsupported comparison classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare Phase 3 vs Phase 4 support surfaces (text)" -AllowedExitCodes @(25) -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") `
            --left (Join-Path $repoRoot "examples\\phase3-sample.hopng.json") `
            --right (Join-Path $repoRoot "examples\\phase4-perspectival-sample.hopng.json")
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Engram support comparison classification: FlattenedOrUnsupported",
            "Support type compatibility: Unavailable",
            "Classification reason: At least one artifact does not expose Phase 4 engram-support sidecars"
        ) -Context "Flattened-or-unsupported comparison text output"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
