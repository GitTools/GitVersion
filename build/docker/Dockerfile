ARG REGISTRY='docker.io'
ARG DISTRO='debian.12'
ARG DOTNET_VERSION='10.0'
ARG VERSION='7.0.0'

FROM $REGISTRY/gittools/build-images:$DISTRO-sdk-$DOTNET_VERSION AS installer
ARG nugetFolder
ARG VERSION

WORKDIR /nuget
COPY $nugetFolder/ ./
RUN dotnet tool install GitVersion.Tool --version "$VERSION" --tool-path /tools --add-source .

FROM $REGISTRY/gittools/build-images:$DISTRO-runtime-$DOTNET_VERSION

WORKDIR /tools
COPY --from=installer /tools .
RUN git config --global --add safe.directory '*'

ENTRYPOINT ["/tools/dotnet-gitversion"]
