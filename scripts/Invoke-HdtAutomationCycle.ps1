<#
.SYNOPSIS
Runs the first end-to-end HDT-local automation cycle.

.DESCRIPTION
This script is the parent automation lane for HDT. It wraps the posture-aware
repo check primitive, emits release-candidate receipts into a local `.audit`
tree, keeps live status surfaces current, and optionally writes an operator
digest when the digest window is due or explicitly forced.

The lane is manual-first and scheduler-ready. It strengthens local artifact
evidence, validation, inspection, and comparison without claiming wider-stack
promotion or runtime authority.
#>
param(
    [ValidateSet("Initial", "Formal", "Closing", "Approved")]
    [string]$DevelopmentPosture,

    [switch]$ForceDigest,

    [string]$AuditRoot,

    [string]$RepoChecksScriptPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cyclePolicyPath = Join-Path $repoRoot "build\local-automation-cycle.json"
$taskingPolicyPath = Join-Path $repoRoot "build\local-automation-tasking.json"
$orchestrationPolicyPath = Join-Path $repoRoot "build\master-thread-orchestration.json"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Get-DefaultCyclePolicy {
    [pscustomobject]@{
        schemaVersion = 1
        defaultDevelopmentPosture = "Closing"
        manualFirst = $true
        schedulerReady = $true
        stewardModelVersion = 1
        workReportCadenceHours = 1
        localReleaseCandidateCadenceHours = 6
        mandatoryHitlDigestCadenceHours = 24
        workReportOutputRoot = ".audit/runs/work-reports"
        releaseCandidateOutputRoot = ".audit/runs/release-candidates"
        digestOutputRoot = ".audit/runs/release-digests"
        statePath = ".audit/state/local-automation-cycle.json"
        taskingStatusJsonPath = ".audit/state/local-automation-tasking-status.json"
        taskingStatusMarkdownPath = ".audit/state/local-automation-tasking-status.md"
        masterThreadOrchestrationStatusJsonPath = ".audit/state/master-thread-orchestration-status.json"
        masterThreadOrchestrationStatusMarkdownPath = ".audit/state/master-thread-orchestration-status.md"
        operatorContinuityInstructionsPath = "docs/OPERATOR_CONTINUITY_INSTRUCTIONS.md"
        repoChecksScriptPath = "scripts/Invoke-HdtRepoChecks.ps1"
        mechanicalContinuationStatuses = @("candidate-ready", "hitl-required")
        blockedStatus = "blocked"
        statusPolicy = [pscustomobject]@{
            candidateReadyPostures = @("Initial", "Formal", "Closing")
            hitlRequiredPostures = @("Approved")
        }
        advisoryOrchestration = [pscustomobject]@{
            requiredPublishedBranch = "main"
            requiredWorktreeState = "clean"
        }
        rootStanding = [pscustomobject]@{
            laneName = "HDT Local Automation Lane"
            oanTechStackRoot = "D:\OAN Tech Stack"
            documentationRepoRoot = "D:\Documentation Repo"
            laneWriteRoot = "__REPO_ROOT__"
            oanTargetSurface = "D:\OAN Tech Stack"
            documentationSourceSet = @(
                "D:\Documentation Repo\architecture\oan-tech-stack-build-interlace-summary.md"
            )
            approvedDocumentationFeeds = @()
            admittedWriteRoots = @(
                "__REPO_ROOT__",
                "__AUDIT_ROOT__"
            )
            verificationCommands = @(
                ".\scripts\Invoke-HdtRepoChecks.ps1 -DevelopmentPosture Closing"
            )
            contractBarriers = @(
                "Governance widening remains HITL-governed.",
                "Final build admission remains outside the HDT local lane.",
                "Writes remain bounded to admitted roots."
            )
            completionTarget = "The HDT local automation lane is lawfully complete when its operator surface, receipt surface, and bounded verification chain are executable and receipted for the intended OAN Tech Stack environment, with only human-governed admission or promotion remaining."
        }
        stewardModel = [pscustomobject]@{
            currentStage = "S1 WitnessSteward"
            canonicalLoop = @(
                "reconcile-root-standing",
                "compile",
                "classify",
                "judge",
                "promote-when-lawful",
                "emit-receipts-and-notices",
                "continue-unless-stop-condition"
            )
        }
        interlacedBoundary = "HDT local automation strengthens local artifact evidence, validation, inspection, and comparison without claiming wider-stack promotion or runtime authority."
    }
}

function Get-DefaultTaskingPolicy {
    [pscustomobject]@{
        schemaVersion = 1
        formalSurfaceMarkdownPath = "docs/HDT_AUTOMATION_LANE.md"
        statusJsonPath = ".audit/state/local-automation-tasking-status.json"
        statusMarkdownPath = ".audit/state/local-automation-tasking-status.md"
        activeTaskMapId = "hdt-automation-lane-v1"
        tasks = @(
            [pscustomobject]@{
                id = "root-standing-reconciliation"
                label = "Root Standing Reconciliation"
            },
            [pscustomobject]@{
                id = "repo-check-cycle"
                label = "Repo Check Cycle"
            },
            [pscustomobject]@{
                id = "triad-emission"
                label = "Triad Emission"
            },
            [pscustomobject]@{
                id = "work-reporting"
                label = "Work Reporting"
            },
            [pscustomobject]@{
                id = "digest-surface"
                label = "Digest Surface"
            },
            [pscustomobject]@{
                id = "promotion-watch"
                label = "Promotion Watch"
            },
            [pscustomobject]@{
                id = "orchestration-status"
                label = "Orchestration Status"
            }
        )
    }
}

function Get-DefaultOrchestrationPolicy {
    [pscustomobject]@{
        schemaVersion = 1
        formalSurfaceMarkdownPath = "docs/HDT_AUTOMATION_LANE.md"
        statusJsonPath = ".audit/state/master-thread-orchestration-status.json"
        statusMarkdownPath = ".audit/state/master-thread-orchestration-status.md"
        requiredPublishedBranch = "main"
        requiredWorktreeState = "clean"
        allowedContinuationStatuses = @("candidate-ready", "hitl-required")
        blockedStatus = "blocked"
        handoffEligibilityRule = "Branch alignment and clean-worktree signals are advisory in the first HDT lane. They are recorded for later handoff eligibility and do not block the local cycle by themselves."
        codexAutomationSupport = [pscustomobject]@{
            nativeRunOnceSupported = $false
            supportState = "manual-first-scheduler-ready"
            reason = "The first HDT automation lane is a local manual conveyor with scheduler-ready cadence fields and live state surfaces, not unattended native run-once orchestration."
        }
    }
}

function Read-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return (Get-Content -Path $Path -Raw | ConvertFrom-Json)
}

