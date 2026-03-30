<#
.SYNOPSIS
Creates a lawful Phase 3 temporal peer sample artifact.

.DESCRIPTION
Use `--output-dir` and `--name` to create a public-safe Phase 3 sample that
remains basis-aligned with the primary sample while exercising delayed
cross-artifact comparison behavior.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase3-peer-sample" @RemainingArgs
exit $LASTEXITCODE
