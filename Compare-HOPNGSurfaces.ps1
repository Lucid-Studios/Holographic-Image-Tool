<#
.SYNOPSIS
Compares two artifacts by governed projection support.

.DESCRIPTION
Use `--left` and `--right` to compare a lawful artifact against incomplete or flattened artifacts through the public PowerShell surface.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "compare-surfaces" @RemainingArgs
exit $LASTEXITCODE
