# SonarScanner fork PR proof

The **Sonar reviewed PR proof** job in the existing **CI** workflow uses the pinned `dotnet-sonarscanner`
tool directly: `begin`, build, test with OpenCover coverage, then `end`. It targets
`GitTools_GitVersion` with explicit PR and commit properties, allowing SonarCloud
to provide its own PR comment and Code Analysis check. Related to #5221.

This is a controlled proof for a reviewed commit, not unattended analysis of
arbitrary fork contributions. Dispatching it trusts the selected commit's build,
dependencies and tests with the Sonar credential. Step-scoped environment
variables do not isolate a credential from code running elsewhere in that job.
Do not add `pull_request_target` or `workflow_run` triggers to this workflow.

Manual dispatch runs only the Sonar job; normal CI jobs, including publication,
are skipped. Push, pull request, merge queue and release-dispatch runs keep their
existing behavior and skip the credentialed Sonar job.

## Run the proof

1. The updated CI workflow must exist on upstream `main`. Review the entire selected PR
   revision, including build scripts and dependencies, and copy its full head SHA.
2. Coordinate a short analysis window: let automatic analyses finish, then disable
   automatic analysis in SonarCloud. This is project-wide; do not leave it disabled
   after the proof. The workflow checks the setting and never changes it itself.
3. Dispatch **CI** on `main`, supplying the upstream PR number
   and reviewed head SHA. It rejects a closed PR or changed head. The existing
   1Password integration supplies `op://gittools/ci/sonarcloud/token` using the
   repository's `OP_SERVICE_ACCOUNT_TOKEN` secret.
4. Verify the completed Sonar analysis revision, imported coverage, and Sonar's own
   check/comment on that PR. The scanner waits for the quality gate; upload success
   alone is not acceptance. The workflow does not manufacture a replacement comment.
5. Restore automatic analysis after the run, including on failure or cancellation,
   and confirm normal PR analysis resumes. No permanent cutover is made by this PR.

All three solutions are built under the scanner. A temporary MSBuild targets file
assigns stable path-based project IDs because `src` and `new-cli` contain projects
with identical names. Coverage is freshly collected from the seven existing `src`
test projects on Ubuntu/net10/managed. This proof does not add coverage collection
to `new-cli` or the build tooling, and does not change normal CI, Codecov or MTP
reporting. No custom C# publisher, Python converter or scanner-artifact handoff is used.

The proof is complete only when a hosted run demonstrates coverage and decoration
on the selected fork PR revision. Safe unattended fork analysis and a permanent
CI-analysis cutover remain separate work; do not close #5221 on this proof alone.

References: [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube-cloud/analyzing-source-code/scanners/sonarscanner-for-dotnet/using),
[.NET coverage](https://docs.sonarsource.com/sonarqube-cloud/analyzing-source-code/test-coverage/dotnet-test-coverage),
[GitHub fork workflow permissions](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#pull_request).
