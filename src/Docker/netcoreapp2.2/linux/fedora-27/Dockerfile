FROM fedora:27
LABEL maintainers="GitTools Maintainers"

ENV DOTNET_VERSION='2.2'
ARG contentFolder

# https://dotnet.microsoft.com/download/linux-package-manager/rhel/sdk-current
RUN rpm -Uvh https://packages.microsoft.com/config/fedora/27/packages-microsoft-prod.rpm

# if you need SDK use dotnet-sdk-$DOTNET_VERSION
RUN yum install -y dotnet-runtime-$DOTNET_VERSION.x86_64 \
    && yum clean all \
    && rm -rf /tmp/*

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["dotnet", "GitVersion.dll"]
