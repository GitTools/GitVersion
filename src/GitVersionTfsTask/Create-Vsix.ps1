param (
[string] $taskFolder
)

Push-Location $taskFolder
Try
{
    & npm install
    & tsc
    & tfx extension create --manifest-globs manifest.json
}
Finally
{
    Pop-Location
}
