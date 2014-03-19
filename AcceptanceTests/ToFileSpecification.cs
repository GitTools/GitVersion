//using System.Collections.Generic;
//using System.IO;
//using System.Web.Script.Serialization;
//using GitHubFlowVersion.AcceptanceTests.Helpers;
//using Xunit;

//namespace GitHubFlowVersion.AcceptanceTests
//{
//    public class ToFileSpecification : RepositorySpecification
//    {
//        private string _tempFile;
//        private ExecutionResults _result;
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

//        public void WhenGitHubFlowVersionIsExecutedWithToFileOptionSet()
//        {
//            _tempFile = Path.Combine(PathHelper.GetTempPath(), "ToFileTest.json");
//            _result = GitVersionHelper.ExecuteIn(RepositoryPath, toFile: _tempFile);
//        }

//        public void ThenProcessExitedWithoutError()
//        {
//            _result.AssertExitedSuccessfully();
//        }

//        public void ThenVariablesShouldBeWrittenToOutputFile()
//        {
//            var output = File.ReadAllText(_tempFile);
//            var variables = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(output);
//            Assert.Equal("1.2.4+001", (string)variables["GitHubFlowVersion_FullSemVer"]);
//        }
//    }
//}