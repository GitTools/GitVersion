param(
    [parameter(Mandatory=$true, Position=0)][string] $version,
    [parameter(Mandatory=$true, Position=1)][string] $nugetPath,
    [parameter(Mandatory=$true, Position=2)][string] $repoPath
)

$result = dotnet tool install GitVersion.Tool --version $version --tool-path /tools --add-source $nugetPath | out-null;

if($LASTEXITCODE -eq 0) {
    & "/tools/dotnet-gitversion" $repoPath /showvariable FullSemver;
} else {
    Write-Output $result
}
