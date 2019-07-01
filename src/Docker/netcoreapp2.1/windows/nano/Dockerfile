FROM gittools/build-images:nanoserver-1809-runtime-2.1
ARG contentFolder

WORKDIR /app
COPY $contentFolder/ ./

ENTRYPOINT ["dotnet", "GitVersion.dll"]
