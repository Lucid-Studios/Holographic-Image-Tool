<#
.SYNOPSIS
Creates a lawful Phase 4 rejected participatory support sample.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-rejected-participatory-sample" @RemainingArgs
exit $LASTEXITCODE
