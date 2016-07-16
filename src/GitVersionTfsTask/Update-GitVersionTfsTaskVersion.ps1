param (
[string] $filePath,
[string] $version
)

if ([string]::IsNullOrWhiteSpace($filePath)) {
    throw "File path needs to be provided."
}

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Version number needs to be provided."
}

Write-Host "Set version in '$filePath' to $version"

$ver = [Version]$version

# Get the task.json as a powershell object
$task = Get-Content -Raw -Path $filePath | ConvertFrom-Json

$task.version.Major = $ver.Major
$task.version.Minor = $ver.Minor
$task.version.Patch = $ver.Build

# get this as a string again

ConvertTo-Json $task -Depth 100 | Set-Content -Path $filePath