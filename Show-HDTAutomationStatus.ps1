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

function Get-LiveGitObservation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$RequiredPublishedBranch,

        [Parameter(Mandatory = $true)]
        [string]$RequiredWorktreeState,

        [Parameter(Mandatory = $true)]
        [string]$EmittedBranch,

        [Parameter(Mandatory = $true)]
        [string]$EmittedWorktreeState
    )

    $branch = (& git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null | Out-String).Trim()
    $gitAvailable = ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($branch))
    if (-not $gitAvailable) {
        $branch = "unknown"
    }

    $statusLines = @(& git -C $RepoRoot status --short 2>$null)
    if ($LASTEXITCODE -ne 0) {
        $statusLines = @()
        $gitAvailable = $false
    }

    $worktreeState = if ($gitAvailable -and $statusLines.Count -eq 0) { "clean" } elseif ($gitAvailable) { "dirty" } else { "unknown" }
    $branchAligned = $gitAvailable -and ($branch -eq $RequiredPublishedBranch)
    $worktreeAligned = $gitAvailable -and ($worktreeState -eq $RequiredWorktreeState)
    $publishReady = $branchAligned -and $worktreeAligned
    $divergesFromEmittedBranch = $branch -ne $EmittedBranch
    $divergesFromEmittedWorktreeState = $worktreeState -ne $EmittedWorktreeState
    $divergesFromEmittedState = $divergesFromEmittedBranch -or $divergesFromEmittedWorktreeState

    $note = if (-not $gitAvailable) {
        "Current repo observation is unavailable; the wrapper is showing the last emitted .audit state only."
    }
    elseif ($divergesFromEmittedState) {
        "Current repo observation differs from the last emitted .audit orchestration surface; refresh the cycle when cadence or intake lawfully requires."
    }
    else {
        "Current repo observation matches the last emitted .audit orchestration surface."
    }

    return [ordered]@{
        available = $gitAvailable
        observedUtc = [DateTime]::UtcNow.ToString("o")
        branch = $branch
        worktreeState = $worktreeState
        requiredPublishedBranch = $RequiredPublishedBranch
        requiredWorktreeState = $RequiredWorktreeState
        branchAligned = $branchAligned
        worktreeAligned = $worktreeAligned
        publishReady = $publishReady
        divergesFromEmittedBranch = $divergesFromEmittedBranch
        divergesFromEmittedWorktreeState = $divergesFromEmittedWorktreeState
        divergesFromEmittedState = $divergesFromEmittedState
        emittedBranch = $EmittedBranch
        emittedWorktreeState = $EmittedWorktreeState
        statusLineCount = $statusLines.Count
        note = $note
    }
}

$stateRoot = Join-Path $resolvedAuditRoot "state"
$cycleState = Read-RequiredJson -Path (Join-Path $stateRoot "local-automation-cycle.json")
$taskingState = Read-RequiredJson -Path (Join-Path $stateRoot "local-automation-tasking-status.json")
$orchestrationState = Read-RequiredJson -Path (Join-Path $stateRoot "master-thread-orchestration-status.json")
$currentObservation = Get-LiveGitObservation `
    -RepoRoot $repoRoot `
    -RequiredPublishedBranch $orchestrationState.requiredPublishedBranch `
    -RequiredWorktreeState $orchestrationState.requiredWorktreeState `
    -EmittedBranch $orchestrationState.branch `
    -EmittedWorktreeState $orchestrationState.worktreeState

$payload = [ordered]@{
    auditRoot = $resolvedAuditRoot
    summary = $cycleState
    tasking = $taskingState
    orchestration = $orchestrationState
    currentObservation = $currentObservation
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
            "Git worktree state (last emitted): $($cycleState.git.worktreeState)",
            "Current observed git branch: $($currentObservation.branch)",
            "Current observed git worktree state: $($currentObservation.worktreeState)",
            "Current observed publish ready: $($currentObservation.publishReady)",
            "Current observation diverges from emitted state: $($currentObservation.divergesFromEmittedState)",
            "Observation note: $($currentObservation.note)"
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
            "Current worktree state (last emitted): $($orchestrationState.worktreeState)",
            "Required published branch: $($orchestrationState.requiredPublishedBranch)",
            "Required worktree state: $($orchestrationState.requiredWorktreeState)",
            "Advisory publish ready: $($orchestrationState.publishReady)",
            "Current observed branch: $($currentObservation.branch)",
            "Current observed worktree state: $($currentObservation.worktreeState)",
            "Current observed publish ready: $($currentObservation.publishReady)",
            "Current observation diverges from emitted state: $($currentObservation.divergesFromEmittedState)",
            "Observation note: $($currentObservation.note)",
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
            "Next mandatory HITL digest run UTC: $($cycleState.nextMandatoryHitlDigestRunUtc)",
            "Current observed git worktree state: $($currentObservation.worktreeState)",
            "Current observation diverges from emitted state: $($currentObservation.divergesFromEmittedState)"
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
            "Current worktree state (last emitted): $($orchestrationState.worktreeState)",
            "Advisory publish ready: $($orchestrationState.publishReady)",
            "Current observed branch: $($currentObservation.branch)",
            "Current observed worktree state: $($currentObservation.worktreeState)",
            "Current observed publish ready: $($currentObservation.publishReady)",
            "Current observation diverges from emitted state: $($currentObservation.divergesFromEmittedState)",
            "Observation note: $($currentObservation.note)",
            "Reasons: $reasonText"
        )
    }
}

exit 0
