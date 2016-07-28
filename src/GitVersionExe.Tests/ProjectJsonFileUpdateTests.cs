namespace GitVersionExe.Tests
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersionCore.Tests;
    using NUnit.Framework;
    using Shouldly;

    public class ProjectJsonFileUpdateTests
    {
        private const string _testPath = @"x:\TestPath";
        private const string _projectJson = @"{
""version"": """"
}";
        private const string _replacedJson = @"{
""version"": ""1.2.3-foo.4+2.Branch.alpha.Sha.ADF.bar""
}";

        [Test]
        public void ShouldCreateBackupsOfTheOriginalFilesAndRemoveThem()
        {
            var fs = new TestFileSystem();
            var filename = CreateProjectJsonAndXProj(fs, "MyProj");
            using (CreateTestProjectJsonFileUpdate(fs))
            {
                fs.ReadAllText(filename + ".bak").ShouldBe(_projectJson);
            }
            fs.Exists(filename + ".bak").ShouldBe(false);
        }

        [Test]
        public void ShouldReplaceJsonAndThenRestore()
        {
            var fs = new TestFileSystem();
            var filename = CreateProjectJsonAndXProj(fs, "MyProj");
            using (CreateTestProjectJsonFileUpdate(fs))
            {
                fs.ReadAllText(filename).ShouldBe(_replacedJson);
            }
            fs.ReadAllText(filename).ShouldBe(_projectJson);
        }


        [Test]
        public void ShouldReplaceJsonAndNotRestoreIfDoNotRestoreFilesCalled()
        {
            var fs = new TestFileSystem();
            var filename = CreateProjectJsonAndXProj(fs, "MyProj");
            using (var update = CreateTestProjectJsonFileUpdate(fs))
            {
                fs.ReadAllText(filename).ShouldBe(_replacedJson);
                update.DoNotRestoreFiles();
            }
            fs.ReadAllText(filename).ShouldBe(_replacedJson);
        }

        [Test]
        public void ShouldRemoveBackupsIfDoNotRestoreFilesCalled()
        {
            var fs = new TestFileSystem();
            var filename = CreateProjectJsonAndXProj(fs, "MyProj");
            using (var update = CreateTestProjectJsonFileUpdate(fs))
            {
                fs.Exists(filename + ".bak").ShouldBe(true);
                update.DoNotRestoreFiles();
            }
            fs.Exists(filename + ".bak").ShouldBe(false);
        }

        [Test]
        public void ShouldNotReplaceJsonOutsideTestPath()
        {
            var fs = new TestFileSystem();
            var filename = CreateProjectJsonAndXProj(fs, "..");
            using (CreateTestProjectJsonFileUpdate(fs))
            {
                fs.ReadAllText(filename).ShouldBe(_projectJson);
            }
        }

        [Test]
        public void ShouldNotReplaceJsonIfNoCorrespondingXProjFile()
        {
            var fs = new TestFileSystem();
            var filename = Path.GetFullPath(Path.Combine(_testPath, "foo", "project.json"));
            fs.WriteAllText(filename, _projectJson);
            using (CreateTestProjectJsonFileUpdate(fs))
            {
                fs.ReadAllText(filename).ShouldBe(_projectJson);
            }
        }


        private string CreateProjectJsonAndXProj(TestFileSystem fs, string subdir)
        {
            var projectJsonFileName = Path.GetFullPath(Path.Combine(_testPath, subdir, "project.json"));
            fs.WriteAllText(projectJsonFileName, _projectJson);
            fs.WriteAllText(Path.Combine(_testPath, subdir, "foo.xproj"), _projectJson);
            return projectJsonFileName;
        }


        private ProjectJsonFileUpdate CreateTestProjectJsonFileUpdate(TestFileSystem fs)
        {

            var semVer = new SemanticVersion(1, 2, 3)
            {
                BuildMetaData = new SemanticVersionBuildMetaData(2, "alpha", "ADF", new DateTimeOffset(2011, 2, 3, 4, 5, 6, 7, TimeSpan.FromHours(2)), "bar"),
                PreReleaseTag = new SemanticVersionPreReleaseTag("foo", 4)
            };
            var variables = VariableProvider.GetVariablesFor(semVer, new TestEffectiveConfiguration(), true);

            return new ProjectJsonFileUpdate(
                new Arguments() { UpdateProjectJson = true },
                _testPath,
                variables,
                fs
           );
        }
    }
}