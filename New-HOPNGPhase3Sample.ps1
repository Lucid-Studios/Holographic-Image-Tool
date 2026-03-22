<#
.SYNOPSIS
Creates a valid Phase 3 sample `.hopng` artifact set.

.DESCRIPTION
Use this wrapper to create a public-safe temporal sample carrier through the
CLI `new-phase3-sample` command. The generated artifact includes lawful
Phase 2 relational sidecars plus Phase 3 event, phase, policy, and optical
channel sidecars suitable for rendering and smoke verification.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase3-sample" @RemainingArgs
exit $LASTEXITCODE
