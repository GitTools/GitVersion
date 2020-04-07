using System;
using System.IO;
using System.Text;
using GitVersion;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using GitVersion.VersionConverters.WixUpdater;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    internal class WixFileTests : TestBase
    {
        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void UpdateWixVersionFile()
        {
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

            var stringBuilder = new StringBuilder();
            void Action(string s) => stringBuilder.AppendLine(s);

            var logAppender = new TestLogAppender(Action);
            var log = new Log(logAppender);

            var sp = ConfigureServices(service =>
            {
                service.AddSingleton<ILog>(log);
            });

            var fileSystem = sp.GetService<IFileSystem>();
            var variableProvider = sp.GetService<IVariableProvider>();
            var versionVariables = variableProvider.GetVariablesFor(semVer, config, false);

            using var wixVersionFileUpdater = sp.GetService<IWixVersionFileUpdater>();

            wixVersionFileUpdater.Execute(versionVariables, new WixVersionContext(workingDir));

            var file = Path.Combine(workingDir, WixVersionFileUpdater.WixVersionFileName);
            fileSystem
                .ReadAllText(file)
                .ShouldMatchApproved(c => c.SubFolder(Path.Combine("Approved")));
        }
    }
}
