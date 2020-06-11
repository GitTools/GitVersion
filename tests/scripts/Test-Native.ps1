param(
    [parameter(Mandatory=$true, Position=0)][string] $runtime,
    [parameter(Mandatory=$true, Position=1)][string] $repoPath
)

& "/native/$runtime/gitversion" $repoPath /showvariable FullSemver;
