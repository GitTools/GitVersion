param (
[string] $filePath,
[string] $major,
[string] $minor,
[string] $patch
)

if ([string]::IsNullOrWhiteSpace($filePath)) {
    throw "File path needs to be provided."
}

if ([string]::IsNullOrWhiteSpace($major)) {
    throw "Major version number needs to be provided."
}

if ([string]::IsNullOrWhiteSpace($minor)) {
    throw "Minor version number needs to be provided."
}

if ([string]::IsNullOrWhiteSpace($patch)) {
    throw "Patch level needs to be provided."
}

Write-Host "Set version in '$filePath' to $major.$minor.$patch"

# Get the task.json as a powershell object
$task = Get-Content -Raw -Path $filePath | ConvertFrom-Json

$task.version.Major = $major
$task.version.Minor = $minor
$task.version.Patch = $patch

# get this as a string again

ConvertTo-Json $task -Depth 100 | Set-Content -Path $filePath