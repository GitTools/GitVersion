#!/usr/bin/sh
# sh /scripts/test-global-tool.sh --version 5.7.1-beta1.56 --nugetPath /nuget --repoPath /repo
while test "$#" -gt 0
do
    case $1 in
    --version)
        version="$2"
        ;;
    --nugetPath)
        nugetPath="$2"
        ;;
    --repoPath)
        repoPath="$2"
        ;;
    esac
    shift
done

git config --global --add safe.directory '*'
result=$(dotnet tool install GitVersion.Tool --version $version --tool-path /tools --add-source $nugetPath) # >/dev/null
status=$?
if test $status -eq 0
then
    /tools/dotnet-gitversion $repoPath /showvariable FullSemver /nocache
else
    echo $result
fi
