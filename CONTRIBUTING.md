# Contributing to GitVersion

We love contributions to get started contributing you might need:

* [Get started with git](https://rogerdudler.github.io/git-guide)
* [How to create a pull request](https://help.github.com/articles/using-pull-requests)
* [An issue to work on](https://github.com/GitTools/GitVersion/labels/up-for-grabs) - We are on [Up for grabs](https://up-for-grabs.net/), our up for grabs issues are tagged `up-for-grabs`
* An understanding of our [architecture](https://gitversion.net/docs/learn/how-it-works#architecture) and how [we write tests](#writing-tests)

Once you know how to create a pull request and have an issue to work on, just post a comment saying you will work on it.
If you end up not being able to complete the task, please post another comment so others can pick it up.

Issues are also welcome, [failing tests](#writing-tests) are even more welcome.

## Contribution Guidelines

* Try to use feature branches rather than developing on main.
* Please include tests covering the change.
* The documentation is stored in the repository under the [`docs`](docs) folder.
    Have a look at the [documentation readme file](docs/readme.md) for guidance
    on how to improve the documentation and please include documentation updates
    with your PR.

## How it works

See [how it works](https://gitversion.net/docs/learn/how-it-works) in GitVersion's documentation

## Writing Tests

We have made it super easy to write tests in GitVersion. Most tests you are interested in are in `GitVersion.Core.Tests\IntegrationTests`.

There is a scenario class for each type of branch. For example MainScenarios, FeatureBranchScenarios etc.

### 1. Find Appropriate Scenario class

Find where your issue would logically sit. Or create a new scenario class if it doesn't fit anywhere in particular.

### 2. Create a test method

We are currently using NUnit, so just create a descriptive test method and attribute it with `[Test]`

### 3. Create a configuration

We use a builder pattern to create a configuration. You can use the `GitFlowConfigurationBuilder` or `GitHubConfigurationBuilder` or `EmptyConfigurationBuilder` to create a configuration builder.

```csharp
var configurationBuilder = GitFlowConfigurationBuilder.New;
```

We can then customize the configuration by chaining methods of the builder. At the end we build the configuration.

For example:

```csharp
var configuration = configurationBuilder
    .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
    .WithNextVersion("1.0.0")
    .Build();
```

### 4. Use a fixture

We have a few fixtures for different scenarios.

* `EmptyRepositoryFixture` - Gives you an empty git repo to start with
* `RemoteRepositoryFixture` - A local repo tracking a test remote repository. The remote repo is available through the `Repository` property, the local is accessible via `LocalRepository`
* `BaseGitFlowRepositoryFixture` - A repo setup for GitFlow (has a develop branch checked out ready to go)

You can use a fixture by just `using` it. Like this

```csharp
using var repo = new EmptyRepositoryFixture();
```

### 5. Writing the scenario

We have a number of extension method off `IRepository` to make it easy to write tests at the flow level and not worry about creating/commiting files.

An example test looks like this:

```csharp
fixture.Repository.MakeATaggedCommit("1.0.0");
fixture.Repository.CreateBranch("feature-test");
fixture.Repository.Checkout("feature-test");
fixture.Repository.MakeACommit();
fixture.Repository.MakeCommits(4);

fixture.AssertFullSemver("1.0.1-test.1-5", configuration);
```

The last line is the most important. `AssertFullSemver` will run GitVersion and assert that the full SemVer it calculates is what you expect.

### 6. Submit a pull request with the failing test

Even better include the fix, but a failing test is a great start

## Release Process

We use Cake for our build and deployment process. The way the release process is setup is:

1. We build releasable artifacts with GitHub Actions
2. We create a milestone for the release if it's not already created. Our milestones are named using the semver.
    For example `5.12.0` or `6.0.0-beta.2`
3. We move all the closed issues and closed pull requests that are going to be included in the release to the milestone.
4. We check that all the issues and pull requests that are going to be included in the release have a label assigned,
    otherwise it will fail the release.
5. We create a release in the GitHub UI, and create a tag and name it using the milestone name. For example `5.12.0` or `6.0.0-beta.2`
6. We specify if the release is a pre-release or latest release in the GitHub UI.
7. We publish the release.
8. The GitHub Actions will create a GitHub release and publish the artifacts to NuGet, Chocolatey, Docker, Homebrew
    and other distribution channels.
9. The issues and pull requests will get updated with message specifying in which release it was included.

## Code Style

In order to apply the code style defined by by the `.editorconfig` file you can use [`dotnet-format`](https://github.com/dotnet/format).

Change to the root folder of the GitVersion repository and use the following command to apply the code style:

```shell
dotnet format ./src/ --exclude **/AddFormats/
```

## Documentation

The documentation is stored in the repository under the [`docs`](docs) folder.
Have a look at the [documentation readme file](docs/readme.md) for guidance.

In order to check locally how the documentation looks like you can use the following command:

```shell
./build.ps1 -Stage docs -Target PreviewDocs
```

## Schemas generation

If there are changes to the GitVersionVariables or to the GitVersionConfiguration, the following command should be executed to update the schema files:

```shell
./build.ps1 -Stage build -Target BuildPrepare
./build.ps1 -Stage docs -Target GenerateSchemas
```
