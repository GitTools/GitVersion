FROM debian:jessie-slim
MAINTAINER GitTools Maintainers <jake@ginnivan.net>

ENV MONO_VERSION 5.4.1.6
ENV GIT_VERSION 3.6.5

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/debian jessie/snapshots/$MONO_VERSION main" > /etc/apt/sources.list.d/mono-official.list \
  && apt-get update \
  && apt-get install -y mono-runtime mono-mcs unzip curl \
  && rm -rf /var/lib/apt/lists/* /tmp/*

WORKDIR /usr/lib/GitVersion/
RUN mkdir /repo
VOLUME /repo

ADD https://github.com/GitTools/GitVersion/releases/download/v$GIT_VERSION/GitVersion_$GIT_VERSION.zip /tmp/GitVersion.zip
RUN unzip -d /usr/lib/GitVersion/ /tmp/GitVersion.zip && \
    rm /tmp/GitVersion.zip && \
    sed -i 's|lib/linux/x86_64|/usr/lib/GitVersion/lib/linux/x86_64|g' /usr/lib/GitVersion/LibGit2Sharp.dll.config

ENTRYPOINT ["mono", "/usr/lib/GitVersion/GitVersion.exe", "/repo"]
