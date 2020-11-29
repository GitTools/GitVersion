#!/usr/bin/pwsh
##########################################################################
# This is the Cake bootstrapper script for PowerShell.
# This file was downloaded from https://github.com/cake-build/resources
# Feel free to change this file to fit your needs.
##########################################################################

<#

.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.

.DESCRIPTION
This Powershell script will download NuGet if missing, restore NuGet tools (including Cake)
and execute your Cake build script with the parameters you provide.

.PARAMETER Script
The build script to execute.
.PARAMETER Target
The build script target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER DockerDistro
The docker ditro to use.
.PARAMETER DockerDotnetVersion
The dotnet version for docker to use.
.PARAMETER SkipUnitTest
Skip executing the tests.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER WhatIf
Performs a dry run of the build script.
No tasks will be executed.
.PARAMETER ScriptArgs
Remaining arguments are added here.

.LINK
https://cakebuild.net
#>

[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target = "Default",
    [string]$Configuration = "Release",
    [string]$DockerDistro = "",
    [string]$DockerDotnetVersion = "",
    [switch]$SkipUnitTest,
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity = "Verbose",
    [Alias("DryRun","Noop")]
    [switch]$WhatIf,
    [switch]$Exclusive,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

Write-Host "Preparing to run build script..."
$DotNetInstallerUri = 'https://dot.net/v1/dotnet-install.ps1';
$DotNetUnixInstallerUri = 'https://dot.net/v1/dotnet-install.sh'
$DotNetChannel = 'LTS'
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

[string[]] $DotNetVersion= ''
foreach($line in Get-Content (Join-Path $PSScriptRoot 'build.config'))
{
  if ($line -like 'DOTNET_VERSION=*') {
      $DotNetVersion = $line.SubString("DOTNET_VERSION=".Length).Split(',')
  }
}

# Make sure tools folder exists
$ToolPath = Join-Path $PSScriptRoot "tools"
if (!(Test-Path $ToolPath)) {
    Write-Verbose "Creating tools directory..."
    New-Item -Path $ToolPath -Type Directory -Force | out-null
}

# SSL FIX
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

Function Remove-PathVariable([string]$VariableToRemove)
{
    $SplitChar = ';'
    if ($IsMacOS -or $IsLinux) {
        $SplitChar = ':'
    }

    $path = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($path -ne $null)
    {
        $newItems = $path.Split($SplitChar, [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join($SplitChar, $newItems), "User")
    }

    $path = [Environment]::GetEnvironmentVariable("PATH", "Process")
    if ($path -ne $null)
    {
        $newItems = $path.Split($SplitChar, [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join($SplitChar, $newItems), "Process")
    }
}

Function Add-PathVariable([string]$PathToAdd)
{
    $SplitChar = ';'
    if ($IsMacOS -or $IsLinux) {
        $SplitChar = ':'
    }

    $env:PATH = "$($PathToAdd)$($SplitChar)$env:PATH"
}

Function Install-Dotnet($DotNetVersion)
{
    #if (!(Check-DotnetInstalled $DotNetVersion))
    #{
        if ($IsMacOS -or $IsLinux) {
            $ScriptPath = Join-Path $InstallPath 'dotnet-install.sh'
            if (!(Test-Path $ScriptPath)) {
                (New-Object System.Net.WebClient).DownloadFile($DotNetUnixInstallerUri, $ScriptPath);
            }

            & bash $ScriptPath --version "$DotNetVersion" --install-dir "$InstallPath" --channel "$DotNetChannel" --no-path
        }
        else {
            $ScriptPath = Join-Path $InstallPath 'dotnet-install.ps1'
            if (!(Test-Path $ScriptPath)) {
                (New-Object System.Net.WebClient).DownloadFile($DotNetInstallerUri, $ScriptPath);
            }

            & $ScriptPath -Channel $DotNetChannel -Version $DotNetVersion -InstallDir $InstallPath;
        }
    #}
}

Function Check-DotnetInstalled($version)
{
    if (Get-Command dotnet -errorAction SilentlyContinue)
    {
        $sdk =  dotnet --list-sdks
        $result = $sdk | ? { $v = $_.Split(" ")[0]; $v -eq $version }
        if ($result -ne $null)
        {
            Write-Host "The dotnet version $version was installed globally, not installing";
            return $true;
        }
    }
    return $false;
}

# Get .NET Core CLI path if installed.
$InstallPath = Join-Path $PSScriptRoot ".dotnet"
if (!(Test-Path $InstallPath)) {
    New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null;
}

foreach($version in $DotNetVersion)
{
    Install-Dotnet $version
}

Remove-PathVariable "$InstallPath"
Add-PathVariable "$InstallPath"
$env:DOTNET_ROOT=$InstallPath

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1

# Install cake local tool
dotnet tool restore

# ###########################################################################
# RUN BUILD SCRIPT
# ###########################################################################

# Build the argument list.

$env:ENABLED_UNIT_TESTS = !$SkipUnitTest
if ($env:ENABLED_DIAGNOSTICS -and $env:ENABLED_DIAGNOSTICS -eq $true) {
    Write-Host "Diagnostics enabled: Yes"
    $Verbosity = "Diagnostic"
} else {
    Write-Host "Diagnostics enabled: No"
}

$Arguments = @{
    target=$Target;
    configuration=$Configuration;
    verbosity=$Verbosity;
    dryrun=$WhatIf;
    exclusive=$Exclusive;
    nuget_useinprocessclient=$true;
    docker_distro=$DockerDistro;
    docker_dotnetversion=$DockerDotnetVersion;
}.GetEnumerator() | ForEach-Object { 
    if ($_.value -ne "") { "--{0}=`"{1}`"" -f $_.key, $_.value }
};

# Start Cake
Write-Host "Running build script..."

& dotnet cake $Script --bootstrap
if ($LASTEXITCODE -eq 0)
{
    & dotnet cake $Script $Arguments
}

if ($env:APPVEYOR) {
    $host.SetShouldExit($LASTEXITCODE)
}
exit $LASTEXITCODE
