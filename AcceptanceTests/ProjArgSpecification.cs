//using System.IO;
//using GitHubFlowVersion.AcceptanceTests.Helpers;
//using Xunit;

//namespace GitHubFlowVersion.AcceptanceTests
//{
//    using global::AcceptanceTests.Properties;

//    public class ProjArgSpecification : RepositorySpecification
//    {
//        private ExecutionResults _result;
//        private const string TaggedVersion = "1.2.3";

//        public void GivenARepositoryWithATaggedCommit()
//        {
//            Repository.MakeATaggedCommit(TaggedVersion);
//        }

//        public void AndGivenThereIsANextVersionTxtFile()
//        {
//            Repository.AddNextVersionTxtFile(TaggedVersion);
//        }

//        public void WhenGitHubFlowVersionIsExecutedWithExecOption()
//        {
//            var buildFile = Path.Combine(RepositoryPath, "TestBuildFile.proj");
//            File.WriteAllBytes(buildFile, Resources.TestBuildFile);
//            _result = GitVersionHelper.ExecuteIn(RepositoryPath, projectFile: "TestBuildFile.proj", targets: "OutputResults");
//        }

//        public void ThenProcessExitedWithoutError()
//        {
//            _result.AssertExitedSuccessfully();
//        }

//        public void AndThenVariablesShouldBeAvailableToProcess()
//        {
//            Assert.Contains("GitHubFlowVersion_FullSemVer: 1.2.4", _result.Output);
//        }
//    }
//}