using System;
using System.Collections.Generic;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion.BuildServers
{
    public static class BuildServerList
    {
        private static List<IBuildServer> supportedBuildServers;

        public static IEnumerable<IBuildServer> GetApplicableBuildServers(ILog log)
        {
            var buildServices = new List<IBuildServer>();

            foreach (var buildServer in supportedBuildServers)
            {
                try
                {
                    if (buildServer.CanApplyToCurrentContext())
                    {
                        log.Info($"Applicable build agent found: '{buildServer.GetType().Name}'.");
                        buildServices.Add(buildServer);
                    }
                }
                catch (Exception ex)
                {
                    log.Warning($"Failed to check build server '{buildServer.GetType().Name}': {ex.Message}");
                }
            }

            return buildServices;
        }

        public static void Init(IEnvironment environment, ILog log)
        {
            supportedBuildServers = new List<IBuildServer>
            {
                new ContinuaCi(environment, log),
                new TeamCity(environment, log),
                new AppVeyor(environment, log),
                new MyGet(environment, log),
                new Jenkins(environment, log),
                new GitLabCi(environment, log),
                new AzurePipelines(environment, log),
                new TravisCI(environment, log),
                new EnvRun(environment, log),
                new Drone(environment, log),
                new CodeBuild(environment, log)
            };
        }
    }
}
