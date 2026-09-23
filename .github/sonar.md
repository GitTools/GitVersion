# SonarCloud CI analysis

Issue [#5221](https://github.com/GitTools/GitVersion/issues/5221) adds coverage to SonarCloud while retaining **SonarCloud's own PR comment and Code Analysis check**. The project binding and quality gate remain unchanged. Codecov, MTP reporting and JUnit artifacts are independent and remain in place.

## Deployment state

Publication is **disabled unless `SONAR_CI_PUBLISH` is exactly `true`**. Keep that variable unset through bootstrap and acceptance. Automatic analysis continues to supply PR decoration meanwhile. The receiver workflow must first exist on upstream `main`; GitHub does not start a new `workflow_run` receiver solely because it appears in a PR.

This is a staged migration. Do not enable unattended publication until the scanner-format security review, hostile-input canary rehearsal and supported merge-queue check mapping are accepted. XML/path validation and artifact hashes do not establish that opaque Roslyn protobuf, UCFG or architecture data is safe to parse with a credential. Those formats remain a rollout blocker, not a claim made by the validator. A manual approval on every fork would not meet the unattended-publication requirement.

## Producer

`CI` invokes `_sonar_build.yml` for upstream push and PR events, including fork and docs-only PRs. It builds all three solutions using SonarScanner for .NET **11.3.0**, without Sonar/1Password credentials. Cake's `SonarProjectIds` task generates stable IDs from repository-relative project paths, so the same project names in `src` and `new-cli` remain distinct.

The job reuses the seven existing Ubuntu 24.04 / net10.0 / managed Cobertura reports from the same run and attempt. It runs only the new C# helper tests additionally, with their own coverage. Cake's `SonarCollect` task validates the project/report inventory and creates `sonar-build-<run>-<attempt>` with seven-day retention. The original test reports are not edited. No Python helper or Python coverage dependency is used.

The schema-2 manifest records repository, event, run/attempt, head SHA, tested merge SHA, base SHA, PR/branch identity, scanner version, source root, file sizes/hashes and scanner configuration fingerprints. The manifest is an assertion, not trusted provenance.

## Receiver and publisher

`sonar_publish.yml` builds its helper from the trusted upstream workflow revision in a separate credential-free job. The receiver downloads only that run's tooling. It never builds, restores packages from, or runs Cake/scripts from the PR.

Before loading a Sonar credential, it independently resolves the upstream CI workflow/run, latest successful attempt, exact source repository and current PR head/base. Empty fork PR associations are resolved through GitHub's commit-to-PR API. It checks the merge commit's parents, downloads only the named run/attempt artifact and validates archive types, paths, sizes, count, inventory, source mapping and project/report completeness. PR sources are materialized as regular git blobs; no checkout filters, hooks, symlinks or submodules are executed. Source and scanner tooling live separately.

A fresh anonymous scanner preparation checks configuration compatibility. The authenticated preparation repeats that check before importing sanitized project metadata and coverage. Fork scanner configuration and binaries are never restored. Opaque analyzer paths currently require identical producer/publisher Ubuntu workspace paths; a mismatch fails instead of partially remapping them.

With publication enabled, the pinned 1Password action loads only `op://gittools/ci/sonarcloud/token`, with `export-env: false`. `OP_SERVICE_ACCOUNT_TOKEN` is confined to that action and is not passed to scanner subprocesses. Use an analysis-scoped Sonar credential for permanent operation; the publisher has no operation to toggle automatic analysis.

The receiver checks freshness again immediately before submission and after processing. Submissions and manual recovery are serialized per independently resolved PR or branch. Scanner/compute/coverage/decoration errors fail visibly. Sonar controls the actual quality gate; an upload exit code cannot manufacture a passing check. An old server task can finish after a newer commit appears, but it must retain the old head identity and cannot validate the new head.

## Validation and recovery

Run the focused helper suite using the repository's MTP runner:

```sh
dotnet test --project build/sonar/Sonar.Tests/Sonar.Tests.csproj --configuration Release
```

For local producer debugging in a normal checkout, build `build/CI.slnx` in Release and run `dotnet run/build.dll --target=BuildPrepare`, then invoke `dotnet run/build.dll --target=SonarProjectIds --sonar-targets=<absolute-targets-file>`. `SonarCollect` accepts `--sonar-coverage`, `--sonar-bundle` and `--sonar-identity`. The existing Cake root discovery requires a `.git` directory; in a linked worktree use the C# executable directly. It exposes `project-ids`, `identity`, `collect`, `verify`, `resolve`, `admit` and `publish`; workflow invocations show the argument order.

A failed source run is never published. Missing reports require a new complete CI attempt, not a mixture of artifacts from multiple attempts. Configuration drift requires rebuilding with the current Sonar rules/analyzers. Stale head/base errors require current CI. For infrastructure recovery, dispatch **Sonar publisher on main**, supplying the successful upstream `run_id` and its exact latest `run_attempt`; recovery uses the same admission rules. Check Sonar's activity page before retrying a timed-out submission.

Before permanent activation, record evidence for fork and same-repository PRs, a newer commit overtaking an older run, retries, docs-only changes, main analysis, a genuine failed gate, missing/malicious artifacts, forbidden-file/network canaries and merge groups. `merge_group` publication currently fails closed because its Sonar check/revision mapping is not yet established; resolve this before changing the required-check mechanism.

## Cutover and rollback

1. Merge reviewed tooling and receiver with `SONAR_CI_PUBLISH` unset and automatic analysis on. Observe admission runs first.
2. Complete independent scanner-format/security review and the hosted acceptance matrix. Rehearse authenticated publication in a coordinated, bounded window; automatic and CI analysis must not run concurrently.
3. Obtain final cutover approval, disable automatic analysis in SonarCloud and enable `SONAR_CI_PUBLISH`. Analyze main and current representative PR heads. Verify Sonar's own comments/checks and actual coverage, with the existing gate unchanged.
4. On regression, unset `SONAR_CI_PUBLISH`, drain running publishers, restore automatic analysis and verify/retrigger affected current PR checks. Do not alternate analysis modes per PR.

Close #5221 only after production acceptance, not merely after merging this disabled bootstrap.

References: [scanner begin/build/end](https://docs.sonarsource.com/sonarqube-cloud/analyzing-source-code/scanners/sonarscanner-for-dotnet/using), [workflow_run and privileged artifacts](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_run), [1Password secret outputs](https://github.com/1Password/load-secrets-action).