function Resolve-HdtManagedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathSpec,

        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$EffectiveAuditRoot
    )

    if ([System.IO.Path]::IsPathRooted($PathSpec)) {
        return $PathSpec
    }

    $normalized = $PathSpec.Replace("/", "\")
    if ($normalized -eq ".audit") {
        return $EffectiveAuditRoot
    }

    if ($normalized.StartsWith(".audit\")) {
        $suffix = $normalized.Substring(7)
        return Join-Path $EffectiveAuditRoot $suffix
    }

    return Join-Path $RepoRoot $normalized
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        return
    }

    $item = Get-Item -LiteralPath $Path
    if (-not $item.PSIsContainer) {
        throw "Path '$Path' already exists as a file and cannot be used as a directory."
    }
}

function Ensure-ParentDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $parent = Split-Path -Parent $Path
    if ([string]::IsNullOrWhiteSpace($parent)) {
        return
    }

    Ensure-Directory -Path $parent
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    Ensure-ParentDirectory -Path $Path
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [object]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 16
    Write-Utf8File -Path $Path -Content $json
}

function Initialize-AuditRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RequestedRoot
    )

    $effectiveRoot = $RequestedRoot
    $usedFallback = $false
    $fallbackReason = $null

    try {
        if (Test-Path -LiteralPath $RequestedRoot) {
            $item = Get-Item -LiteralPath $RequestedRoot
            if (-not $item.PSIsContainer) {
                throw "Requested audit root '$RequestedRoot' is a file."
            }
        }
        else {
            Ensure-Directory -Path $RequestedRoot
        }
    }
    catch {
        $usedFallback = $true
        $fallbackReason = $_.Exception.Message
        $effectiveRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-automation-fallback-" + [guid]::NewGuid().ToString("N"))
        Ensure-Directory -Path $effectiveRoot
    }

    [pscustomobject]@{
        RequestedRoot = $RequestedRoot
        EffectiveRoot = $effectiveRoot
        UsedFallback = $usedFallback
        FallbackReason = $fallbackReason
    }
}

function Get-ShortOperatorCascadeForm {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DocumentationPath
    )

    $lines = Get-Content -Path $DocumentationPath
    $headingIndex = [Array]::IndexOf($lines, "## Short Operator Cascade Form")
    if ($headingIndex -lt 0) {
        throw "Unable to locate '## Short Operator Cascade Form' in '$DocumentationPath'."
    }

    $buffer = New-Object System.Collections.Generic.List[string]
    for ($i = $headingIndex + 1; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -like "## *") {
            break
        }

        $buffer.Add($line)
    }

    while ($buffer.Count -gt 0 -and [string]::IsNullOrWhiteSpace($buffer[0])) {
        $buffer.RemoveAt(0)
    }

    while ($buffer.Count -gt 0 -and [string]::IsNullOrWhiteSpace($buffer[$buffer.Count - 1])) {
        $buffer.RemoveAt($buffer.Count - 1)
    }

    return ($buffer.ToArray() -join [Environment]::NewLine).Trim()
}

function Invoke-RepoChecks {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath,

        [Parameter(Mandatory = $true)]
        [string]$Posture
    )

    $startedUtc = [DateTime]::UtcNow
    $output = & $ScriptPath -DevelopmentPosture $Posture 2>&1
    $endedUtc = [DateTime]::UtcNow
    $outputText = ($output | Out-String).Trim()
    $exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }

    [pscustomobject]@{
        startedUtc = $startedUtc.ToString("o")
        endedUtc = $endedUtc.ToString("o")
        durationMs = [Math]::Round(($endedUtc - $startedUtc).TotalMilliseconds)
        exitCode = $exitCode
        succeeded = ($exitCode -eq 0)
        output = $outputText
    }
}

function Get-GitMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $branch = (& git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($branch)) {
        $branch = "unknown"
    }

    $headSha = (& git -C $RepoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($headSha)) {
        $headSha = "unknown"
    }

    $shortSha = (& git -C $RepoRoot rev-parse --short HEAD 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($shortSha)) {
        $shortSha = "nosha"
    }

    $statusLines = @(& git -C $RepoRoot status --short 2>$null)
    if ($LASTEXITCODE -ne 0) {
        $statusLines = @()
    }

    $untrackedCount = @($statusLines | Where-Object { $_ -match '^\?\?' }).Count
    $modifiedCount = $statusLines.Count - $untrackedCount
    $isClean = ($statusLines.Count -eq 0)

    [pscustomobject]@{
        branch = $branch
        headSha = $headSha
        shortSha = $shortSha
        statusLines = $statusLines
        modifiedCount = $modifiedCount
        untrackedCount = $untrackedCount
        isClean = $isClean
        worktreeState = if ($isClean) { "clean" } else { "dirty" }
    }
}

function Get-CycleStatus {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$CyclePolicy,

        [Parameter(Mandatory = $true)]
        [string]$Posture,

        [Parameter(Mandatory = $true)]
        [bool]$RepoChecksSucceeded,

        [string[]]$FailureReasons
    )

    if (-not $RepoChecksSucceeded -or $FailureReasons.Count -gt 0) {
        return $CyclePolicy.blockedStatus
    }

    if ($CyclePolicy.statusPolicy.hitlRequiredPostures -contains $Posture) {
        return "hitl-required"
    }

    return "candidate-ready"
}

function Get-RecommendedAction {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status
    )

    switch ($Status) {
        "candidate-ready" { return "continue-mechanically" }
        "hitl-required" { return "review-required-before-adoption" }
        default { return "resolve-blocker" }
    }
}

function Should-WriteDigest {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$CyclePolicy,

        [Parameter()]
        [psobject]$PreviousState,

        [Parameter(Mandatory = $true)]
        [string]$CurrentStatus,

        [Parameter(Mandatory = $true)]
        [DateTime]$NowUtc,

        [Parameter(Mandatory = $true)]
        [bool]$ForceRequested
    )

    if ($ForceRequested) {
        return $true
    }

    if ($CurrentStatus -eq $CyclePolicy.blockedStatus) {
        return $true
    }

    if ($null -eq $PreviousState) {
        return $true
    }

    if ($PreviousState.status -ne $CurrentStatus) {
        return $true
    }

    if ([string]::IsNullOrWhiteSpace($PreviousState.lastDigestEmittedUtc)) {
        return $true
    }

    try {
        $lastDigestUtc = [DateTime]::Parse($PreviousState.lastDigestEmittedUtc).ToUniversalTime()
    }
    catch {
        return $true
    }

    return (($NowUtc - $lastDigestUtc).TotalHours -ge [double]$CyclePolicy.mandatoryHitlDigestCadenceHours)
}

