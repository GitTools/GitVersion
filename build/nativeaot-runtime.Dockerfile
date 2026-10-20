ARG BASE=ubuntu:24.04
FROM ${BASE}
RUN if [ -f /etc/alpine-release ]; then \
      apk add --no-cache git icu-data-full icu-libs libgcc libstdc++ openssl python3 zlib; \
    else \
      apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates git libgcc-s1 libicu74 libssl3t64 libstdc++6 python3 zlib1g && \
      rm -rf /var/lib/apt/lists/*; \
    fi
ENV HOME=/tmp/nativeaot-home PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
USER 65532:65532
WORKDIR /tmp
