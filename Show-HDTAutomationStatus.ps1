<#
.SYNOPSIS
Shows the current HDT local automation status surface.

.DESCRIPTION
Reads the live HDT automation state files under `.audit/state` and emits either
human-readable status text or a combined JSON payload for operator use.
#>
[CmdletBinding()]
param(
    [ValidateSet("summary", "tasking", "orchestration", "all")]
    [string]$View = "all",

    [switch]$Json,

    [string]$AuditRoot
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
        throw "Required automation state file was not found: $Path"
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

    Write-Output "$Title"
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

$stateRoot = Join-Path $resolvedAuditRoot "state"
$cycleState = Read-RequiredJson -Path (Join-Path $stateRoot "local-automation-cycle.json")
$taskingState = Read-RequiredJson -Path (Join-Path $stateRoot "local-automation-tasking-status.json")
$orchestrationState = Read-RequiredJson -Path (Join-Path $stateRoot "master-thread-orchestration-status.json")

$payload = [ordered]@{
    auditRoot = $resolvedAuditRoot
    summary = $cycleState
    tasking = $taskingState
    orchestration = $orchestrationState
}

if ($Json) {
    switch ($View) {
        "summary" { $payload = $cycleState }
        "tasking" { $payload = $taskingState }
        "orchestration" { $payload = $orchestrationState }
    }

    $payload | ConvertTo-Json -Depth 8
    exit 0
}

switch ($View) {
    "summary" {
        Add-Section -Title "HDT Automation Summary" -Lines @(
            "Status: $($cycleState.status)",
            "Development posture: $($cycleState.developmentPosture)",
            "Steward stage: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'stewardStage')",
            "Active loop phase: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'activeLoopPhase')",
            "Work classification: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'workClassification')",
            "Research posture: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'researchPosture')",
            "Governance action: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'governanceAction')",
            "Recommended action: $($cycleState.recommendedAction)",
            "Last bundle id: $($cycleState.lastBundleId)",
            "Last bundle path: $($cycleState.lastBundlePath)",
            "Last digest path: $($cycleState.lastDigestBundlePath)",
            "Last work report path: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'lastWorkReportBundlePath')",
            "Digest disposition: $($cycleState.digestDisposition)",
            "Work report disposition: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'workReportDisposition')",
            "Next recommended work report run UTC: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'nextRecommendedWorkReportRunUtc')",
            "Next recommended release-candidate run UTC: $($cycleState.nextRecommendedReleaseCandidateRunUtc)",
            "Next mandatory HITL digest run UTC: $($cycleState.nextMandatoryHitlDigestRunUtc)",
            "Git branch: $($cycleState.git.branch)",
            "Git worktree state: $($cycleState.git.worktreeState)"
        )
    }
    "tasking" {
        $taskLines = @(
            "Cycle status: $($taskingState.cycleStatus)",
            "Development posture: $($taskingState.developmentPosture)",
            "Recommended action: $($taskingState.recommendedAction)",
            "Last bundle id: $($taskingState.lastBundleId)"
        )
        foreach ($task in $taskingState.tasks) {
            $taskLines += "Task [$($task.id)]: status=$($task.status)"
        }

        Add-Section -Title "HDT Automation Tasking" -Lines $taskLines
    }
    "orchestration" {
        $reasonText = if ($orchestrationState.reasons.Count -gt 0) {
            ($orchestrationState.reasons -join "; ")
        }
        else {
            "none"
        }

        Add-Section -Title "HDT Automation Orchestration" -Lines @(
            "Cycle status: $($orchestrationState.cycleStatus)",
            "Current branch: $($orchestrationState.branch)",
            "Current worktree state: $($orchestrationState.worktreeState)",
            "Required published branch: $($orchestrationState.requiredPublishedBranch)",
            "Required worktree state: $($orchestrationState.requiredWorktreeState)",
            "Advisory publish ready: $($orchestrationState.publishReady)",
            "Reasons: $reasonText"
        )
    }
    default {
        Add-Section -Title "HDT Automation Summary" -Lines @(
            "Status: $($cycleState.status)",
            "Development posture: $($cycleState.developmentPosture)",
            "Steward stage: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'stewardStage')",
            "Active loop phase: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'activeLoopPhase')",
            "Recommended action: $($cycleState.recommendedAction)",
            "Last bundle id: $($cycleState.lastBundleId)",
            "Digest disposition: $($cycleState.digestDisposition)",
            "Work report disposition: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'workReportDisposition')",
            "Next recommended work report run UTC: $(Get-OptionalValue -InputObject $cycleState -PropertyName 'nextRecommendedWorkReportRunUtc')",
            "Next recommended release-candidate run UTC: $($cycleState.nextRecommendedReleaseCandidateRunUtc)",
            "Next mandatory HITL digest run UTC: $($cycleState.nextMandatoryHitlDigestRunUtc)"
        )

        $taskLines = @()
        foreach ($task in $taskingState.tasks) {
            $taskLines += "Task [$($task.id)]: status=$($task.status)"
        }
        Add-Section -Title "HDT Automation Tasking" -Lines $taskLines

        $reasonText = if ($orchestrationState.reasons.Count -gt 0) {
            ($orchestrationState.reasons -join "; ")
        }
        else {
            "none"
        }
        Add-Section -Title "HDT Automation Orchestration" -Lines @(
            "Current branch: $($orchestrationState.branch)",
            "Current worktree state: $($orchestrationState.worktreeState)",
            "Advisory publish ready: $($orchestrationState.publishReady)",
            "Reasons: $reasonText"
        )
    }
}

exit 0
