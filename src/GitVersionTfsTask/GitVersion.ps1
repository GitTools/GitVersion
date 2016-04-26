param(
    [string]$updateAssemblyInfo,
    [string]$updateAssemblyInfoFilename,
    [string]$additionalArguments,
    [string]$gitVersionPath
)

Write-Verbose "updateAssemblyInfo = $updateAssemblyInfo"
Write-Verbose "updateAssemblyInfoFilename = $updateAssemblyInfoFilename"
Write-Verbose "additionalArguments = $additionalArguments"
Write-Verbose "gitVersionPath = $gitVersionPath"

Write-Verbose "Importing modules"
import-module "Microsoft.TeamFoundation.DistributedTask.Task.Internal"
import-module "Microsoft.TeamFoundation.DistributedTask.Task.Common"

$updateAssemblyInfoFlag = Convert-String $updateAssemblyInfo Boolean
Write-Verbose "updateAssemblyInfo (converted) = $updateAssemblyInfoFlag"

$currentDirectory = Convert-Path .
$sourcesDirectory = $env:Build_SourcesDirectory

Write-Host (Get-LocalizedString -Key "Current Directory:  {0}" -ArgumentList $currentDirectory)
Write-Host (Get-LocalizedString -Key "Sources Directory:  {0}" -ArgumentList $sourcesDirectory)

if(!$gitVersionPath)
{
    $gitVersionPath = Join-Path -Path $currentDirectory -ChildPath "GitVersion.exe"
}

if (-not $gitVersionPath)
{
    throw (Get-LocalizedString -Key "Unable to locate {0}" -ArgumentList 'gitversion.exe')
}

$argsGitVersion = "$sourcesDirectory" + " /output buildserver /nofetch"

if($updateAssemblyInfoFlag)
{
  $argsGitVersion = $argsGitVersion + " /updateassemblyinfo"

  if(!$updateAssemblyInfoFilename)
  {
    $argsGitVersion = $argsGitVersion + " true"
  }
  else
  {
    $argsGitVersion = $argsGitVersion + " $updateAssemblyInfoFilename"
  }
}

if($additionalArguments)
{
  $argsGitVersion = $argsGitVersion + " $additionalArguments"
}

Write-Host (Get-LocalizedString -Key "Invoking GitVersion with {0}" -ArgumentList $argsGitVersion)

Invoke-Tool -Path $GitVersionPath -Arguments "$argsGitVersion"
