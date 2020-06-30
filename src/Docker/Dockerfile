ARG DISTRO='debian-9'
ARG DOTNET_VERSION='2.1'

FROM gittools/build-images:$DISTRO-sdk-$DOTNET_VERSION as installer
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./
RUN dotnet tool install GitVersion.Tool --tool-path /tools --add-source .

FROM gittools/build-images:$DISTRO-runtime-$DOTNET_VERSION

WORKDIR /tools
COPY --from=installer /tools .

ENTRYPOINT ["/tools/dotnet-gitversion"]
