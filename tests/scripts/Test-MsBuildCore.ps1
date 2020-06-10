param(
    [parameter(Mandatory=$true, Position=0)][string] $version,
    [parameter(Mandatory=$true, Position=1)][string] $nugetPath,
    [parameter(Mandatory=$true, Position=2)][string] $repoPath,
    [parameter(Mandatory=$true, Position=3)][string] $targetframework
)

$result = dotnet build $repoPath --source $nugetPath --source https://api.nuget.org/v3/index.json -p:GitVersionTaskVersion=$version -p:TargetFramework=$targetframework *>&1;

if($LASTEXITCODE -eq 0) {
    & "dotnet" $repoPath/build/$targetframework/app.dll;
} else {
    Write-Output $result
}
