FROM gittools/libgit2sharp-mono

MAINTAINER GitTools Maintainers <jake@ginnivan.net>
ARG GitVersionZip

# Add GitVersion

ADD ./releaseArtifacts/$GitVersionZip .
RUN apt-get install unzip
RUN unzip -d /usr/lib/GitVersion/ $GitVersionZip
RUN rm $GitVersionZip
WORKDIR /usr/lib/GitVersion/

# Libgit2 can't resolve relative paths, patch to absolute path
RUN sed -i 's|lib/linux/x86_64|/usr/lib/GitVersion/lib/linux/x86_64|g' /usr/lib/GitVersion/LibGit2Sharp.dll.config

RUN mkdir /repo
VOLUME /repo

ENTRYPOINT ["mono", "./GitVersion.exe", "/repo"]