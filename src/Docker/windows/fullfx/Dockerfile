FROM microsoft/dotnet-framework:4.7.2-runtime
LABEL maintainers="GitTools Maintainers"
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["GitVersion.exe"]
