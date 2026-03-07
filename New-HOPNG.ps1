<#
.SYNOPSIS
Creates a new `.hopng` artifact set.

.DESCRIPTION
Use this wrapper to create a new Phase 1 artifact carrier through the public PowerShell surface.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new" @RemainingArgs
exit $LASTEXITCODE
