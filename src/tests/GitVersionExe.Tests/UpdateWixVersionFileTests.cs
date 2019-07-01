namespace GitVersionExe.Tests
{
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    using GitVersion;
    using GitTools.Testing;
    using System.Collections.Generic;
    using System.Xml;

    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    class UpdateWixVersionFileTests
    {
        private string WixVersionFileName;

        [SetUp]
        public void Setup()
        {
            WixVersionFileName = WixVersionFileUpdater.GetWixVersionFileName();
        }

        [Test]
        [Category("NoMono")]
        [Description("Doesn't work on Mono/Unix because of the path heuristics that needs to be done there in order to figure out whether the first argument actually is a path.")]
        public void WixVersionFileCreationTest()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeATaggedCommit("1.2.3");
                fixture.MakeACommit();

                GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " -updatewixversionfile");
                Assert.IsTrue(File.Exists(Path.Combine(fixture.RepositoryPath, WixVersionFileName)));
            }
        }

        [Test]
        [Category("NoMono")]
        [Description("Doesn't work on Mono/Unix because of the path heuristics that needs to be done there in order to figure out whether the first argument actually is a path.")]
        public void WixVersionFileVarCountTest()
        {
            //Make sure we have captured all the version variables by count in the Wix version file
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeATaggedCommit("1.2.3");
                fixture.MakeACommit();

                var gitVersionExecutionResults = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: null);
                VersionVariables vars = gitVersionExecutionResults.OutputVariables;

                GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " -updatewixversionfile");

                var gitVersionVarsInWix = GetGitVersionVarsInWixFile(Path.Combine(fixture.RepositoryPath, WixVersionFileName));
                var gitVersionVars = VersionVariables.AvailableVariables;

                Assert.AreEqual(gitVersionVars.Count(), gitVersionVarsInWix.Count);
            }
        }

        [Test]
        [Category("NoMono")]
        [Description("Doesn't work on Mono/Unix because of the path heuristics that needs to be done there in order to figure out whether the first argument actually is a path.")]
        public void WixVersionFileContentTest()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeATaggedCommit("1.2.3");
                fixture.MakeACommit();

                var gitVersionExecutionResults = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: null);
                VersionVariables vars = gitVersionExecutionResults.OutputVariables;

                GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " -updatewixversionfile");

                var gitVersionVarsInWix = GetGitVersionVarsInWixFile(Path.Combine(fixture.RepositoryPath, WixVersionFileName));
                var gitVersionVars = VersionVariables.AvailableVariables;

                foreach (var variable in gitVersionVars)
                {
                    string value;
                    vars.TryGetValue(variable, out value);
                    //Make sure the variable is present in the Wix file
                    Assert.IsTrue(gitVersionVarsInWix.ContainsKey(variable));
                    //Make sure the values are equal
                    Assert.AreEqual(value, gitVersionVarsInWix[variable]);
                }
            }
        }

        private Dictionary<string, string> GetGitVersionVarsInWixFile(string file)
        {
            var gitVersionVarsInWix = new Dictionary<string, string>();
            using (var reader = new XmlTextReader(file))
            {
                while (reader.Read())
                {
                    if (reader.Name == "define")
                    {
                        string[] component = reader.Value.Split('=');
                        gitVersionVarsInWix[component[0]] = component[1].TrimStart('"').TrimEnd('"');
                    }
                }
            }
            return gitVersionVarsInWix;
        }
    }
}
