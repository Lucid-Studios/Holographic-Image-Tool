<#
.SYNOPSIS
Validates a `.hopng` artifact set.

.DESCRIPTION
Returns `0` for a valid artifact and a deterministic non-zero validation code when required structure, trust material, or lawful relation fails.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "validate" @RemainingArgs
exit $LASTEXITCODE
