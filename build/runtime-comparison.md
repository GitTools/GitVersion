# .NET runtime comparison

This optional Linux experiment supports #5210. It does not impose a timing gate
or establish a GitVersion speedup. Run the **Runtime comparison** workflow manually;
pull requests changing the harness also run it. Both frameworks are published
with the SDK pinned in `global.json`, from the same source on one runner.

The three variants separate runtime-only effects (the identical net10 publish
directory on runtimes 10 and 11) from framework/build differences (net11 on 11).
Each run selects exact installed runtime versions with `dotnet exec --fx-version`
and disables roll-forward. Untimed host traces verify the selected CoreCLR path.
Environment metadata records source SHA, dirty state, SDK, runtimes, Git and CPU.
Child processes receive a minimal environment so CI branch/agent variables,
Git configuration overrides and inherited host tracing cannot alter the fixtures.
The workflow resolves installed patch versions once and records them; comparisons
across workflow runs must account for changing patches and runner hardware.

Two reproducible packed repositories contain 100 and 5,000 linear commits, fixed
timestamps and a version tag on the first commit. They are deliberately bounded fixtures, not a model
of every branching strategy or repository. Head IDs are retained with results.
Every case runs both backends and either bypasses GitVersion's cache or uses a
cache populated during warm-up. Bypass is not a cold operating-system disk cache.
Each invocation is a fresh process; repeated invocations are not in-process JIT
warm-up. Case order rotates between repetitions to reduce ordering bias.

GNU time records elapsed time, user/system CPU and peak RSS for each invocation.
The output includes all 15 samples per case, min/median/p95/max summaries, and
full version JSON. Every output must equal the same fixture's reference across
backends, runtimes and cache modes. Timing thresholds are intentionally absent;
exit failures, invalid metrics, runtime-selection failure and output differences
fail the experiment. Raw samples remain available after partial failures.

Local Linux reproduction (Node.js, Git, GNU time, SDK 11 and runtimes 10/11 needed):

```sh
dotnet publish src/GitVersion.App -c Release -f net10.0 --no-self-contained -o artifacts/runtime-app/net10
dotnet publish src/GitVersion.App -c Release -f net11.0 --no-self-contained -o artifacts/runtime-app/net11
node build/runtime-comparison.mjs artifacts/runtime-app/net10/gitversion.dll artifacts/runtime-app/net11/gitversion.dll <exact-runtime-10> <exact-runtime-11> artifacts/runtime-comparison
```

Use a new output directory on each run. For a quick harness smoke check set
`COMPARISON_REPETITIONS=2 COMPARISON_SIZES=10,30`; do not present smoke timings
as performance evidence. Download the workflow artifact to inspect `summary.json`,
`samples.json`, per-invocation output/metrics and host traces.

This initial experiment measures direct framework-dependent CLI execution only,
including process startup. It does not isolate SDK tool dispatch, allocations,
GC, individual calculation phases or decompression. It does not cover cold disks,
Windows/macOS, alternate CPUs, shallow repositories or complex merge histories.
Shared hosted-runner noise and RC2 maturity limit conclusions. Repeat promising
results on a controlled runner and GA before proposing production optimizations;
profile object inflation before considering a span-decoder rewrite.
