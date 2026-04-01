<#
.SYNOPSIS
Creates an unlawful Phase 4 perspectival-support `.hopng` artifact set.

.DESCRIPTION
Use this wrapper to create a deterministic invalid Phase 4 perspectival
artifact through the CLI `new-phase4-invalid-perspectival-sample` command.
The generated artifact is signed but violates the support-only entry contract
so validation can exercise counterfeit or overclaim failure paths.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-invalid-perspectival-sample" @RemainingArgs
exit $LASTEXITCODE
