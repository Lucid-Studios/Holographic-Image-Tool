<#
.SYNOPSIS
Runs the repo-local Phase 4 support-comparison positive-path smoke.

.DESCRIPTION
This smoke script creates lawful Phase 4 peer artifacts through the public
wrapper surface and compares them through the public engram-support comparison
command. It also checks the committed lawful comparison corpus so root and
branch coherence remain artifact-backed rather than scratch-only.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-phase4-compare-" + [guid]::NewGuid().ToString("N"))

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
        & (Join-Path $repoRoot "New-HOPNGPhase4PerspectivalSample.ps1") --output-dir $tempDir --name phase4-perspectival-left --json
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 perspectival support peer" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4PerspectivalPeerSample.ps1") --output-dir $tempDir --name phase4-perspectival-right --json
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 participatory support sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4ParticipatorySample.ps1") --output-dir $tempDir --name phase4-participatory-left --json
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 participatory support peer" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4ParticipatoryPeerSample.ps1") --output-dir $tempDir --name phase4-participatory-right --json
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 restricted perspectival support sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4RestrictedPerspectivalSample.ps1") --output-dir $tempDir --name phase4-perspectival-restricted --json
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 deferred perspectival support sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4DeferredPerspectivalSample.ps1") --output-dir $tempDir --name phase4-perspectival-deferred --json
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 rejected participatory support sample" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4RejectedParticipatorySample.ps1") --output-dir $tempDir --name phase4-participatory-rejected --json
    }

    $perspectivalLeft = Join-Path $tempDir "phase4-perspectival-left.hopng.json"
    $perspectivalRight = Join-Path $tempDir "phase4-perspectival-right.hopng.json"
    $participatoryLeft = Join-Path $tempDir "phase4-participatory-left.hopng.json"
    $participatoryRight = Join-Path $tempDir "phase4-participatory-right.hopng.json"
    $restrictedPerspectival = Join-Path $tempDir "phase4-perspectival-restricted.hopng.json"
    $deferredPerspectival = Join-Path $tempDir "phase4-perspectival-deferred.hopng.json"
    $rejectedParticipatory = Join-Path $tempDir "phase4-participatory-rejected.hopng.json"

    Invoke-HdtStep -Label "Compare lawful perspectival support pair (prime)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $perspectivalLeft --right $perspectivalRight --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "StrengthenedSupport") {
            throw "Expected strengthened support classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare lawful participatory support pair (prime)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $participatoryLeft --right $participatoryRight --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "CoherentSupport") {
            throw "Expected coherent support classification, got '$($comparisonJson.classification)'."
        }
    }

    Invoke-HdtStep -Label "Compare lawful restricted perspectival support pair (prime)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $perspectivalLeft --right $restrictedPerspectival --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "RestrictedSupport") {
            throw "Expected restricted support classification, got '$($comparisonJson.classification)'."
        }

        if ($comparisonJson.workingIntentTransitionStatus -ne "Restricted") {
            throw "Expected restricted working-intent transition, got '$($comparisonJson.workingIntentTransitionStatus)'."
        }
    }

    Invoke-HdtStep -Label "Compare lawful deferred perspectival support pair (prime)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $perspectivalLeft --right $deferredPerspectival --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "DeferredSupport") {
            throw "Expected deferred support classification, got '$($comparisonJson.classification)'."
        }

        if ($comparisonJson.workingIntentTransitionStatus -ne "Deferred") {
            throw "Expected deferred working-intent transition, got '$($comparisonJson.workingIntentTransitionStatus)'."
        }
    }

    Invoke-HdtStep -Label "Compare lawful rejected participatory support pair (prime)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $participatoryLeft --right $rejectedParticipatory --view prime --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "RejectedSupport") {
            throw "Expected rejected support classification, got '$($comparisonJson.classification)'."
        }

        if ($comparisonJson.workingIntentTransitionStatus -ne "Rejected") {
            throw "Expected rejected working-intent transition, got '$($comparisonJson.workingIntentTransitionStatus)'."
        }
    }

    Invoke-HdtStep -Label "Compare lawful perspectival support pair (prime text)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $perspectivalLeft --right $perspectivalRight --view prime
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Engram support comparison classification: StrengthenedSupport",
            "Support type compatibility: Aligned",
            "Support identity compatibility: RootTraceable",
            "Counterfeit pressure: none",
            "Similarity score: ",
            "Classification reason: "
        ) -Context "Perspectival support comparison text output"
    }

    Invoke-HdtStep -Label "Compare lawful restricted perspectival support pair (prime text)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $perspectivalLeft --right $restrictedPerspectival --view prime
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Engram support comparison classification: RestrictedSupport",
            "Support shapes: root_constructor_support vs root_constructor_support",
            "Intent classifications: bounded_support_evidence vs restricted_support_evidence",
            "Working-intent transition: Restricted",
            "Classification reason: "
        ) -Context "Restricted support comparison text output"
    }

    Invoke-HdtStep -Label "Compare lawful rejected participatory support pair (prime text)" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $participatoryLeft --right $rejectedParticipatory --view prime
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        Assert-OutputContains -Output $comparisonText -ExpectedFragments @(
            "Engram support comparison classification: RejectedSupport",
            "Support shapes: branch_set_support vs branch_set_support",
            "Intent classifications: reviewable_support_evidence vs rejected_support_evidence",
            "Working-intent transition: Rejected",
            "Classification reason: "
        ) -Context "Rejected support comparison text output"
    }

    $committedPerspectivalLeft = Join-Path $repoRoot "examples\\phase4-perspectival-sample.hopng.json"
    $committedPerspectivalRight = Join-Path $repoRoot "examples\\phase4-perspectival-peer.hopng.json"
    $committedParticipatoryLeft = Join-Path $repoRoot "examples\\phase4-participatory-sample.hopng.json"
    $committedParticipatoryRight = Join-Path $repoRoot "examples\\phase4-participatory-peer.hopng.json"
    $committedRestrictedPerspectival = Join-Path $repoRoot "examples\\phase4-restricted-perspectival.hopng.json"
    $committedDeferredPerspectival = Join-Path $repoRoot "examples\\phase4-deferred-perspectival.hopng.json"
    $committedRejectedParticipatory = Join-Path $repoRoot "examples\\phase4-rejected-participatory.hopng.json"

    Invoke-HdtStep -Label "Compare committed lawful Phase 4 perspectival support corpus" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $committedPerspectivalLeft --right $committedPerspectivalRight --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "StrengthenedSupport") {
            throw "Committed lawful perspectival comparison did not stay strengthened."
        }
    }

    Invoke-HdtStep -Label "Compare committed lawful Phase 4 participatory support corpus" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $committedParticipatoryLeft --right $committedParticipatoryRight --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "CoherentSupport") {
            throw "Committed lawful participatory comparison did not stay coherent."
        }
    }

    Invoke-HdtStep -Label "Compare committed lawful restricted Phase 4 perspectival support corpus" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $committedPerspectivalLeft --right $committedRestrictedPerspectival --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "RestrictedSupport") {
            throw "Committed restricted perspectival comparison did not stay restricted."
        }
    }

    Invoke-HdtStep -Label "Compare committed lawful deferred Phase 4 perspectival support corpus" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $committedPerspectivalLeft --right $committedDeferredPerspectival --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "DeferredSupport") {
            throw "Committed deferred perspectival comparison did not stay deferred."
        }
    }

    Invoke-HdtStep -Label "Compare committed lawful rejected Phase 4 participatory support corpus" -Action {
        $comparison = & (Join-Path $repoRoot "Compare-HOPNGEngramSupport.ps1") --left $committedParticipatoryLeft --right $committedRejectedParticipatory --json
        $comparisonText = ($comparison | Out-String).Trim()
        $comparisonText
        $comparisonJson = $comparisonText | ConvertFrom-Json
        if ($comparisonJson.classification -ne "RejectedSupport") {
            throw "Committed rejected participatory comparison did not stay rejected."
        }
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
