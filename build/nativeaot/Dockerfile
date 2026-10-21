# .NET SDK 11.0.100
FROM mcr.microsoft.com/dotnet/sdk@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea AS sdk

# Build against the oldest supported glibc baseline, not the hosted runner's libc.
# UBI 8.10
FROM registry.access.redhat.com/ubi8/ubi@sha256:73071333d0cd7c526b6d7d01f0c9b709fd4a96c47853b1c9071e0a11284bd1da AS glibc
RUN dnf install -y clang gcc-c++ git libicu openssl-libs zlib-devel && dnf clean all
COPY --from=sdk /usr/share/dotnet /usr/share/dotnet
ENV PATH=/usr/share/dotnet:$PATH DOTNET_ROOT=/usr/share/dotnet DOTNET_ROLL_FORWARD=Major

# .NET SDK 11.0.100 on Alpine 3.24
FROM mcr.microsoft.com/dotnet/sdk@sha256:52556c0fb9d0f609ae9aeab9efeeba96f4acc2f7038f7696f2235ccbdbb8e290 AS musl
RUN apk add --no-cache build-base clang git icu-data-full zlib-dev
ENV DOTNET_ROLL_FORWARD=Major
