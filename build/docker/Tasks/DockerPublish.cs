using System;
using Cake.Common.Diagnostics;
using Cake.Frosting;
using Common.Utilities;

namespace Docker.Tasks
{
    [TaskName(nameof(DockerPublish))]
    [TaskDescription("Publish the docker images containing the GitVersion Tool")]
    [TaskArgument(Arguments.DockerRegistry, Constants.GitHub, Constants.DockerHub)]
    [TaskArgument(Arguments.DockerDotnetVersion, Constants.Version50, Constants.Version31)]
    [TaskArgument(Arguments.DockerDistro, Constants.Alpine312, Constants.Debian10, Constants.Ubuntu2004)]
    [IsDependentOn(typeof(DockerPublishInternal))]
    public class DockerPublish : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(DockerPublish)} works only on GitHub Actions.");
            return shouldRun;
        }
    }

    [TaskName(nameof(DockerPublishInternal))]
    [TaskDescription("Publish the docker images containing the GitVersion Tool")]
    [IsDependentOn(typeof(DockerTest))]
    public class DockerPublishInternal : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(DockerPublish)} works only on GitHub Actions.");
            shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(DockerPublish)} works only on Docker on Linux agents.");
            shouldRun &= context.ShouldRun(context.IsStableRelease || context.IsPreRelease, $"{nameof(DockerPublish)} works only for releases.");

            return shouldRun;
        }

        public override void Run(BuildContext context)
        {
            var username = context.Credentials?.Docker?.UserName;
            if (string.IsNullOrEmpty(username))
            {
                throw new InvalidOperationException("Could not resolve Docker user name.");
            }

            var password = context.Credentials?.Docker?.Password;
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Could not resolve Docker password.");
            }

            foreach (var dockerImage in context.Images)
            {
                context.DockerPush(dockerImage);
            }
        }
    }
}
