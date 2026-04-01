<#
.SYNOPSIS
Compares two artifacts by governed Phase 4 engram-support semantics.

.DESCRIPTION
Use `--left` and `--right` to compare two support-bearing artifacts for root
or branch coherence, strengthened support, counterfeit pressure, and support
type compatibility.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "compare-engram-support" @RemainingArgs
exit $LASTEXITCODE
