param (
[string] $taskFolder
)

Push-Location $taskFolder
Try
{
    & tfx extension create --manifest-globs manifest.json
}
Finally
{
    Pop-Location
}
