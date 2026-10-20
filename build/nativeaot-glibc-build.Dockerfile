FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.2 AS sdk
FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS sdk10
FROM registry.access.redhat.com/ubi8/ubi:8.10
RUN dnf install -y clang gcc-c++ git libicu openssl-libs python3.11 zlib-devel && \
    dnf clean all && ln -s /usr/bin/python3.11 /usr/local/bin/python3
COPY --from=sdk /usr/share/dotnet /usr/share/dotnet
COPY --from=sdk10 /usr/share/dotnet/sdk /usr/share/dotnet/sdk
COPY --from=sdk10 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=sdk10 /usr/share/dotnet/templates /usr/share/dotnet/templates
ENV PATH=/usr/share/dotnet:$PATH DOTNET_ROOT=/usr/share/dotnet \
    PYTHONUTF8=1 PYTHONIOENCODING=utf-8 PYTHONDONTWRITEBYTECODE=1
USER 65532:65532
