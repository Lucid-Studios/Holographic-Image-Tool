<#
.SYNOPSIS
Runs the repo-local Phase 4 entry positive-path smoke.

.DESCRIPTION
This smoke script creates lawful perspectival and participatory Phase 4 entry
artifacts through the public PowerShell wrapper surface, validates them, and
checks that Prime-safe inspection exposes support summaries without silently
upgrading support evidence into later-phase authority.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-phase4-smoke-" + [guid]::NewGuid().ToString("N"))

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

function Assert-NoCommittedExamplePrivateKeys {
    $privateKeys = @(Get-ChildItem -Path (Join-Path $repoRoot "examples") -Filter "*.ed25519.private.key" -File -Recurse)
    if ($privateKeys.Count -gt 0) {
        $paths = ($privateKeys | ForEach-Object { $_.FullName }) -join ", "
        throw "Committed examples unexpectedly include private signing keys: $paths"
    }

    $global:LASTEXITCODE = 0
}

try {
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    Invoke-HdtStep -Label "Assert committed example corpus excludes private keys" -Action {
        Assert-NoCommittedExamplePrivateKeys
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 perspectival scratch artifact" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4PerspectivalSample.ps1") --output-dir $tempDir --name phase4-perspectival --json
    }

    $perspectivalManifestPath = Join-Path $tempDir "phase4-perspectival.hopng.json"

    Invoke-HdtStep -Label "Validate lawful Phase 4 perspectival artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $perspectivalManifestPath --json
    }

    Invoke-HdtStep -Label "Inspect lawful Phase 4 perspectival artifact" -Action {
        $inspect = & (Join-Path $repoRoot "Show-HOPNG.ps1") --path $perspectivalManifestPath --view prime --json
        $inspectText = ($inspect | Out-String).Trim()
        $inspectText
        $inspectJson = $inspectText | ConvertFrom-Json
        if ($null -eq $inspectJson.engramSupportSummary) {
            throw "Phase 4 perspectival prime-safe inspection did not expose an engram support summary."
        }

        if ($null -eq $inspectJson.engramStabilityField) {
            throw "Phase 4 perspectival prime-safe inspection did not expose an engram stability field."
        }

        if ($inspectJson.engramSupportSummary.supportType -ne "perspectival") {
            throw "Phase 4 perspectival prime-safe inspection reported the wrong support type."
        }

        if ($inspectJson.engramSupportSummary.workingIntentState -ne "supported_intent") {
            throw "Phase 4 perspectival prime-safe inspection reported the wrong working-intent state."
        }
    }

    Invoke-HdtStep -Label "Render inherited temporal stack for lawful Phase 4 perspectival artifact" -Action {
        & (Join-Path $repoRoot "Render-HOPNGPhaseStack.ps1") --path $perspectivalManifestPath --view prime --json
    }

    Invoke-HdtStep -Label "Create lawful Phase 4 participatory scratch artifact" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4ParticipatorySample.ps1") --output-dir $tempDir --name phase4-participatory --json
    }

    $participatoryManifestPath = Join-Path $tempDir "phase4-participatory.hopng.json"

    Invoke-HdtStep -Label "Validate lawful Phase 4 participatory artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $participatoryManifestPath --json
    }

    Invoke-HdtStep -Label "Inspect lawful Phase 4 participatory artifact" -Action {
        $inspect = & (Join-Path $repoRoot "Show-HOPNG.ps1") --path $participatoryManifestPath --view prime --json
        $inspectText = ($inspect | Out-String).Trim()
        $inspectText
        $inspectJson = $inspectText | ConvertFrom-Json
        if ($null -eq $inspectJson.engramSupportSummary) {
            throw "Phase 4 participatory prime-safe inspection did not expose an engram support summary."
        }

        if ($null -eq $inspectJson.engramStabilityField) {
            throw "Phase 4 participatory prime-safe inspection did not expose an engram stability field."
        }

        if ($inspectJson.engramSupportSummary.supportType -ne "participatory") {
            throw "Phase 4 participatory prime-safe inspection reported the wrong support type."
        }

        if ($inspectJson.engramSupportSummary.workingIntentState -ne "reviewable_support") {
            throw "Phase 4 participatory prime-safe inspection reported the wrong working-intent state."
        }
    }

    Invoke-HdtStep -Label "Render inherited temporal stack for lawful Phase 4 participatory artifact" -Action {
        & (Join-Path $repoRoot "Render-HOPNGPhaseStack.ps1") --path $participatoryManifestPath --view prime --json
    }

    $committedPerspectivalManifestPath = Join-Path $repoRoot "examples\\phase4-perspectival-sample.hopng.json"
    $committedParticipatoryManifestPath = Join-Path $repoRoot "examples\\phase4-participatory-sample.hopng.json"
    $committedRestrictedPerspectivalManifestPath = Join-Path $repoRoot "examples\\phase4-restricted-perspectival.hopng.json"
    $committedDeferredPerspectivalManifestPath = Join-Path $repoRoot "examples\\phase4-deferred-perspectival.hopng.json"
    $committedRejectedParticipatoryManifestPath = Join-Path $repoRoot "examples\\phase4-rejected-participatory.hopng.json"

    Invoke-HdtStep -Label "Validate committed lawful Phase 4 perspectival reference artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $committedPerspectivalManifestPath --json
    }

    Invoke-HdtStep -Label "Inspect committed lawful Phase 4 perspectival reference artifact" -Action {
        $inspect = & (Join-Path $repoRoot "Show-HOPNG.ps1") --path $committedPerspectivalManifestPath --view prime --json
        $inspectText = ($inspect | Out-String).Trim()
        $inspectText
        $inspectJson = $inspectText | ConvertFrom-Json
        if ($inspectJson.engramSupportSummary.supportType -ne "perspectival") {
            throw "Committed lawful perspectival reference artifact reported the wrong support type."
        }

        if ($null -eq $inspectJson.engramStabilityField) {
            throw "Committed lawful perspectival reference artifact did not expose an engram stability field."
        }
    }

    Invoke-HdtStep -Label "Validate committed lawful Phase 4 participatory reference artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $committedParticipatoryManifestPath --json
    }

    Invoke-HdtStep -Label "Inspect committed lawful Phase 4 participatory reference artifact" -Action {
        $inspect = & (Join-Path $repoRoot "Show-HOPNG.ps1") --path $committedParticipatoryManifestPath --view prime --json
        $inspectText = ($inspect | Out-String).Trim()
        $inspectText
        $inspectJson = $inspectText | ConvertFrom-Json
        if ($inspectJson.engramSupportSummary.supportType -ne "participatory") {
            throw "Committed lawful participatory reference artifact reported the wrong support type."
        }

        if ($null -eq $inspectJson.engramStabilityField) {
            throw "Committed lawful participatory reference artifact did not expose an engram stability field."
        }
    }

    Invoke-HdtStep -Label "Validate committed lawful restricted Phase 4 perspectival reference artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $committedRestrictedPerspectivalManifestPath --json
    }

    Invoke-HdtStep -Label "Inspect committed lawful restricted Phase 4 perspectival reference artifact" -Action {
        $inspect = & (Join-Path $repoRoot "Show-HOPNG.ps1") --path $committedRestrictedPerspectivalManifestPath --view prime --json
        $inspectText = ($inspect | Out-String).Trim()
        $inspectText
        $inspectJson = $inspectText | ConvertFrom-Json
        if ($inspectJson.engramSupportSummary.supportType -ne "perspectival") {
            throw "Committed restricted perspectival reference artifact reported the wrong support type."
        }

        if ($inspectJson.engramSupportSummary.workingIntentState -ne "restricted_support") {
            throw "Committed restricted perspectival reference artifact reported the wrong working-intent state."
        }

        if ($inspectJson.engramSupportSummary.intentClassification -ne "restricted_support_evidence") {
            throw "Committed restricted perspectival reference artifact reported the wrong intent classification."
        }

        if ($inspectJson.engramSupportSummary.supportShape -ne "root_constructor_support") {
            throw "Committed restricted perspectival reference artifact reported the wrong support shape."
        }

        if ([string]::IsNullOrWhiteSpace($inspectJson.engramSupportSummary.stateReason)) {
            throw "Committed restricted perspectival reference artifact did not expose a state reason."
        }
    }

    Invoke-HdtStep -Label "Validate committed lawful deferred Phase 4 perspectival reference artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $committedDeferredPerspectivalManifestPath --json
    }

    Invoke-HdtStep -Label "Inspect committed lawful deferred Phase 4 perspectival reference artifact" -Action {
        $inspect = & (Join-Path $repoRoot "Show-HOPNG.ps1") --path $committedDeferredPerspectivalManifestPath --view prime --json
        $inspectText = ($inspect | Out-String).Trim()
        $inspectText
        $inspectJson = $inspectText | ConvertFrom-Json
        if ($inspectJson.engramSupportSummary.supportType -ne "perspectival") {
            throw "Committed deferred perspectival reference artifact reported the wrong support type."
        }

        if ($inspectJson.engramSupportSummary.workingIntentState -ne "deferred_support") {
            throw "Committed deferred perspectival reference artifact reported the wrong working-intent state."
        }

        if ($inspectJson.engramSupportSummary.intentClassification -ne "deferred_support_evidence") {
            throw "Committed deferred perspectival reference artifact reported the wrong intent classification."
        }

        if ($inspectJson.engramSupportSummary.supportShape -ne "root_constructor_support") {
            throw "Committed deferred perspectival reference artifact reported the wrong support shape."
        }

        if ([string]::IsNullOrWhiteSpace($inspectJson.engramSupportSummary.stateReason)) {
            throw "Committed deferred perspectival reference artifact did not expose a state reason."
        }
    }

    Invoke-HdtStep -Label "Validate committed lawful rejected Phase 4 participatory reference artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $committedRejectedParticipatoryManifestPath --json
    }

    Invoke-HdtStep -Label "Inspect committed lawful rejected Phase 4 participatory reference artifact" -Action {
        $inspect = & (Join-Path $repoRoot "Show-HOPNG.ps1") --path $committedRejectedParticipatoryManifestPath --view prime --json
        $inspectText = ($inspect | Out-String).Trim()
        $inspectText
        $inspectJson = $inspectText | ConvertFrom-Json
        if ($inspectJson.engramSupportSummary.supportType -ne "participatory") {
            throw "Committed rejected participatory reference artifact reported the wrong support type."
        }

        if ($inspectJson.engramSupportSummary.workingIntentState -ne "rejected_support") {
            throw "Committed rejected participatory reference artifact reported the wrong working-intent state."
        }

        if ($inspectJson.engramSupportSummary.intentClassification -ne "rejected_support_evidence") {
            throw "Committed rejected participatory reference artifact reported the wrong intent classification."
        }

        if ($inspectJson.engramSupportSummary.supportShape -ne "branch_set_support") {
            throw "Committed rejected participatory reference artifact reported the wrong support shape."
        }

        if ([string]::IsNullOrWhiteSpace($inspectJson.engramSupportSummary.stateReason)) {
            throw "Committed rejected participatory reference artifact did not expose a state reason."
        }
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
