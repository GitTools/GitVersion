#!/usr/bin/sh
# sh /scripts/test-msbuild-task.sh --version 7.0.0 --nugetPath /nuget --repoPath /repo/tests/integration --targetframework net10.0
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
    *)
        ;;
    esac
    shift
done

workPath=$(mktemp -d) || exit 1
trap 'rm -rf "$workPath"' EXIT
# Use the image's SDK without inheriting the checkout's global.json pin.
cd "$workPath" || exit 1
git config --global --add safe.directory '*'
set --
if test -n "$GITVERSION_RELEASE_COMMIT"
then
    releaseArguments="--no-normalize --commit $GITVERSION_RELEASE_COMMIT"
    if test -n "$GITVERSION_RELEASE_LABEL"
    then
        releaseArguments="$releaseArguments --override-config calculation.branches.main.label=$GITVERSION_RELEASE_LABEL"
    fi
    set -- "-p:GitVersion_CommandLineArguments=$releaseArguments"
fi
result=$(dotnet build "$repoPath/Program.cs" --output "$workPath/build" --source "$nugetPath" --source https://api.nuget.org/v3/index.json -p:GitVersionMsBuildVersion="$version" -p:TargetFramework="$targetframework" "$@")
status=$?
if test $status -eq 0
then
    # DockerTestRun validates stdout against the expected FullSemVer in
    # build/common/Utilities/DockerContextExtensions.cs. Keep app output on stdout.
    dotnet "$workPath/build/app.dll"
    status=$?
else
    echo "$result"
fi
exit "$status"
