$version = "5.1.3"
$packageName = "GitVersion.Portable"
$url32 = "https://www.nuget.org/api/v2/package/GitVersion.CommandLine/$version";

$packageArgs = @{
    packageName    = $packageName
    url            = $url32
    unzipLocation  = Split-Path $MyInvocation.MyCommand.Definition
}

Install-ChocolateyZipPackage @packageArgs
$toolLocation = "$env:ChocolateyInstall\lib\$packageName\tools\"
Generate-BinFile "gitversion" "$toolLocation\GitVersion.exe"
