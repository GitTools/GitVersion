param (
[string] $filePath,
[string] $major,
[string] $minor,
[string] $patch
)

# Get the task.json as a powershell object
$task = Get-Content -Raw -Path $filePath | ConvertFrom-Json

$task.version.Major = $major
$task.version.Minor = $minor
$task.version.Patch = $patch

# get this as a string again

ConvertTo-Json $task -Depth 100 | Set-Content -Path $filePath