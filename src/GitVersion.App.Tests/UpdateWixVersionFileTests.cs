using GitVersion.Helpers;
using GitVersion.Output.WixUpdater;
using GitVersion.OutputVariables;

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

        GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " --update-wix-version-file");
        Assert.That(FileSystemHelper.File.Exists(FileSystemHelper.Path.Combine(fixture.RepositoryPath, this.wixVersionFileName)), Is.True);
    }

    [Test]
    public void WixVersionFileVarCountTest()
    {
        //Make sure we have captured all the version variables by count in the Wix version file
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: null);

        GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " --update-wix-version-file");

        var gitVersionVarsInWix = GetGitVersionVarsInWixFile(FileSystemHelper.Path.Combine(fixture.RepositoryPath, this.wixVersionFileName));
        var gitVersionVars = GitVersionVariables.AvailableVariables;

        Assert.That(gitVersionVarsInWix, Has.Count.EqualTo(gitVersionVars.Count));
    }

    [Test]
    public void WixVersionFileContentTest()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        var gitVersionExecutionResults = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: null);
        var vars = gitVersionExecutionResults.OutputVariables;
        vars.ShouldNotBeNull();

        GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " --update-wix-version-file");

        var gitVersionVarsInWix = GetGitVersionVarsInWixFile(FileSystemHelper.Path.Combine(fixture.RepositoryPath, this.wixVersionFileName));
        var gitVersionVars = GitVersionVariables.AvailableVariables;

        foreach (var variable in gitVersionVars)
        {
            vars.TryGetValue(variable, out var value);
            Assert.Multiple(() =>
            {
                //Make sure the variable is present in the Wix file
                Assert.That(gitVersionVarsInWix.ContainsKey(variable), Is.True);
                //Make sure the values are equal
                Assert.That(gitVersionVarsInWix[variable], Is.EqualTo(value));
            });
        }
    }

    private static Dictionary<string, string> GetGitVersionVarsInWixFile(string file)
    {
        var gitVersionVarsInWix = new Dictionary<string, string>();
        using var reader = new XmlTextReader(file);
        while (reader.Read())
        {
            if (reader.Name != "define")
                continue;

            var component = reader.Value.Split('=');
            gitVersionVarsInWix[component[0]] = component[1].TrimStart('"').TrimEnd('"');
        }
        return gitVersionVarsInWix;
    }
}
