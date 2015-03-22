# Contributing to GitVersion
We love contributions to get started contributing you might need:

 - [Get started with git](http://rogerdudler.github.io/git-guide)
 - [How to create a pull request](https://help.github.com/articles/using-pull-requests)
 - [An issue to work on](https://github.com/ParticularLabs/GitVersion/labels/up-for-grabs) - We are on [Up for grabs](http://up-for-grabs.net/), our up for grabs issues are tagged `up-for-grabs`
 - An understanding of our [architecture](#architecture) and how [we write tests](#writing-tests)

Once you know how to create a pull request and have an issue to work on, just post a comment saying you will work on it. 
If you end up not being able to complete the task, please post another comment so others can pick it up.

Issues are also welcome, [failing tests](#writing-tests) are even more welcome. 

# Architecture
GitVersion has three distict steps for calculating versions in v3.0.

1. If the current commit is tagged, the tag is used and build metadata (Excluding commit count) is added. The other two steps will not execute
2. A set of strategies are evaluated to decide on the base version and some metadata about that version. 
   These strategies include HighestReachableTag, NextVersionInConfig, MergedBranchWithVersion, VersionInBranchName etc.
3. The highest base version is selected, using that base version the new version is calculated. 

Visually it looks something like this:
![Version Calculation](http://www.plantuml.com:80/plantuml/png/fLCxJyCm4DxzAsuib4P914i69De1CS38Vd6kYIN7ZcodK8aVp-KX6Y2fKCbY9NV-7lVb2WoOeoVOMRDNfH0lz1vUoNbbpGwrR3K6ws1p3rlk-bN8u972f2AC3GHEbLN8m1D1Jjg-mPuXAZvx9kL1ZW1KY5dOZczMI0Pf54VnHtf7jpaAWJg0sW-uXw4PK3Eb1sMaevfCW6i1_0m6po1l7HfPJUxvu5XYUOHLWq5MLptCudmMK9--u5glJ0dIEaVo1Dw3JgVM6Km4cM9mzyrQXHuQHnj7chhl0JcnIrHjno1wiWtgfi8eWVK_7OQAmBHrJWvORFVM2PmrE7AcWZGh-Lj0FvptVvLiUPnCdG_XhNhOov9wQ1fzv7nw5S5EwSvw6CDQNfnMwUAP0XQyQpj70nkx3Nn3p5NFY9IshbNWepKi8ublWFiSPkC0ee8El75Dv5aOxqZQBScbWpWn0Pe2wb6aM1p4Eea_0G00)

[Edit Diagram](http://www.plantuml.com/plantuml/form?url=http://www.plantuml.com/plantuml/png/fLCxJyCm4DxzAsuib4P914i69De1CS38Vd6kYIN7ZcodK8aVp-KX6Y2fKCbY9NV-7lVb2WoOeoVOMRDNfH0lz1vUoNbbpGwrR3K6ws1p3rlk-bN8u972f2AC3GHEbLN8m1D1Jjg-mPuXAZvx9kL1ZW1KY5dOZczMI0Pf54VnHtf7jpaAWJg0sW-uXw4PK3Eb1sMaevfCW6i1_0m6po1l7HfPJUxvu5XYUOHLWq5MLptCudmMK9--u5glJ0dIEaVo1Dw3JgVM6Km4cM9mzyrQXHuQHnj7chhl0JcnIrHjno1wiWtgfi8eWVK_7OQAmBHrJWvORFVM2PmrE7AcWZGh-Lj0FvptVvLiUPnCdG_XhNhOov9wQ1fzv7nw5S5EwSvw6CDQNfnMwUAP0XQyQpj70nkx3Nn3p5NFY9IshbNWepKi8ublWFiSPkC0ee8El75Dv5aOxqZQBScbWpWn0Pe2wb6aM1p4Eea_0G00)

**\*** Some strategies allow the version to be incremented, others don't. More info below  
**+** This version is out of context with the rest of the example. It is here just to show what happens if the check is true

## Base Version Strategies
Currently we have the following strategies

 - `HighestTagBaseVersionStrategy` - Finds the highest reachable tag from the current branch
 - `VersionInBranchBaseVersionStrategy` - Extracts version information from the branch name. eg `release/3.0.0` will find `3.0.0`
 - `ConfigNextVersionBaseVersionStrategy` - Returns the version from the GitVersion.yaml file
 - `MergeMessageBaseVersionStrategy` - Finds version numbers from merge messages. eg. `Merge 'release/3.0.0' into 'master'` will return `3.0.0`
 - `FallbackBaseVersionStrategy` - Always returns 0.1.0 for new repositories

Each strategy needs to return an instance of `BaseVersion` which has the following properties

 - `Source` - Description of the source. eg `Merge message 'Merge 'release/3.0.0' into 'master''
 - `ShouldIncrement` - Some strategies should have the version incremented, others do not. eg `ConfigNextVersionBaseVersionStrategy` returns false, `HighestTagBaseVersionStrategy` returns true
 - `SemanticVersion` - SemVer of the base version strategy
 - `BaseVersionSource` - Sha of the source. Commits will be counted from this Sha. Can be null (eg ConfigNextVersionBaseVersionStrategy returns null)
 - `BranchNameOverride` - When `useBranchNameAsTag` is used, this allows the branch name to be changed by a base version.  
   VersionInBranchBaseVersionStrategy uses this to strip out anything before the first - or /. So `foo` ends up being evaluated as `foo`. If in doubt, just use null

# Writing Tests
We have made it super easy to write tests in GitVersion. Most tests you are interested in are in `GitVersionCore.Tests\IntegrationTests`. 

There is a scenario class for each type of branch. For example MasterScenarios, FeatureBranchScenarios etc.

## 1. Find Appropriate Scenario class
Find where your issue would logically sit. Or create a new scenario class if it doesn't fit anywhere in particular.

## 2. Create a test method
We are currently using NUnit, so just create a descriptive test method and attribute it with `[Test]`

## 3. Use a fixture
We have a few fixtures for different scenarios. 

 - `EmptyRepositoryFixture` - Gives you an empty git repo to start with
 - `RemoteRepositoryFixture` - A local repo tracking a test remote repository. The remote repo is available through the `Repository` property, the local is accessible via `LocalRepository`
 - `BaseGitFlowRepositoryFixture` - A repo setup for GitFlow (has a develop branch checked out ready to go)

You can use a fixture by just `using` it. Like this
``` csharp
using (var fixture = new EmptyRepositoryFixture(new Config()))
{
}
```

## 4. Customise config
If you are using non-default configuration just modify the `Config` class before creating the fixture

## 5. Writing the scenario
We have a number of extension method off `IRepository` to make it easy to write tests at the flow level and not worry about creating/commiting files.

An example test looks like this:
``` csharp
fixture.Repository.MakeATaggedCommit("1.0.0");
fixture.Repository.CreateBranch("feature-test");
fixture.Repository.Checkout("feature-test");
fixture.Repository.MakeACommit();
fixture.Repository.MakeCommits(4);

fixture.AssertFullSemver("1.0.1-test.1+5");
```

The last line is the most important. `AssertFullSemver` will run GitVersion and assert that the full SemVer it calculates is what you expect.

## 6. Submit a pull request with the failing test
Even better include the fix, but a failing test is a great start