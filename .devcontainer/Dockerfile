FROM mcr.microsoft.com/devcontainers/dotnet@sha256:9eb314432a5fa67773a53ab754cdc381cae614906162f9fc44cdb7f7837b91c7

# Keep the LTS runtime for build tooling and install the SDK pinned by the repository.
COPY global.json /tmp/gitversion-global.json
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && dotnet_dir="$(dirname "$(readlink -f "$(command -v dotnet)")")" \
    && bash /tmp/dotnet-install.sh --channel 10.0 --runtime dotnet --install-dir "$dotnet_dir" --no-path --skip-non-versioned-files \
    && bash /tmp/dotnet-install.sh --jsonfile /tmp/gitversion-global.json --install-dir "$dotnet_dir" --no-path \
    && rm /tmp/dotnet-install.sh /tmp/gitversion-global.json

# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>
