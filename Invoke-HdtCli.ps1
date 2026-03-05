param(
    [Parameter(Mandatory = $true)]
    [string]$Command,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

$project = Join-Path $PSScriptRoot "Hdt.Cli\Hdt.Cli.csproj"
& dotnet run --project $project -- $Command @RemainingArgs
exit $LASTEXITCODE
