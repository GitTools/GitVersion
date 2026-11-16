#!/usr/bin/sh
# sh /scripts/test-global-tool.sh --version 7.0.0 --nugetPath /nuget --repoPath /repo
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
    *)
        ;;
    esac
    shift
done

toolPath=$(mktemp -d) || exit 1
trap 'rm -rf "$toolPath"' EXIT
# Use the image's SDK without inheriting the checkout's global.json pin.
cd "$toolPath" || exit 1
git config --global --add safe.directory '*'
result=$(dotnet tool install GitVersion.Tool --version "$version" --tool-path "$toolPath" --add-source "$nugetPath")
status=$?
if test $status -eq 0
then
    "$toolPath/dotnet-gitversion" "$repoPath" --show-variable FullSemver --no-cache
    status=$?
else
    echo "$result"
fi
exit "$status"
