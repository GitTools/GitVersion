#!/usr/bin/sh
# sh /scripts/test-msbuild-task.sh --version 5.7.1-beta1.56 --nugetPath /nuget --repoPath /repo/tests/integration/core --targetframework net5.0
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
    --targetframework)
        targetframework="$2"
        ;;
    esac
    shift
done

result=$(dotnet build $repoPath --source $nugetPath --source https://api.nuget.org/v3/index.json -p:GitVersionMsBuildVersion=$version -p:TargetFrameworks=$targetframework) # >/dev/null
status=$?
if test $status -eq 0
then
    dotnet $repoPath/build/$targetframework/app.dll;
else
    echo $result
fi
