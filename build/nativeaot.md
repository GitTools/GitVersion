# Opt-in managed-only NativeAOT preview

The established `src/GitVersion.App` CLI has an explicit managed-only build flavor.
Normal builds, release workflows, tool packages, MSBuild tasks and Docker images
retain their existing defaults. Experimental `new-cli/` is outside this flavor.
This executable still requires Git and the target operating system's native
runtime dependencies; it does not require an installed .NET runtime.

## Local validation and optional archive

Use the SDK selected by the unchanged `global.json`, PowerShell 7.2+, Python 3,
Git and the target platform's NativeAOT compiler/linker prerequisites. Pass an
explicit runtime patch version available for that framework and RID. For example:

```powershell
pwsh ./build/publish-nativeaot.ps1 -Framework net10.0 -RuntimeIdentifier osx-arm64 -RuntimeVersion 10.0.13
```

Add `-Package` to create a preview zip only after every validation gate passes.
`-Dotnet` and `-Python` select executable paths. `-OutputDirectory` must be new;
otherwise a unique temporary directory is used and retained as evidence.
`-ParityRepository` selects existing local history to clone for parity; its
default is this checkout. Both executables must be runnable on the build host.
Cross-OS publication is unsupported, and cross-architecture compilation alone
does not validate an artifact. Do not select a RID simply because restore works.

The helper publishes strict managed-only NativeAOT first, then an ordinary
dual-backend, self-contained JIT comparator from the same source. The parity
runner explicitly selects the managed backend in both processes. Both publish
commands pin the framework, RID and `RuntimeFrameworkVersion`; runtime rollforward
is disabled. Publications run sequentially using the normal project obj/bin
locations and distinct final output directories. Do not run builds concurrently
in this checkout. Native restore assets are captured and checked before the
ordinary restore replaces them. No package
versions, global SDK selection or production release settings are rewritten.

Source HEAD and a digest of tracked/new `src` files and root build inputs must
remain stable across publication and parity. Dirty implementations are allowed
and fingerprinted. Keep the checkout stable while running; the helper fails if
these inputs change. The parity report also records its own source digest and
both executable checksums. The supplied runtime version is a build input, not a
claim that merely running the harness detects or proves a binary's runtime build.

Before the JIT comparator restores its legitimate libgit2 dependency, the helper
rejects any `libgit2` dependency in resolved NativeAOT libraries, targets or
project references, or any matching file in native output. Unused central
package-version declarations are not dependencies. It runs `nativeaot-parity.py` with generated fixtures and the
selected existing repository history, requires a successful complete report,
and checks its executable hashes, source revision and framework/runtime inputs.
Failed publishing, missing tools, failed parity or changed inputs prevents
archive creation. ILLink and NativeAOT IL compiler diagnostics are errors for
the NativeAOT publish; no IL warnings are excluded. Global build warnings are not
promoted: net11 currently reports existing NU1510 redundant package-reference
advisories in Core, which are separate from trim/AOT analysis.

`parity.json`, `inputs.json`, `native-project.assets.json` and both publication directories
remain in the output directory. The optional archive has the distinct name
`gitversion-nativeaot-preview-<TFM>-<RID>-<revision>.zip`, and contains only the
native executable, repository `LICENSE` and `SHA256SUMS`. Its checksum is beside
the zip. Debug symbols remain in publication output and are not packaged. This
is a local preview archive, not a signed release or a replacement global tool.
Third-party notices, native OS dependency inspection, signing and release
provenance still require the normal release review before public distribution.

## Evidence and remaining gates

### Target-host validation matrix

`.github/workflows/nativeaot-validation.yml` validates both frameworks on native
Windows x64/ARM64, Linux glibc x64/ARM64, Linux musl x64/ARM64 and macOS ARM64
runners. It checks out the exact PR head, pins the SDK and runtime/compiler patch,
publishes the native and ordinary managed flavors sequentially, rejects libgit2
from the native closure, and requires all 99 unique parity cases on that host.
`nativeaot-platform-validation.py` rejects a mismatched host OS, CPU or libc and
records source fingerprints, executable hashes, toolchain information and logs.
Compilation alone does not satisfy the gate.

