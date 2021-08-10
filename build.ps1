#!/usr/bin/pwsh
<#
.PARAMETER Stage
The build stage to execute.
.PARAMETER Target
The build script target to run.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER WhatIf
Performs a dry run of the build script.
No tasks will be executed.
.PARAMETER ScriptArgs
Remaining arguments are added here.
#>

[CmdletBinding()]
Param(
    [ValidateSet("artifacts", "build", "docker", "docs", "publish", "release")]
    [string]$Stage = "build",
    [string]$Target = "Default",
    [string]$Verbosity = "Normal",
    [Alias("DryRun","Noop")]
    [switch]$WhatIf,
    [switch]$Exclusive,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

$env:DOTNET_ROLL_FORWARD="major"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1
$env:DOTNET_NOLOGO=$true

# ###########################################################################
# RUN BUILD SCRIPT
# ###########################################################################

# Build the argument list.
$Arguments = @{
    target=$Target;
    verbosity=$Verbosity;
    dryrun=$WhatIf;
    exclusive=$Exclusive;
    nuget_useinprocessclient=$true;
}.GetEnumerator() | ForEach-Object {
    if ($_.value -ne "") { "--{0}=`"{1}`"" -f $_.key, $_.value }
};

# Start Cake
Write-Host "Running build stage $Stage..."

& dotnet run --project build/$Stage/$Stage.csproj -- $Arguments

if ($env:APPVEYOR) {
    $host.SetShouldExit($LASTEXITCODE)
}
exit $LASTEXITCODE
