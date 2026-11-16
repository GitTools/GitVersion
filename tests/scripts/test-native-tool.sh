#!/usr/bin/sh
# sh /scripts/test-native-tool.sh --version 7.0.0 --runtime linux-musl-x64 /nuget --repoPath /repo
while test "$#" -gt 0
do
    case $1 in
    --version)
        version="$2"
        ;;
    --runtime)
        runtime="$2"
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
git config --global --add safe.directory '*'
result=$(tar -xvf "/native/gitversion-$runtime-$version.tar.gz" -C "$toolPath")
status=$?
if test $status -eq 0
then
    "$toolPath/gitversion" "$repoPath" --show-variable FullSemver --no-cache
    status=$?
else
    echo "$result"
fi
exit "$status"
