param(
    [parameter(Mandatory=$true, Position=0)][string] $version,
    [parameter(Mandatory=$true, Position=0)][string] $runtime,
    [parameter(Mandatory=$true, Position=1)][string] $repoPath
)

$result = tar -xvpf "/native/gitversion-$runtime-$version.tar.gz" -C "/native" | out-null;

if($LASTEXITCODE -eq 0) {
    & "/native/gitversion" $repoPath /showvariable FullSemver;
} else {
    Write-Output $result
}
