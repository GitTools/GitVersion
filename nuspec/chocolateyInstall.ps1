$version = "5.1.3"
$packageName = "GitVersion.Portable"
$url32 = "https://github.com/GitTools/GitVersion/releases/download/$version/gitversion-windows-$version.tar.gz";

$packageArgs = @{
    packageName    = $packageName
    url            = $url32
    unzipLocation  = Split-Path $MyInvocation.MyCommand.Definition
}

$toolLocation = "$env:ChocolateyInstall\lib\$packageName\"
Install-ChocolateyZipPackage @packageArgs
$File = Get-ChildItem -File -Path $toolLocation -Filter *.tar
Get-ChocolateyUnzip -fileFullPath $File.FullName -destination $toolLocation

Generate-BinFile "gitversion" "$toolLocation\GitVersion.exe"
