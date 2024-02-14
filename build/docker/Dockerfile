ARG REGISTRY='docker.io'
ARG DISTRO='debian.11'
ARG DOTNET_VERSION='6.0'
ARG VERSION='5.12.0'

FROM $REGISTRY/gittools/build-images:$DISTRO-sdk-$DOTNET_VERSION as installer
ARG nugetFolder
ARG VERSION

WORKDIR /nuget
COPY $nugetFolder/ ./
RUN dotnet tool install GitVersion.Tool --version $VERSION --tool-path /tools --add-source .

FROM $REGISTRY/gittools/build-images:$DISTRO-runtime-$DOTNET_VERSION

WORKDIR /tools
COPY --from=installer /tools .
RUN git config --global --add safe.directory '*'

ENTRYPOINT ["/tools/dotnet-gitversion"]
