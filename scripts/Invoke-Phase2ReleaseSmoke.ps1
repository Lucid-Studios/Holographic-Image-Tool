Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-smoke-" + [guid]::NewGuid().ToString("N"))

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

    Invoke-HdtStep -Label "Create scratch artifact" -Action {
        & (Join-Path $repoRoot "New-HOPNG.ps1") --output-dir $tempDir --name smoke-artifact --signer "Smoke Tester" --key-id "smoke-key" --json
    }

    Invoke-HdtStep -Label "Validate scratch artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path (Join-Path $tempDir "smoke-artifact.hopng.json") --json
    }

    Invoke-HdtStep -Label "Inspect scratch artifact" -Action {
        & (Join-Path $repoRoot "Show-HOPNG.ps1") --path (Join-Path $tempDir "smoke-artifact.hopng.json") --view prime --json
    }

    Invoke-HdtStep -Label "Validate Phase 1 reference artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path (Join-Path $repoRoot "examples\\phase1-sample.hopng.json") --json
    }

    Invoke-HdtStep -Label "Validate Phase 2 reference artifact" -Action {
        & (Join-Path $repoRoot "Test-HOPNG.ps1") --path (Join-Path $repoRoot "examples\\phase2-sample.hopng.json") --json
    }

    Invoke-HdtStep -Label "Merge Phase 2 reference artifact" -Action {
        & (Join-Path $repoRoot "Merge-HOPNGLayers.ps1") --path (Join-Path $repoRoot "examples\\phase2-sample.hopng.json") --json
    }

    Invoke-HdtStep -Label "Compare Phase 2 against Phase 1 reference artifact" -Action {
        & (Join-Path $repoRoot "Compare-HOPNGSurfaces.ps1") --left (Join-Path $repoRoot "examples\\phase2-sample.hopng.json") --right (Join-Path $repoRoot "examples\\phase1-sample.hopng.json") --json
    } -AllowedExitCodes @(25)
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}
