<#
.SYNOPSIS
Creates a lawful Phase 4 deferred perspectival support sample.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-deferred-perspectival-sample" @RemainingArgs
exit $LASTEXITCODE
