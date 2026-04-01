<#
.SYNOPSIS
Creates a lawful Phase 4 participatory peer support sample.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-participatory-peer-sample" @RemainingArgs
exit $LASTEXITCODE
