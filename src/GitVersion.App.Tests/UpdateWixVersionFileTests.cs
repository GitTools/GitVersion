using GitTools.Testing;
using GitVersion.OutputVariables;
using GitVersion.VersionConverters.WixUpdater;
using NUnit.Framework;

namespace GitVersion.App.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
internal class UpdateWixVersionFileTests
{
    private string wixVersionFileName;

    [SetUp]
    public void Setup() => this.wixVersionFileName = WixVersionFileUpdater.WixVersionFileName;

    [Test]
    public void WixVersionFileCreationTest()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " /updatewixversionfile");
        Assert.IsTrue(File.Exists(Path.Combine(fixture.RepositoryPath, this.wixVersionFileName)));
    }

    [Test]
    public void WixVersionFileVarCountTest()
    {
        //Make sure we have captured all the version variables by count in the Wix version file
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: null);

        GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " /updatewixversionfile");

        var gitVersionVarsInWix = GetGitVersionVarsInWixFile(Path.Combine(fixture.RepositoryPath, this.wixVersionFileName));
        var gitVersionVars = VersionVariables.AvailableVariables;

        Assert.AreEqual(gitVersionVars.Count(), gitVersionVarsInWix.Count);
    }

    [Test]
    public void WixVersionFileContentTest()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        var gitVersionExecutionResults = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: null);
        var vars = gitVersionExecutionResults.OutputVariables;

        GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " /updatewixversionfile");

        var gitVersionVarsInWix = GetGitVersionVarsInWixFile(Path.Combine(fixture.RepositoryPath, this.wixVersionFileName));
        var gitVersionVars = VersionVariables.AvailableVariables;

        foreach (var variable in gitVersionVars)
        {
            vars.TryGetValue(variable, out var value);
            //Make sure the variable is present in the Wix file
            Assert.IsTrue(gitVersionVarsInWix.ContainsKey(variable));
            //Make sure the values are equal
            Assert.AreEqual(value, gitVersionVarsInWix[variable]);
        }
    }

    private static Dictionary<string, string> GetGitVersionVarsInWixFile(string file)
    {
        var gitVersionVarsInWix = new Dictionary<string, string>();
        using (var reader = new XmlTextReader(file))
        {
            while (reader.Read())
            {
                if (reader.Name == "define")
                {
                    var component = reader.Value.Split('=');
                    gitVersionVarsInWix[component[0]] = component[1].TrimStart('"').TrimEnd('"');
                }
            }
        }
        return gitVersionVarsInWix;
    }
}
