# Replacing LibGit2Sharp with a managed Git implementation

Research and migration plan for [#236](https://github.com/arturcic/GitVersion/issues/236) — researched 2026-07, based on a full survey of the `next/v7` tree.

## 1. Motivation

LibGit2Sharp wraps the native `libgit2` library and has been a recurring source of pain for GitVersion for close to a decade:

- **Native binary load failures** on end-user machines and CI images — GitTools/GitVersion
  [#1097](https://github.com/GitTools/GitVersion/issues/1097),
  [#1203](https://github.com/GitTools/GitVersion/issues/1203),
  [#1744](https://github.com/GitTools/GitVersion/issues/1744),
  [#1852](https://github.com/GitTools/GitVersion/issues/1852),
  [#2615](https://github.com/GitTools/GitVersion/issues/2615),
  [#2884](https://github.com/GitTools/GitVersion/issues/2884). The failure mode is always the same class:
  the RID-specific `libgit2-*.so/.dylib/.dll` doesn't match the runtime OS/arch/libc (musl vs glibc,
  new Ubuntu OpenSSL versions, ARM variants) or cannot be located by the MSBuild task's assembly-load context.
- **Packaging weight**: `LibGit2Sharp.NativeBinaries` unpacks ~12 native binaries
  (linux x64/arm/arm64/musl/ppc64le, osx x64/arm64, win x64/x86/arm64) into `runtimes/<rid>/native/`
  of every published artifact — and `GitVersion.MsBuild` ships that payload **per TFM** (net8.0/net9.0/net10.0).
- **Platform lag**: new RIDs, libcs and OS versions require waiting on libgit2 + LibGit2Sharp + NativeBinaries releases
  (GitVersion is currently pinned to LibGit2Sharp 0.31.0).
- **Feature lag**: libgit2 trails git itself (SHA-256 repos, reftable, partial clone), and its global native state
  complicates concurrent use inside MSBuild.

Precedent: Nerdbank.GitVersioning faced the same problem and built a managed read-only engine
([dotnet/Nerdbank.GitVersioning#505](https://github.com/dotnet/Nerdbank.GitVersioning/issues/505),
[#521](https://github.com/dotnet/Nerdbank.GitVersioning/pull/521)). It became their default backend and delivered
**>10x throughput on history walks** compared to libgit2. NBGV kept libgit2 for write paths; the plan below goes one
step further and removes the native dependency entirely.

## 2. Landscape: managed Git options in .NET (2026)

| Candidate | Reads | Writes | Fetch/clone | Maintained | Assessment |
|---|---|---|---|---|---|
| [NBGV ManagedGit](https://github.com/dotnet/Nerdbank.GitVersioning/tree/main/src/NerdBank.GitVersioning/ManagedGit) | objects, packs (incl. deltas), refs; tuned for version calculation | No | No | Active, but internal to NBGV (MIT) | **Primary porting source** — proven pack/idx/delta code, the source of the >10x speedup |
| [GitReader](https://github.com/kekyo/GitReader) (kekyo) | full traversal: branches/tags/commits, packfiles, worktrees, index, .gitignore | No | No | Active (v1.18, .NET 10 TFMs, zero deps) | Porting inspiration for worktree handling and commit-graph; viable external dependency if vendoring were rejected |
| [ManagedGitLib](https://github.com/GlebChili/ManagedGitLib) | standalone extraction of NBGV ManagedGit | No | No | Stale (~2022) | Proof that vendoring/extracting NBGV's code works; not a dependency to take |
| DotGit, GitRead.Net, GitSharp, NGit | partial readers / ancient ports | No | No | Dead | Skip |
| `git` CLI shell-out (MinVer-style) | yes | yes | yes, with system credential helpers | n/a | **Chosen for writes + network** — requires `git` on PATH, which every CI normalization scenario already implies |

**The decisive fact:** no maintained managed .NET library implements git *write* operations or the smart-HTTP/SSH
*transport* (fetch/clone). Every managed option is read-only. Any replacement strategy must therefore pair a managed
reader with something else for the write/network surface — and that something is the `git` CLI, which is guaranteed
present in exactly the scenarios (CI normalization, dynamic repositories) that need it.

### Decisions

1. **Hybrid backend**: vendored fully-managed reader for all read/history operations; `git` CLI shell-out for
   writes and network. Zero native binaries ship with GitVersion afterwards.
2. **Vendored, not an external package**: the managed reader lives in this repo (ported from NBGV ManagedGit, MIT,
   with attribution), giving full control over revwalk semantics — which version output depends on.
3. **`src/` (stable) tree first**; `new-cli` follows by source-linking the read-only subset, exactly as it
   source-links the LibGit2Sharp adapter today.

## 3. Current LibGit2Sharp surface (survey results)

### 3.1 The seam is clean

`GitVersion.Core` has **no LibGit2Sharp reference**. It consumes only its own abstraction in
`src/GitVersion.Core/Git/` (~20 interfaces: `IGitRepository`, `IMutatingGitRepository`, `IGitRepositoryInfo`,
`ICommit`, `IBranch`, `ITag`, `IReference`, `IRemote`, `IRefSpec`, `IObjectId`, their collections, and support types
`CommitFilter`, `AuthenticationInfo`, `ReferenceName`). Only three projects reference LibGit2Sharp:

1. `src/GitVersion.LibGit2Sharp` — the production adapter
2. `src/GitVersion.Testing` — test fixtures
3. `new-cli/GitVersion.Core.Libgit2Sharp` — source-links the adapter's files, read-only subset
   (excludes `GitRepository.mutating.cs` and `GitRepositoryInfo.cs`)

### 3.2 Read surface (the bulk — must be reimplemented)

- Repository discovery and open: walk-up discovery, `.git` file indirection (worktrees/submodules),
  commondir split, bare detection, shallow detection (`repo.Info.IsShallow`)
- HEAD, branch/tag/reference/remote enumeration (incl. glob queries via `Refs.FromGlob`)
- Commit metadata: id, parents, committer `When`, message
- **Revwalk**: `CommitCollection.QueryBy(CommitFilter)` → libgit2 `IncludeReachableFrom` /
  `ExcludeReachableFrom` / `FirstParentOnly` / `SortBy` (Topological, Time). Ordering is
  version-output-affecting.
- **Merge-base**: `repo.ObjectDatabase.FindMergeBase(c1, c2)`
- **Tree diff**: `ICommit.DiffPaths` via `repo.Diff.Compare<TreeChanges>(tree, parentTree)` (changed paths only)
- **Status**: `UncommittedChangesCount()` via `Diff.Compare<TreeChanges>(Head.Tip.Tree, DiffTargets.Index | DiffTargets.WorkingDirectory)` with `RetrieveStatus()` fallback

### 3.3 Write + network surface (small, one consumer)

Everything mutating funnels through **`src/GitVersion.Core/Core/GitPreparer.cs`** (CI normalization and dynamic
repositories), implemented in `src/GitVersion.LibGit2Sharp/Git/GitRepository.mutating.cs` and the collection wrappers:

- `Clone(url, workdir, auth)` — no-checkout clone with username/password credentials
- `Fetch(remote, refSpecs, auth, log)` — authenticated HTTPS (username/password only; no SSH path exists today)
- `Checkout(spec)`; `CreateBranchForPullRequestBranch(auth)` (uses network `ls-remote` under the hood)
- `References.Add/UpdateTarget`, `Branches.UpdateTrackedBranch/Remove`, `Remotes.Update(refspec)/Remove`
- Error mapping is already string-based (parses "401"/"404" out of exception messages);
  `LockedFileException` is retried via Polly (`RepositoryExtensions.RunSafe`)

### 3.4 Known abstraction leaks (to fix during migration)

- Public `LibGit2SharpExtensions.ToGitRepository(IRepository)` exposes a LibGit2Sharp type
- `CommitFilter.SortBy` is numerically cast to `LibGit2Sharp.CommitSortStrategies`
- `CommitFilter.IncludeReachableFrom`/`ExcludeReachableFrom` are typed `object?`

### 3.5 Test fixtures

`src/GitVersion.Testing` (~300 LoC of real LibGit2Sharp usage in `Fixtures/RepositoryFixtureBase.cs` +
`Extensions/GitTestExtensions.cs`) is *write-heavy*: stage/commit/tag/branch/merge-no-ff/checkout/clone/fetch/config.
It already shells out to real `git` for `init -b`, shallow `pull --depth 1` and `gc` — so a pure git-CLI fixture
backend is a natural completion of an existing pattern. Six test files additionally use LibGit2Sharp directly
(mostly `ToGitRepository()` and `Signature`).

## 4. Target architecture

```
src/
  GitVersion.Git.Managed/          vendored managed reader (no GitVersion.Core dependency)
    Objects/    GitObjectId (hash-agnostic), pack + .idx v2 readers, delta streams,
                pack cache, loose-object reader, multi-pack-index reader
    Parsing/    commit parser (author/committer/when/message/encoding), tree parser,
                annotated-tag parser
    Refs/       loose refs, packed-refs, symbolic refs, HEAD, glob enumeration
    Repository/ discovery (walk-up, .git file, commondir), worktree resolver, shallow info
    History/    revwalk (topo/time, first-parent, exclusions — libgit2-parity),
                merge-base (paint-down-to-common), commit-graph reader (later, optional)
    Diff/       recursive tree-vs-tree changed-paths diff (no rename detection)
    Status/     .git/index v2–v4 reader, workdir/index status for UncommittedChangesCount
  GitVersion.Git.CommandLine/      git CLI executor (writes + network only)
  GitVersion.Git/                  thin adapter implementing IGitRepository (managed reader)
                                   + IMutatingGitRepository (CLI mutator); mirrors the
                                   current adapter file layout so DI is unchanged
```

Port-vs-fresh decisions:

| Component | Decision |
|---|---|
| Pack/idx/delta reading, pack cache, loose objects, object id | **Port from NBGV ManagedGit** (MIT, attribution in headers + NOTICE) |
| Commit/tree parsing | Port + extend (NBGV skips author/message — GitVersion needs them) |
| Annotated tags, multi-pack-index, discovery/worktrees, shallow | Fresh (small, well-specified formats) |
| **Revwalk**, **merge-base**, tree diff, index/status | **Fresh** — highest-parity-risk pieces, written against libgit2's documented algorithms |
| SHA-256 repos | Hash-agnostic `GitObjectId` from day one; detect `extensions.objectformat=sha256` and fail with a clear message initially (parity: libgit2 can't read them either) |
| Reftable (`extensions.refstorage=reftable`) | Detect and fail with a clear message; reader in backlog |

CLI executor hygiene:

- Arguments via `ProcessStartInfo.ArgumentList` (no shell, no quoting bugs); `LC_ALL=C`, `GIT_TERMINAL_PROMPT=0`,
  `-c core.quotepath=false`
- **Plumbing-only parsing** (`rev-parse`, `ls-remote`, `update-ref`, `symbolic-ref`, `for-each-ref --format`,
  `config`); porcelain commands (`clone`, `fetch`, `checkout`, `branch`) invoked for side effects only
- Auth: per-invocation `-c http.<url>.extraHeader=Authorization: Basic <b64>` — never URL userinfo,
  never persisted to config
- stderr classification mapped to today's exceptions: `.lock` failures → `LockedFileException`
  (keeps the Polly retry untouched), 401/403/404 → the same messages `GitPreparer` expects
- One-time `git version` probe; require ≥ 2.30, actionable error if git is absent when a mutating
  operation is requested

## 5. Phased migration (every phase shippable)

### Release strategy: one switch, two backends, a public testing window

A single public environment variable — **`GITVERSION_GIT_BACKEND=libgit2|managed`** — selects the entire
backend. `managed` opts into the new implementation stack as far as it exists at each release (first the
CLI mutator, later also the managed reader). The version-to-default mapping is explicit:

| Release | Default backend | Opt-in / opt-out |
|---|---|---|
| **v7.0** | `libgit2` | `GITVERSION_GIT_BACKEND=managed` to test the new backend |
| **v7.1** | `managed` | `GITVERSION_GIT_BACKEND=libgit2` to fall back |
| v7.x (later) | `managed` | libgit2 removed (Phase E) once the fallback has gone several releases unused/regression-free |

Both backends ship side by side throughout the v7.0–v7.x window so users can validate `managed` against
their real repositories and report issues before libgit2 is dropped. For that whole window, the CI
unit-test matrix runs the full suite against both backends (`git_backend` dimension in `_unit_tests.yml`) —
both legs must stay green.

### Phase A — CLI mutator first (reads stay on libgit2) · ~2–3 weeks
Replace `GitRepository.mutating.cs` + mutating collection members with `GitVersion.Git.CommandLine`.
Reads keep using libgit2; the wrapper drops its cached collection wrappers after each CLI mutation so
external ref changes become visible (the libgit2 handle itself must not be disposed — wrappers already
handed out reference its native memory). Selected via `GITVERSION_GIT_BACKEND=managed`.
This immediately retires the most fragile native code paths (network, SSL, credentials).

### Phase B-pre — interface cleanups · ~3–5 days
Core-owned `CommitSortStrategies` enum (explicit mapping in the libgit2 adapter instead of a numeric cast),
typed `CommitFilter` reachable-from members, remove/internalize `ToGitRepository`.

### Phase B — managed reader behind a flag · ~8–12 weeks
Build `GitVersion.Git.Managed` bottom-up with per-layer unit tests against git-CLI-generated fixture repos
(loose/packed/mixed objects, packed-refs, worktrees, shallow, multi-pack-index, index v4).
Backend selection via `GITVERSION_GIT_BACKEND=managed|libgit2` (default `libgit2`). Validation:
- **CI matrix**: full integration suites (`GitVersion.Core.Tests`, `GitVersion.App.Tests`) run on both backends,
  three OSes — the suites assert exact SemVer strings over complex histories and are the strongest parity oracle
- **DualBackendParityTests**: open the same fixture with both backends; deep-equality on ref enumeration,
  order-sensitive `QueryBy` sequences, `FindMergeBase` over all commit pairs (incl. criss-cross and
  equal-timestamp fixtures), `DiffPaths`, `UncommittedChangesCount`
- **Real-world corpus script**: `gitversion /nocache /output json` diffed across backends on this repo,
  GitReleaseManager, dotnet/runtime, a shallow CI-style clone, and a worktree checkout

### Phase C — default flip in v7.1 · ~1 week + multi-release soak
**v7.0 ships with default `libgit2`** (managed opt-in); **v7.1 flips the default to `managed`** with
`GITVERSION_GIT_BACKEND=libgit2` as the fallback while users validate the new backend. The dual-backend
CI matrix stays green throughout the window.

### Phase D — fixture migration (parallel with B) · ~2–3 weeks
`GitVersion.Testing` moves to pure git-CLI writes via the existing `ExecuteGitCmd` pattern:
deterministic `GIT_AUTHOR_*`/`GIT_COMMITTER_*` env (several tests advance commit time explicitly),
`git commit --allow-empty`, `-c commit.gpgsign=false -c gc.auto=0`. Migrate the six direct-usage test files.
If suite time regresses from process spawns, batch history creation with `git fast-import`.

### Phase E — remove LibGit2Sharp · ~1–2 weeks
Delete `src/GitVersion.LibGit2Sharp` and `new-cli/GitVersion.Core.Libgit2Sharp`; drop the package from
`Directory.Packages.props`; re-point new-cli source-linking at `GitVersion.Git` + `GitVersion.Git.Managed`
(read-only subset). Add a packaging assertion test: **zero `runtimes/**/native/*` entries** in the
`GitVersion.MsBuild` and `GitVersion.Tool` nupkgs. Document the new runtime requirement: `git` on PATH is needed
**only** for dynamic-repo/CI-normalization scenarios — pure version calculation on a prepared checkout needs no
git binary at all (a strict improvement for MSBuild-task users).

### Phase F (optional) — performance accelerators · ~2–3 weeks
Commit-graph reader (generation numbers for topo sort and merge-base), benchmark-driven pack cache tuning.

## 6. Risks and mitigations

| Risk | Mitigation |
|---|---|
| **Revwalk ordering divergence → wrong versions** | Port libgit2's `revwalk.c` algorithm precisely (time-queue with insertion-order tiebreak, Kahn-style topo over it, mark-uninteresting propagation); order-sensitive dual-backend sequence tests; corpus diffing as the Phase C gate |
| Merge-base candidate choice on criss-cross merges (libgit2 returns one of several) | Replicate libgit2's paint-down selection; assert *version output* on criss-cross fixtures plus one SHA-level canary test to detect drift |
| Shallow clones (the CI default) | `.git/shallow` boundaries treated as parentless in the walker; existing `MakeShallow` fixture covers it |
| Worktrees / `.git`-file / commondir | Dedicated `WorktreeResolver`; existing worktree tests + new fixtures |
| Rename detection assumed in `DiffPaths` | Verify adapter options at Phase B kickoff — libgit2's default `Compare<TreeChanges>` doesn't do rename detection, so a plain recursive tree diff is expected to be exact parity |
| Performance regressions | Port NBGV's pack cache; BenchmarkDotNet suite (end-to-end + revwalk/merge-base/status micro); Phase C gate: no scenario >20% slower — expectation is large wins per NBGV precedent |
| git CLI availability/version drift | Only the mutator needs git; version probe (≥2.30) with actionable error; CI leg on a container with the oldest supported git |
| CLI output stability | Plumbing + `--format` parsing only; stderr matching limited to error classification with conservative fallback (unknown → generic error carrying stderr, as today) |
| Locale/encoding | `LC_ALL=C`; honor the commit `encoding` header with Latin-1 fallback (matching git); `core.quotepath=false` |
| Concurrency (parallel MSBuild tasks) | Managed reader is read-only and lock-free; per-instance or thread-safe pack cache; removes libgit2's native global state entirely |
| SHA-256 / reftable repos | Detect and fail with clear messages (no regression — libgit2 can't read them either); hash-agnostic design keeps the door open |
| License/attribution of vendored code | MIT headers + NOTICE entries for NBGV ManagedGit (and GitReader if any code is taken) |

## 7. Verification strategy

1. Full integration suite × backend × OS matrix green through Phases B–D
2. `DualBackendParityTests` structural deep-equality on adversarial fixtures (equal timestamps, criss-cross,
   octopus merges, packed+loose refs, annotated+lightweight tags on one commit, shallow, worktree,
   multi-pack-index, index v4)
3. Real-world corpus JSON diffing recorded before the Phase C flip
4. Benchmark gate (≥ parity everywhere, expected wins on history walks)
5. Packaging assertion: no native binaries in shipped nupkgs (Phase E)

## 8. Effort summary

| Phase | Scope | Estimate |
|---|---|---|
| A | git-CLI mutator + auth/error/lock mapping | 2–3 weeks |
| B-pre | interface cleanups | 3–5 days |
| B | managed reader + adapter + parity harness | 8–12 weeks |
| C | default flip + soak | 1 week + 1 release |
| D | fixture migration (parallel with B) | 2–3 weeks |
| E | LibGit2Sharp removal, new-cli re-link, packaging tests | 1–2 weeks |
| F | commit-graph accelerator (optional) | 2–3 weeks |

**Total to a LibGit2Sharp-free GitVersion: ~4–5 months elapsed including soak cycles.**
