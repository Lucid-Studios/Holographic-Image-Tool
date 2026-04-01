<#
.SYNOPSIS
Creates a lawful Phase 4 perspectival peer support sample.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-perspectival-peer-sample" @RemainingArgs
exit $LASTEXITCODE
