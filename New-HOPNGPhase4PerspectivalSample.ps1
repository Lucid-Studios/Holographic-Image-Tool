<#
.SYNOPSIS
Creates a lawful Phase 4 perspectival-support `.hopng` artifact set.

.DESCRIPTION
Use this wrapper to create a support-only Phase 4 entry artifact through the
CLI `new-phase4-perspectival-sample` command. The generated artifact inherits
the approved Phase 3 temporal baseline and adds bounded perspectival engram
support scaffolding without asserting later-phase identity authority.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase4-perspectival-sample" @RemainingArgs
exit $LASTEXITCODE
