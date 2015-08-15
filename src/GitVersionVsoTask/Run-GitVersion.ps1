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

$localBranch = $branch.Trim().Replace("refs/heads/", "").Replace("refs/","")

Set-Location $loc

$haveLocalBranch = $false
# VSO checks out the commit as a detached HEAD
# loop through and checkout all branches and the one we want
# then reset merge to get to the desired commit id
foreach ($remoteBranch in . git branch -r) {
  
  $lb = $remoteBranch.Trim().Replace("origin/", "")
  . git checkout $lb 2>&1 | write-host

  # keep track of if we have a matching local branch
  # pull requests will not show up here
  if($lb -eq $localBranch) {
     $haveLocalBranch = $true
  }
}  

# if we have a local branch, check that out and set it at the right commit
if($haveLocalBranch){
  . git checkout $localBranch 2>&1 | write-host 
  . git reset --merge $commitId 2>&1 | write-host
} else {
  # try to create a local branch from this commit - likely a PR
  . git checkout -b $localBranch $commitId 2>&1 | write-host
}



# Call GitVersion.exe
$gvPath = Get-PathToGitVersionExe
Write-Output "Path to GitVersion.exe = $gvPath"
Invoke-Tool -Path $gvPath -Arguments "`"$loc`" /output buildserver /nofetch /updateAssemblyInfo $updateAssms $additionalArguments"


