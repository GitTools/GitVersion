param (
    [string] [Parameter(Mandatory = $false)]
    $updateAssemblyInfo,
    [string] [Parameter(Mandatory = $false)]
    $additionalArguments
)

Write-Verbose 'Entering Run-GitVersion.ps1'

# Import the Task.Common dll that has all the cmdlets we need for Build
import-module "Microsoft.TeamFoundation.DistributedTask.Task.Common"

# Returns a path to GitVersion.exe
function Get-PathToGitVersionExe() { 
 	$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.ScriptBlock.File 
 	$targetPath = Join-Path -Path $PSScriptRoot -ChildPath "GitVersion.exe"  
 	return $targetPath 
} 


## Execution starts here


$updateAssms = Convert-String $updateAssemblyInfo Boolean

$loc = $($env:BUILD_SOURCESDIRECTORY)
$branch = $($env:BUILD_SOURCEBRANCH)

# Call GitVersion.exe
$gvPath = Get-PathToGitVersionExe
Write-Output "Path to GitVersion.exe = $gvPath"
Invoke-Tool -Path $gvPath -Arguments "`"$loc`" /output buildserver /nofetch /b $branch /updateAssemblyInfo $updateAssms $additionalArguments"


