param (
)
Set-PSDebug -Trace 1

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir
Write-Host $dir
Try
{
    & npm install
    & tsc
}
Finally
{
    Pop-Location
}
