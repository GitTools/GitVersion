FROM mono:5.14
LABEL maintainers="GitTools Maintainers"
ARG contentFolder

RUN sed -i '/jessie-updates/d' /etc/apt/sources.list && apt-get update && \
    apt-get install -y libgit2-dev && \
    ln -s /usr/lib/x86_64-linux-gnu/libgit2.so /lib/x86_64-linux-gnu/libgit2-15e1193.so

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["mono", "GitVersion.exe"]
