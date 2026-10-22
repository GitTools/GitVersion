ARG SDK_IMAGE=docker.io/gittools/build-images:ubuntu.26.04-sdk-10.0@sha256:4e350fb8f690367fa3fd4885d5f5e94b0c302019a80b104fe3451d42c6098cd5
ARG RUNTIME_IMAGE=docker.io/gittools/build-images:ubuntu.26.04-runtime-10.0@sha256:3ef40b72012d1d05abdcbc4c8861d62ca6118b7696e6900a9fb6bc455d8f1df1
ARG VERSION='7.0.0'

FROM ${SDK_IMAGE} AS installer
USER root
ARG nugetFolder
ARG VERSION

WORKDIR /nuget
COPY $nugetFolder/ ./
RUN dotnet tool install GitVersion.Tool --version "$VERSION" --tool-path /tools --add-source . \
    && chmod -R a+rX /tools

FROM ${RUNTIME_IMAGE}
USER root

WORKDIR /tools
COPY --from=installer /tools .
COPY --chmod=755 entrypoint.sh /tools/entrypoint.sh
RUN mkdir -p /home/gittools \
    && chown 10001:10001 /home/gittools \
    && git config --system --add safe.directory '*'

ENV HOME=/home/gittools
USER 10001:10001

ENTRYPOINT ["/tools/entrypoint.sh"]
