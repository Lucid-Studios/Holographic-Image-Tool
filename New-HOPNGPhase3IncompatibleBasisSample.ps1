<#
.SYNOPSIS
Creates a lawful Phase 3 sample with an intentionally incompatible primary comparison basis.

.DESCRIPTION
Use `--output-dir` and `--name` to create a public-safe Phase 3 sample that
validates cleanly on its own but should compare as `Incompatible` against the
main widened-horizon temporal reference lane.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

& (Join-Path $PSScriptRoot "Invoke-HdtCli.ps1") -Command "new-phase3-incompatible-basis-sample" @RemainingArgs
exit $LASTEXITCODE
