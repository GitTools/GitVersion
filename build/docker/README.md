# Docker base image pins

`base-images.json` records the multiarchitecture manifest digest for each
supported registry, distribution, image kind, and .NET version. The Cake build
selects the SDK and runtime references from this file and passes them as
`SDK_IMAGE` and `RUNTIME_IMAGE`. An unsupported combination fails instead of
falling back to a mutable tag.

The Dockerfile also pins its default Ubuntu SDK and runtime images, so direct
builds use immutable references. To build a different combination directly,
pass full image references including `@sha256:` digests through those two build
arguments.

When updating build images or the matrix in `build/common/Utilities/Constants.cs`:

1. Inspect each SDK and runtime tag in both `docker.io` and `ghcr.io`:

   ```sh
   docker buildx imagetools inspect docker.io/gittools/build-images:ubuntu.26.04-sdk-10.0
   ```

2. Record the top-level manifest digest in `base-images.json`, after verifying
   the manifest contains both `linux/amd64` and `linux/arm64`. Each registry has
   its own digest; do not copy a digest between registries without verification.
3. If either default Ubuntu 10.0 image changed, update the matching Dockerfile
   argument as well.
4. Build and exercise a representative image before merging the new pins.

The npm overrides for AJV and YAML select patched versions. The scoped
`unified-args` override uses Chokidar 4 to remove the vulnerable `braces` chain;
remark's watcher receives resolved file paths and does not rely on Chokidar 3
glob expansion. Recheck these overrides when updating remark's dependencies.
