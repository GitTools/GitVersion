ARG BASE=ubuntu:24.04
FROM ${BASE}
RUN set -eu; \
    if [ -f /etc/alpine-release ]; then \
      apk add --no-cache git icu-data-full icu-libs libgcc libstdc++ openssl python3 zlib; \
    elif command -v dnf >/dev/null; then \
      python=python3; \
      ssl=openssl-libs; \
      if [ "$(. /etc/os-release; echo "${VERSION_ID%%.*}")" = 8 ]; then python=python3.11; fi; \
      if [ "$(. /etc/os-release; echo "$ID")" = fedora ] && \
         [ "$(. /etc/os-release; echo "$VERSION_ID")" -ge 45 ]; then ssl=openssl3-libs; fi; \
      dnf install -y "$python" "$ssl" ca-certificates git libgcc libicu libstdc++ zlib; \
      dnf clean all; \
      if [ "$python" = python3.11 ]; then ln -s /usr/bin/python3.11 /usr/local/bin/python3; fi; \
    elif command -v tdnf >/dev/null; then \
      tdnf install -y ca-certificates git icu libgcc libstdc++ openssl python3 zlib && tdnf clean all; \
    elif command -v zypper >/dev/null; then \
      python=python3; \
      if [ "$(. /etc/os-release; echo "${VERSION_ID%%.*}")" = 15 ]; then python=python311; fi; \
      zypper --non-interactive install --no-recommends \
        "$python" ca-certificates git libicu-devel libopenssl-devel libstdc++6 zlib; \
      zypper clean --all; \
      if [ "$python" = python311 ]; then ln -s /usr/bin/python3.11 /usr/local/bin/python3; fi; \
    else \
      apt-get update && \
      icu="$(apt-cache search --names-only '^libicu[0-9]+$' | cut -d ' ' -f 1 | sort -V | tail -n 1)" && \
      ssl="$(apt-cache search --names-only '^libssl[0-9]+(t64)?$' | cut -d ' ' -f 1 | sort -V | tail -n 1)" && \
      test -n "$icu" && test -n "$ssl" && apt-get install -y --no-install-recommends \
        "$icu" "$ssl" ca-certificates git libgcc-s1 libstdc++6 python3 zlib1g && \
      rm -rf /var/lib/apt/lists/*; \
    fi
ENV HOME=/tmp/nativeaot-home PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
USER 65532:65532
WORKDIR /tmp
