using System.Diagnostics;
using System.Linq;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class IntegrationTests
{


    [Test, Explicit]
    public void ProcessAllTheCommits()
    {
        using (var repository = new Repository(@"C:\Code\Particular\NServiceBus"))
        {
            foreach (var branch in repository.Branches)
            {

                foreach (var commit in branch.Commits)
                {
                    string versionPart;
                    if (MergeMessageParser.TryParse(commit, out versionPart))
                    {
                        Debug.WriteLine(versionPart);
                        SemanticVersion version;
                        if (SemanticVersionParser.TryParse(versionPart, out version))
                        {
                            Debug.WriteLine("{0}.{1}.{2}.{3}.{4}.{5}", version.Major, version.Minor, version.Patch, version.Stability, version.PreReleasePartOne, version.Suffix);
                        }
                    }
                }
            }
        }
    }

    [Test, Explicit]
    public void TimingOnNSB()
    {
        var startNew = Stopwatch.StartNew();
        using (var repository = new Repository(@"C:\Code\Particular\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            finder.FindVersion();
        }
        Debug.WriteLine(startNew.ElapsedMilliseconds);
        startNew = Stopwatch.StartNew();
        using (var repository = new Repository(@"C:\Code\Particular\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            finder.FindVersion();
        }
        Debug.WriteLine(startNew.ElapsedMilliseconds);
    }
    
    [Test,Explicit]
    public void DirectoryDateFinderTest()
    {
        var stopwatch = Stopwatch.StartNew();
        DirectoryDateFinder.GetLastDirectoryWrite(@"C:\Code\Particular\NServiceBus\.git");
        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
        stopwatch = Stopwatch.StartNew();
        DirectoryDateFinder.GetLastDirectoryWrite(@"C:\Code\Particular\NServiceBus\.git");
        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
    }

    [Test,Explicit]
    public void NServiceBusRelease()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "release-4.1.0");
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleasePartOne);
            Debug.WriteLine(version.Version.PreReleasePartTwo);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }

    [Test,Explicit]
    public void NServiceBusReleaseSpecificCommit()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "release-4.1.0");
            var commit = branch.Commits.First(x => x.Id.Sha == "c0e0a5e13775552cd3e08e039f453e4cf1fd4235");

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleasePartOne);
            Debug.WriteLine(version.Version.PreReleasePartTwo);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }

    [Test,Explicit]
    public void NServiceBusHotfix()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "hotfix-4.1.1");
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleasePartOne);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
            Debug.WriteLine(new TeamCityVersionBuilder().GenerateBuildVersion(version));
            
        }
    }

    [Test,Explicit]
    public void NServiceBusMaster()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "master");
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleasePartOne);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }

    [Test,Explicit]
    public void NServiceBusDevelop()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleasePartOne);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }

    [Test,Explicit]
    public void NServiceBusHead()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Head;
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleasePartOne);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }
    [Test,Explicit]
    public void GitTests()
    {
        using (var repository = new Repository(@"C:\Code\Experiments"))
        {

            foreach (var tag in repository.Tags)
            {
                Debug.WriteLine(tag.Annotation.Tagger.When);
            }
        }
    }
    [Test,Explicit]
    public void NServiceBusDevelopOlderCommit()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");
            var commit = branch.Commits.First(x => x.Id.Sha == "c0e0a5e13775552cd3e08e039f453e4cf1fd4235");

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleasePartOne);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }
    
    [Test,Explicit]
    public void Foo()
    {
        using (var repository = new Repository(@"C:\Code\Particular\ServicePulse"))
        {
            var branch = repository.Branches.First(x => x.Name == "feature-newUI");
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleasePartOne);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }
}