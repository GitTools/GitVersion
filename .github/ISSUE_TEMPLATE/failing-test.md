---
name: Failing test
about: Describe the scenario you have as a unit test.
title: "[Failing test]"
labels: failing test case
assignees: ''

---

**Describe the bug**
A clear and concise description of what the bug is or a link to it

## Test code

```csharp
using var fixture = new EmptyRepositoryFixture();
fixture.Repository.MakeACommit();
fixture.BranchTo("develop");
fixture.Repository.MakeCommits(3);

fixture.AssertFullSemver("0.1.0-alpha.1");
```
