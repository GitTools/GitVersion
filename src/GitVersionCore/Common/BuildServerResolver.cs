using System;
using System.Collections.Generic;
using GitVersion.Logging;

namespace GitVersion
{
    public class BuildServerResolver : IBuildServerResolver
    {
        private readonly IEnumerable<IBuildServer> buildServers;
        private readonly ILog log;
        public BuildServerResolver(IEnumerable<IBuildServer> buildServers, ILog log)
        {
            this.log = log;
            this.buildServers = buildServers ?? Array.Empty<IBuildServer>();
        }

        public IBuildServer Resolve()
        {
            IBuildServer instance = null;
            foreach (var buildServer in buildServers)
            {
                try
                {
                    if (buildServer.CanApplyToCurrentContext())
                    {
                        log.Info($"Applicable build agent found: '{buildServer.GetType().Name}'.");
                        instance = buildServer;
                    }
                }
                catch (Exception ex)
                {
                    log.Warning($"Failed to check build server '{buildServer.GetType().Name}': {ex.Message}");
                }
            }

            return instance;
        }
    }
}
