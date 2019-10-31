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
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

Write-Host "Preparing to run build script..."
$DotNetInstallerUri = 'https://dot.net/v1/dotnet-install.ps1';
$DotNetUnixInstallerUri = 'https://dot.net/v1/dotnet-install.sh'
$DotNetChannel = 'LTS'
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

[string] $CakeVersion = ''
[string[]] $DotNetVersion= ''
foreach($line in Get-Content (Join-Path $PSScriptRoot 'build.config'))
{
  if ($line -like 'CAKE_VERSION=*') {
      $CakeVersion = $line.SubString(13)
  }
  elseif ($line -like 'DOTNET_VERSION=*') {
      $DotNetVersion = $line.SubString(15).Split(',')
  }
}


if ([string]::IsNullOrEmpty($CakeVersion) -or [string]::IsNullOrEmpty($DotNetVersion)) {
    'Failed to parse Cake / .NET Core SDK Version'
    exit 1
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
    if ($IsMacOS -or $IsLinux) {
        $ScriptPath = Join-Path $InstallPath 'dotnet-install.sh'
        (New-Object System.Net.WebClient).DownloadFile($DotNetUnixInstallerUri, $ScriptPath);

        & bash $ScriptPath --version "$DotNetVersion" --install-dir "$InstallPath" --channel "$DotNetChannel" --no-path
    }
    else {
        $ScriptPath = Join-Path $InstallPath 'dotnet-install.ps1'
        (New-Object System.Net.WebClient).DownloadFile($DotNetInstallerUri, $ScriptPath);

        & $ScriptPath -Channel $DotNetChannel -Version $DotNetVersion -InstallDir $InstallPath;
    }
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

###########################################################################
# INSTALL CAKE
###########################################################################

# Make sure Cake has been installed.
[string] $CakeExePath = ''
[string] $CakeInstalledVersion = Get-Command dotnet-cake -ErrorAction SilentlyContinue  | % {&$_.Source --version}

if ($CakeInstalledVersion -eq $CakeVersion) {
    # Cake found locally
    $CakeExePath = (Get-Command dotnet-cake).Source
}
else {
    $CakePath = [System.IO.Path]::Combine($ToolPath, '.store', 'cake.tool', $CakeVersion) # Old PowerShell versions Join-Path only supports one child path

    $CakeExePath = (Get-ChildItem -Path $ToolPath -Filter "dotnet-cake*" -File| ForEach-Object FullName | Select-Object -First 1)


    if ((!(Test-Path -Path $CakePath -PathType Container)) -or (!(Test-Path $CakeExePath -PathType Leaf))) {

        if ((![string]::IsNullOrEmpty($CakeExePath)) -and (Test-Path $CakeExePath -PathType Leaf))
        {
            & dotnet tool uninstall --tool-path $ToolPath Cake.Tool
        }

        & dotnet tool install --tool-path $ToolPath --version $CakeVersion Cake.Tool
        if ($LASTEXITCODE -ne 0)
        {
            'Failed to install cake'
            exit 1
        }
        $CakeExePath = (Get-ChildItem -Path $ToolPath -Filter "dotnet-cake*" -File| ForEach-Object FullName | Select-Object -First 1)
    }
}

# ###########################################################################
# RUN BUILD SCRIPT
# ###########################################################################

# Build the argument list.

$env:ENABLED_UNIT_TESTS = !$SkipUnitTest
$Arguments = @{
    target=$Target;
    configuration=$Configuration;
    verbosity=$Verbosity;
    dryrun=$WhatIf;
    nuget_useinprocessclient=$true;
    docker_distro=$DockerDistro;
    docker_dotnetversion=$DockerDotnetVersion;
}.GetEnumerator() | ForEach-Object { "--{0}=`"{1}`"" -f $_.key, $_.value };

# Start Cake
Write-Host "Running build script..."

& "$CakeExePath" $Script --bootstrap
if ($LASTEXITCODE -eq 0)
{
    & "$CakeExePath" $Script $Arguments
}

if ($env:APPVEYOR) {
    $host.SetShouldExit($LASTEXITCODE)
}
exit $LASTEXITCODE
