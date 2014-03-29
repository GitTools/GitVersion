using System.Diagnostics;
using System.Linq;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class IntegrationTests
{

    [Test, Explicit]
    public void ProcessAllTheCommits()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
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
                            Debug.WriteLine("{0}.{1}.{2}.{3}.{4}", version.Major, version.Minor, version.Patch, version.PreReleaseTag, version.BuildMetaData);
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
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");

            var finder = new GitVersionFinder();
            finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
        }
        Debug.WriteLine(startNew.ElapsedMilliseconds);
        startNew = Stopwatch.StartNew();
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");

            var finder = new GitVersionFinder();
            finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
        }
        Debug.WriteLine(startNew.ElapsedMilliseconds);
    }
    
    [Test,Explicit]
    public void DirectoryDateFinderTest()
    {
        var stopwatch = Stopwatch.StartNew();
        DirectoryDateFinder.GetLastDirectoryWrite(@"C:\Code\NServiceBus\.git");
        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
        stopwatch = Stopwatch.StartNew();
        DirectoryDateFinder.GetLastDirectoryWrite(@"C:\Code\NServiceBus\.git");
        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
    }

    [Test,Explicit]
    public void NServiceBusRelease()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "release-4.1.0");

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
            Debug.WriteLine(version.BuildMetaData);
        }
    }

    [Test,Explicit]
    public void NServiceBusReleaseSpecificCommit()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "release-4.1.0");
            repository.Checkout("c0e0a5e13775552cd3e08e039f453e4cf1fd4235");

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
            Debug.WriteLine(version.BuildMetaData);
        }
    }

    [Test,Explicit]
    public void NServiceBusHotfix()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "hotfix-4.1.1");

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
            Debug.WriteLine(version.BuildMetaData);
        }
    }

    [Test,Explicit]
    public void NServiceBusMaster()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "master");

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch  
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
            Debug.WriteLine(version.BuildMetaData);
        }
    }

    [Test,Explicit]
    public void NServiceBusDevelop()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
            Debug.WriteLine(version.BuildMetaData);
        }
    }

    [Test,Explicit]
    public void NServiceBusHead()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus"))
        {
            var branch = repository.Head;

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
            Debug.WriteLine(version.BuildMetaData);
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
            repository.Checkout("c0e0a5e13775552cd3e08e039f453e4cf1fd4235");

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
            Debug.WriteLine(version.BuildMetaData);
        }
    }
    
    [Test,Explicit]
    public void Foo()
    {
        using (var repository = new Repository(@"C:\Code\ServiceControl"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
        }
    }
    [Test, Explicit]
    public void NServiceBusNhibernate()
    {
        using (var repository = new Repository(@"C:\Code\NServiceBus.Nhibernate"))
        {
            var branch = repository.FindBranch("develop");

            var finder = new GitVersionFinder();
            var version = finder.FindVersion(new GitVersionContext
            {
                Repository = repository,
                CurrentBranch = branch
            });
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreReleaseTag);
            Debug.WriteLine(version.BuildMetaData);
        }
    }
}