function Should-WriteWorkReport {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$CyclePolicy,

        [Parameter()]
        [psobject]$PreviousState,

        [Parameter(Mandatory = $true)]
        [string]$CurrentStatus,

        [Parameter(Mandatory = $true)]
        [DateTime]$NowUtc,

        [Parameter(Mandatory = $true)]
        [bool]$ForceRequested
    )

    if ($ForceRequested) {
        return $true
    }

    if ($null -eq $PreviousState) {
        return $true
    }

    if ($PreviousState.status -ne $CurrentStatus) {
        return $true
    }

    if ([string]::IsNullOrWhiteSpace($PreviousState.lastWorkReportEmittedUtc)) {
        return $true
    }

    try {
        $lastReportUtc = [DateTime]::Parse($PreviousState.lastWorkReportEmittedUtc).ToUniversalTime()
    }
    catch {
        return $true
    }

    return (($NowUtc - $lastReportUtc).TotalHours -ge [double]$CyclePolicy.workReportCadenceHours)
}

function Resolve-TemplateValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value,

        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$AuditRoot
    )

    return $Value.Replace("__REPO_ROOT__", $RepoRoot).Replace("__AUDIT_ROOT__", $AuditRoot)
}

function Resolve-TemplateArray {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Values,

        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$AuditRoot
    )

    $resolved = New-Object System.Collections.Generic.List[string]
    foreach ($value in $Values) {
        $resolved.Add((Resolve-TemplateValue -Value ([string]$value) -RepoRoot $RepoRoot -AuditRoot $AuditRoot))
    }

    return $resolved.ToArray()
}

function Get-RootStandingSnapshot {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$CyclePolicy,

        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$EffectiveAuditRoot,

        [Parameter(Mandatory = $true)]
        [DateTime]$NowUtc
    )

    $rootStanding = $CyclePolicy.rootStanding
    $laneWriteRoot = Resolve-TemplateValue -Value ([string]$rootStanding.laneWriteRoot) -RepoRoot $RepoRoot -AuditRoot $EffectiveAuditRoot
    $admittedWriteRoots = Resolve-TemplateArray -Values @($rootStanding.admittedWriteRoots) -RepoRoot $RepoRoot -AuditRoot $EffectiveAuditRoot

    $docSources = New-Object System.Collections.Generic.List[object]
    foreach ($source in @($rootStanding.documentationSourceSet)) {
        $resolvedSource = Resolve-TemplateValue -Value ([string]$source) -RepoRoot $RepoRoot -AuditRoot $EffectiveAuditRoot
        $docSources.Add([ordered]@{
            path = $resolvedSource
            exists = (Test-Path -LiteralPath $resolvedSource)
        })
    }

    [ordered]@{
        evaluatedUtc = $NowUtc.ToString("o")
        laneName = $rootStanding.laneName
        oanTechStackRoot = [ordered]@{
            path = $rootStanding.oanTechStackRoot
            exists = (Test-Path -LiteralPath $rootStanding.oanTechStackRoot)
        }
        documentationRepoRoot = [ordered]@{
            path = $rootStanding.documentationRepoRoot
            exists = (Test-Path -LiteralPath $rootStanding.documentationRepoRoot)
        }
        laneWriteRoot = [ordered]@{
            path = $laneWriteRoot
            exists = (Test-Path -LiteralPath $laneWriteRoot)
        }
        oanTargetSurface = $rootStanding.oanTargetSurface
        documentationSourceSet = $docSources.ToArray()
        approvedDocumentationFeeds = @($rootStanding.approvedDocumentationFeeds)
        admittedWriteRoots = $admittedWriteRoots
        verificationCommands = @($rootStanding.verificationCommands)
        contractBarriers = @($rootStanding.contractBarriers)
        completionTarget = $rootStanding.completionTarget
    }
}

function Get-ResearchPosture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status,

        [Parameter(Mandatory = $true)]
        [string]$DevelopmentPosture
    )

    if ($Status -eq "blocked") {
        return "refused"
    }

    switch ($DevelopmentPosture) {
        "Initial" { return "observed" }
        "Formal" { return "inferred" }
        default { return "bonded" }
    }
}

function Get-WorkClassification {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status
    )

    switch ($Status) {
        "candidate-ready" { return "advancing_surface" }
        "hitl-required" { return "held_candidate" }
        default { return "held_candidate" }
    }
}

function Get-GovernanceAction {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status
    )

    switch ($Status) {
        "candidate-ready" { return "admit" }
        "hitl-required" { return "escalate" }
        default { return "hold" }
    }
}

$requestedAuditRoot = if ([string]::IsNullOrWhiteSpace($AuditRoot)) {
    Join-Path $repoRoot ".audit"
}
else {
    $AuditRoot
}

$auditRootInfo = Initialize-AuditRoot -RequestedRoot $requestedAuditRoot
$failureReasons = New-Object System.Collections.Generic.List[string]
if ($auditRootInfo.UsedFallback) {
    $failureReasons.Add("Requested audit root could not be used: $($auditRootInfo.FallbackReason)")
}

$cyclePolicy = Get-DefaultCyclePolicy
$taskingPolicy = Get-DefaultTaskingPolicy
$orchestrationPolicy = Get-DefaultOrchestrationPolicy

try {
    $cyclePolicy = Read-JsonFile -Path $cyclePolicyPath
}
catch {
    $failureReasons.Add("Cycle policy load failed; using built-in defaults: $($_.Exception.Message)")
}

try {
    $taskingPolicy = Read-JsonFile -Path $taskingPolicyPath
}
catch {
    $failureReasons.Add("Tasking policy load failed; using built-in defaults: $($_.Exception.Message)")
}

try {
    $orchestrationPolicy = Read-JsonFile -Path $orchestrationPolicyPath
}
catch {
    $failureReasons.Add("Orchestration policy load failed; using built-in defaults: $($_.Exception.Message)")
}

if (-not $PSBoundParameters.ContainsKey("DevelopmentPosture") -or [string]::IsNullOrWhiteSpace($DevelopmentPosture)) {
    $DevelopmentPosture = $cyclePolicy.defaultDevelopmentPosture
}

$operatorContinuityPath = Resolve-HdtManagedPath -PathSpec $cyclePolicy.operatorContinuityInstructionsPath -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
$shortOperatorCascadeForm = $null
try {
    $shortOperatorCascadeForm = Get-ShortOperatorCascadeForm -DocumentationPath $operatorContinuityPath
}
catch {
    $failureReasons.Add("Unable to load short operator cascade form: $($_.Exception.Message)")
    $shortOperatorCascadeForm = "Operate under bounded cascade authorization for the active Holographic Data Tool milestone."
}

$effectiveRepoChecksScriptPath = if ([string]::IsNullOrWhiteSpace($RepoChecksScriptPath)) {
    Resolve-HdtManagedPath -PathSpec $cyclePolicy.repoChecksScriptPath -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
}
else {
    $RepoChecksScriptPath
}

