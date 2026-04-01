<#
.SYNOPSIS
Shows the latest or requested HDT automation receipt bundle and digest.

.DESCRIPTION
Reads the HDT local automation release-candidate and digest receipts from the
live `.audit` tree and emits either operator-readable text or combined JSON.
#>
[CmdletBinding()]
param(
    [ValidateSet("bundle", "digest", "all")]
    [string]$View = "all",

    [switch]$Json,

    [string]$AuditRoot,

    [string]$BundleId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$resolvedAuditRoot = if ([string]::IsNullOrWhiteSpace($AuditRoot)) {
    Join-Path $repoRoot ".audit"
}
else {
    if ([System.IO.Path]::IsPathRooted($AuditRoot)) {
        $AuditRoot
    }
    else {
        Join-Path $repoRoot $AuditRoot
    }
}

function Read-RequiredJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required automation receipt file was not found: $Path"
    }

    return (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json)
}

function Add-Section {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Title,

        [Parameter(Mandatory = $true)]
        [string[]]$Lines
    )

    Write-Output $Title
    foreach ($line in $Lines) {
        Write-Output "  $line"
    }
}

function Get-OptionalValue {
    param(
        [Parameter(Mandatory = $true)]
        [object]$InputObject,

        [Parameter(Mandatory = $true)]
        [string]$PropertyName,

        [string]$DefaultValue = ""
    )

    $property = $InputObject.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        return $DefaultValue
    }

    return [string]$property.Value
}

function Get-OptionalNestedBooleanValue {
    param(
        [Parameter(Mandatory = $true)]
        [object]$InputObject,

        [Parameter(Mandatory = $true)]
        [string]$PropertyName,

        [Parameter(Mandatory = $true)]
        [string]$NestedPropertyName,

        [bool]$DefaultValue = $false
    )

    $property = $InputObject.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value) {
        return $DefaultValue
    }

    $nestedProperty = $property.Value.PSObject.Properties[$NestedPropertyName]
    if ($null -eq $nestedProperty) {
        return $DefaultValue
    }

    return [bool]$nestedProperty.Value
}

$cycleStatePath = Join-Path $resolvedAuditRoot "state\local-automation-cycle.json"
$cycleState = Read-RequiredJson -Path $cycleStatePath

$candidateBundleId = if ([string]::IsNullOrWhiteSpace($BundleId)) {
    $cycleState.lastBundleId
}
else {
    $BundleId
}

$digestBundleId = if ([string]::IsNullOrWhiteSpace($BundleId)) {
    $cycleState.lastDigestBundleId
}
else {
    $BundleId
}

$bundleManifestPath = Join-Path $resolvedAuditRoot ("runs\release-candidates\{0}\build-evidence-manifest.json" -f $candidateBundleId)
$digestJsonPath = Join-Path $resolvedAuditRoot ("runs\release-digests\{0}\release-candidate-digest.json" -f $digestBundleId)

$bundleReceipt = $null
$digestReceipt = $null

if ($View -in @("bundle", "all")) {
    $bundleReceipt = Read-RequiredJson -Path $bundleManifestPath
}

if ($View -in @("digest", "all")) {
    $digestReceipt = Read-RequiredJson -Path $digestJsonPath
}

$payload = [ordered]@{
    auditRoot = $resolvedAuditRoot
    cycleStatePath = $cycleStatePath
}

if ($null -ne $bundleReceipt) {
    $payload.bundle = [ordered]@{
        manifest = $bundleReceipt
        manifestPath = $bundleManifestPath
        summaryPath = $bundleReceipt.paths.summaryPath
    }
}

if ($null -ne $digestReceipt) {
    $payload.digest = [ordered]@{
        receipt = $digestReceipt
        receiptPath = $digestJsonPath
        markdownPath = Join-Path $resolvedAuditRoot ("runs\release-digests\{0}\release-candidate-digest.md" -f $digestBundleId)
    }
}

if ($Json) {
    $payload | ConvertTo-Json -Depth 8
    exit 0
}

switch ($View) {
    "bundle" {
        $workReportEmitted = Get-OptionalNestedBooleanValue -InputObject $bundleReceipt -PropertyName "workReport" -NestedPropertyName "emitted"
        Add-Section -Title "HDT Automation Bundle" -Lines @(
            "Bundle id: $($bundleReceipt.bundleId)",
            "Status: $($bundleReceipt.status)",
            "Development posture: $($bundleReceipt.developmentPosture)",
            "Steward stage: $(Get-OptionalValue -InputObject $bundleReceipt -PropertyName 'stewardStage')",
            "Work classification: $(Get-OptionalValue -InputObject $bundleReceipt -PropertyName 'workClassification')",
            "Governance action: $(Get-OptionalValue -InputObject $bundleReceipt -PropertyName 'governanceAction')",
            "Recommended action: $($bundleReceipt.recommendedAction)",
            "Repo checks exit code: $($bundleReceipt.repoChecks.exitCode)",
            "Branch: $($bundleReceipt.git.branch)",
            "Worktree state: $($bundleReceipt.git.worktreeState)",
            "Digest emitted: $($bundleReceipt.digest.emitted)",
            "Work report emitted: $workReportEmitted",
            "Manifest path: $bundleManifestPath"
        )
    }
    "digest" {
        Add-Section -Title "HDT Automation Digest" -Lines @(
            "Bundle id: $($digestReceipt.bundleId)",
            "Status: $($digestReceipt.status)",
            "Development posture: $($digestReceipt.developmentPosture)",
            "Recommended action: $($digestReceipt.recommendedAction)",
            "HITL still required: $($digestReceipt.hitlStillRequired)",
            "Digest path: $digestJsonPath",
            "Last release bundle path: $($digestReceipt.lastBundlePath)",
            "Note: $($digestReceipt.note)"
        )
    }
    default {
        $workReportEmitted = Get-OptionalNestedBooleanValue -InputObject $bundleReceipt -PropertyName "workReport" -NestedPropertyName "emitted"
        Add-Section -Title "HDT Automation Bundle" -Lines @(
            "Bundle id: $($bundleReceipt.bundleId)",
            "Status: $($bundleReceipt.status)",
            "Development posture: $($bundleReceipt.developmentPosture)",
            "Steward stage: $(Get-OptionalValue -InputObject $bundleReceipt -PropertyName 'stewardStage')",
            "Work classification: $(Get-OptionalValue -InputObject $bundleReceipt -PropertyName 'workClassification')",
            "Governance action: $(Get-OptionalValue -InputObject $bundleReceipt -PropertyName 'governanceAction')",
            "Recommended action: $($bundleReceipt.recommendedAction)",
            "Repo checks exit code: $($bundleReceipt.repoChecks.exitCode)",
            "Branch: $($bundleReceipt.git.branch)",
            "Worktree state: $($bundleReceipt.git.worktreeState)",
            "Work report emitted: $workReportEmitted",
            "Manifest path: $bundleManifestPath"
        )

        Add-Section -Title "HDT Automation Digest" -Lines @(
            "Bundle id: $($digestReceipt.bundleId)",
            "Status: $($digestReceipt.status)",
            "Development posture: $($digestReceipt.developmentPosture)",
            "Recommended action: $($digestReceipt.recommendedAction)",
            "HITL still required: $($digestReceipt.hitlStillRequired)",
            "Digest path: $digestJsonPath",
            "Last release bundle path: $($digestReceipt.lastBundlePath)"
        )
    }
}

exit 0
