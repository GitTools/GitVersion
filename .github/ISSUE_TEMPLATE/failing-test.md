---
name: Failing test
about: Describe this issue template's purpose here.
title: "[Failing test]"
labels: failing test case
assignees: ''

---

**Describe the bug**
A clear and concise description of what the bug is or a link to it

## Test code
```
using var fixture = new EmptyRepositoryFixture();
fixture.Repository.MakeACommit();
fixture.BranchTo("develop");
fixture.Repository.MakeCommits(3);

fixture.AssertFullSemver("0.1.0-alpha.1");
```
