using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Caching;
using LibGit2Sharp;
using CommitFilter = GitVersion.Git.CommitFilter;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class BranchContextTests : TestBase
{
    [TestCase("feature/upper", "feature/lower", "feature/upper")]
    [TestCase(null, "feature/lower", "feature/lower")]
    [TestCase("", "feature/lower", "feature/lower")]
    [TestCase(" \t", "feature/lower", "feature/lower")]
    [TestCase("refs/heads/feature/work", null, "feature/work")]
    [TestCase("refs/remotes/origin/feature/work", null, "feature/work")]
    [TestCase("origin/feature/work", null, "origin/feature/work")]
    [TestCase("feature/é", null, "feature/é")]
    [TestCase("@", null, "@")]
    public void ResolvesEnvironmentAliases(string? upper, string? alias, string expected)
    {
        var input = Resolve(upper, alias);
        input.ContextBranch.ShouldNotBeNull().Friendly.ShouldBe(expected);
        input.TargetBranch.ShouldBeNull();
    }

    [TestCase(null, null)]
    [TestCase("", "")]
    [TestCase(" \t", "\r\n")]
    public void BlankAliasesFallBackToProvider(string? upper, string? alias)
    {
        var input = Resolve(upper, alias);
        input.ContextBranch.ShouldBeNull();
        input.TargetBranch.ShouldBe("refs/heads/provider");
    }

    [TestCase("HEAD")]
    [TestCase("-branch")]
    [TestCase("a..b")]
    [TestCase("a@{1}")]
    [TestCase("@{-1}")]
    [TestCase("a b")]
    [TestCase("a\tb")]
    [TestCase("a\u007fb")]
    [TestCase("a~b")]
    [TestCase("a^b")]
    [TestCase("a:b")]
    [TestCase("a?b")]
    [TestCase("a*b")]
    [TestCase("a[b")]
    [TestCase("a\\b")]
    [TestCase("a/")]
    [TestCase("/a")]
    [TestCase("a//b")]
    [TestCase(".a")]
    [TestCase("a/.b")]
    [TestCase("a.")]
    [TestCase("a.lock/b")]
    [TestCase("a/b.lock")]
    [TestCase("refs/heads/")]
    [TestCase("refs/remotes//feature/work")]
    [TestCase("refs/remotes/origin")]
    [TestCase("refs/tags/1.2.3")]
    [TestCase("refs/pull/42/merge")]
    public void RejectsInvalidEnvironmentBranch(string name)
        => Should.Throw<WarningException>(() => Resolve(name, null)).Message.ShouldContain("Invalid branch name");

    [Test]
    public void ExplicitTargetWinsEvenOverInvalidEnvironment()
    {
        var input = Resolve("bad name", "other", "feature/explicit");
        input.TargetBranch.ShouldBe("feature/explicit");
        input.ContextBranch.ShouldBeNull();
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(false, true, true)]
    [TestCase(true, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(true, true, true)]
    public void OverridePreservesCheckoutAndExistingRef(bool detached, bool existing, bool noNormalize)
    {
        using var fixture = new EmptyRepositoryFixture();
        var repository = fixture.Repository;
        repository.MakeATaggedCommit("1.0.0");
        var ancestor = repository.Head.Tip;
        if (existing)
        {
            repository.CreateBranch("feature/work", ancestor);
        }
        var head = repository.MakeACommit();
        if (detached)
        {
            Commands.Checkout(repository, head);
        }
        AddUnavailableRemote(repository);
        var beforeRefs = SnapshotRefs(repository);
        var environment = GitHubEnvironment("refs/heads/provider");
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/work");
        using var services = CreateServices(fixture.RepositoryPath, environment, noNormalize: noNormalize);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var context = services.GetRequiredService<Lazy<GitVersionContext>>().Value;

        variables.BranchName.ShouldBe("feature/work");
        variables.Sha.ShouldBe(head.Sha);
        variables.FullSemVer.ShouldBe("1.0.1-work.1+1");
        variables.VersionSourceSha.ShouldBe(ancestor.Sha);
        variables.VersionSourceDistance.ShouldBe("1");
        context.CurrentBranch.Tip.ShouldNotBeNull().Sha.ShouldBe(head.Sha);
        context.CurrentBranchCommits.Select(commit => commit.Sha).ShouldBe([head.Sha, ancestor.Sha]);
        services.GetRequiredService<IRepositoryStore>().FindBranch(context.CurrentBranch.Name).ShouldBeSameAs(context.CurrentBranch);
        repository.Head.Tip.Sha.ShouldBe(head.Sha);
        repository.Info.IsHeadDetached.ShouldBe(detached);
        SnapshotRefs(repository).ShouldBe(beforeRefs);
    }

    [TestCase("GIT_BRANCH")]
    [TestCase("Git_Branch")]
    public void LocalBuildAcceptsMissingContextBranch(string variable)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var head = fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, head);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(variable, "feature/local");
        using var services = CreateServices(fixture.RepositoryPath, environment);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.BranchName.ShouldBe("feature/local");
        variables.Sha.ShouldBe(head.Sha);
        fixture.Repository.Branches["feature/local"].ShouldBeNull();
        fixture.Repository.Info.IsHeadDetached.ShouldBeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HistoricalTagRetainsCheckedOutCommit(bool annotated)
    {
        using var fixture = new EmptyRepositoryFixture();
        var repository = fixture.Repository;
        var tagged = repository.MakeACommit();
        if (annotated)
        {
            repository.ApplyTag("1.2.3", tagged.Author, "release");
        }
        else
        {
            repository.ApplyTag("1.2.3");
        }
        repository.MakeACommit();
        Commands.Checkout(repository, tagged);
        AddUnavailableRemote(repository);
        var environment = GitHubEnvironment("refs/tags/1.2.3");
        environment.SetEnvironmentVariable("GITHUB_REF_TYPE", "tag");
        environment.SetEnvironmentVariable("GIT_BRANCH", "main");
        var beforeRefs = SnapshotRefs(repository);
        using var services = CreateServices(fixture.RepositoryPath, environment);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.FullSemVer.ShouldBe("1.2.3");
        variables.BranchName.ShouldBe("main");
        variables.Sha.ShouldBe(tagged.Sha);
        repository.Info.IsHeadDetached.ShouldBeTrue();
        SnapshotRefs(repository).ShouldBe(beforeRefs);
    }

    [Test]
    public void PullRequestOverrideVersionsMergeRatherThanSourceTip()
    {
        using var fixture = new EmptyRepositoryFixture();
        var repository = fixture.Repository;
        repository.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/work");
        var source = repository.MakeACommit();
        fixture.Checkout(MainBranch);
        repository.MakeACommit();
        repository.Merge(repository.Branches["feature/work"], source.Author, new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward });
        var merge = repository.Head.Tip;
        Commands.Checkout(repository, merge);
        AddUnavailableRemote(repository);
        var environment = GitHubEnvironment("refs/pull/42/merge");
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/work");
        using var services = CreateServices(fixture.RepositoryPath, environment);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var context = services.GetRequiredService<Lazy<GitVersionContext>>().Value;

        variables.BranchName.ShouldBe("feature/work");
        variables.Sha.ShouldBe(merge.Sha);
        context.CurrentCommit.Parents.Count.ShouldBe(2);
        context.CurrentBranchCommits.Select(commit => commit.Sha).ShouldContain(source.Sha);
        repository.Branches["feature/work"].Tip.Sha.ShouldBe(source.Sha);
        repository.Info.IsHeadDetached.ShouldBeTrue();
    }

    [Test]
    public void ExplicitApiTargetKeepsBranchTipSelection()
    {
        using var fixture = new EmptyRepositoryFixture();
        var repository = fixture.Repository;
        repository.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/target");
        var target = repository.MakeACommit();
        fixture.Checkout(MainBranch);
        var head = repository.MakeACommit();
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/context");
        using var services = CreateServices(fixture.RepositoryPath, environment, targetBranch: "feature/target");

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.Sha.ShouldBe(target.Sha);
        variables.BranchName.ShouldBe("feature/target");
        repository.Head.Tip.Sha.ShouldBe(head.Sha);
    }

    [Test]
    public void CacheSeparatesBranchContextsAndTargetSelection()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var head = fixture.Repository.MakeACommit();
        var results = new List<string>();
        foreach (var branch in new[] { "feature/one", "feature/two", "feature/one" })
        {
            var environment = new TestEnvironment();
            environment.SetEnvironmentVariable("GIT_BRANCH", branch);
            using var services = CreateServices(fixture.RepositoryPath, environment, noCache: false);
            var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
            variables.BranchName.ShouldBe(branch);
            variables.Sha.ShouldBe(head.Sha);
            results.Add(variables.FullSemVer);
            services.GetRequiredService<IGitVersionCacheProvider>().LoadVersionVariablesFromDiskCache()
                .ShouldNotBeNull().BranchName.ShouldBe(branch);
        }
        results[0].ShouldNotBe(results[1]);
        results[2].ShouldBe(results[0]);

        var explicitEnvironment = new TestEnvironment();
        explicitEnvironment.SetEnvironmentVariable("GIT_BRANCH", "feature/one");
        using var targetServices = CreateServices(fixture.RepositoryPath, explicitEnvironment, targetBranch: MainBranch, noCache: false);
        targetServices.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables().BranchName.ShouldBe(MainBranch);
    }

    [Test]
    public void CacheSeparatesContextFromSameNamedExplicitTarget()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var head = fixture.Repository.MakeACommit();
        fixture.BranchTo("feature/work");
        var target = fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, head);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GIT_BRANCH", "refs/heads/feature/work");
        using (var contextServices = CreateServices(fixture.RepositoryPath, environment, noCache: false))
        {
            contextServices.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables().Sha.ShouldBe(head.Sha);
            contextServices.GetRequiredService<IGitVersionCacheProvider>().LoadVersionVariablesFromDiskCache()
                .ShouldNotBeNull().Sha.ShouldBe(head.Sha);
        }
        using var targetServices = CreateServices(fixture.RepositoryPath, environment,
            targetBranch: "refs/heads/feature/work", noCache: false);

        var variables = targetServices.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.Sha.ShouldBe(target.Sha);
        variables.BranchName.ShouldBe("feature/work");
        fixture.Repository.Head.Tip.Sha.ShouldBe(head.Sha);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ContextMatchesRealBranchDespiteDivergentNamedRef(bool mainline)
    {
        using var fixture = new EmptyRepositoryFixture();
        var repository = fixture.Repository;
        repository.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/work");
        repository.MakeATaggedCommit("9.0.0");
        var other = repository.Head.Tip;
        fixture.Checkout(MainBranch);
        var head = repository.MakeACommit();
        Commands.Checkout(repository, head);
        var configuration = mainline
            ? GitHubFlowConfigurationBuilder.New.WithVersionStrategy(VersionStrategies.Mainline).Build()
            : GitHubFlowConfigurationBuilder.New.Build();
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/work");
        string contextualVersion;
        using (var services = CreateServices(fixture.RepositoryPath, environment, configuration: configuration))
        {
            var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
            variables.Sha.ShouldBe(head.Sha);
            variables.Major.ShouldBe("1");
            contextualVersion = variables.FullSemVer;
            var branch = services.GetRequiredService<Lazy<GitVersionContext>>().Value.CurrentBranch;
            var store = services.GetRequiredService<IRepositoryStore>();
            store.FindBranch(branch.Name).ShouldBeSameAs(branch);
            var physicalBranch = services.GetRequiredService<IGitRepository>().Branches[branch.Name.Canonical].ShouldNotBeNull();
            var branches = new ContextualBranchCollection(services.GetRequiredService<IGitRepository>().Branches, (ContextualBranch)branch);
            store.ExcludingBranches([]).Single(candidate => candidate.Name.Canonical == branch.Name.Canonical).ShouldBeSameAs(branch);
            foreach (var excluded in new[] { branch, physicalBranch })
            {
                var remaining = store.ExcludingBranches([excluded]).ToArray();
                remaining.ShouldNotContain(candidate => candidate.Name.Canonical == branch.Name.Canonical);
                remaining.ShouldContain(candidate => candidate.Name.Friendly == MainBranch);
                Should.Throw<InvalidOperationException>(() => branches.UpdateTrackedBranch(excluded, "refs/remotes/origin/feature/work"))
                    .Message.ShouldBe("Calculation branch context cannot update repository refs.");
            }
            store.GetCommitsReacheableFrom(branch.Tip.ShouldNotBeNull(), branch).Single().Sha.ShouldBe(head.Sha);
            services.GetRequiredService<IGitRepository>().Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = branch.Tip,
                ExcludeReachableFrom = branch
            }).ShouldBeEmpty();
        }
        repository.Branches["feature/work"].Tip.Sha.ShouldBe(other.Sha);
        // A physical branch at the same commit is the behavioral oracle.
        repository.Branches.Remove("feature/work");
        repository.CreateBranch("feature/work", head);
        fixture.Checkout("feature/work");
        using var actualServices = CreateServices(fixture.RepositoryPath, new TestEnvironment(), configuration: configuration);
        actualServices.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables().FullSemVer.ShouldBe(contextualVersion);
    }

    [Test]
    public void ContextPinsHeadWithNonMonotonicCommitDatesAndIgnoredHead()
    {
        using var fixture = new EmptyRepositoryFixture();
        var repository = fixture.Repository;
        repository.MakeATaggedCommit("1.0.0");
        var parent = repository.MakeACommit();
        var when = parent.Committer.When.AddDays(-1);
        var signature = new Signature("Test", "test@example.com", when);
        var head = repository.Commit("older timestamp", signature, signature, new CommitOptions { AllowEmptyCommit = true });
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/work");
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(IgnoreConfigurationBuilder.New.WithShas(head.Sha).Build()).Build();
        using var services = CreateServices(fixture.RepositoryPath, environment, configuration: configuration);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
        var context = services.GetRequiredService<Lazy<GitVersionContext>>().Value;

        variables.Sha.ShouldBe(head.Sha);
        context.CurrentBranchCommits.Select(commit => commit.Sha).ShouldContain(parent.Sha);
        context.CurrentBranchCommits.Select(commit => commit.Sha).ShouldContain(head.Sha);
        context.Configuration.Ignore.Filter(context.CurrentBranchCommits).Select(commit => commit.Sha).ShouldNotContain(head.Sha);
    }

    [Test]
    public void ExplicitCommitRetainsLegacySelectionUnderContext()
    {
        using var fixture = new EmptyRepositoryFixture();
        var repository = fixture.Repository;
        repository.MakeATaggedCommit("1.0.0");
        var selected = repository.MakeACommit();
        var head = repository.MakeACommit();
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/work");
        using var services = CreateServices(fixture.RepositoryPath, environment,
            configure: options => options.RepositoryInfo.CommitId = selected.Sha);
        var store = services.GetRequiredService<IRepositoryStore>();
        var configuration = services.GetRequiredService<Lazy<IGitVersionConfiguration>>().Value;
        var expected = store.GetCurrentCommit(store.Head, selected.Sha, configuration.Ignore).ShouldNotBeNull();

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.Sha.ShouldBe(expected.Sha);
        variables.BranchName.ShouldBe("feature/work");
        repository.Head.Tip.Sha.ShouldBe(head.Sha);
    }

    [TestCase("TF_BUILD", "BUILD_SOURCEBRANCH")]
    [TestCase("JENKINS_URL", "BRANCH_NAME")]
    [TestCase("JENKINS_URL", "GIT_LOCAL_BRANCH")]
    public void EnvironmentWinsOverProviderBranch(string marker, string providerVariable)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var head = fixture.Repository.MakeACommit();
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(marker, "true");
        environment.SetEnvironmentVariable(providerVariable, MainBranch);
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/environment");
        environment.SetEnvironmentVariable("Git_Branch", "feature/alias");
        using var services = CreateServices(fixture.RepositoryPath, environment, noNormalize: true);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.BranchName.ShouldBe("feature/environment");
        variables.Sha.ShouldBe(head.Sha);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    public void BlankOverrideUsesAzureProviderAndThenHead(string? value)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var head = fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("feature/provider", head);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("TF_BUILD", "true");
        environment.SetEnvironmentVariable("GIT_BRANCH", value);
        environment.SetEnvironmentVariable("Git_Branch", value);
        environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", "refs/heads/feature/provider");
        using (var services = CreateServices(fixture.RepositoryPath, environment, noNormalize: true))
        {
            var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
            variables.BranchName.ShouldBe("feature/provider");
            variables.Sha.ShouldBe(head.Sha);
        }
        environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", null);
        using var headServices = CreateServices(fixture.RepositoryPath, environment, noNormalize: true);
        headServices.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables().BranchName.ShouldBe(MainBranch);
    }

    [TestCase("TF_BUILD")]
    [TestCase("GITHUB_ACTIONS")]
    public void ExplicitTargetControlsNormalizationAheadOfEnvironmentAndProvider(string marker)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var head = fixture.Repository.MakeACommit();
        AddUnavailableRemote(fixture.Repository);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(marker, "true");
        environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", "refs/heads/feature/provider");
        environment.SetEnvironmentVariable("GITHUB_REF", "refs/heads/feature/provider");
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/environment");
        using var services = CreateServices(fixture.RepositoryPath, environment, targetBranch: MainBranch);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.BranchName.ShouldBe(MainBranch);
        variables.Sha.ShouldBe(head.Sha);
        fixture.Repository.Branches["feature/environment"].ShouldBeNull();
        fixture.Repository.Branches["feature/provider"].ShouldBeNull();
    }

    [TestCase("GIT_BRANCH")]
    [TestCase("Git_Branch")]
    public void TeamCityUsesEitherAlias(string variable)
    {
        using var fixture = new EmptyRepositoryFixture();
        var head = fixture.Repository.MakeACommit();
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("TEAMCITY_VERSION", "2026.1");
        environment.SetEnvironmentVariable(variable, "feature/teamcity");
        using var services = CreateServices(fixture.RepositoryPath, environment, noNormalize: true);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.BranchName.ShouldBe("feature/teamcity");
        variables.Sha.ShouldBe(head.Sha);
        services.GetRequiredService<ICurrentBuildAgent>().PreventFetch().ShouldBeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t")]
    public void BlankTeamCityAliasesDoNotPreventFetch(string? value)
    {
        using var fixture = new EmptyRepositoryFixture();
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("TEAMCITY_VERSION", "2026.1");
        environment.SetEnvironmentVariable("GIT_BRANCH", value);
        environment.SetEnvironmentVariable("Git_Branch", value);
        using var services = CreateServices(fixture.RepositoryPath, environment);

        services.GetRequiredService<ICurrentBuildAgent>().PreventFetch().ShouldBeFalse();
        services.GetRequiredService<BranchResolver>().ContextBranch.ShouldBeNull();
    }

    [Test]
    public void CheckedOutConfigurationControlsContextualBranchAndOutput()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("feature/configured");
        var head = fixture.Repository.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("feature", branch => branch.WithIncrement(IncrementStrategy.Minor)
                .WithLabel("configured").WithCustomVersionFormat("context-{BranchName}-{Sha}"))
            .Build();
        var yaml = new ConfigurationSerializer().Serialize(configuration);
        var file = Path.Combine(fixture.RepositoryPath, "GitVersion.yml");
        File.WriteAllText(file, yaml);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GIT_BRANCH", "feature/configured");
        using var services = CreateServices(fixture.RepositoryPath, environment, readConfigurationFile: true);

        var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();

        variables.FullSemVer.ShouldBe("1.1.0-configured.1+1");
        variables.CustomVersion.ShouldBe($"context-feature-configured-{head.Sha}");
        variables.Sha.ShouldBe(head.Sha);
        File.ReadAllText(file).ShouldBe(yaml);
        fixture.Repository.Head.FriendlyName.ShouldBe(MainBranch);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DynamicCloneAndRepeatedPreparationPreserveDefaultHead(bool contextual)
    {
        using var remote = new EmptyRepositoryFixture();
        remote.Repository.MakeATaggedCommit("1.0.0");
        var head = remote.Repository.MakeACommit();
        var scratch = Directory.CreateTempSubdirectory("gitversion-3954-").FullName;
        try
        {
            var environment = new TestEnvironment();
            if (contextual)
            {
                environment.SetEnvironmentVariable("GIT_BRANCH", "feature/dynamic");
            }
            var working = Path.Combine(scratch, "working");
            Directory.CreateDirectory(working);
            using var services = CreateServices(working, environment, targetBranch: contextual ? null : MainBranch,
                configure: options =>
                {
                    options.RepositoryInfo.TargetUrl = remote.RepositoryPath.TrimEnd(Path.DirectorySeparatorChar);
                    options.RepositoryInfo.ClonePath = Path.Combine(scratch, "clones");
                });
            for (var invocation = 0; invocation < 2; invocation++)
            {
                var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
                variables.Sha.ShouldBe(head.Sha);
                variables.BranchName.ShouldBe(contextual ? "feature/dynamic" : MainBranch);
            }
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(scratch);
        }
    }

    [Test]
    public void DynamicExistingCloneUsesItsCheckout()
    {
        using var remote = new EmptyRepositoryFixture();
        remote.Repository.MakeATaggedCommit("1.0.0");
        var head = remote.Repository.MakeACommit();
        var scratch = Directory.CreateTempSubdirectory("gitversion-3954-").FullName;
        try
        {
            var url = remote.RepositoryPath.TrimEnd(Path.DirectorySeparatorChar);
            var clone = Path.Combine(scratch, "clones", Path.GetFileName(url));
            Repository.Clone(url, clone);
            remote.Repository.MakeATaggedCommit("9.0.0");
            var working = Path.Combine(scratch, "working");
            Directory.CreateDirectory(working);
            foreach (var name in new[] { "feature/one", "feature/two" })
            {
                var environment = new TestEnvironment();
                environment.SetEnvironmentVariable("GIT_BRANCH", name);
                using var services = CreateServices(working, environment, configure: options =>
                {
                    options.RepositoryInfo.TargetUrl = url;
                    options.RepositoryInfo.ClonePath = Path.Combine(scratch, "clones");
                });
                var variables = services.GetRequiredService<IGitVersionCalculateTool>().CalculateVersionVariables();
                variables.Sha.ShouldBe(head.Sha);
                variables.BranchName.ShouldBe(name);
                services.GetRequiredService<IGitRepositoryInfo>().DynamicGitRepositoryPath.ShouldBe(Path.Combine(clone, ".git"));
            }
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(scratch);
        }
    }

    [TestCase(false, false, false, false)]
    [TestCase(true, false, false, false)]
    [TestCase(false, true, false, false)]
    [TestCase(false, false, true, false)]
    [TestCase(false, false, false, true)]
    public void ContextualPullRequestPrefersTargetFromSuccessfulFetch(bool noFetch, bool noNormalize, bool localBuild, bool preventFetch)
    {
        using var remote = new EmptyRepositoryFixture();
        remote.Repository.MakeATaggedCommit("1.0.0");
        remote.BranchTo("support/1.0");
        var originalTarget = remote.Repository.Head.Tip;
        remote.Checkout(MainBranch);
        var scratch = Directory.CreateTempSubdirectory("gitversion-context-target-").FullName;
        try
        {
            var path = Path.Combine(scratch, "checkout");
            Repository.Clone(remote.RepositoryPath, path);
            using var repository = new Repository(path);
            var target = repository.CreateBranch("support/1.0", repository.Lookup<LibGit2Sharp.Commit>(originalTarget.Sha));
            Commands.Checkout(repository, repository.CreateBranch("feature/work", target.Tip));
            var source = repository.MakeACommit();
            Commands.Checkout(repository, target);
            repository.Merge(repository.Branches["feature/work"], source.Author,
                new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward });
            var merge = repository.Commit("Merge pull request 42 from feature/work into support/1.0", source.Author, source.Author,
                new CommitOptions { AmendPreviousCommit = true });
            Commands.Checkout(repository, merge);
            repository.Refs.UpdateTarget(repository.Refs[target.CanonicalName], originalTarget.Sha);
            remote.Checkout("support/1.0");
            var fetchedTarget = remote.Repository.MakeACommit();
            var localRefs = SnapshotRefs(repository).Where(reference => reference.StartsWith("refs/heads/", StringComparison.Ordinal)).ToArray();
            var environment = new TestEnvironment();
            if (!localBuild)
            {
                environment.SetEnvironmentVariable(preventFetch ? "TF_BUILD" : ContinuaCi.EnvironmentVariableName, "true");
            }
            environment.SetEnvironmentVariable("GIT_BRANCH", "pull/42/merge");
            var configuration = GitFlowConfigurationBuilder.New
                .WithBranch("support", builder => builder.WithIncrement(IncrementStrategy.Patch))
                .Build();
            using var services = CreateServices(path, environment, noNormalize: noNormalize, configuration: configuration,
                configure: options => options.Settings.NoFetch = noFetch);
            services.GetRequiredService<IGitPreparer>().Prepare();
            var context = services.GetRequiredService<Lazy<GitVersionContext>>().Value;
            context.CurrentCommit.Parents.Count.ShouldBe(2);
            MergeMessage.TryParse(context.CurrentCommit, configuration, out var mergeMessage).ShouldBeTrue();
            mergeMessage.IsMergedPullRequest.ShouldBeTrue();
            mergeMessage.TargetBranch.ShouldBe("support/1.0");
            var selected = services.GetRequiredService<IEffectiveBranchConfigurationFinder>()
                .GetConfigurations(context.CurrentBranch, configuration).ToArray().ShouldHaveSingleItem().Branch;
            var fetched = !noFetch && !noNormalize && !localBuild && !preventFetch;

            selected.Name.Canonical.ShouldBe(fetched ? "refs/remotes/origin/support/1.0" : "refs/heads/support/1.0");
            selected.Tip.ShouldNotBeNull().Sha.ShouldBe(fetched ? fetchedTarget.Sha : originalTarget.Sha);
            context.CurrentCommit.Sha.ShouldBe(merge.Sha);
            repository.Info.IsHeadDetached.ShouldBeTrue();
            SnapshotRefs(repository).Where(reference => reference.StartsWith("refs/heads/", StringComparison.Ordinal)).ShouldBe(localRefs);
            if (fetched)
            {
                var cacheKeys = services.GetRequiredService<IGitVersionCacheKeyFactory>();
                var fetchedKey = cacheKeys.Create(null);
                var fetchedRefs = SnapshotRefs(repository);
                services.GetRequiredService<IOptions<GitVersionOptions>>().Value.Settings.NoFetch = true;
                services.GetRequiredService<IGitPreparer>().Prepare();
                var withoutFetch = services.GetRequiredService<IEffectiveBranchConfigurationFinder>()
                    .GetConfigurations(context.CurrentBranch, configuration).ToArray().ShouldHaveSingleItem().Branch;

                withoutFetch.Name.Canonical.ShouldBe("refs/heads/support/1.0");
                withoutFetch.Tip.ShouldNotBeNull().Sha.ShouldBe(originalTarget.Sha);
                SnapshotRefs(repository).ShouldBe(fetchedRefs);
                cacheKeys.Create(null).ShouldNotBe(fetchedKey);
            }
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(scratch);
        }
    }

    private static BranchResolver Resolve(string? upper, string? alias, string? target = null)
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GIT_BRANCH", upper);
        environment.SetEnvironmentVariable("Git_Branch", alias);
        var agent = Substitute.For<ICurrentBuildAgent>();
        agent.GetCurrentBranch(Arg.Any<bool>()).Returns("refs/heads/provider");
        return new BranchResolver(Options.Create(new GitVersionOptions { RepositoryInfo = { TargetBranch = target } }), environment, agent);
    }

    private static ServiceProvider CreateServices(string path, TestEnvironment environment, bool noNormalize = false,
        string? targetBranch = null, bool noCache = true, IGitVersionConfiguration? configuration = null,
        Action<GitVersionOptions>? configure = null, bool readConfigurationFile = false)
    {
        var options = Options.Create(new GitVersionOptions
        {
            WorkingDirectory = path,
            RepositoryInfo = { TargetBranch = targetBranch },
            Settings = { NoCache = noCache, NoFetch = true, NoNormalize = noNormalize }
        });
        configure?.Invoke(options.Value);
        var services = (ServiceProvider)ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<IEnvironment>(environment);
            if (!readConfigurationFile)
            {
                services.AddSingleton(new Lazy<IGitVersionConfiguration>(() => configuration ?? GitFlowConfigurationBuilder.New.Build()));
            }
        });
        services.DiscoverRepository();
        return services;
    }

    private static TestEnvironment GitHubEnvironment(string reference)
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        environment.SetEnvironmentVariable("GITHUB_REF", reference);
        return environment;
    }

    private static void AddUnavailableRemote(Repository repository)
        => repository.Network.Remotes.Add("origin", Path.Combine(repository.Info.WorkingDirectory, "missing-remote"));

    private static string[] SnapshotRefs(Repository repository)
        => [.. repository.Refs.Select(reference => $"{reference.CanonicalName}:{reference.TargetIdentifier}").Order()];
}
