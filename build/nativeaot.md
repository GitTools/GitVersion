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

Linux also executes six independent smoke checks in UBI 8.10, Ubuntu 22.04/24.04,
or Alpine 3.21/3.23 (according to framework) and 3.24
containers without any installed .NET runtime/SDK. The executable and scripts
are mounted read-only, networking is disabled, and Git history/configuration
checks run in a temporary writable filesystem, both as root and UID 65532.
The runtime image sets `HOME=/tmp/nativeaot-home`: .NET 11's named-mutex
implementation initializes IPC state under `$HOME/.dotnet`, even in a native
executable. This directory is runtime state, not an installed .NET runtime/SDK.
A read-only container must provide a writable home as well as temporary storage;
the test retains GitVersion's existing cross-process repository lock.
The tagged calculation must independently produce `1.2.3`; this is not a
native-versus-itself parity comparison. These checks establish the tested
container environment only, rather than every installation or minimum OS floor.

The workflow retains JSON reports and logs for seven days, including Linux
container image identity. It does not upload executables, create preview
archives, publish packages/images, change existing release jobs or promote a
NativeAOT distribution channel. A configured job is not evidence that it passed:
record completed run URLs and target-specific reports before closing a gate.

### OS support targets

The compatibility target is the OS versions supported by the corresponding
.NET major version, within this preview's seven existing RIDs. This is a target,
not a claim that each OS has passed. The [.NET 10 OS matrix][dotnet10-os] and
[.NET 11 OS matrix][dotnet11-os] were checked on 2026-10-20; these references pin
the policy snapshot. Edition, architecture, lifecycle and ESU qualifications in
those matrices still apply.

| Target family | net10.0 oldest listed target | net11.0 oldest listed target |
| --- | --- | --- |
| macOS | 15 | 15 |
| Ubuntu | 22.04 | 22.04 |
| RHEL and compatible derivatives | 8 | 8 |
| Alpine | 3.21 | 3.23 |
| Windows Server / Server Core x64 | 2012 with ESU | 2016 |
| Windows desktop | Windows 10 1607 Enterprise, with matrix qualifications | Windows 10 1607 Enterprise, with matrix qualifications |
| Nano Server x64 | 2019 | 2019 |

Other supported Linux targets (Azure Linux, CentOS Stream, Debian, Fedora,
openSUSE and SLES) remain in the linked policy's scope and need target-specific
validation. Their compatibility is not inferred from an Ubuntu test or a shared
libc version. Platforms/architectures outside the preview's existing RIDs are
not introduced by this validation change.

