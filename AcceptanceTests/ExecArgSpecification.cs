//using System.IO;
//using GitHubFlowVersion.AcceptanceTests.Helpers;
//using Xunit;

//namespace GitHubFlowVersion.AcceptanceTests
//{
//    using global::AcceptanceTests.Properties;

//    public class ExecArgSpecification : RepositorySpecification
//    {
//        private ExecutionResults _result;
//        private const string MsBuild = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";
//        private const string TaggedVersion = "1.2.3";

//        public void GivenARepositoryWithATaggedCommit()
//        {
//            Repository.MakeATaggedCommit(TaggedVersion);
//            Repository.MakeACommit();
//        }

//        public void AndGivenThereIsANextVersionTxtFile()
//        {
//            Repository.AddNextVersionTxtFile(TaggedVersion);
//        }

//        public void WhenGitHubFlowVersionIsExecutedWithExecOption()
//        {
//            var buildFile = Path.Combine(RepositoryPath, "TestBuildFile.proj");
//            File.WriteAllBytes(buildFile, Resources.TestBuildFile);
//            _result = GitVersionHelper.ExecuteIn(RepositoryPath, exec: MsBuild, execArgs: "TestBuildFile.proj /target:OutputResults");
//        }

//        public void ThenProcessExitedWithoutError()
//        {
//            _result.AssertExitedSuccessfully();
//        }

//        public void AndThenVariablesShouldBeAvailableToProcess()
//        {
//            Assert.Contains("GitHubFlowVersion_FullSemVer: 1.2.4+001", _result.Output);
//        }
//    }
//}