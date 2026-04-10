ARG REGISTRY='docker.io'
ARG DISTRO='debian.12'
ARG DOTNET_VERSION='8.0'
ARG VERSION='6.4.0'

FROM $REGISTRY/gittools/build-images:$DISTRO-sdk-$DOTNET_VERSION as installer
ARG nugetFolder
ARG VERSION

WORKDIR /nuget
COPY $nugetFolder/ ./
RUN dotnet tool install GitVersion.Tool --version $VERSION --tool-path /tmp/tools --add-source .

FROM $REGISTRY/gittools/build-images:$DISTRO-runtime-$DOTNET_VERSION

WORKDIR /tmp/tools
COPY --from=installer /tmp/tools .
RUN git config --global --add safe.directory '*'

ENTRYPOINT ["/tmp/tools/dotnet-gitversion"]
