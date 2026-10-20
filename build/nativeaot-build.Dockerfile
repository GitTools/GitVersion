ARG BASE=mcr.microsoft.com/dotnet/sdk:11.0.100-rc.2-alpine3.24
FROM mcr.microsoft.com/dotnet/sdk:10.0.402-alpine3.24 AS sdk10
FROM ${BASE}
RUN if [ -f /etc/alpine-release ]; then \
      apk add --no-cache build-base clang git icu-data-full python3 zlib-dev; \
    else \
      apt-get update && apt-get install -y --no-install-recommends clang git python3 zlib1g-dev && \
      rm -rf /var/lib/apt/lists/*; \
    fi
ENV PYTHONUTF8=1 PYTHONIOENCODING=utf-8 PYTHONDONTWRITEBYTECODE=1
COPY --from=sdk10 /usr/share/dotnet/sdk /usr/share/dotnet/sdk
COPY --from=sdk10 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=sdk10 /usr/share/dotnet/templates /usr/share/dotnet/templates
USER 65532:65532
