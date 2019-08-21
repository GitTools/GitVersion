ARG DISTRO='debian-9'
ARG DOTNET_VERSION='2.1'
ARG DOTNET_VARIANT='runtime'

FROM gittools/build-images:$DISTRO-$DOTNET_VARIANT-$DOTNET_VERSION
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["dotnet", "/app/GitVersion.dll"]
