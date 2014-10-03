namespace GitVersion
{
    using System;
    using System.Net;
    using System.Text;

    public class AppVeyor : BuildServerBase
    {
        Authentication authentication;

        public AppVeyor(Authentication authentication)
        {
            this.authentication = authentication;
        }

        public override bool CanApplyToCurrentContext()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPVEYOR"));
        }

        public override void PerformPreProcessingSteps(string gitDirectory)
        {
            if (string.IsNullOrEmpty(gitDirectory))
            {
                throw new WarningException("Failed to find .git directory on agent.");
            }

            GitHelper.NormalizeGitDirectory(gitDirectory, authentication);
        }

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            var buildNumber = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER");

            var restBase = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");

            var request = (HttpWebRequest)WebRequest.Create(restBase + "api/build");
            request.Method = "PUT";

            var data = string.Format("{{ \"version\": \"{0} (Build {1})\" }}", versionToUseForBuildNumber, buildNumber);
            var bytes = Encoding.UTF8.GetBytes(data);
            request.ContentLength = bytes.Length;
            request.ContentType = "application/json";

            using (var writeStream = request.GetRequestStream())
            {
                writeStream.Write(bytes, 0, bytes.Length);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
                {
                    var message = String.Format("Request failed. Received HTTP {0}", response.StatusCode);
                    return message;
                }
            }

            return string.Format("Set AppVeyor build number to '{0} (Build {1})'.", versionToUseForBuildNumber, buildNumber);
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            Environment.SetEnvironmentVariable("GitVersion." + name, value);

            return new[]
            {
                string.Format("Adding Environment Variable. name='GitVersion.{0}' value='{1}']", name, value),
            };
        }
    }
}