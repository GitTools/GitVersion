# SonarCloud coverage proof (#5221)

This draft validates the build and artifact handoff for a future SonarCloud coverage integration. It runs only for `arturcic/GitVersion:codex/5221-sonar-coverage` PRs. It does not run scanner `end`, upload an analysis, use a Sonar token or change automatic analysis. The current SonarCloud app continues to post its normal PR comments and status.

The additional `Sonar proof` jobs compile `src`, `new-cli` and `build` using SonarScanner for .NET 11.3.0 without credentials, then reuse the existing Ubuntu/net10.0/managed test reports from the same CI attempt. Tests are not rerun. A fresh runner checks the bundle's revision/run/PR identity, file hashes, C# project scope and source paths. It requires one Cobertura report for every legacy test project and verifies that owned covered sources appear in the analysis input.

Artifacts expire after seven days:

- `sonar-proof-<run>-<attempt>`: manifest, analysis output, source lists and the selected coverage reports.
- `sonar-proof-logs-<run>-<attempt>`: anonymous scanner preparation and compilation diagnostics.

A green proof means those data survived the handoff and mapped to the matching checkout. It does **not** prove SonarCloud coverage import, complete equivalence to automatic analysis, safe use of an untrusted artifact with credentials, or PR decoration by the new analysis. Hashes detect transport changes; they do not authenticate a malicious producer. Both proof jobs are secret-free and use the PR's code.

The future publisher needs trusted orchestration, independent GitHub API identity checks, a pinned/validated analysis-data contract and fresh trusted scanner configuration. Fork-provided executables, configuration and arbitrary project properties must never be run or trusted with credentials. A scanner's source list is data, not permission to read arbitrary paths. Simply moving this verifier into a secret-bearing `workflow_run` job is insufficient.

Before switching the existing SonarCloud project, prove coverage import and the SonarCloud app's own PR comments/status together on the exact analyzed revision, with a prepared rollback to automatic analysis. Automatic and CI analysis cannot coexist on the same SonarCloud project. This proof does not close #5221, and its branch-specific workflow must be replaced or removed before merging a production implementation.

Run the helper checks with:

```sh
python3 -m unittest discover -s .github/scripts -p 'test_sonar_proof.py' -v
```

References: [SonarScanner for .NET](https://docs.sonarsource.com/sonarqube-cloud/analyzing-source-code/scanners/sonarscanner-for-dotnet/using), [automatic analysis](https://docs.sonarsource.com/sonarqube-cloud/analyzing-source-code/automatic-analysis), [C# coverage parameters](https://docs.sonarsource.com/sonarqube-cloud/analyzing-source-code/test-coverage/test-coverage-parameters).
