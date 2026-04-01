<#
.SYNOPSIS
Creates an unlawful Phase 4 participatory-support `.hopng` artifact set.

.DESCRIPTION
Use this wrapper to create a deterministic invalid Phase 4 participatory
artifact through the CLI `new-phase4-invalid-participatory-sample` command.
The generated artifact is signed but violates branch and stance handoff rules
so validation can exercise entry-boundary failure paths.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-invalid-participatory-sample" @RemainingArgs
exit $LASTEXITCODE
