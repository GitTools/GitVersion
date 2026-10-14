#!/bin/sh
set -eu

# An overridden UID may not be able to use the image user's home directory.
if [ ! -d "$HOME" ] || [ ! -w "$HOME" ]; then
    HOME=$(mktemp -d)
    export HOME
fi

exec /tools/dotnet-gitversion "$@"
