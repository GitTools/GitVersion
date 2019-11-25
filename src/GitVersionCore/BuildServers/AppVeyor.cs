using System;
using System.Net.Http;
using System.Net.Http.Headers;
using GitVersion.OutputVariables;
using GitVersion.Logging;
using Newtonsoft.Json;

namespace GitVersion.BuildServers
{
    public class AppVeyor : BuildServerBase
    {
        public AppVeyor(IEnvironment environment, ILog log) : base(environment, log)
        {
        }

        public const string EnvironmentVariableName = "APPVEYOR";

        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            var buildNumber = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER");

            using var httpClient = GetHttpClient();

            var body = new
            {
                version = $"{variables.FullSemVer}.build.{buildNumber}",
            };

            var stringContent = new StringContent(JsonConvert.SerializeObject(body));
            httpClient.PutAsync("api/build", stringContent).Wait();

            return $"Set AppVeyor build number to '{variables.FullSemVer}'.";
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            using var httpClient = GetHttpClient();

            var body = new
            {
                name = $"GitVersion_{name}",
                value = $"{value}"
            };

            var stringContent = new StringContent(JsonConvert.SerializeObject(body));
            httpClient.PostAsync("api/build/variables", stringContent).Wait();

            return new[]
            {
                $"Adding Environment Variable. name='GitVersion_{name}' value='{value}']"
            };
        }

        private HttpClient GetHttpClient()
        {
            var headerValue = new MediaTypeWithQualityHeaderValue("application/json");
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(Environment.GetEnvironmentVariable("APPVEYOR_API_URL"))
            };
            httpClient.DefaultRequestHeaders.Accept.Add(headerValue);
            return httpClient;
        }


        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            var pullRequestBranchName = Environment.GetEnvironmentVariable("APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH");
            if (!string.IsNullOrWhiteSpace(pullRequestBranchName))
            {
                return pullRequestBranchName;
            }
            return Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH");
        }

        public override bool PreventFetch() => false;
    }
}
