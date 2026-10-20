ARG BASE=mcr.microsoft.com/dotnet/sdk:11.0.100-rc.2-alpine3.24
FROM ${BASE}
RUN if [ -f /etc/alpine-release ]; then \
      apk add --no-cache build-base clang git icu-data-full python3 zlib-dev; \
    else \
      apt-get update && apt-get install -y --no-install-recommends clang git python3 zlib1g-dev && \
      rm -rf /var/lib/apt/lists/*; \
    fi
ENV PYTHONUTF8=1 PYTHONIOENCODING=utf-8 PYTHONDONTWRITEBYTECODE=1
USER 65532:65532
