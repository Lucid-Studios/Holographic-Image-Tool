<#
.SYNOPSIS
Runs the repo-local Phase 3 temporal smoke path.

.DESCRIPTION
This smoke script creates a valid Phase 3 sample artifact through the public
PowerShell wrapper surface, validates it, inspects it, and renders the
temporal stack in both prime-safe and privileged modes.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-phase3-smoke-" + [guid]::NewGuid().ToString("N"))

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

    Invoke-HdtStep -Label "Create Phase 3 scratch artifact" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase3Sample.ps1") --output-dir $tempDir --name phase3-smoke --json
    }

    $manifestPath = Join-Path $tempDir "phase3-smoke.hopng.json"

    Invoke-HdtStep -Label "Validate Phase 3 scratch artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $manifestPath --json
    }

    Invoke-HdtStep -Label "Inspect Phase 3 scratch artifact" -Action {
        & (Join-Path $repoRoot "Show-HOPNG.ps1") --path $manifestPath --view prime --json
    }

    Invoke-HdtStep -Label "Render Phase 3 scratch artifact (prime)" -Action {
        & (Join-Path $repoRoot "Render-HOPNGPhaseStack.ps1") --path $manifestPath --view prime --json
    }

    Invoke-HdtStep -Label "Render Phase 3 scratch artifact (prime text)" -Action {
        $render = & (Join-Path $repoRoot "Render-HOPNGPhaseStack.ps1") --path $manifestPath --view prime
        $renderText = ($render | Out-String).Trim()
        $renderText
        Assert-OutputContains -Output $renderText -ExpectedFragments @(
            "Temporal stack status: LawfullyDerived",
            "Final state: ",
            "Final basis signals: ",
            "Horizon summaries: "
        ) -Context "Prime text render"
    }

    Invoke-HdtStep -Label "Render Phase 3 scratch artifact (privileged)" -Action {
        & (Join-Path $repoRoot "Render-HOPNGPhaseStack.ps1") --path $manifestPath --view privileged --json
    }

    Invoke-HdtStep -Label "Render Phase 3 scratch artifact (privileged text)" -Action {
        $render = & (Join-Path $repoRoot "Render-HOPNGPhaseStack.ps1") --path $manifestPath --view privileged
        $renderText = ($render | Out-String).Trim()
        $renderText
        Assert-OutputContains -Output $renderText -ExpectedFragments @(
            "Temporal stack status: LawfullyDerived",
            "Payload mode: full_payload",
            "Final derived force: ",
            "Validation issues: 0"
        ) -Context "Privileged text render"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