if (-not (Test-Path -LiteralPath $effectiveRepoChecksScriptPath)) {
    $failureReasons.Add("Repo checks script '$effectiveRepoChecksScriptPath' was not found.")
}

$cycleStatePath = Resolve-HdtManagedPath -PathSpec $cyclePolicy.statePath -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
$previousState = $null
if (Test-Path -LiteralPath $cycleStatePath) {
    try {
        $previousState = Read-JsonFile -Path $cycleStatePath
    }
    catch {
        $failureReasons.Add("Previous cycle state is unreadable: $($_.Exception.Message)")
    }
}

$repoChecksReceipt = [pscustomobject]@{
    startedUtc = $null
    endedUtc = $null
    durationMs = 0
    exitCode = if (-not (Test-Path -LiteralPath $effectiveRepoChecksScriptPath)) { -1 } else { $null }
    succeeded = $false
    output = ""
}

if (Test-Path -LiteralPath $effectiveRepoChecksScriptPath) {
    try {
        $repoChecksReceipt = Invoke-RepoChecks -ScriptPath $effectiveRepoChecksScriptPath -Posture $DevelopmentPosture
    }
    catch {
        $repoChecksReceipt = [pscustomobject]@{
            startedUtc = [DateTime]::UtcNow.ToString("o")
            endedUtc = [DateTime]::UtcNow.ToString("o")
            durationMs = 0
            exitCode = if ($null -eq $LASTEXITCODE) { -1 } else { $LASTEXITCODE }
            succeeded = $false
            output = $_.Exception.Message
        }
    }
}

$repoChecksSucceeded = ($repoChecksReceipt.succeeded -eq $true)
if (-not $repoChecksSucceeded) {
    $failureReasons.Add("Repo checks did not complete successfully.")
}

$nowUtc = [DateTime]::UtcNow
$gitMetadata = Get-GitMetadata -RepoRoot $repoRoot
$rootStandingSnapshot = Get-RootStandingSnapshot -CyclePolicy $cyclePolicy -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot -NowUtc $nowUtc
$cycleStatus = Get-CycleStatus -CyclePolicy $cyclePolicy -Posture $DevelopmentPosture -RepoChecksSucceeded $repoChecksSucceeded -FailureReasons $failureReasons.ToArray()
$recommendedAction = Get-RecommendedAction -Status $cycleStatus
$researchPosture = Get-ResearchPosture -Status $cycleStatus -DevelopmentPosture $DevelopmentPosture
$workClassification = Get-WorkClassification -Status $cycleStatus
$governanceAction = Get-GovernanceAction -Status $cycleStatus
$stewardStage = $cyclePolicy.stewardModel.currentStage
$activeLoopPhase = "emit-receipts-and-notices"
$digestDue = Should-WriteDigest -CyclePolicy $cyclePolicy -PreviousState $previousState -CurrentStatus $cycleStatus -NowUtc $nowUtc -ForceRequested $ForceDigest.IsPresent
$workReportDue = Should-WriteWorkReport -CyclePolicy $cyclePolicy -PreviousState $previousState -CurrentStatus $cycleStatus -NowUtc $nowUtc -ForceRequested $ForceDigest.IsPresent

$bundleTimestamp = $nowUtc.ToString("yyyyMMddTHHmmssZ")
$bundleId = "$bundleTimestamp-$($gitMetadata.shortSha)"
$workReportOutputRoot = Resolve-HdtManagedPath -PathSpec $cyclePolicy.workReportOutputRoot -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
$releaseCandidateOutputRoot = Resolve-HdtManagedPath -PathSpec $cyclePolicy.releaseCandidateOutputRoot -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
$digestOutputRoot = Resolve-HdtManagedPath -PathSpec $cyclePolicy.digestOutputRoot -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
$taskingStatusJsonPath = Resolve-HdtManagedPath -PathSpec $cyclePolicy.taskingStatusJsonPath -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
$taskingStatusMarkdownPath = Resolve-HdtManagedPath -PathSpec $cyclePolicy.taskingStatusMarkdownPath -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
$orchestrationStatusJsonPath = Resolve-HdtManagedPath -PathSpec $cyclePolicy.masterThreadOrchestrationStatusJsonPath -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot
$orchestrationStatusMarkdownPath = Resolve-HdtManagedPath -PathSpec $cyclePolicy.masterThreadOrchestrationStatusMarkdownPath -RepoRoot $repoRoot -EffectiveAuditRoot $auditRootInfo.EffectiveRoot

$releaseBundlePath = Join-Path $releaseCandidateOutputRoot $bundleId
$manifestPath = Join-Path $releaseBundlePath "build-evidence-manifest.json"
$summaryPath = Join-Path $releaseBundlePath "build-evidence-summary.md"
$repoChecksReceiptPath = Join-Path $releaseBundlePath "repo-checks-receipt.json"
$gitWorktreeReceiptPath = Join-Path $releaseBundlePath "git-worktree-receipt.json"
$dopingHeaderJsonPath = Join-Path $releaseBundlePath "doping-header.json"
$dopingHeaderMarkdownPath = Join-Path $releaseBundlePath "doping-header.md"
$receiptJsonPath = Join-Path $releaseBundlePath "receipt.json"
$receiptMarkdownPath = Join-Path $releaseBundlePath "receipt.md"
$noticeJsonPath = Join-Path $releaseBundlePath "notice.json"
$noticeMarkdownPath = Join-Path $releaseBundlePath "notice.md"

$digestBundlePath = $null
$digestJsonPath = $null
$digestMarkdownPath = $null
if ($digestDue) {
    $digestBundlePath = Join-Path $digestOutputRoot $bundleId
    $digestJsonPath = Join-Path $digestBundlePath "release-candidate-digest.json"
    $digestMarkdownPath = Join-Path $digestBundlePath "release-candidate-digest.md"
}

$workReportBundlePath = $null
$workReportJsonPath = $null
$workReportMarkdownPath = $null
if ($workReportDue) {
    $workReportBundlePath = Join-Path $workReportOutputRoot $bundleId
    $workReportJsonPath = Join-Path $workReportBundlePath "work-report.json"
    $workReportMarkdownPath = Join-Path $workReportBundlePath "work-report.md"
}

$requiredBranch = $orchestrationPolicy.requiredPublishedBranch
$requiredWorktreeState = $orchestrationPolicy.requiredWorktreeState
$branchAligned = ($gitMetadata.branch -eq $requiredBranch)
$worktreeAligned = ($gitMetadata.worktreeState -eq $requiredWorktreeState)
$publishReady = ($branchAligned -and $worktreeAligned -and ($cycleStatus -in $orchestrationPolicy.allowedContinuationStatuses))

