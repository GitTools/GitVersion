FROM mcr.microsoft.com/dotnet/core/runtime:2.1-nanoserver-sac2016
LABEL maintainers="GitTools Maintainers"
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["dotnet", "GitVersion.dll"]