Linux also executes six independent smoke checks in Ubuntu 24.04 or Alpine 3.24
containers without any installed .NET runtime/SDK. The executable and scripts
are mounted read-only, networking is disabled, and Git history/configuration
checks run in a temporary writable filesystem, both as root and UID 65532.
The tagged calculation must independently produce `1.2.3`; this is not a
native-versus-itself parity comparison. These checks establish the tested
container environment only, rather than every installation or minimum OS floor.

The workflow retains JSON reports and logs for seven days, including Linux
container image identity. It does not upload executables, create preview
archives, publish packages/images, change existing release jobs or promote a
NativeAOT distribution channel. A configured job is not evidence that it passed:
record completed run URLs and target-specific reports before closing a gate.

### Earlier local validation

Local macOS ARM64 validation on 2026-10-20 published net10.0/10.0.13 and
net11.0/11.0.0-rc.2.26504.105 with strict linker/compiler warning policies.
Both native executables passed all 99 corpus cases against ordinary JIT builds
from the same source. The unchanged pre-implementation net10 JIT snapshot also
passed all 99 comparisons. After including #5389, the helper passed its complete
publish/closure/parity sequence for both TFMs and created local preview archives. Payload/outer
checksums and Unix permissions were verified, and the extracted executable
matched full output on existing repository history. All seven regression suites
passed on both TFMs: 76,092 tests, no failures or skips. Initial executables were built
from commit `2788dd4ee`; the later documentation-only rebase preserves the
validated `src/` SHA256 `f86fc1b69f259cfc969f150d7db912110ea57df10d58ace7cc9334aecffe6556`.
Those counts and that fingerprint describe the initial validation. The CI
follow-up isolates legacy model overloads under `Formatting/VersionModels/`,
outside the experimental CLI's shared `Formatting/*.cs` links. Its new CLI build
and generator test pass, as do 168 formatter tests per TFM and strict native
publication plus all 99 parity cases per TFM. The parity cases also pass with
inherited Buildkite, Drone, Travis and CodeBuild variables set; the runner clears
those selectors before selecting each fixture's adapter. This follow-up's tested
`src/` SHA256 is `af9f5020adc6d244d465855f94cb61e717cf1c501dc38955fc7f7e07fc1df423`.
Research's startup/size numbers came from incomplete binaries and are not
performance claims for this implementation.

The parity harness checks CLI startup/schema, configuration validation and
migration, scalar queries, effective YAML, representative GitHubFlow/GitFlow/
Mainline histories, tags/merges/detached HEAD/packed/dirty/shallow/unshallowed
history, cache, legacy modes, custom formatting, requested de-DE/tr-TR cultures,
output files/templates/project/WiX, linked worktrees, local dynamic clone/fetch,
missing Git, file logging, build agents including local AppVeyor HTTP capture,
backend rejection and existing repository history. Outputs, exit codes,
diagnostic text and side effects must agree. Only wall-clock log timestamps
and captured file-log scope durations are normalized; messages remain exact.
Cache writers start empty and compare exact cache file bytes; cache readers
start from the same managed-written seed. Backend process-start failures and
timeouts are failed report cases. Source identity queries ignore inherited Git
repository/configuration selectors, and packaging requires all 99 unique cases
to pass, including existing repository history.
This is a bounded regression corpus, not exhaustive proof of every Git topology,
credential failure, process signal, culture, output mode or repository size.

Windows x64/ARM64, Linux glibc x64/ARM64, Alpine/musl x64/ARM64, and
installation on a machine without .NET remain separate execution gates until
recorded on their target hosts. Global/local tool packaging and dispatcher
behavior, a native Docker image, nonroot container behavior, OS compatibility
floors, signing/notarization, third-party notices and release pipeline integration
are unvalidated. No artifact is eligible for a platform solely because this
helper accepts its RID. Keep existing release defaults until those gates pass
and a separate release decision explicitly promotes a NativeAOT channel.
