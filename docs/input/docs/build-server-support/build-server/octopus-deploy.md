---
Order: 80
Title: Octopus Deploy
---

While not a build server, there are a few things to consider when using Octopus
Deploy with GitVersion.

 GitVersion follows [continuous delivery](../../reference/versioning-modes/continuous-delivery)
 versioning by default. This means builds will keep producing *the same version*
 with just metadata differing. For example, when you start a new release (say
 `1.0.0`) with git flow, the branch will start with a semver like
 `1.0.0-beta.1+0`, and the Octopus NuGet package will have a version of
 `1.0.0-beta0001`. As you commit changes to this release branch the *metadata*
 of the semver will increase like so: `1.0.0-beta.1+1`, `1.0.0-beta.1+2`, etc.
 However, the version of the corresponding Octopus NuGet package will retain the
 *same* `1.0.0-beta0001` version you started with. The problem is Octopus Deploy
 will prevent you from deploying these revisions because it sees the same NuGet
 package version and thinks nothing has changed.

Because Octopus Deploy uses NuGet like this you cannot continue to push
revisions in this manner without some intervention (or changes to GitVersion's
configuration). To work around this problem we have two possible options:

The solutions to this issue are a bit different for GitHubFlow and GitFlow.

## GitHubFlow Solutions

### Promote to Octopus feed

The first option is to keep the continuous delivery default in GitVersion,
depending on which build server you have this approach may or may not work for
you.  For instance in TFS Build vNext you cannot chain builds to publish
artifacts built in one build in another.

1. Your CI build creates the stable NuGet package
  - Do *not* publish this package into the Octopus nuget feed
2. When you want to push a package into the Octopus deployment pipeline you trigger the second build
  - it will either take the package built from the first build in the chain (your CI build?) or rebuild
  - It will publish that package into the Octopus deploy feed
  - The build then is *tagged* with the version, this will cause GitVersion to increment the version

This means that CI builds are *not* available to Octopus deploy, there will be a
manual build in your *build server* which pushes the package to Octopus deploy.

### Tag to release

Another simple option is to tag a stable version to release, the basic idea is:

1. GitVersion is set to continuous deployment mode, so master will create `-ci.x`
pre-release builds
1. CI Builds only create NuGet packages for stable builds
1. You tag master with a stable version of the next version then push it
1. The CI build triggers, GitVersion will always respect tags so you will get a
stable version
1. The stable package will be pushed to Octopus
1. Because of the tag, then next build will be incremented and will be producing
pre-release packages of the next build


#### Script to create the release

Here is an example script which could be used to tag the stable version, it uses
GitVersion to calculate the version so you just run `./CreateRelease.ps1` and it
will tag and push the tag.

``` powershell
[CmdletBinding()]
param()

##### Config #####
# Path to GitVersion.exe
$gitversion = "tools\GitVersion\GitVersion.exe"
function Create-AdditionalReleaseArtifacts
{
 param( [string]$Version )

 # Put any custom release logic here (like generating release notes?)
}
### END Config ###

$ErrorActionPreference = "Stop"
trap
{
   Pop-Location
   Write-Error "$_"
   Exit 1
}

Push-Location $PSScriptRoot

# Make sure there are no pending changes
$pendingChanges = & git status --porcelain
if ($pendingChanges -ne $null)
{
  throw 'You have pending changes, aborting release'
}

# Pull latest, fast-forward only so that it git stops if there is an error
& git fetch origin
& git checkout master
& git merge origin/master --ff-only

# Determine version to release
$output = & $gitversion /output json
$versionInfoJson = $output -join "`n"

$versionInfo = $versionInfoJson | ConvertFrom-Json
$stableVersion = $versionInfo.MajorMinorPatch

# Create release
Create-AdditionalReleaseArtifacts $stableVersion
# Always create a new commit because some CI servers cannot be triggered by just pushing a tag
& git commit -Am "Create release $stableVersion" --allow-empty
& git tag $stableVersion
if ($LASTEXITCODE -ne 0) {
    & git reset --hard HEAD^
    throw "No changes detected since last release"
}

& git push origin master --tags

Pop-Location
```

#### Sample build script (build.ps1)

``` powershell
[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"
trap
{
   Pop-Location
   Write-Error "$_"
   Exit 1
}
Push-Location $PSScriptRoot

# Tools
$gitversion = "tools\GitVersion\GitVersion.exe"
$octo = "tools\Octo\Octo.exe"
$nuget = "tools\NuGet\NuGet.exe"
# Calculate version
$output = & $gitversion /output json /l GitVersion.log /updateAssemblyInfo /nofetch
if ($LASTEXITCODE -ne 0) {
    Write-Verbose "$output"
    throw "GitVersion Exit Code: $LASTEXITCODE"
}

$versionInfoJson = $output -join "`n"
Write-Host $versionInfoJson

$versionInfo = $versionInfoJson | ConvertFrom-Json
$nugetVersion = $versionInfo.NuGetVersion

#Build your project here
msbuild MyProj.sln

# Only create nuget package for stable
if ($versionInfo.PreReleaseTag -eq '')
{
    Write-Host
    Write-Host "Creating a release" -ForegroundColor Magenta

    # You probably want to specify output directory too
    & $nuget pack "src\myProj\MyProj.nuspec" -version $nugetVersion
}
```

### Configure GitVersion to [increment per commit](../../more-info/incrementing-per-commit)

As mentioned above, this means you will burn multiple versions per release. This
might not be an issue for you, but can confuse consumers of your library as the
version has semantic meaning.
