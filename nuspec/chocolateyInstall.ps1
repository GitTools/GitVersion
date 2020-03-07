$packageName = "GitVersion.Portable"

$pp = Get-PackageParameters

if (!$pp.Version) { $pp.Version = $env:ChocolateyPackageVersion }

$version = $pp.Version

Write-Host "Installing version $version"
$url32 = "https://www.nuget.org/api/v2/package/GitVersion.CommandLine/$version";

$packageArgs = @{
    packageName    = $packageName
    url            = $url32
    unzipLocation  = Split-Path $MyInvocation.MyCommand.Definition
}

Install-ChocolateyZipPackage @packageArgs
$toolLocation = "$env:ChocolateyInstall\lib\$packageName\tools\"
Generate-BinFile "gitversion" "$toolLocation\GitVersion.exe"
