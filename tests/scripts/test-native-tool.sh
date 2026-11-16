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
    set -- "$repoPath" --show-variable FullSemver --no-cache
    if test -n "$GITVERSION_RELEASE_COMMIT"
    then
        set -- "$@" --no-normalize --commit "$GITVERSION_RELEASE_COMMIT"
        if test -n "$GITVERSION_RELEASE_LABEL"
        then
            set -- "$@" --override-config "calculation.branches.main.label=$GITVERSION_RELEASE_LABEL"
        fi
    fi
    "$toolPath/gitversion" "$@"
    status=$?
else
    echo "$result"
fi
exit "$status"
