namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public static class BuildServerList
    {
        static List<IBuildServer> BuildServers = new List<IBuildServer>
        {
            new ContinuaCi(),
            new TeamCity(),
            new AppVeyor(),
            new MyGet(),
            new Jenkins(),
            new GitLabCi(),
            new VsoAgent(),
			new TravisCI(),
        };

        public static IEnumerable<IBuildServer> GetApplicableBuildServers()
        {
            var buildServices = new List<IBuildServer>();

            foreach (var buildServer in BuildServers)
            {
                try
                {
                    if (buildServer.CanApplyToCurrentContext())
                    {
                        Logger.WriteInfo(string.Format("Applicable build agent found: '{0}'.", buildServer.GetType().Name));
                        buildServices.Add(buildServer);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteWarning(string.Format("Failed to check build server '{0}': {1}", buildServer.GetType().Name, ex.Message));
                }
            }

            return buildServices;
        }
    }
}