$orchestrationReasons = New-Object System.Collections.Generic.List[string]
if (-not $branchAligned) {
    $orchestrationReasons.Add("Current branch '$($gitMetadata.branch)' does not match required published branch '$requiredBranch'.")
}

if (-not $worktreeAligned) {
    $orchestrationReasons.Add("Current worktree state '$($gitMetadata.worktreeState)' does not match required worktree state '$requiredWorktreeState'.")
}

if ($cycleStatus -notin $orchestrationPolicy.allowedContinuationStatuses) {
    $orchestrationReasons.Add("Current cycle status '$cycleStatus' is not a continuation status.")
}

if ($orchestrationReasons.Count -eq 0) {
    $orchestrationReasons.Add("Branch, worktree, and cycle signals are aligned for later handoff eligibility.")
}

$manifest = [ordered]@{
    schemaVersion = 1
    bundleId = $bundleId
    createdUtc = $nowUtc.ToString("o")
    status = $cycleStatus
    developmentPosture = $DevelopmentPosture
    stewardStage = $stewardStage
    activeLoopPhase = $activeLoopPhase
    workClassification = $workClassification
    researchPosture = $researchPosture
    governanceAction = $governanceAction
    recommendedAction = $recommendedAction
    repoRoot = $repoRoot
    requestedAuditRoot = $auditRootInfo.RequestedRoot
    effectiveAuditRoot = $auditRootInfo.EffectiveRoot
    usedAuditRootFallback = $auditRootInfo.UsedFallback
    interlacedBoundary = $cyclePolicy.interlacedBoundary
    operatorCascadePrompt = $shortOperatorCascadeForm
    rootStanding = $rootStandingSnapshot
    repoChecks = [ordered]@{
        scriptPath = $effectiveRepoChecksScriptPath
        exitCode = $repoChecksReceipt.exitCode
        succeeded = $repoChecksSucceeded
        durationMs = $repoChecksReceipt.durationMs
    }
    git = [ordered]@{
        branch = $gitMetadata.branch
        headSha = $gitMetadata.headSha
        shortSha = $gitMetadata.shortSha
        worktreeState = $gitMetadata.worktreeState
        modifiedCount = $gitMetadata.modifiedCount
        untrackedCount = $gitMetadata.untrackedCount
    }
    digest = [ordered]@{
        emitted = $digestDue
        bundlePath = $digestBundlePath
    }
    workReport = [ordered]@{
        emitted = $workReportDue
        bundlePath = $workReportBundlePath
    }
    paths = [ordered]@{
        manifestPath = $manifestPath
        summaryPath = $summaryPath
        repoChecksReceiptPath = $repoChecksReceiptPath
        gitWorktreeReceiptPath = $gitWorktreeReceiptPath
        dopingHeaderJsonPath = $dopingHeaderJsonPath
        dopingHeaderMarkdownPath = $dopingHeaderMarkdownPath
        receiptJsonPath = $receiptJsonPath
        receiptMarkdownPath = $receiptMarkdownPath
        noticeJsonPath = $noticeJsonPath
        noticeMarkdownPath = $noticeMarkdownPath
        cycleStatePath = $cycleStatePath
        taskingStatusJsonPath = $taskingStatusJsonPath
        orchestrationStatusJsonPath = $orchestrationStatusJsonPath
        workReportJsonPath = $workReportJsonPath
        workReportMarkdownPath = $workReportMarkdownPath
    }
    failureReasons = $failureReasons.ToArray()
}

$repoChecksReceiptDocument = [ordered]@{
    schemaVersion = 1
    bundleId = $bundleId
    developmentPosture = $DevelopmentPosture
    status = $cycleStatus
    startedUtc = $repoChecksReceipt.startedUtc
    endedUtc = $repoChecksReceipt.endedUtc
    durationMs = $repoChecksReceipt.durationMs
    exitCode = $repoChecksReceipt.exitCode
    succeeded = $repoChecksSucceeded
    scriptPath = $effectiveRepoChecksScriptPath
    output = $repoChecksReceipt.output
}

$gitWorktreeReceiptDocument = [ordered]@{
    schemaVersion = 1
    bundleId = $bundleId
    capturedUtc = $nowUtc.ToString("o")
    branch = $gitMetadata.branch
    headSha = $gitMetadata.headSha
    shortSha = $gitMetadata.shortSha
    worktreeState = $gitMetadata.worktreeState
    modifiedCount = $gitMetadata.modifiedCount
    untrackedCount = $gitMetadata.untrackedCount
    statusLines = $gitMetadata.statusLines
}

$lastDigestEmittedUtc = if ($digestDue) { $nowUtc.ToString("o") } elseif ($null -ne $previousState) { $previousState.lastDigestEmittedUtc } else { $null }
$lastWorkReportEmittedUtc = if ($workReportDue) { $nowUtc.ToString("o") } elseif ($null -ne $previousState) { $previousState.lastWorkReportEmittedUtc } else { $null }
$latestDigestBundlePath = if ($digestDue) { $digestBundlePath } elseif ($null -ne $previousState) { $previousState.lastDigestBundlePath } else { $null }
$latestDigestBundleId = if ($digestDue) { $bundleId } elseif ($null -ne $previousState) { $previousState.lastDigestBundleId } else { $null }
$latestWorkReportBundlePath = if ($workReportDue) { $workReportBundlePath } elseif ($null -ne $previousState) { $previousState.lastWorkReportBundlePath } else { $null }
$latestWorkReportBundleId = if ($workReportDue) { $bundleId } elseif ($null -ne $previousState) { $previousState.lastWorkReportBundleId } else { $null }
$latestDigestReceiptPath = if ($null -ne $latestDigestBundlePath) { Join-Path $latestDigestBundlePath "release-candidate-digest.json" } else { $null }
$latestWorkReportReceiptPath = if ($null -ne $latestWorkReportBundlePath) { Join-Path $latestWorkReportBundlePath "work-report.json" } else { $null }
$cycleStateDocument = [ordered]@{
    schemaVersion = 1
    status = $cycleStatus
    developmentPosture = $DevelopmentPosture
    stewardStage = $stewardStage
    activeLoopPhase = $activeLoopPhase
    workClassification = $workClassification
    researchPosture = $researchPosture
    governanceAction = $governanceAction
    updatedUtc = $nowUtc.ToString("o")
    lastBundleId = $bundleId
    lastBundlePath = $releaseBundlePath
    lastDigestBundleId = $latestDigestBundleId
    lastDigestBundlePath = $latestDigestBundlePath
    lastDigestEmittedUtc = $lastDigestEmittedUtc
    lastWorkReportBundleId = $latestWorkReportBundleId
    lastWorkReportBundlePath = $latestWorkReportBundlePath
    lastWorkReportEmittedUtc = $lastWorkReportEmittedUtc
    digestDisposition = if ($digestDue) { "emitted" } else { "skipped-not-due" }
    workReportDisposition = if ($workReportDue) { "emitted" } else { "skipped-not-due" }
    recommendedAction = $recommendedAction
    repoChecksExitCode = $repoChecksReceipt.exitCode
    repoChecksSucceeded = $repoChecksSucceeded
    requestedAuditRoot = $auditRootInfo.RequestedRoot
    effectiveAuditRoot = $auditRootInfo.EffectiveRoot
    usedAuditRootFallback = $auditRootInfo.UsedFallback
    nextRecommendedWorkReportRunUtc = $nowUtc.AddHours([double]$cyclePolicy.workReportCadenceHours).ToString("o")
    nextRecommendedReleaseCandidateRunUtc = $nowUtc.AddHours([double]$cyclePolicy.localReleaseCandidateCadenceHours).ToString("o")
    nextMandatoryHitlDigestRunUtc = $nowUtc.AddHours([double]$cyclePolicy.mandatoryHitlDigestCadenceHours).ToString("o")
    git = [ordered]@{
        branch = $gitMetadata.branch
        shortSha = $gitMetadata.shortSha
        worktreeState = $gitMetadata.worktreeState
    }
    rootStanding = [ordered]@{
        laneName = $rootStandingSnapshot.laneName
        oanTechStackAvailable = $rootStandingSnapshot.oanTechStackRoot.exists
        documentationRepoAvailable = $rootStandingSnapshot.documentationRepoRoot.exists
        laneWriteRoot = $rootStandingSnapshot.laneWriteRoot.path
        admittedWriteRoots = $rootStandingSnapshot.admittedWriteRoots
    }
    operatorCascadePrompt = $shortOperatorCascadeForm
    interlacedBoundary = $cyclePolicy.interlacedBoundary
    failureReasons = $failureReasons.ToArray()
}

