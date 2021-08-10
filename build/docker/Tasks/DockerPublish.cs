using System;
using Cake.Common.Diagnostics;
using Cake.Frosting;
using Common.Utilities;
using Docker.Utilities;

namespace Docker.Tasks
{
    [TaskName(nameof(DockerPublish))]
    [TaskDescription("Publish the docker images containing the GitVersion Tool")]
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
            DockerPublish(context, DockerRegistry.GitHub);
            DockerPublish(context, DockerRegistry.DockerHub);
        }
        private static void DockerPublish(BuildContext context, DockerRegistry dockerRegistry)
        {
            context.Credentials = Credentials.GetCredentials(context, dockerRegistry);
            context.DockerRegistryPrefix = dockerRegistry == DockerRegistry.GitHub
                ? Constants.GitHubContainerRegistry
                : Constants.DockerHubRegistry;

            context.Information($"Docker image prefix: {context.DockerRegistryPrefix}");
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
                context.DockerPush(dockerImage, context.DockerRegistryPrefix);
            }
        }
    }
}
