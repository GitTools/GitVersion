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
$commitId = $($env:BUILD_SOURCEVERSION)

$localBranch = $branch.Trim().Replace("refs/heads/", "")

Set-Location $loc

# VSO checks out the commit as a detached HEAD
# loop through and checkout all branches and the one we want
# then reset hard to get to the desired commit id
foreach ($remoteBranch in . git branch -r) {
  . git checkout $remoteBranch.Trim().Replace("origin/", "") 2>&1 | write-host

}  
. git checkout $localBranch 2>&1 | write-host 
. git reset --hard $commitId 2>&1 | write-host

# Call GitVersion.exe
$gvPath = Get-PathToGitVersionExe
Write-Output "Path to GitVersion.exe = $gvPath"
Invoke-Tool -Path $gvPath -Arguments "`"$loc`" /output buildserver /nofetch /updateAssemblyInfo $updateAssms $additionalArguments"


