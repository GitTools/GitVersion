param(
    [ValidateNotNullOrEmpty()]
    [string] $Username = 'gittools',

    [Parameter(Mandatory)]
    [string] $Version,

    [string] $VersionZip = $Version,

    [string] $OSImageTag = '1709',

    [switch] $Publish
)

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

$repo = "$Username/gitversion-win"
$repoLatest = "$repo`:latest"
$repoVersion = "$repo`:$Version"
Write-Information "Building $repoVersion"
docker build -t $repoLatest -t $repoVersion --build-arg GV_VERSION=$Version --build-arg GV_VERSION_ZIP=$VersionZip --build-arg OS_IMAGE_TAG=$OSImageTag .

if ($Publish) {
    Write-Information "Pushing $repoVersion (including 'latest' tag)"
    docker push $repoVersion
    docker push $repoLatest
}
