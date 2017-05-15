param (
)

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir
Try
{
    & npm install
    & tsc
}
Finally
{
    Pop-Location
}
