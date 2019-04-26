FROM mcr.microsoft.com/dotnet/framework/runtime:4.7.2
LABEL maintainers="GitTools Maintainers"
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["GitVersion.exe"]
