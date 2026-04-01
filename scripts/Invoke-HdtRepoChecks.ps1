<#
.SYNOPSIS
Runs the repo-local HDT verification chain.

.DESCRIPTION
This helper serializes restore, build, tests, and smoke scripts so the repo
has one mature local check path. It is intended for day-to-day development,
release maintenance, and posture-aware local automation on the current build.
#>
param(
    [ValidateSet("Initial", "Formal", "Closing", "Approved")]
    [string]$DevelopmentPosture = "Closing",

    [switch]$SkipPhase2Smoke,
    [switch]$SkipPhase3Smoke,
    [switch]$SkipPhase3ComparisonSmoke,
    [switch]$SkipPhase3ComparisonFailureSmoke,
    [switch]$SkipPhase3FailureSmoke,
    [switch]$SkipPhase4EntrySmoke,
    [switch]$SkipPhase4EntryFailureSmoke,
    [switch]$SkipPhase4SupportComparisonSmoke,
    [switch]$SkipPhase4SupportComparisonFailureSmoke
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot "HolographicDataTool.sln"
$testsProject = Join-Path $repoRoot "Hdt.Tests\Hdt.Tests.csproj"
$repoLocalAppData = Join-Path $repoRoot ".nuget-appdata"
$repoLocalNuGetDirectory = Join-Path $repoLocalAppData "NuGet"
$repoLocalNuGetConfig = Join-Path $repoLocalNuGetDirectory "NuGet.Config"
$userPackageCache = Join-Path $HOME ".nuget\packages"
$effectivePackageCache = if (Test-Path $userPackageCache) { $userPackageCache } else { Join-Path $repoRoot ".nuget-packages" }

New-Item -ItemType Directory -Force $repoLocalNuGetDirectory, $effectivePackageCache | Out-Null
$env:APPDATA = $repoLocalAppData
$env:NUGET_PACKAGES = $effectivePackageCache

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="$effectivePackageCache" />
  </config>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
"@ | Set-Content -Path $repoLocalNuGetConfig

$postureCatalog = @{
    Initial = [pscustomobject]@{
        Description = "Exploratory local iteration on bounded hypotheses."
        ResearchBestPractice = "Use quick, low-cost feedback to test assumptions before making stronger claims."
        RequirePhase2Smoke = $false
        RequirePhase3Smoke = $false
        RequirePhase3ComparisonSmoke = $false
        RequirePhase3ComparisonFailureSmoke = $false
        RequirePhase3FailureSmoke = $false
        RequirePhase4EntrySmoke = $false
        RequirePhase4EntryFailureSmoke = $false
        RequirePhase4SupportComparisonSmoke = $false
        RequirePhase4SupportComparisonFailureSmoke = $false
    }
    Formal = [pscustomobject]@{
        Description = "Reproducible development posture with positive-path reference checks."
        ResearchBestPractice = "Use explicit protocol, representative corpora, and repeatable verification before broadening claims."
        RequirePhase2Smoke = $true
        RequirePhase3Smoke = $true
        RequirePhase3ComparisonSmoke = $true
        RequirePhase3ComparisonFailureSmoke = $false
        RequirePhase3FailureSmoke = $false
        RequirePhase4EntrySmoke = $true
        RequirePhase4EntryFailureSmoke = $false
        RequirePhase4SupportComparisonSmoke = $true
        RequirePhase4SupportComparisonFailureSmoke = $false
    }
    Closing = [pscustomobject]@{
        Description = "Pre-promotion closeout with positive and negative-path verification."
        ResearchBestPractice = "Use adversarial checks, negative controls, and failure-path evidence before declaring the lane stable."
        RequirePhase2Smoke = $true
        RequirePhase3Smoke = $true
        RequirePhase3ComparisonSmoke = $true
        RequirePhase3ComparisonFailureSmoke = $true
        RequirePhase3FailureSmoke = $true
        RequirePhase4EntrySmoke = $true
        RequirePhase4EntryFailureSmoke = $true
        RequirePhase4SupportComparisonSmoke = $true
        RequirePhase4SupportComparisonFailureSmoke = $true
    }
    Approved = [pscustomobject]@{
        Description = "Operator-approved baseline posture with full mechanical verification receipts."
        ResearchBestPractice = "Use reviewable receipts, immutable reference corpora, and explicit human approval before treating a result as adopted."
        RequirePhase2Smoke = $true
        RequirePhase3Smoke = $true
        RequirePhase3ComparisonSmoke = $true
        RequirePhase3ComparisonFailureSmoke = $true
        RequirePhase3FailureSmoke = $true
        RequirePhase4EntrySmoke = $true
        RequirePhase4EntryFailureSmoke = $true
        RequirePhase4SupportComparisonSmoke = $true
        RequirePhase4SupportComparisonFailureSmoke = $true
    }
}
$posture = $postureCatalog[$DevelopmentPosture]

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

function Assert-PostureRequirement {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RequirementLabel,

        [Parameter(Mandatory = $true)]
        [bool]$RequiredByPosture,

        [Parameter(Mandatory = $true)]
        [bool]$SkipRequested
    )

    if ($RequiredByPosture -and $SkipRequested) {
        throw "Development posture '$DevelopmentPosture' requires '$RequirementLabel'. Remove the skip switch or lower the requested posture."
    }
}

