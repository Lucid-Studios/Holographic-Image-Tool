<#
.SYNOPSIS
Derives a governed Phase 2 projection trace.

.DESCRIPTION
This wrapper evaluates declared Phase 2 relation, projection, and legibility material and returns lawful, incomplete, or flattened outcomes.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "merge-layers" @RemainingArgs
exit $LASTEXITCODE
