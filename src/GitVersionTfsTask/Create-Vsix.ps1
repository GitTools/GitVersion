param (
[string] $taskFolder
)

Push-Location $taskFolder
Try
{
    & npm install
    & npm run clean
    & npm run build
    & npm run package -- --version "$Env:GitVersion_SemVer" --noversiontransform
}
Finally
{
    Pop-Location
}