function Write-HdtPostureBanner {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [psobject]$Definition
    )

    Write-Host "==> Development posture: $Name"
    Write-Host "    Intent: $($Definition.Description)"
    Write-Host "    Research best practice: $($Definition.ResearchBestPractice)"

    if ($Name -eq "Approved") {
        Write-Host "    Note: this script verifies approved-posture mechanics but does not replace explicit HITL approval."
    }
}

Assert-PostureRequirement -RequirementLabel "Phase 2 smoke path" -RequiredByPosture $posture.RequirePhase2Smoke -SkipRequested $SkipPhase2Smoke
Assert-PostureRequirement -RequirementLabel "Phase 3 release smoke path" -RequiredByPosture $posture.RequirePhase3Smoke -SkipRequested $SkipPhase3Smoke
Assert-PostureRequirement -RequirementLabel "Phase 3 comparison smoke path" -RequiredByPosture $posture.RequirePhase3ComparisonSmoke -SkipRequested $SkipPhase3ComparisonSmoke
Assert-PostureRequirement -RequirementLabel "Phase 3 comparison failure smoke path" -RequiredByPosture $posture.RequirePhase3ComparisonFailureSmoke -SkipRequested $SkipPhase3ComparisonFailureSmoke
Assert-PostureRequirement -RequirementLabel "Phase 3 failure smoke path" -RequiredByPosture $posture.RequirePhase3FailureSmoke -SkipRequested $SkipPhase3FailureSmoke
Assert-PostureRequirement -RequirementLabel "Phase 4 entry smoke path" -RequiredByPosture $posture.RequirePhase4EntrySmoke -SkipRequested $SkipPhase4EntrySmoke
Assert-PostureRequirement -RequirementLabel "Phase 4 entry failure smoke path" -RequiredByPosture $posture.RequirePhase4EntryFailureSmoke -SkipRequested $SkipPhase4EntryFailureSmoke
Assert-PostureRequirement -RequirementLabel "Phase 4 support comparison smoke path" -RequiredByPosture $posture.RequirePhase4SupportComparisonSmoke -SkipRequested $SkipPhase4SupportComparisonSmoke
Assert-PostureRequirement -RequirementLabel "Phase 4 support comparison failure smoke path" -RequiredByPosture $posture.RequirePhase4SupportComparisonFailureSmoke -SkipRequested $SkipPhase4SupportComparisonFailureSmoke

Write-HdtPostureBanner -Name $DevelopmentPosture -Definition $posture

Invoke-HdtStep -Label "Restore solution" -Action {
    # Keep restore inside repo-local config surfaces and serialize the graph
    # walk, which is unstable in this sandbox when parallelized.
    & dotnet restore $solution --configfile $repoLocalNuGetConfig -m:1 `
        -p:RestoreUseStaticGraphEvaluation=false `
        -p:RestoreDisableParallel=true
}

Invoke-HdtStep -Label "Build solution" -Action {
    & dotnet build $solution --no-restore -m:1
}

Invoke-HdtStep -Label "Run test suite" -Action {
    & dotnet test $testsProject --no-build --no-restore
}

if ($posture.RequirePhase2Smoke -and -not $SkipPhase2Smoke) {
    Invoke-HdtStep -Label "Run Phase 2 smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase2ReleaseSmoke.ps1")
    }
}

if ($posture.RequirePhase3Smoke -and -not $SkipPhase3Smoke) {
    Invoke-HdtStep -Label "Run Phase 3 smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase3ReleaseSmoke.ps1")
    }
}

if ($posture.RequirePhase3ComparisonSmoke -and -not $SkipPhase3ComparisonSmoke) {
    Invoke-HdtStep -Label "Run Phase 3 comparison smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase3ComparisonSmoke.ps1")
    }
}

if ($posture.RequirePhase3ComparisonFailureSmoke -and -not $SkipPhase3ComparisonFailureSmoke) {
    Invoke-HdtStep -Label "Run Phase 3 comparison failure smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase3ComparisonFailureSmoke.ps1")
    }
}

if ($posture.RequirePhase3FailureSmoke -and -not $SkipPhase3FailureSmoke) {
    Invoke-HdtStep -Label "Run Phase 3 failure smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase3FailureSmoke.ps1")
    }
}

if ($posture.RequirePhase4EntrySmoke -and -not $SkipPhase4EntrySmoke) {
    Invoke-HdtStep -Label "Run Phase 4 entry smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase4EntrySmoke.ps1")
    }
}

if ($posture.RequirePhase4EntryFailureSmoke -and -not $SkipPhase4EntryFailureSmoke) {
    Invoke-HdtStep -Label "Run Phase 4 entry failure smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase4EntryFailureSmoke.ps1")
    }
}

if ($posture.RequirePhase4SupportComparisonSmoke -and -not $SkipPhase4SupportComparisonSmoke) {
    Invoke-HdtStep -Label "Run Phase 4 support comparison smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase4SupportComparisonSmoke.ps1")
    }
}

if ($posture.RequirePhase4SupportComparisonFailureSmoke -and -not $SkipPhase4SupportComparisonFailureSmoke) {
    Invoke-HdtStep -Label "Run Phase 4 support comparison failure smoke path" -Action {
        & (Join-Path $PSScriptRoot "Invoke-Phase4SupportComparisonFailureSmoke.ps1")
    }
}

exit 0