$taskStatuses = @(
    [ordered]@{
        id = "root-standing-reconciliation"
        status = if ($rootStandingSnapshot.oanTechStackRoot.exists -and $rootStandingSnapshot.documentationRepoRoot.exists -and $rootStandingSnapshot.laneWriteRoot.exists) { "reconciled" } else { "partial" }
        oanTechStackAvailable = $rootStandingSnapshot.oanTechStackRoot.exists
        documentationRepoAvailable = $rootStandingSnapshot.documentationRepoRoot.exists
    },
    [ordered]@{
        id = "repo-check-cycle"
        status = if ($repoChecksSucceeded) { "completed" } else { "blocked" }
        latestBundlePath = $releaseBundlePath
        latestReceiptPath = $repoChecksReceiptPath
    },
    [ordered]@{
        id = "triad-emission"
        status = "emitted"
        dopingHeaderPath = $dopingHeaderJsonPath
        receiptPath = $receiptJsonPath
        noticePath = $noticeJsonPath
    },
    [ordered]@{
        id = "work-reporting"
        status = if ($workReportDue) { "emitted" } else { "not-due" }
        latestBundlePath = $latestWorkReportBundlePath
        latestReceiptPath = $latestWorkReportReceiptPath
    },
    [ordered]@{
        id = "digest-surface"
        status = if ($digestDue) { "emitted" } else { "not-due" }
        latestBundlePath = $latestDigestBundlePath
        latestReceiptPath = $latestDigestReceiptPath
    },
    [ordered]@{
        id = "promotion-watch"
        status = $cycleStatus
        recommendedAction = $recommendedAction
    },
    [ordered]@{
        id = "orchestration-status"
        status = if ($publishReady) { "advisory-ready" } else { "advisory-misaligned" }
        publishReady = $publishReady
    }
)

$taskingStatusDocument = [ordered]@{
    schemaVersion = 1
    updatedUtc = $nowUtc.ToString("o")
    activeTaskMapId = $taskingPolicy.activeTaskMapId
    cycleStatus = $cycleStatus
    recommendedAction = $recommendedAction
    developmentPosture = $DevelopmentPosture
    lastBundleId = $bundleId
    lastBundlePath = $releaseBundlePath
    tasks = $taskStatuses
}

$orchestrationStatusDocument = [ordered]@{
    schemaVersion = 1
    updatedUtc = $nowUtc.ToString("o")
    cycleStatus = $cycleStatus
    branch = $gitMetadata.branch
    worktreeState = $gitMetadata.worktreeState
    requiredPublishedBranch = $requiredBranch
    requiredWorktreeState = $requiredWorktreeState
    branchAligned = $branchAligned
    worktreeAligned = $worktreeAligned
    publishReady = $publishReady
    reasons = $orchestrationReasons.ToArray()
    supportState = $orchestrationPolicy.codexAutomationSupport.supportState
    handoffEligibilityRule = $orchestrationPolicy.handoffEligibilityRule
}

$summaryLines = @(
    "# HDT Release Candidate Bundle",
    "",
    "- Bundle id: ``$bundleId``",
    "- Status: ``$cycleStatus``",
    "- Development posture: ``$DevelopmentPosture``",
    "- Steward stage: ``$stewardStage``",
    "- Work classification: ``$workClassification``",
    "- Governance action: ``$governanceAction``",
    "- Recommended action: ``$recommendedAction``",
    "- Repo checks exit code: ``$($repoChecksReceipt.exitCode)``",
    "- Branch: ``$($gitMetadata.branch)``",
    "- Worktree state: ``$($gitMetadata.worktreeState)``",
    "- Digest emitted: ``$digestDue``",
    "- Work report emitted: ``$workReportDue``",
    "",
    "## Boundary",
    "",
    $cyclePolicy.interlacedBoundary,
    "",
    "## Short Operator Cascade Form",
    "",
    $shortOperatorCascadeForm
)

if ($failureReasons.Count -gt 0) {
    $summaryLines += @(
        "",
        "## Failure Reasons",
        ""
    )

    foreach ($reason in $failureReasons) {
        $summaryLines += "- $reason"
    }
}

$digestDocument = if ($digestDue) {
    [ordered]@{
        schemaVersion = 1
        bundleId = $bundleId
        emittedUtc = $nowUtc.ToString("o")
        status = $cycleStatus
        developmentPosture = $DevelopmentPosture
        recommendedAction = $recommendedAction
        lastBundlePath = $releaseBundlePath
        hitlStillRequired = ($cycleStatus -eq "hitl-required")
        note = if ($cycleStatus -eq "hitl-required") {
            "Mechanical verification is complete for the requested posture, but explicit HITL approval is still required before adoption."
        }
        else {
            "The local cycle remains a bounded evidence lane and does not claim wider-stack promotion or runtime authority."
        }
        operatorCascadePrompt = $shortOperatorCascadeForm
    }
}
else {
    $null
}

