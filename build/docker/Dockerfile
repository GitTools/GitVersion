ARG REGISTRY='docker.io'
ARG DISTRO='debian.12'
ARG DOTNET_VERSION='10.0'
ARG VERSION='7.0.0'

FROM $REGISTRY/gittools/build-images:$DISTRO-sdk-$DOTNET_VERSION AS installer
USER root
ARG nugetFolder
ARG VERSION

WORKDIR /nuget
COPY $nugetFolder/ ./
RUN dotnet tool install GitVersion.Tool --version "$VERSION" --tool-path /tools --add-source . \
    && chmod -R a+rX /tools

FROM $REGISTRY/gittools/build-images:$DISTRO-runtime-$DOTNET_VERSION
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
