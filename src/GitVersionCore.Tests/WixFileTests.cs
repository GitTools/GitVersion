using System;
using System.IO;
using System.Text;
using GitVersion;
using GitVersion.OutputVariables;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    class WixFileTests
    {
        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void UpdateWixVersionFile()
        {
            var fileSystem = new TestFileSystem();
            var workingDir = Path.GetTempPath();
            var semVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                BuildMetaData = "5.Branch.develop"
            };

            semVer.BuildMetaData.VersionSourceSha = "versionSourceSha";
            semVer.BuildMetaData.Sha = "commitSha";
            semVer.BuildMetaData.ShortSha = "commitShortSha";
            semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2019-02-20 23:59:59Z");

            var config = new TestEffectiveConfiguration(buildMetaDataPadding: 2, legacySemVerPadding: 5);
            var vars = VariableProvider.GetVariablesFor(semVer, config, false);

            StringBuilder log = new StringBuilder();
            Action<string> action = s => log.AppendLine(s);
            Logger.SetLoggers(action, action, action, action);
            using (var wixVersionFileUpdater = new WixVersionFileUpdater(workingDir, vars, fileSystem))
            {
                wixVersionFileUpdater.Update();
                fileSystem.ReadAllText(wixVersionFileUpdater.WixVersionFile).
                    ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved")));
            }
        }
    }
}
