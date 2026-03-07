<#
.SYNOPSIS
Runs the HDT CLI through the local .NET project.

.DESCRIPTION
This wrapper keeps PowerShell as the operator surface while delegating command execution to `Hdt.Cli`.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Command,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

$project = Join-Path $PSScriptRoot "Hdt.Cli\Hdt.Cli.csproj"
& dotnet run --project $project -- $Command @RemainingArgs
exit $LASTEXITCODE
