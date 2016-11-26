﻿namespace GitVersionExe.Tests
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersionCore.Tests;
    using NUnit.Framework;
    using Shouldly;

    public class ProjectJsonFileUpdateTests
    {
        
        private static readonly string _testPath = Environment.OSVersion.Platform == PlatformID.Unix ? "/usr/TestPath" : @"x:\TestPath";
        private static string _projectJson = "{\n\"version\": \"\"\n}".Replace("\n", Environment.NewLine);
        private static string _replacedJson = "{\n\"version\": \"1.2.3-foo.4+2.Branch.alpha.Sha.ADF.bar\"\n}".Replace("\n", Environment.NewLine);

        [Test]
        public void ShouldCreateBackupsOfTheOriginalFilesAndRemoveThem()
        {
            var fs = new TestFileSystem();
            var filename = CreateProjectJson(fs, "MyProj");
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
            var filename = CreateProjectJson(fs, "MyProj");
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
            var filename = CreateProjectJson(fs, "MyProj");
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
            var filename = CreateProjectJson(fs, "MyProj");
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
            var filename = CreateProjectJson(fs, "..");
            using (CreateTestProjectJsonFileUpdate(fs))
            {
                fs.ReadAllText(filename).ShouldBe(_projectJson);
            }
        }


        private string CreateProjectJson(TestFileSystem fs, string subdir)
        {
            var projectJsonFileName = Path.GetFullPath(Path.Combine(_testPath, subdir, "project.json"));
            fs.WriteAllText(projectJsonFileName, _projectJson);
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