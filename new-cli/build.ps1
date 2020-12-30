#!/usr/bin/env pwsh
dotnet run --project build/Build.csproj -- $args
exit $LASTEXITCODE;