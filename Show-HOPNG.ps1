<#
.SYNOPSIS
Shows a `.hopng` artifact view.

.DESCRIPTION
Use `--view prime` for the public-safe inspection surface and `--view privileged` for the full governed artifact view.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "show" @RemainingArgs
exit $LASTEXITCODE
