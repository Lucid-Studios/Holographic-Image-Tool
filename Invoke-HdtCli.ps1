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
$configuration = if ([string]::IsNullOrWhiteSpace($env:HDT_DOTNET_CONFIGURATION)) { "Release" } else { $env:HDT_DOTNET_CONFIGURATION }
& dotnet run --project $project --configuration $configuration --no-build --no-restore -- $Command @RemainingArgs
exit $LASTEXITCODE