A confirmed older-OS failure explains why a baseline build is necessary: the
Ubuntu 24.04-built ARM64 net10 executable fails startup on Ubuntu 22.04 with a
missing `GLIBC_2.38` symbol version. [NativeAOT's deployment guidance][nativeaot-os]
requires building on an appropriately old Linux baseline; the portable .NET
runtime's libc support does not remove a native executable's linker imports.
The workflow therefore builds glibc executables on UBI 8.10, tests them there
and on Ubuntu 22.04/24.04 without .NET, and selects Alpine 3.21 for net10 or 3.23
for net11 alongside 3.24. It uses macOS 15 for native ARM64 publication/parity.
Baseline execution is recorded separately from the earlier newer-host matrix.
The [completed baseline matrix](https://github.com/GitTools/GitVersion/actions/runs/39483650846)
validated commit `4f2871a9822606eadee54dc4a4790cd60509019c`: all 14 jobs passed
99/99 parity, including native macOS 15 ARM64 execution. Forty Linux executions
without .NET passed six checks each (240 total): glibc on UBI 8.10 and Ubuntu
22.04/24.04, and musl on each framework's Alpine floor plus 3.24, both CPU
architectures and both root/nonroot users. These are the tested userspace
baselines, subject to the kernel and full-OS limitations below.
Local ARM64 bring-up passed strict net10 publication and all 99 parity cases on
the UBI 8 baseline. That executable also passed all six no-.NET checks on UBI 8,
Ubuntu 22.04 and Ubuntu 24.04 as root and UID 65532; the original Ubuntu 22.04
startup failure was reproduced before the repair. Existing musl net10 output
also passed the six checks on Alpine 3.21 under both users. These local checks
do not substitute for the complete committed-head, two-framework CI matrix.
Linux containers exercise older userspace libraries on the runner's kernel;
they do not prove execution on each distribution's older kernel or full VM.
Older Windows/Server Core/Nano versions require appropriate clean VMs or
containers; the current Windows runners do not establish their compatibility.

The earlier [completed native matrix on 2026-10-20](https://github.com/GitTools/GitVersion/actions/runs/39480323312)
validated commit `7677a27b324484e7bc2a26a079e67c8cbe455e5e`, rebased onto
`main` at `bde36696007d412195a0f2dd5f43d6ae820de025`. All 14 jobs passed:

| Native target | Host | net10.0 / 10.0.13 | net11.0 / 11.0.0-rc.2.26504.105 |
| --- | --- | --- | --- |
| win-x64 | Windows Server 2025 | 99/99 | 99/99 |
| win-arm64 | Windows 11 ARM64 | 99/99 | 99/99 |
| linux-x64 | Ubuntu 24.04 x64 | 99/99 | 99/99 |
| linux-arm64 | Ubuntu 24.04 ARM64 | 99/99 | 99/99 |
| linux-musl-x64 | Alpine 3.24 x64 container | 99/99 | 99/99 |
| linux-musl-arm64 | Alpine 3.24 ARM64 container | 99/99 | 99/99 |
| osx-arm64 | macOS 26 ARM64 | 99/99 | 99/99 |

Each job also passed the seven validation-gate regression tests, strict native
publication, native dependency inspection and libgit2 closure rejection. All
reports identify that exact commit and SDK `11.0.100-rc.2.26504.105`.
Linux's four targets additionally passed all six no-.NET smoke checks for both
frameworks as UID 0 and UID 65532: 16 executions, 96 checks. The tests exposed
and repaired remote-only branch loss in the existing-history fixture and the
cold .NET 11 container's need for writable runtime IPC storage.

A local macOS ARM64 net10.0 experiment on the same commit also exercised the
existing SDK's native tool-packaging path. Strict managed-only AOT packing
created a RID payload package and pointer package with the unique version
`0.0.0-nativeaot-local-validation` in an isolated feed. Tool-path, local-manifest
and isolated global installation selected the ARM64 native executable; each
calculated tagged version `1.2.3` and rejected libgit2. The installed tool-path
payload passed all 99 comparisons against a same-source ordinary JIT build.
Installation used explicit `--arch arm64` and required the SDK's Microsoft
host-pack dependency in that feed. No packages were published and the user's
global tools were not changed. This validates that local macOS/net10 path only;
it does not establish installation on a clean host, the other RIDs/framework,
signing or production dispatcher/release integration.

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

Clean Windows/macOS machines without modern .NET remain unvalidated; their
hosted runners have SDKs installed. The tested host versions do not establish
minimum OS/native-library compatibility floors. Global/local native-tool
packaging and dispatcher behavior across all RIDs, a distributable native Docker
image, signing/notarization, third-party distribution notices, release provenance
and release pipeline integration remain separate gates. The disposable Linux
validation images establish root/nonroot execution only; they are not release
images. No artifact is eligible for a platform solely because this helper
accepts its RID. Keep the PR as a draft and retain existing release defaults
until those gates pass and a separate release decision explicitly promotes a
NativeAOT channel.

[dotnet10-os]: https://github.com/dotnet/core/blob/ffed591b0fb8f9aab50ecc28f2c765049fb28d8f/release-notes/10.0/supported-os.md
[dotnet11-os]: https://github.com/dotnet/core/blob/ffed591b0fb8f9aab50ecc28f2c765049fb28d8f/release-notes/11.0/supported-os.md
[nativeaot-os]: https://learn.microsoft.com/dotnet/core/deploying/native-aot/
