[CmdletBinding()]
param (
    [string]$variant,
    [string]$os
)
$configs = (Get-Content ./src/docker/docker.json | ConvertFrom-Json).($os)

$map = @{}
foreach($version in $configs.PSObject.Properties) {
    foreach($distro in $version.Value) {
        $entry = @{}
        $entry.Add("DOTNET_VERSION", $version.Name)
        $entry.Add("DISTRO", $distro)
        $map.Add("$($version.Name) $distro", [pscustomobject]$entry)
    }
}

$matrix = ([pscustomobject]$map | ConvertTo-Json -Compress) -replace "`"", "'"
Write-Host "##vso[task.setVariable variable=dockerConfigs;isOutput=true]$matrix"
