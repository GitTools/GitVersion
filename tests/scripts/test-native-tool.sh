#!/usr/bin/sh
# sh /scripts/test-native-tool.sh --version 5.7.1-beta1-56 --runtime linux-musl-x64 /nuget --repoPath /repo
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
    esac
    shift
done

git config --global --add safe.directory '*'
result=$(tar -xvpf /native/gitversion-$runtime-$version.tar.gz -C /native) # >/dev/null
status=$?
if test $status -eq 0
then
    /native/gitversion $repoPath /showvariable FullSemver;
else
    echo $result
fi