$digestLines = if ($digestDue) {
    @(
        "# HDT Release Candidate Digest",
        "",
        "- Bundle id: ``$bundleId``",
        "- Status: ``$cycleStatus``",
        "- Development posture: ``$DevelopmentPosture``",
        "- Recommended action: ``$recommendedAction``",
        "- Last release-candidate bundle: ``$releaseBundlePath``",
        "",
        "## Note",
        "",
        $digestDocument.note,
        "",
        "## Short Operator Cascade Form",
        "",
        $shortOperatorCascadeForm
    )
}
else {
    @()
}

$taskingMarkdownLines = @(
    "# HDT Local Automation Tasking Status",
    "",
    "- Updated UTC: ``$($nowUtc.ToString("o"))``",
    "- Cycle status: ``$cycleStatus``",
    "- Development posture: ``$DevelopmentPosture``",
    "- Steward stage: ``$stewardStage``",
    "- Recommended action: ``$recommendedAction``",
    "",
    "## Tasks",
    "",
    "- Root standing reconciliation: ``$($taskStatuses[0].status)``",
    "- Repo check cycle: ``$($taskStatuses[1].status)``",
    "- Triad emission: ``$($taskStatuses[2].status)``",
    "- Work reporting: ``$($taskStatuses[3].status)``",
    "- Digest surface: ``$($taskStatuses[4].status)``",
    "- Promotion watch: ``$($taskStatuses[5].status)``",
    "- Orchestration status: ``$($taskStatuses[6].status)``"
)

$orchestrationMarkdownLines = @(
    "# HDT Master-Thread Orchestration Status",
    "",
    "- Updated UTC: ``$($nowUtc.ToString("o"))``",
    "- Cycle status: ``$cycleStatus``",
    "- Current branch: ``$($gitMetadata.branch)``",
    "- Current worktree state: ``$($gitMetadata.worktreeState)``",
    "- Required published branch: ``$requiredBranch``",
    "- Required worktree state: ``$requiredWorktreeState``",
    "- Advisory publish ready: ``$publishReady``",
    "",
    "## Reasons",
    ""
)

foreach ($reason in $orchestrationReasons) {
    $orchestrationMarkdownLines += "- $reason"
}

$dopingHeaderDocument = [ordered]@{
    schemaVersion = 1
    bundleId = $bundleId
    emittedUtc = $nowUtc.ToString("o")
    laneName = $rootStandingSnapshot.laneName
    stewardStage = $stewardStage
    activeLoopPhase = "reconcile-root-standing"
    currentStanding = $cycleStatus
    rootStanding = $rootStandingSnapshot
    canonicalLoop = @($cyclePolicy.stewardModel.canonicalLoop)
    contractBarriers = @($rootStandingSnapshot.contractBarriers)
    admittedWriteRoots = @($rootStandingSnapshot.admittedWriteRoots)
    operatorCascadePrompt = $shortOperatorCascadeForm
}

$receiptDocument = [ordered]@{
    schemaVersion = 1
    bundleId = $bundleId
    emittedUtc = $nowUtc.ToString("o")
    status = $cycleStatus
    developmentPosture = $DevelopmentPosture
    stewardStage = $stewardStage
    activeLoopPhase = $activeLoopPhase
    workClassification = $workClassification
    researchPosture = $researchPosture
    governanceAction = $governanceAction
    recommendedAction = $recommendedAction
    repoChecks = [ordered]@{
        exitCode = $repoChecksReceipt.exitCode
        succeeded = $repoChecksSucceeded
        durationMs = $repoChecksReceipt.durationMs
    }
    digestEmitted = $digestDue
    workReportEmitted = $workReportDue
    rootStanding = [ordered]@{
        oanTechStackAvailable = $rootStandingSnapshot.oanTechStackRoot.exists
        documentationRepoAvailable = $rootStandingSnapshot.documentationRepoRoot.exists
        laneWriteRootAvailable = $rootStandingSnapshot.laneWriteRoot.exists
    }
    failureReasons = $failureReasons.ToArray()
}

$activeHolds = if ($failureReasons.Count -gt 0) {
    [object[]]$failureReasons.ToArray()
}
else {
    [object[]]@($orchestrationReasons | Where-Object { $_ -notlike "*aligned*" })
}

$noticeDocument = [ordered]@{
    schemaVersion = 1
    bundleId = $bundleId
    emittedUtc = $nowUtc.ToString("o")
    status = $cycleStatus
    recommendedAction = $recommendedAction
    governanceAction = $governanceAction
    nextWorkReportRunUtc = $cycleStateDocument.nextRecommendedWorkReportRunUtc
    nextReleaseCandidateRunUtc = $cycleStateDocument.nextRecommendedReleaseCandidateRunUtc
    nextMandatoryHitlDigestRunUtc = $cycleStateDocument.nextMandatoryHitlDigestRunUtc
    hitlRequired = ($cycleStatus -eq "hitl-required")
    activeHolds = @($activeHolds)
    downstreamAssumptions = @(
        "OAN Tech Stack remains executable truth.",
        "Documentation Repo remains the first lawful compile surface.",
        "HDT remains a bounded local evidence lane."
    )
    approvedDocumentationFeeds = @($rootStandingSnapshot.approvedDocumentationFeeds)
}

$dopingHeaderLines = @(
    "# HDT Automation Doping Header",
    "",
    "- Bundle id: ``$bundleId``",
    "- Lane name: ``$($rootStandingSnapshot.laneName)``",
    "- Steward stage: ``$stewardStage``",
    "- Active loop phase before execution: ``reconcile-root-standing``",
    "- Current standing: ``$cycleStatus``",
    "- OAN Tech Stack available: ``$($rootStandingSnapshot.oanTechStackRoot.exists)``",
    "- Documentation Repo available: ``$($rootStandingSnapshot.documentationRepoRoot.exists)``",
    "- Lane write root: ``$($rootStandingSnapshot.laneWriteRoot.path)``",
    "",
    "## Contract Barriers",
    ""
)
foreach ($barrier in $rootStandingSnapshot.contractBarriers) {
    $dopingHeaderLines += "- $barrier"
}

$receiptLines = @(
    "# HDT Automation Receipt",
    "",
    "- Bundle id: ``$bundleId``",
    "- Status: ``$cycleStatus``",
    "- Development posture: ``$DevelopmentPosture``",
    "- Steward stage: ``$stewardStage``",
    "- Work classification: ``$workClassification``",
    "- Research posture: ``$researchPosture``",
    "- Governance action: ``$governanceAction``",
    "- Repo checks exit code: ``$($repoChecksReceipt.exitCode)``",
    "- Digest emitted: ``$digestDue``",
    "- Work report emitted: ``$workReportDue``"
)

