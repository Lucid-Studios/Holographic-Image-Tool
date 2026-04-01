<#
.SYNOPSIS
Runs the repo-local Phase 4 entry negative-path smoke.

.DESCRIPTION
This smoke script creates invalid Phase 4 entry artifacts through the public
wrapper surface and verifies that support-only overclaim and branch or handoff
violations fail deterministically through the core validator.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-phase4-failure-" + [guid]::NewGuid().ToString("N"))

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

try {
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    Invoke-HdtStep -Label "Create invalid Phase 4 perspectival scratch artifact" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4InvalidPerspectivalSample.ps1") --output-dir $tempDir --name phase4-perspectival-invalid --json
    }

    $invalidPerspectivalManifestPath = Join-Path $tempDir "phase4-perspectival-invalid.hopng.json"

    Invoke-HdtStep -Label "Validate invalid Phase 4 perspectival artifact" -AllowedExitCodes @(35) -Action {
        $validation = & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $invalidPerspectivalManifestPath --json
        $validationText = ($validation | Out-String).Trim()
        $validationText
        $validationJson = $validationText | ConvertFrom-Json
        if ($validationJson.isValid) {
            throw "Invalid perspectival validation unexpectedly passed."
        }

        if (-not ($validationJson.errors | Where-Object { $_.code -eq 35 })) {
            throw "Invalid perspectival validation did not report the expected Phase 4 error code."
        }
    }

    Invoke-HdtStep -Label "Create invalid Phase 4 participatory scratch artifact" -Action {
        & (Join-Path $repoRoot "New-HOPNGPhase4InvalidParticipatorySample.ps1") --output-dir $tempDir --name phase4-participatory-invalid --json
    }

    $invalidParticipatoryManifestPath = Join-Path $tempDir "phase4-participatory-invalid.hopng.json"

    Invoke-HdtStep -Label "Validate invalid Phase 4 participatory artifact" -AllowedExitCodes @(36) -Action {
        $validation = & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $invalidParticipatoryManifestPath --json
        $validationText = ($validation | Out-String).Trim()
        $validationText
        $validationJson = $validationText | ConvertFrom-Json
        if ($validationJson.isValid) {
            throw "Invalid participatory validation unexpectedly passed."
        }

        if (-not ($validationJson.errors | Where-Object { $_.code -eq 36 })) {
            throw "Invalid participatory validation did not report the expected Phase 4 error code."
        }
    }

    $committedInvalidPerspectivalManifestPath = Join-Path $repoRoot "examples\\phase4-invalid-perspectival.hopng.json"
    $committedInvalidParticipatoryManifestPath = Join-Path $repoRoot "examples\\phase4-invalid-participatory.hopng.json"

    Invoke-HdtStep -Label "Validate committed invalid Phase 4 perspectival reference artifact" -AllowedExitCodes @(35) -Action {
        $validation = & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $committedInvalidPerspectivalManifestPath --json
        $validationText = ($validation | Out-String).Trim()
        $validationText
        $validationJson = $validationText | ConvertFrom-Json
        if ($validationJson.isValid) {
            throw "Committed invalid perspectival reference artifact unexpectedly validated."
        }

        if (-not ($validationJson.errors | Where-Object { $_.code -eq 35 })) {
            throw "Committed invalid perspectival reference artifact did not report the expected Phase 4 error code."
        }
    }

    Invoke-HdtStep -Label "Validate committed invalid Phase 4 participatory reference artifact" -AllowedExitCodes @(36) -Action {
        $validation = & (Join-Path $repoRoot "Test-HOPNG.ps1") --path $committedInvalidParticipatoryManifestPath --json
        $validationText = ($validation | Out-String).Trim()
        $validationText
        $validationJson = $validationText | ConvertFrom-Json
        if ($validationJson.isValid) {
            throw "Committed invalid participatory reference artifact unexpectedly validated."
        }

        if (-not ($validationJson.errors | Where-Object { $_.code -eq 36 })) {
            throw "Committed invalid participatory reference artifact did not report the expected Phase 4 error code."
        }
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

exit 0
