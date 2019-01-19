FROM centos:7
HEALTHCHECK NONE

ENV DOTNET_VERSION='2.2' \
    GITVERSION_URL='https://github.com/GitTools/GitVersion/releases/download/v4.0.0/GitVersion-bin-coreclr-v4.0.0.zip' \
    GITVERSION_HOME=/opt/gitversion \
    GITVERSION_BIN=/usr/bin/gitversion

# https://dotnet.microsoft.com/download/linux-package-manager/rhel/sdk-current
RUN rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm \
    && yum update cache

# if you need SDK use dotnet-sdk-$DOTNET_VERSION
RUN yum install -y dotnet-runtime-$DOTNET_VERSION.x86_64 unzip libgit2-devel.x86_64 \
    && yum clean all

# https://github.com/GitTools/GitVersion
RUN mkdir $GITVERSION_HOME \
    && cd $GITVERSION_HOME \
    && curl -L -o "gitversion.zip" $GITVERSION_URL\
    && unzip gitversion.zip \
    && rm -f gitversion.zip \
    && ln -s /usr/lib64/libgit2.so /usr/lib64/libgit2-15e1193.so


RUN echo "#!/usr/bin/env bash" > $GITVERSION_BIN \
    && echo "dotnet $GITVERSION_HOME/GitVersion.dll \$@" >> $GITVERSION_BIN \
    && chmod +x $GITVERSION_BIN

WORKDIR /app

ENTRYPOINT ["gitversion"]