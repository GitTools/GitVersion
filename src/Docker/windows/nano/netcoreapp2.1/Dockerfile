FROM microsoft/dotnet:2.1-runtime
LABEL maintainers="GitTools Maintainers"
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["dotnet", "GitVersion.dll"]
