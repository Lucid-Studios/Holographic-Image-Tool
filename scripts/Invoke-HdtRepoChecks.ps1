<#
.SYNOPSIS
Runs the repo-local HDT verification chain.

.DESCRIPTION
This helper serializes restore, build, tests, and smoke scripts so the repo
has one mature local check path. It is intended for day-to-day development
and release hardening on the current build.
#>
param(
    [switch]$SkipPhase2Smoke,
    [switch]$SkipPhase3Smoke,
    [switch]$SkipPhase3FailureSmoke
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot "HolographicDataTool.sln"
$testsProject = Join-Path $repoRoot "Hdt.Tests\Hdt.Tests.csproj"

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

Invoke-HdtStep -Label "Restore solution" -Action {
    & dotnet restore $solution
}

Invoke-HdtStep -Label "Build solution" -Action {
    & dotnet build $solution --no-restore
}

Invoke-HdtStep -Label "Run test suite" -Action {
    & dotnet test $testsProject --no-build --no-restore
}

if (-not $SkipPhase2Smoke) {
    Invoke-HdtStep -Label "Run Phase 2 smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase2ReleaseSmoke.ps1")
    }
}

if (-not $SkipPhase3Smoke) {
    Invoke-HdtStep -Label "Run Phase 3 smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase3ReleaseSmoke.ps1")
    }
}

if (-not $SkipPhase3FailureSmoke) {
    Invoke-HdtStep -Label "Run Phase 3 failure smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase3FailureSmoke.ps1")
    }
}

exit 0
