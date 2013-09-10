using System;
using System.Linq;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class TokenResolverTests
{
    
    void DoWithCurrentRepo(Action<Repository> doWithRepo)
    {
        using (var repo = new Repository(GitDirFinder.TreeWalkForGitDir(Environment.CurrentDirectory)))
        {
            if (doWithRepo != null) doWithRepo(repo);
        }
    }

    [Test]
    public void Replace_branch()
    {
        DoWithCurrentRepo(repo =>
            {
                var branchName = repo.Head.Name;


                var resolver = new FormatStringTokenResolver();
                var result = resolver.ReplaceTokens("%branch%", repo, new SemanticVersion());

                Assert.AreEqual(branchName, result);
            });
    }

    [Test]
    public void Semver_stage()
    {
        DoWithCurrentRepo(repo =>
        {
            var resolver = new FormatStringTokenResolver();
            var semanticVersion = new SemanticVersion
                                  {
                                      Stage = Stage.ReleaseCandidate
                                  };
            var result = resolver.ReplaceTokens("%semVerStage%", repo, semanticVersion);

            Assert.AreEqual("RC", result);
        });
    }

    [Test]
    public void Replace_suffix()
    {
        DoWithCurrentRepo(repo =>
        {
            var resolver = new FormatStringTokenResolver();
            var semanticVersion = new SemanticVersion
                                  {
                                      Suffix = "suffix"
                                  };
            var result = resolver.ReplaceTokens("%semVerSuffix%", repo, semanticVersion);

            Assert.AreEqual("suffix", result);
        });
    }

    [Test]
    public void Replace_preRelease()
    {
        DoWithCurrentRepo(repo =>
        {
            var resolver = new FormatStringTokenResolver();
            var semanticVersion = new SemanticVersion
                                  {
                                      PreRelease = 10
                                  };
            var result = resolver.ReplaceTokens("%semVerPreRelease%", repo, semanticVersion);

            Assert.AreEqual("10", result);
        });
    }

    [Test]
    public void Replace_githash()
    {
        DoWithCurrentRepo(repo =>
            {
                var sha = repo.Head.Tip.Sha;
                var resolver = new FormatStringTokenResolver();
                var semanticVersion = new SemanticVersion();
                var result = resolver.ReplaceTokens("%githash%", repo, semanticVersion);

                Assert.AreEqual(sha, result);
            });
    }


    [Test]
    public void Replace_user()
    {
        DoWithCurrentRepo(repo =>
            {
                var currentUser = Environment.UserName;
                var resolver = new FormatStringTokenResolver();
                var semanticVersion = new SemanticVersion();
                var result = resolver.ReplaceTokens("%user%", repo, semanticVersion);

                Assert.IsTrue(result.EndsWith(currentUser));
            });
    }

    [Test]
    public void Replace_machine()
    {
        DoWithCurrentRepo(repo =>
            {
                var machineName = Environment.MachineName;


                var resolver = new FormatStringTokenResolver();
                var semanticVersion = new SemanticVersion();
                var result = resolver.ReplaceTokens("%machine%", repo, semanticVersion);

                Assert.AreEqual(machineName, result);
            });
    }

    [Test]
    public void Replace_environment_variables()
    {
        DoWithCurrentRepo(repo =>
            {
                var environmentVariables = Environment.GetEnvironmentVariables();
                var expected = string.Join("--", environmentVariables.Values.Cast<string>());

                var replacementTokens = string.Join("--", environmentVariables.Keys.Cast<string>()
                                                                              .Select(key => "%env[" + key + "]%")
                                                                              .ToArray());
                var resolver = new FormatStringTokenResolver();
                var semanticVersion = new SemanticVersion();
                var result = resolver.ReplaceTokens(replacementTokens, repo, semanticVersion);

                Assert.AreEqual(expected, result);
            });
    }
}