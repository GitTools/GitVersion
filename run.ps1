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
    [string]$Script = "run.cake",
    [string]$Target = "Default",
    [string]$Configuration = "Release",
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity = "Verbose",
    [Alias("DryRun","Noop")]
    [switch]$WhatIf,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

Write-Host "Preparing to run build script..."

if ($PSEdition -eq "Desktop") { $IsWindows = $true }

$CakeVersion = "0.30.0"

$DotNetChannel = "Current";
$DotNetInstaller = if ($IsWindows) { "dotnet-install.ps1" } else { "dotnet-install.sh" }
$DotNetInstallerUri = "https://dot.net/v1/$DotNetInstaller";
$DotNetVersion = (Get-Content ./src/global.json | ConvertFrom-Json).sdk.version;

$NugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

# SSL FIX
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;

# Make sure tools folder exists
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$ToolPath = Join-Path $PSScriptRoot "tools"
if (!(Test-Path $ToolPath)) {
    Write-Verbose "Creating tools directory..."
    New-Item -Path $ToolPath -Type directory | out-null
}

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################
function Remove-PathVariable([string]$VariableToRemove) {
    $path = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($path -ne $null) {
        $newItems = $path.Split(';', [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "User")
    }

    $path = [Environment]::GetEnvironmentVariable("PATH", "Process")
    if ($path -ne $null) {
        $newItems = $path.Split(';', [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "Process")
    }
}

# Get .NET Core CLI path if installed.
$FoundDotNetCliVersion = $null;
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $FoundDotNetCliVersion = dotnet --version;
}

if($FoundDotNetCliVersion -ne $DotNetVersion) {
    $InstallPath = Join-Path $PSScriptRoot ".dotnet"

    if (!(Test-Path $InstallPath)) {
        New-Item -ItemType Directory $InstallPath | Out-Null;
    }

    [string] $InstalledDotNetVersion = Get-ChildItem -Path ./.dotnet -File `
                                        | Where-Object { $_.Name -eq 'dotnet' -or $_.Name -eq 'dotnet.exe' } `
                                        | ForEach-Object { &$_.FullName --version }

    if ($InstalledDotNetVersion -ne $DotNetVersion)
    {
        (New-Object System.Net.WebClient).DownloadFile($DotNetInstallerUri, "$InstallPath/$DotNetInstaller");
        $Cmd = "$InstallPath/$DotNetInstaller -Channel $DotNetChannel -Version $DotNetVersion -InstallDir $InstallPath -NoPath"
        if (!$IsWindows) { $Cmd = "bash $Cmd" }
        Invoke-Expression "& $Cmd"

        Remove-PathVariable "$InstallPath"
        $env:PATH = "$InstallPath;$env:PATH"
    }
}

# Temporarily skip verification of addins.
$env:CAKE_SETTINGS_SKIPVERIFICATION='true'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1

###########################################################################
# INSTALL NUGET
###########################################################################

# Make sure nuget.exe exists.
$NugetPath = Join-Path $ToolPath "nuget.exe"
if (!(Test-Path $NugetPath)) {
    Write-Host "Downloading NuGet.exe..."
    (New-Object System.Net.WebClient).DownloadFile($NugetUrl, $NugetPath);
}

###########################################################################
# INSTALL CAKE
###########################################################################

# Make sure Cake has been installed.
Write-Host "Installing Cake..."
$CakeInstallPath = Join-Path $PSScriptRoot ".cake"
if (!(Test-Path $CakeInstallPath)) {
    New-Item -ItemType Directory $CakeInstallPath | Out-Null;
    Invoke-Expression "& dotnet tool install Cake.Tool --version $CakeVersion --tool-path $CakeInstallPath"
}

# ###########################################################################
# # RUN BUILD SCRIPT
# ###########################################################################

# Build the argument list.
$Arguments = @{
    target=$Target;
    configuration=$Configuration;
    verbosity=$Verbosity;
    dryrun=$WhatIf;
    nuget_useinprocessclient=$true;
}.GetEnumerator() | ForEach-Object { "--{0}=`"{1}`"" -f $_.key, $_.value };

# Start Cake
Write-Host "Running build script..."

$Cmd = "$CakeInstallPath/dotnet-cake $Script $Arguments"
Invoke-Expression "& $Cmd"

exit $LASTEXITCODE
