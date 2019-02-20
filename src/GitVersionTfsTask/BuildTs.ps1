param (
)

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir
Write-Host $dir
Try
{
    & npm install
    & node_modules/.bin/tsc
}
Finally
{
    Pop-Location
}
