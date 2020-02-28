param(
    [parameter(Mandatory=$true, Position=0)][string] $version,
    [parameter(Mandatory=$true, Position=1)][string] $nugetPath,
    [parameter(Mandatory=$true, Position=2)][string] $repoPath,
    [parameter(Mandatory=$true, Position=3)][string] $toolPath
)

$result = dotnet tool install GitVersion.Tool --version $version --tool-path $toolPath --add-source $nugetPath | out-null;

if($LASTEXITCODE -eq 0) {
    & "$rootPrefix/gitversion/dotnet-gitversion" $repoPath /showvariable FullSemver;
} else {
    Write-Output $result
}
