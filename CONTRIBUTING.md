# Contributing to GitVersion
We love contributions to get started contributing you might need:

 - [Get started with git](http://rogerdudler.github.io/git-guide)
 - [How to create a pull request](https://help.github.com/articles/using-pull-requests)
 - [An issue to work on](https://github.com/GitTools/GitVersion/labels/up-for-grabs) - We are on [Up for grabs](http://up-for-grabs.net/), our up for grabs issues are tagged `up-for-grabs`
 - An understanding of our [http://gitversion.readthedocs.org/en/latest/more-info/how-it-works/#architecture](#architecture) and how [we write tests](#writing-tests)

Once you know how to create a pull request and have an issue to work on, just post a comment saying you will work on it.
If you end up not being able to complete the task, please post another comment so others can pick it up.

Issues are also welcome, [failing tests](#writing-tests) are even more welcome.

# Contribution Guidelines
 - Try to use feature branches rather than developing on master.
 - Please include tests covering the change.
 - The documentation is stored in the repository under the [`docs`](docs) folder.
   Have a look at the [documentation readme file](docs/readme.md) for guidance
   on how to improve the documentation and please include documentation updates
   with your PR.

# How it works
See [how it works](http://gitversion.readthedocs.org/en/latest/more-info/how-it-works/) in GitVersion's documentation

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
