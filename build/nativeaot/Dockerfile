FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.2 AS sdk

# Build against the oldest supported glibc baseline, not the hosted runner's libc.
FROM registry.access.redhat.com/ubi8/ubi:8.10 AS glibc
RUN dnf install -y clang gcc-c++ git libicu openssl-libs zlib-devel && dnf clean all
COPY --from=sdk /usr/share/dotnet /usr/share/dotnet
ENV PATH=/usr/share/dotnet:$PATH DOTNET_ROOT=/usr/share/dotnet DOTNET_ROLL_FORWARD=Major

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.2-alpine3.24 AS musl
RUN apk add --no-cache build-base clang git icu-data-full zlib-dev
ENV DOTNET_ROLL_FORWARD=Major
