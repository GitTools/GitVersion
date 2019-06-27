FROM centos:7
LABEL maintainers="GitTools Maintainers"

ENV DOTNET_VERSION='2.1'
ARG contentFolder

# https://dotnet.microsoft.com/download/linux-package-manager/rhel/sdk-current
RUN rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm \
    && yum update cache

# if you need SDK use dotnet-sdk-$DOTNET_VERSION
RUN yum install -y dotnet-runtime-$DOTNET_VERSION.x86_64 \
    && yum clean all \
    && rm -rf /tmp/*

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["dotnet", "GitVersion.dll"]