if ($failureReasons.Count -gt 0) {
    $receiptLines += @(
        "",
        "## Failure Reasons",
        ""
    )

    foreach ($reason in $failureReasons) {
        $receiptLines += "- $reason"
    }
}

$noticeLines = @(
    "# HDT Automation Notice",
    "",
    "- Bundle id: ``$bundleId``",
    "- Status: ``$cycleStatus``",
    "- Recommended action: ``$recommendedAction``",
    "- Governance action: ``$governanceAction``",
    "- HITL required now: ``$($cycleStatus -eq "hitl-required")``",
    "- Next work report UTC: ``$($cycleStateDocument.nextRecommendedWorkReportRunUtc)``",
    "- Next release-candidate run UTC: ``$($cycleStateDocument.nextRecommendedReleaseCandidateRunUtc)``",
    "- Next mandatory HITL digest UTC: ``$($cycleStateDocument.nextMandatoryHitlDigestRunUtc)``"
)

$workReportDocument = if ($workReportDue) {
    [ordered]@{
        schemaVersion = 1
        bundleId = $bundleId
        emittedUtc = $nowUtc.ToString("o")
        currentLaneStanding = $cycleStatus
        stewardStage = $stewardStage
        activeLoopPhase = $activeLoopPhase
        majorArtifactsTouched = @(
            $releaseBundlePath,
            $manifestPath,
            $summaryPath,
            $dopingHeaderJsonPath,
            $receiptJsonPath,
            $noticeJsonPath
        ) + $(if ($digestDue) { @($digestJsonPath, $digestMarkdownPath) } else { @() })
        verificationChanges = @(
            "Repo checks exit code: $($repoChecksReceipt.exitCode)",
            "Digest emitted: $digestDue",
            "Work report emitted: $workReportDue"
        )
        activeHolds = @($activeHolds)
        nextLawfulAction = $recommendedAction
        hitlRequired = ($cycleStatus -eq "hitl-required")
    }
}
else {
    $null
}

$workReportLines = if ($workReportDue) {
    $lines = @(
        "# HDT Automation Work Report",
        "",
        "- Current lane standing: ``$cycleStatus``",
        "- Current steward stage: ``$stewardStage``",
        "- Active phase in loop: ``$activeLoopPhase``",
        "- Next lawful action: ``$recommendedAction``",
        "- HITL required now: ``$($cycleStatus -eq "hitl-required")``",
        "",
        "## Major Artifacts Touched",
        ""
    )

    foreach ($artifact in $workReportDocument.majorArtifactsTouched) {
        $lines += "- $artifact"
    }

    $lines += @(
        "",
        "## Verification Changes",
        ""
    )
    foreach ($item in $workReportDocument.verificationChanges) {
        $lines += "- $item"
    }

    $lines += @(
        "",
        "## Active Holds",
        ""
    )

    $activeHolds = @($workReportDocument.activeHolds)
    if ($activeHolds.Count -eq 0) {
        $lines += "- none"
    }
    else {
        foreach ($hold in $activeHolds) {
            $lines += "- $hold"
        }
    }

    $lines
}
else {
    @()
}

$writeSucceeded = $false
try {
    Ensure-Directory -Path $releaseBundlePath
    Write-JsonFile -Path $repoChecksReceiptPath -Value $repoChecksReceiptDocument
    Write-JsonFile -Path $gitWorktreeReceiptPath -Value $gitWorktreeReceiptDocument
    Write-JsonFile -Path $dopingHeaderJsonPath -Value $dopingHeaderDocument
    Write-Utf8File -Path $dopingHeaderMarkdownPath -Content ($dopingHeaderLines -join [Environment]::NewLine)
    Write-JsonFile -Path $receiptJsonPath -Value $receiptDocument
    Write-Utf8File -Path $receiptMarkdownPath -Content ($receiptLines -join [Environment]::NewLine)
    Write-JsonFile -Path $noticeJsonPath -Value $noticeDocument
    Write-Utf8File -Path $noticeMarkdownPath -Content ($noticeLines -join [Environment]::NewLine)
    Write-JsonFile -Path $cycleStatePath -Value $cycleStateDocument

    if ($digestDue) {
        Ensure-Directory -Path $digestBundlePath
        Write-JsonFile -Path $digestJsonPath -Value $digestDocument
        Write-Utf8File -Path $digestMarkdownPath -Content ($digestLines -join [Environment]::NewLine)
    }

    if ($workReportDue) {
        Ensure-Directory -Path $workReportBundlePath
        Write-JsonFile -Path $workReportJsonPath -Value $workReportDocument
        Write-Utf8File -Path $workReportMarkdownPath -Content ($workReportLines -join [Environment]::NewLine)
    }

    Write-JsonFile -Path $taskingStatusJsonPath -Value $taskingStatusDocument
    Write-Utf8File -Path $taskingStatusMarkdownPath -Content ($taskingMarkdownLines -join [Environment]::NewLine)
    Write-JsonFile -Path $orchestrationStatusJsonPath -Value $orchestrationStatusDocument
    Write-Utf8File -Path $orchestrationStatusMarkdownPath -Content ($orchestrationMarkdownLines -join [Environment]::NewLine)
    Write-Utf8File -Path $summaryPath -Content ($summaryLines -join [Environment]::NewLine)
    Write-JsonFile -Path $manifestPath -Value $manifest
    $writeSucceeded = $true
}
catch {
    $cycleStatus = $cyclePolicy.blockedStatus
    $recommendedAction = Get-RecommendedAction -Status $cycleStatus
    Write-Error "Automation cycle write failure: $($_.Exception.Message)"
}

Write-Host "==> HDT automation cycle complete"
Write-Host "    Status: $cycleStatus"
Write-Host "    Development posture: $DevelopmentPosture"
Write-Host "    Release bundle: $releaseBundlePath"
Write-Host "    Cycle state: $cycleStatePath"
if ($digestDue -and $null -ne $digestBundlePath) {
    Write-Host "    Digest bundle: $digestBundlePath"
}
else {
    Write-Host "    Digest bundle: not emitted"
}
if ($workReportDue -and $null -ne $workReportBundlePath) {
    Write-Host "    Work report bundle: $workReportBundlePath"
}
else {
    Write-Host "    Work report bundle: not emitted"
}
if ($auditRootInfo.UsedFallback) {
    Write-Host "    Audit root fallback: $($auditRootInfo.EffectiveRoot)"
}
Write-Host "    Recommended action: $recommendedAction"

if (-not $writeSucceeded -or $cycleStatus -eq $cyclePolicy.blockedStatus) {
    exit 1
}

exit 0
