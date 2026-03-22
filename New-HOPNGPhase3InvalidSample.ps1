<#
.SYNOPSIS
Creates an intentionally invalid Phase 3 sample `.hopng` artifact set.

.DESCRIPTION
Use this wrapper to create a signed Phase 3 artifact whose temporal contract
fails deterministic phase derivation. It is intended for failure-path
verification, validator testing, and release-hardening checks.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase3-invalid-sample" @RemainingArgs
exit $LASTEXITCODE
