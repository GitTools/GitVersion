ARG BASE=ubuntu:24.04
FROM ${BASE}
RUN if [ -f /etc/alpine-release ]; then \
      apk add --no-cache git icu-data-full icu-libs libgcc libstdc++ openssl python3 zlib; \
    elif command -v dnf >/dev/null; then \
      dnf install -y ca-certificates git libgcc libicu libstdc++ openssl-libs python3.11 zlib && \
      dnf clean all && ln -s /usr/bin/python3.11 /usr/local/bin/python3; \
    else \
      icu=libicu74; ssl=libssl3t64; \
      if [ "$(. /etc/os-release; echo "$VERSION_ID")" = 22.04 ]; then icu=libicu70; ssl=libssl3; fi; \
      apt-get update && apt-get install -y --no-install-recommends \
        "$icu" "$ssl" ca-certificates git libgcc-s1 libstdc++6 python3 zlib1g && \
      rm -rf /var/lib/apt/lists/*; \
    fi
ENV HOME=/tmp/nativeaot-home PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
USER 65532:65532
WORKDIR /tmp
