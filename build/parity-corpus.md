# parity-corpus.ps1 — real-world corpus parity check

Phase B validation tooling for the managed-git migration
(see `docs/design/managed-git-migration.md`, §5 Phase B and §7 item 3).

Runs the locally built `GitVersion.App` twice per repository/checkout shape —
once with `GITVERSION_GIT_BACKEND=libgit2`, once with `=managed` — using
`--output json --no-cache --no-fetch --no-normalize` (read-only, deterministic)
and diffs the JSON version variables field by field.

## Usage

```bash
# default corpus: GitVersion, GitReleaseManager, and this checkout itself
pwsh ./build/parity-corpus.ps1

# custom corpus / subset of variants / reuse binaries
pwsh ./build/parity-corpus.ps1 -Repos /path/to/repo -Variants full,worktree -SkipBuild

# keep clones in a persistent cache
pwsh ./build/parity-corpus.ps1 -WorkDir ~/.cache/gitversion-parity
```

## Parameters

| Parameter | Default | Meaning |
|---|---|---|
| `-Repos` | GitVersion, GitReleaseManager, local checkout | Git URLs or local paths |
| `-WorkDir` | `$TMPDIR/gitversion-parity-corpus` | Clone cache; repeat runs reuse it |
| `-Variants` | `full,shallow,worktree` | Checkout shapes to exercise |
| `-Configuration` / `-Framework` | `Release` / `net10.0` | Which App build to run |
| `-SkipBuild` | off | Reuse existing `GitVersion.App` binaries |

## What it does per repository

1. Full clone into the cache (skipped when already present).
2. Derives a **shallow** clone (`git clone --depth 1 file://<full-clone>`, run with
   `--allow-shallow`) and a **linked worktree** (`git worktree add --detach`) — the two
   CI-style shapes called out in the migration design.
3. Runs both backends per variant, normalizes/parses the JSON, and reports:
   - `MATCH` — identical variables,
   - `DIFF` — any field difference, or one backend failing while the other succeeds
     (a parity failure; a readable per-field diff is printed),
   - `ERROR` — variant setup failed or *both* backends refused the shape
     (environment limitation, not counted as a parity failure).
4. Prints a summary table with per-backend elapsed milliseconds (coarse perf signal).

## Exit code

The number of repo/variant combinations with result `DIFF`. `0` = full parity.
Errors do not abort the run; remaining repos/variants are still checked.
