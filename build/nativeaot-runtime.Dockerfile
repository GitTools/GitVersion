ARG BASE=ubuntu:24.04
FROM ${BASE}
RUN if [ -f /etc/alpine-release ]; then \
      apk add --no-cache git python3 icu-libs icu-data-full libgcc libstdc++ zlib openssl; \
    else \
      apt-get update && apt-get install -y --no-install-recommends git python3 libicu74 libssl3t64 libgcc-s1 libstdc++6 zlib1g ca-certificates && \
      rm -rf /var/lib/apt/lists/*; \
    fi
ENV PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
USER 65532:65532
WORKDIR /tmp
