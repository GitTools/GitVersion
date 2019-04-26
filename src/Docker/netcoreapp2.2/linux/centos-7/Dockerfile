FROM centos:7
LABEL maintainers="GitTools Maintainers"

ENV DOTNET_VERSION='2.2'
ARG contentFolder

# https://dotnet.microsoft.com/download/linux-package-manager/rhel/sdk-current
RUN rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm \
    && yum update cache

# if you need SDK use dotnet-sdk-$DOTNET_VERSION
RUN yum install -y dotnet-runtime-$DOTNET_VERSION.x86_64 unzip libgit2-devel.x86_64 \
    && yum clean all \
    && rm -rf /tmp/*

RUN ln -s /usr/lib64/libgit2.so /usr/lib64/libgit2-15e1193.so

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["dotnet", "GitVersion.dll"]
