<#
.SYNOPSIS
Creates a lawful Phase 4 restricted perspectival support sample.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-restricted-perspectival-sample" @RemainingArgs
exit $LASTEXITCODE
