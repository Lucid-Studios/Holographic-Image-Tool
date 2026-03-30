<#
.SYNOPSIS
Creates a lawful Phase 3 temporal divergent peer sample artifact.

.DESCRIPTION
Use `--output-dir` and `--name` to create a public-safe Phase 3 sample that
remains basis-aligned with the primary sample while exercising divergent
cross-artifact comparison behavior.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase3-divergent-peer-sample" @RemainingArgs
exit $LASTEXITCODE
