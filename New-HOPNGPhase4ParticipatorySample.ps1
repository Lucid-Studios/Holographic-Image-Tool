<#
.SYNOPSIS
Creates a lawful Phase 4 participatory-support `.hopng` artifact set.

.DESCRIPTION
Use this wrapper to create a support-only Phase 4 entry artifact through the
CLI `new-phase4-participatory-sample` command. The generated artifact inherits
the approved Phase 3 temporal baseline and adds bounded participatory engram
support scaffolding without asserting later-phase authority.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-participatory-sample" @RemainingArgs
exit $LASTEXITCODE
