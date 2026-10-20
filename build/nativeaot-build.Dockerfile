ARG BASE=mcr.microsoft.com/dotnet/sdk:11.0.100-rc.2-alpine3.24
FROM ${BASE}
USER 0
RUN if [ -f /etc/alpine-release ]; then \
      apk add --no-cache clang build-base zlib-dev python3 git icu-data-full; \
    else \
      apt-get update && apt-get install -y --no-install-recommends clang zlib1g-dev python3 git && \
      rm -rf /var/lib/apt/lists/*; \
    fi
ENV PYTHONUTF8=1 PYTHONIOENCODING=utf-8 PYTHONDONTWRITEBYTECODE=1
