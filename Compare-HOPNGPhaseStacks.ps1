<#
.SYNOPSIS
Compares two artifacts by governed temporal phase-stack semantics.

.DESCRIPTION
Use `--left` and `--right` to compare two lawful Phase 3 artifacts under
explicit basis alignment, horizon, and temporal-state rules.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "compare-phase-stacks" @RemainingArgs
exit $LASTEXITCODE
