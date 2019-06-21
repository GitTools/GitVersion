FROM mono:5.14
LABEL maintainers="GitTools Maintainers"
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["mono", "GitVersion.exe"]
