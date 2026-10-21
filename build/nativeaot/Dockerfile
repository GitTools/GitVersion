# .NET SDK 11.0.100-rc.2
FROM mcr.microsoft.com/dotnet/sdk@sha256:bfabf51e74c5a7cf2fc4eac852145db86f4867d475257d809518d6a2b0fe71e1 AS sdk

# Build against the oldest supported glibc baseline, not the hosted runner's libc.
# UBI 8.10
FROM registry.access.redhat.com/ubi8/ubi@sha256:73071333d0cd7c526b6d7d01f0c9b709fd4a96c47853b1c9071e0a11284bd1da AS glibc
RUN dnf install -y clang gcc-c++ git libicu openssl-libs zlib-devel && dnf clean all
COPY --from=sdk /usr/share/dotnet /usr/share/dotnet
ENV PATH=/usr/share/dotnet:$PATH DOTNET_ROOT=/usr/share/dotnet DOTNET_ROLL_FORWARD=Major

# .NET SDK 11.0.100-rc.2 on Alpine 3.24
FROM mcr.microsoft.com/dotnet/sdk@sha256:6bd5660fc4e5a9d1992eec9f3705346e866c609c2decc02d786feb073e582cb1 AS musl
RUN apk add --no-cache build-base clang git icu-data-full zlib-dev
ENV DOTNET_ROLL_FORWARD=Major
