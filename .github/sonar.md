# SonarCloud analysis and coverage

CI prepares SonarScanner for .NET without credentials on exactly one unit-test
matrix leg: **Ubuntu 24.04 / .NET 10 / managed Git backend**. Cake builds and tests
`src` after preparation. The same job then builds `new-cli` and the build solution
under the scanner, without rerunning the existing tests. Temporary path-based
project IDs distinguish identically named projects across solutions.

The leg uploads analysis data and the existing test-results artifact containing
Cobertura coverage. Names include the run and attempt. Other legs retain their
normal tests. Upstream push and PR runs, including fork and Dependabot PRs, produce
the artifacts; docs-only changes also run CI. Merge-queue and release-dispatch runs
do not produce Sonar artifacts.

## Trusted publication

**Sonar analysis publisher** starts automatically after CI succeeds. The trigger
is workflow-wide; the receiver verifies that the latest source attempt contains
exactly one successful canonical unit-test job, then downloads its named artifacts.
It resolves the current PR/head/merge revision or branch revision through GitHub.

The import script is loaded from the trusted default-branch workflow revision.
The source checkout is data only: the publisher does not run fork build scripts,
tests, package restores or downloaded executables. Before loading a Sonar token,
the script checks source ownership, project inventory, file types/limits and
coverage, and reconstructs project metadata with fixed scanner settings. Producer
and publisher use identical workspace paths for the scanner's binary analysis data.

When publication is enabled, a fresh trusted scanner performs authenticated
preparation. The script compares analyzer/rules fingerprints, installs staged
analysis data and coverage, and the scanner finalizes and uploads the report.
The source head is checked again immediately before upload. The scanner waits for
the quality gate; Sonar's installed GitHub app supplies its own PR comment/check.

## Activation and acceptance

`SONAR_CI_PUBLISH` must be exactly `true` to load the token and upload. Leave it unset
until the hosted handoff, import safety and coverage/decoration acceptance checks
are complete. The token comes from `op://gittools/ci/sonarcloud/token` through the
existing 1Password integration. No custom C# application or Python is used.

The receiver must first exist on upstream `main` for `workflow_run` to trigger it.
Before cutover, verify actual imported coverage and current-revision Sonar comments
and checks for fork, same-repository and Dependabot PRs, plus main and merge-queue
behavior. Artifact validation alone is not a completed Sonar analysis.

Keep automatic analysis enabled during bootstrap. At cutover, disable it once in
SonarCloud under **Administration → Analysis Method**, retain the GitHub app and
repository binding, and enable CI publication. Do not run competing automatic and
CI analyses on the same project. On rollback, disable publication, drain in-flight
publishers, restore automatic analysis and verify current PR checks. The workflows
never change the Sonar analysis-mode setting themselves.

Related to #5221; close it only after the automatic path meets these acceptance
conditions, not merely after merging the workflows.

References: [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube-cloud/analyzing-source-code/scanners/sonarscanner-for-dotnet/using),
[GitHub workflow_run](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_run).
