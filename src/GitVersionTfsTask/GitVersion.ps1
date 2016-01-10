param(
    [string]$updateAssemblyInfo,
    [string]$updateAssemblyInfoFilename,
    [string]$additionalArguments,
    [string]$gitVersionPath
)

Write-Verbose "Importing modules"
import-module "Microsoft.TeamFoundation.DistributedTask.Task.Internal"
import-module "Microsoft.TeamFoundation.DistributedTask.Task.Common"

$currentDirectory = Convert-Path .
$sourcesDirectory = $env:Build_SourcesDirectory

Write-Host (Get-LocalizedString -Key "Current Directory:  {0}" -ArgumentList $currentDirectory)
Write-Host (Get-LocalizedString -Key "Sources Directory:  {0}" -ArgumentList $sourcesDirectory)

Write-Host (Get-LocalizedString -Key "Check/Set GitVersion path")

if(!$gitVersionPath)
{
    $gitVersionPath = Join-Path -Path $currentDirectory -ChildPath "GitVersion.exe"
}

if (-not $gitVersionPath)
{
    throw (Get-LocalizedString -Key "Unable to locate {0}" -ArgumentList 'gitversion.exe')
}

$argsGitVersion = "$sourcesDirectory" + " /output buildserver /nofetch"

if($updateAssemblyInfo)
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
