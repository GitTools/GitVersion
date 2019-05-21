FROM mcr.microsoft.com/dotnet/core/runtime:2.2-nanoserver-1809
LABEL maintainers="GitTools Maintainers"
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["dotnet", "GitVersion.dll"]
