ARG REGISTRY='docker.io'
ARG DISTRO='debian-9'
ARG DOTNET_VERSION='3.1'
ARG VERSION='5.5.1'

FROM $REGISTRY/gittools/build-images:$DISTRO-sdk-$DOTNET_VERSION as installer
ARG contentFolder
ARG VERSION

WORKDIR /app
COPY $contentFolder/ ./
RUN dotnet tool install GitVersion.Tool --version $VERSION --tool-path /tools --add-source .

FROM $REGISTRY/gittools/build-images:$DISTRO-runtime-$DOTNET_VERSION

WORKDIR /tools
COPY --from=installer /tools .

ENTRYPOINT ["/tools/dotnet-gitversion"]
