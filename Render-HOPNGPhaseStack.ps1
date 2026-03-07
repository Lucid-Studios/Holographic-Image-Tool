<#
.SYNOPSIS
Render the Phase 3 diagnostic temporal stack for a `.hopng` artifact.

.DESCRIPTION
Invokes the Holographic Data Tool CLI `render-phase-stack` command.
Phase 3 Milestone 1 outputs a JSON-capable diagnostic stack and a simple
console summary. Prime-safe views expose temporal metadata and flags only;
privileged views may expose full temporal payloads when policy allows.

.EXAMPLE
.\Render-HOPNGPhaseStack.ps1 --path .\artifacts\temporal-sample.hopng.json --json

.EXAMPLE
.\Render-HOPNGPhaseStack.ps1 --path .\artifacts\temporal-sample.hopng.json --view prime --h 10
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "render-phase-stack" @RemainingArgs
exit $LASTEXITCODE
